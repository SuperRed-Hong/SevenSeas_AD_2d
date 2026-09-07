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
    [SerializeField]
    [Tooltip("The gyroscope axis used to detect the cast gesture.")]
    private CastSensorAxis castAxis = CastSensorAxis.X;

    [SerializeField]
    [Tooltip("Reverses the selected axis so the opposite flick direction triggers a cast.")]
    private bool invertAxis;

    [SerializeField, Min(0.01f)]
    [Tooltip("Angular velocity in radians per second required to begin sampling a possible cast.")]
    private float triggerThreshold = 1.25f;

    [SerializeField, Min(0f)]
    [Tooltip("Angular velocity below which the detector becomes ready for another cast.")]
    private float rearmThreshold = 0.45f;

    [SerializeField, Min(0.05f)]
    [Tooltip("Time in seconds used to capture the gesture's peak angular velocity after triggering.")]
    private float sampleWindow = 0.35f;

    [SerializeField, Min(0f)]
    [Tooltip("Time in seconds after a detected cast before the detector can become ready again.")]
    private float cooldownDuration = 0.75f;

    [SerializeField, Min(0f)]
    [Tooltip("Response speed of the angular-velocity filter. Higher values react faster; zero disables smoothing.")]
    private float filterSharpness = 20f;

    [Header("Power")]
    [SerializeField, Min(0f)]
    [Tooltip("Filtered peak angular velocity that maps to zero cast power.")]
    private float minimumPeak = 1.5f;

    [SerializeField, Min(0f)]
    [Tooltip("Filtered peak angular velocity that maps to maximum cast power.")]
    private float maximumPeak = 7f;

    [SerializeField]
    [Tooltip("Controls how normalized peak angular velocity is converted into cast power.")]
    private AnimationCurve powerCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Launch")]
    [SerializeField]
    [Tooltip("Launch velocity used when cast power is zero.")]
    private Vector2 minimumLaunchVelocity = new Vector2(3.5f, 6.5f);

    [SerializeField]
    [Tooltip("Launch velocity used when cast power is one.")]
    private Vector2 maximumLaunchVelocity = new Vector2(9.5f, 12.5f);

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
