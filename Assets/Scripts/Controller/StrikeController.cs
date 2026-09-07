using System;
using UnityEngine;

public sealed class StrikeController : MonoBehaviour
{
    [SerializeField] private CastGestureDetector gestureDetector;

    [SerializeField] private FishingInputSource inputSource;
    [SerializeField] private StrikeWindowProfile tuningProfile;


    public event Action Succeeded;
    public event Action TimedOut;
    public event Action AttemptRejected;

    public bool IsActive { get; private set; }

    public float RingRadius01 { get; private set; } = 1f;

    // Store the visible band generated for the current attempt.
    // Both the HUD and hit detection read these same boundaries.
    public float TargetBandInnerRadius { get; private set; }
    public float TargetBandOuterRadius { get; private set; }

    // Expose the current travel direction for presentation and debugging.
    public bool IsExpanding { get; private set; }

    public bool IsCoolingDown => cooldownRemaining > 0f;

    public float CooldownRemaining01 =>
        tuningProfile != null &&
        tuningProfile.AttemptCooldown > 0f
            ? Mathf.Clamp01(
                cooldownRemaining /
                tuningProfile.AttemptCooldown)
            : 0f;

    private float elapsedTime;
    private float cooldownRemaining;


    public void BeginCheck()
    {
        if (tuningProfile == null)
        {
            Debug.LogError(
                $"{nameof(StrikeController)} requires a " +
                $"{nameof(StrikeWindowProfile)}.",
                this);

            return;
        }
        
        
        //Read the configured width and choose one center per attempt.
        float halfWidth = tuningProfile.TargetBandWidth * 0.5f;
        float centerRadius = UnityEngine.Random.Range(tuningProfile.MinBandCenterRadius, tuningProfile.MaxBandCenterRadius);
        
        // Keep the entire visible band inside the normalized radius range

        centerRadius = Mathf.Clamp(centerRadius, halfWidth, 1 - halfWidth);
        
        TargetBandInnerRadius = centerRadius - halfWidth;
        TargetBandOuterRadius = centerRadius + halfWidth;
        
        // Reset timing and start at the outer edge, moving inward.
        elapsedTime = 0f;
        cooldownRemaining = 0f;
        RingRadius01 = 1f;
        IsExpanding = false;
        
        
        IsActive = true;
    }

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        cooldownRemaining = Mathf.Max(
            0f,
            cooldownRemaining - Time.deltaTime);

        

        // The duration covers both Legs: Inward, the outward.
        float safeDuration = Mathf.Max(0.01f, tuningProfile.WindowDuration);
        float progress01 = Mathf.Clamp01(elapsedTime / safeDuration);
        
        //p Progress 0 -> 0.5 ->1 produce radius 1-> 0 -> 1
        RingRadius01 = Mathf.Abs(1f - 2f * progress01);

        IsExpanding = progress01 >= 0.5;
        
        if (elapsedTime < tuningProfile.WindowDuration)
        {
            return;
        }

        CompleteWithTimeout();
    }

    private void CompleteWithTimeout()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RingRadius01 = 1f;

        TimedOut?.Invoke();
    }

    private void HandleAttempt()
    {
        // Ignore attempts when the check is inactive, gameplay is paused,
        // or a rejected-attempt cooldown is already running.
        if (!IsActive ||
            Time.timeScale <= 0f ||
            IsCoolingDown)
        {
            return;
        }

        float forgiveness = tuningProfile.HitForgivenessRadius;
        float effectiveInner = Mathf.Clamp01(TargetBandInnerRadius - forgiveness);
        float effectiveOuter = Mathf.Clamp01(TargetBandOuterRadius + forgiveness);
        
        bool isInsideTargetBand = RingRadius01 >= effectiveInner && RingRadius01 <=effectiveOuter;

        if (isInsideTargetBand)
        {
            CompleteWithSuccess();
            return;
        }

        cooldownRemaining = tuningProfile.AttemptCooldown;
        AttemptRejected?.Invoke();
    }

    private void CompleteWithSuccess()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Succeeded?.Invoke();
    }

    private void OnEnable()
    {
        if (inputSource != null)
        {
            inputSource.StrikePerformed += HandleAttempt;
        }
    }

    private void OnDisable()
    {
        if (inputSource != null)
        {
            inputSource.StrikePerformed -= HandleAttempt;
        }
    }

    public void CancelCheck()
    {
        IsActive = false;
        cooldownRemaining = 0f;
    }
}