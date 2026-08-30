using UnityEngine;

public class WaterTrail : MonoBehaviour
{
    public Transform target;
    public LineRenderer lineRenderer;

    public float trailLength = 2f;

    private Vector3 previousPosition;

    void Start()
    {
        previousPosition = target.position;

        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(0, previousPosition);
        lineRenderer.SetPosition(1, target.position);
    }

    void Update()
    {
        Vector3 currentPosition = target.position;

        Vector3 direction = currentPosition - previousPosition;

        if (direction.magnitude > trailLength)
        {
            previousPosition =
                currentPosition - direction.normalized * trailLength;
        }

        lineRenderer.SetPosition(0, previousPosition);
        lineRenderer.SetPosition(1, currentPosition);
    }
}