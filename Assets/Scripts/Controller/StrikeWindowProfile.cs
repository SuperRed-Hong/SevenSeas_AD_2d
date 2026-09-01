using UnityEngine;

[CreateAssetMenu(
    fileName = "StrikeWindowProfile",
    menuName = "Seven Seas/Strike Window Profile")]
public sealed class StrikeWindowProfile : ScriptableObject
{
    private const float MinimumBandWidth = 0.01f;

    [Header("Timing")]
    [SerializeField, Min(0.01f)]
    [Tooltip("Total time for the moving ring to shrink from radius 1 to 0.")]
    private float windowDuration = 1.5f;

    [SerializeField, Min(0f)]
    [Tooltip("Input lockout duration after a rejected strike attempt.")]
    private float attemptCooldown = 0.35f;

    [Header("Target Band")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("Inner normalized radius of the successful strike band.")]
    private float targetBandInnerRadius = 0.24f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Outer normalized radius of the successful strike band.")]
    private float targetBandOuterRadius = 0.40f;

    [Header("Rejected Attempt Feedback")]
    [SerializeField, Min(0f)]
    [Tooltip("Strength of the camera shake after a rejected strike attempt.")]
    private float shakeAmplitude = 0.12f;

    [SerializeField, Min(0f)]
    [Tooltip("Duration of the camera shake after a rejected strike attempt.")]
    private float shakeDuration = 0.15f;

    public float WindowDuration => windowDuration;
    public float AttemptCooldown => attemptCooldown;
    public float TargetBandInnerRadius => targetBandInnerRadius;
    public float TargetBandOuterRadius => targetBandOuterRadius;
    public float ShakeAmplitude => shakeAmplitude;
    public float ShakeDuration => shakeDuration;

    private void OnValidate()
    {
        windowDuration = Mathf.Max(0.01f, windowDuration);
        attemptCooldown = Mathf.Max(0f, attemptCooldown);

        targetBandInnerRadius = Mathf.Clamp(
            targetBandInnerRadius,
            0f,
            1f - MinimumBandWidth);

        targetBandOuterRadius = Mathf.Clamp(
            targetBandOuterRadius,
            targetBandInnerRadius + MinimumBandWidth,
            1f);

        shakeAmplitude = Mathf.Max(0f, shakeAmplitude);
        shakeDuration = Mathf.Max(0f, shakeDuration);
    }
}