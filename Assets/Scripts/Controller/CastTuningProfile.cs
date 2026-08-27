using UnityEngine;

public enum CastSensorAxis
{
    X,
    Y,
    Z
}

[CreateAssetMenu(
    fileName = "CastTuningProfile",
    menuName = "Seven Seas/Cast Tuning Profile")]
public sealed class CastTuningProfile : ScriptableObject
{
    [Header("Sensor")]
    [SerializeField] private CastSensorAxis castAxis = CastSensorAxis.X;
    [SerializeField] private bool invertAxis;
    [SerializeField, Min(0.01f)] private float triggerThreshold = 1.25f;
    [SerializeField, Min(0f)] private float rearmThreshold = 0.45f;
    [SerializeField, Min(0.05f)] private float sampleWindow = 0.35f;
    [SerializeField, Min(0f)] private float cooldownDuration = 0.75f;
    [SerializeField, Min(0f)] private float filterSharpness = 20f;

    [Header("Power")]
    [SerializeField, Min(0f)] private float minimumPeak = 1.5f;
    [SerializeField, Min(0f)] private float maximumPeak = 7f;
    [SerializeField] private AnimationCurve powerCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Launch")]
    [SerializeField] private Vector2 minimumLaunchVelocity = new Vector2(3.5f, 6.5f);
    [SerializeField] private Vector2 maximumLaunchVelocity = new Vector2(9.5f, 12.5f);

    public CastSensorAxis CastAxis => castAxis;
    public bool InvertAxis => invertAxis;
    public float TriggerThreshold => triggerThreshold;
    public float RearmThreshold => rearmThreshold;
    public float SampleWindow => sampleWindow;
    public float CooldownDuration => cooldownDuration;
    public float FilterSharpness => filterSharpness;
    public float MinimumPeak => minimumPeak;
    public float MaximumPeak => maximumPeak;

    public float EvaluatePower(float peak)
    {
        float rangeMaximum = Mathf.Max(minimumPeak + 0.001f, maximumPeak);
        float normalized = Mathf.InverseLerp(minimumPeak, rangeMaximum, peak);
        return Mathf.Clamp01(powerCurve.Evaluate(normalized));
    }

    public Vector2 EvaluateLaunchVelocity(float power)
    {
        return Vector2.Lerp(
            minimumLaunchVelocity,
            maximumLaunchVelocity,
            Mathf.Clamp01(power));
    }
}
