using UnityEngine;

[CreateAssetMenu(
    fileName = "StrikeWindowProfile",
    menuName = "Seven Seas/Strike Window Profile")]
public sealed class StrikeWindowProfile : ScriptableObject
{


    [Header("Timing")]
    [SerializeField, Min(0.01f)]
    [Tooltip("Total duration of both ring passes: inward, then outward.")]
    private float windowDuration = 3f;

    [SerializeField, Min(0f)]
    [Tooltip("Input lockout duration after a rejected strike attempt.")]
    private float attemptCooldown = 0.35f;


    [Header("Rejected Attempt Feedback")]
    [SerializeField, Min(0f)]
    [Tooltip("Strength of the camera shake after a rejected strike attempt.")]
    private float shakeAmplitude = 0.12f;

    [SerializeField, Min(0f)]
    [Tooltip("Duration of the camera shake after a rejected strike attempt.")]
    private float shakeDuration = 0.15f;

    [Header("Random Target Band")]
    [SerializeField, Range(0.01f, 0.99f)]
    [Tooltip("Width of the visible target band, normalized to the maximum ring radius.")]
    private float targetBandWidth = 0.16f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Minimum radius from which the target band center is randomly selected.")]
    private float minBandCenterRadius = 0.25f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Maximum randomized band center radius. Keep it away from the outer edge.")]
    private float maxBandCenterRadius = 0.70f;

    [SerializeField, Min(0f)]
    [Tooltip("Hidden extra radius accepted on each side of the visible target band.")]
    private float hitForgivenessRadius = 0.04f;
    
    
    public float WindowDuration => windowDuration;
    public float AttemptCooldown => attemptCooldown;

    public float ShakeAmplitude => shakeAmplitude;
    public float ShakeDuration => shakeDuration;

    
    // Expose configuration without allowing consumers to modify the shared asset.
    public float TargetBandWidth => targetBandWidth;
    public float MinBandCenterRadius => minBandCenterRadius;
    public float MaxBandCenterRadius => maxBandCenterRadius;
    public float HitForgivenessRadius => hitForgivenessRadius;
    
    
    private void OnValidate()
    {
        windowDuration = Mathf.Max(0.01f, windowDuration);
        attemptCooldown = Mathf.Max(0f, attemptCooldown);


        shakeAmplitude = Mathf.Max(0f, shakeAmplitude);
        shakeDuration = Mathf.Max(0f, shakeDuration);
        
        
        // Keep the band width valid and leave room for random placement.
        targetBandWidth = Mathf.Clamp(targetBandWidth, 0.01f, 0.99f);

        float halfWidth = targetBandWidth * 0.5f;

// Keep the entire visible band inside the normalized radius range.
        float minimumCenter = halfWidth;
        float maximumCenter = 1f - halfWidth;

// Preserve a nonzero interval between the minimum and maximum centers.
        minBandCenterRadius = Mathf.Clamp(
            minBandCenterRadius,
            minimumCenter,
            maximumCenter - 0.001f);

        maxBandCenterRadius = Mathf.Clamp(
            maxBandCenterRadius,
            minBandCenterRadius + 0.001f,
            maximumCenter);

// Forgiveness cannot be negative. Warn when it becomes visually misleading.
        hitForgivenessRadius = Mathf.Max(0f, hitForgivenessRadius);

        if (hitForgivenessRadius >= halfWidth)
        {
            Debug.LogWarning(
                "Strike forgiveness is at least half the target band width. " +
                "The accepted range may be significantly wider than the visual cue.",
                this);
        }
    }
}