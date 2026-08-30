using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterTrailMeshTest : MonoBehaviour
{
    public Transform target;

    public int maxPoints = 20;
    public float minDistance = 0.15f;

    public float tailWidth = 0.1f;
    public float headWidth = 1.0f;

    [Range(0f, 1f)]
    public float tailAlpha = 0f;

    [Range(0f, 1f)]
    public float headAlpha = 0.7f;

    private Mesh mesh;

    private List<Vector3> points = new List<Vector3>();

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Water Trail Mesh";

        GetComponent<MeshFilter>().mesh = mesh;

        if (target != null)
        {
            points.Add(target.position);
        }
    }

    void Update()
    {
        if (target == null)
            return;

        RecordTargetPosition();
        BuildTrail();
    }

    void RecordTargetPosition()
    {
        Vector3 currentPosition = target.position;

        if (points.Count == 0)
        {
            points.Add(currentPosition);
            return;
        }

        Vector3 lastPoint = points[points.Count - 1];

        if (Vector3.Distance(lastPoint, currentPosition) >= minDistance)
        {
            points.Add(currentPosition);
        }

        if (points.Count > maxPoints)
        {
            points.RemoveAt(0);
        }
    }

    void BuildTrail()
    {
        if (points.Count < 2)
        {
            mesh.Clear();
            return;
        }

        int count = points.Count;

        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uvs = new Vector2[count * 2];
        Color[] colors = new Color[count * 2];

        int[] triangles = new int[(count - 1) * 6];

        for (int i = 0; i < count; i++)
        {
            Vector3 tangent;

            if (i == 0)
            {
                tangent =
                    (points[1] - points[0]).normalized;
            }
            else if (i == count - 1)
            {
                tangent =
                    (points[i] - points[i - 1]).normalized;
            }
            else
            {
                tangent =
                    (points[i + 1] - points[i - 1]).normalized;
            }

            Vector3 normal =
                new Vector3(-tangent.y, tangent.x, 0f);

            float t = i / (float)(count - 1);

            float width =
                Mathf.Lerp(tailWidth, headWidth, t);

            Vector3 offset =
                normal * width * 0.5f;

            // World position → WaterTrail 自己的 Local Position
            Vector3 center =
                transform.InverseTransformPoint(points[i]);

            vertices[i * 2] =
                center + offset;

            vertices[i * 2 + 1] =
                center - offset;

            uvs[i * 2] =
                new Vector2(t, 0f);

            uvs[i * 2 + 1] =
                new Vector2(t, 1f);

            float alpha =
                Mathf.Lerp(tailAlpha, headAlpha, t);

            Color color =
                new Color(0.75f, 0.95f, 1f, alpha);

            colors[i * 2] = color;
            colors[i * 2 + 1] = color;
        }

        int triangleIndex = 0;

        for (int i = 0; i < count - 1; i++)
        {
            int a = i * 2;
            int b = i * 2 + 1;
            int c = i * 2 + 2;
            int d = i * 2 + 3;

            triangles[triangleIndex++] = a;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = b;

            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = d;
            triangles[triangleIndex++] = b;
        }

        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;

        mesh.RecalculateBounds();
    }
}