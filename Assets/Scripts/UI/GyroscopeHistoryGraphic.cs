using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class GyroscopeHistoryGraphic : MaskableGraphic
{
    private struct Sample
    {
        public float Time;
        public Vector3 Value;
    }

    [SerializeField] private GyroscopeReader reader;
    [SerializeField, Min(0.5f)] private float historyDuration = 3f;
    [SerializeField, Min(0.1f)] private float displayRange = 6f;
    [SerializeField, Min(0.5f)] private float lineThickness = 2f;
    [SerializeField] private Color xColor = new Color(1f, 0.28f, 0.28f, 1f);
    [SerializeField] private Color yColor = new Color(0.28f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color zColor = new Color(0.25f, 0.55f, 1f, 1f);
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.14f);

    private readonly List<Sample> samples = new List<Sample>(256);

    public float DisplayRange
    {
        get => displayRange;
        set
        {
            displayRange = Mathf.Max(0.1f, value);
            SetVerticesDirty();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;

        reader = MotionTestServices.Resolve(reader);
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        if (reader != null && reader.IsEnabled)
        {
            samples.Add(new Sample
            {
                Time = now,
                Value = reader.AngularVelocity
            });
        }

        float oldestAllowedTime = now - historyDuration;
        int removeCount = 0;

        while (removeCount < samples.Count && samples[removeCount].Time < oldestAllowedTime)
        {
            removeCount++;
        }

        if (removeCount > 0)
        {
            samples.RemoveRange(0, removeCount);
        }

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = rectTransform.rect;
        float centerY = rect.center.y;

        AddLine(
            vertexHelper,
            new Vector2(rect.xMin, centerY),
            new Vector2(rect.xMax, centerY),
            gridColor,
            1f);

        for (int i = 1; i < 3; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, i / 3f);
            AddLine(
                vertexHelper,
                new Vector2(x, rect.yMin),
                new Vector2(x, rect.yMax),
                gridColor,
                1f);
        }

        DrawTrace(vertexHelper, rect, 0, xColor);
        DrawTrace(vertexHelper, rect, 1, yColor);
        DrawTrace(vertexHelper, rect, 2, zColor);
    }

    private void DrawTrace(VertexHelper vertexHelper, Rect rect, int axis, Color traceColor)
    {
        if (samples.Count < 2)
        {
            return;
        }

        float now = Time.unscaledTime;
        Vector2 previous = GetPoint(samples[0], rect, now, axis);

        for (int i = 1; i < samples.Count; i++)
        {
            Vector2 current = GetPoint(samples[i], rect, now, axis);
            AddLine(vertexHelper, previous, current, traceColor, lineThickness);
            previous = current;
        }
    }

    private Vector2 GetPoint(Sample sample, Rect rect, float now, int axis)
    {
        float normalizedTime = 1f - Mathf.Clamp01((now - sample.Time) / historyDuration);
        float value = axis switch
        {
            0 => sample.Value.x,
            1 => sample.Value.y,
            2 => sample.Value.z,
            _ => 0f
        };
        float normalizedValue = Mathf.Clamp(value / displayRange, -1f, 1f);

        return new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, normalizedTime),
            Mathf.Lerp(rect.yMin, rect.yMax, normalizedValue * 0.5f + 0.5f));
    }

    private static void AddLine(
        VertexHelper vertexHelper,
        Vector2 start,
        Vector2 end,
        Color lineColor,
        float thickness)
    {
        Vector2 direction = end - start;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * thickness * 0.5f;
        int index = vertexHelper.currentVertCount;

        vertexHelper.AddVert(start - normal, lineColor, Vector2.zero);
        vertexHelper.AddVert(start + normal, lineColor, Vector2.zero);
        vertexHelper.AddVert(end + normal, lineColor, Vector2.zero);
        vertexHelper.AddVert(end - normal, lineColor, Vector2.zero);

        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }
}
