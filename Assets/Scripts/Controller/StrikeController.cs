using System;
using UnityEngine;

public sealed class StrikeController : MonoBehaviour
{
    [SerializeField]
    private CastGestureDetector gestureDetector;

    [SerializeField]
    private FishingInputSource inputSource;
    [SerializeField]
    private StrikeWindowProfile tuningProfile;


    public event Action Succeeded;
    public event Action TimedOut;
    public event Action AttemptRejected;

    public bool IsActive { get; private set; }

    public float RingRadius01 { get; private set; } = 1f;

    public float TargetBandInnerRadius =>
        tuningProfile != null
            ? tuningProfile.TargetBandInnerRadius
            : 0f;

    public float TargetBandOuterRadius =>
        tuningProfile != null
            ? tuningProfile.TargetBandOuterRadius
            : 0f;

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
        elapsedTime = 0f;
        cooldownRemaining = 0f;
        RingRadius01 = 1f;
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

        // Convert elapsed time into normalized progress from 0 to 1.
        float progress01 =
            Mathf.Clamp01(elapsedTime / tuningProfile.WindowDuration);

        // Shrink linearly from radius 1 at the start to 0 at the end.
        RingRadius01 = 1f - progress01;

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
        RingRadius01 = 0f;

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

        bool isInsideTargetBand =
            RingRadius01 >= tuningProfile.TargetBandInnerRadius &&
            RingRadius01 <= tuningProfile.TargetBandOuterRadius;

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