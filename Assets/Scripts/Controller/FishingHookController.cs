using System;
using UnityEngine;

public enum FishingHookFlightState
{
    Docked,
    Flying,
    Landed
}

public sealed class FishingHookController : MonoBehaviour
{
    #region Inspector References

    [SerializeField] private Transform launchPoint;

    [SerializeField] private HookFlightProfile flightProfile;

    [SerializeField] [Tooltip("The child transform used to display the hook's airborne height.")]
    private Transform hookVisual;

    [SerializeField] private GameObject hookShadow;

    #endregion

    #region Runtime State and Events

    private float flightDuration;
    private float initialUpwardSpeed;
    private float flightGravity;


    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float flightElapsed;

    public event Action<Vector2> Landed;

    public FishingHookFlightState State { get; private set; } = FishingHookFlightState.Docked;

    #endregion


    #region Unity Lifecycle

    // Follow the player's launch point after all regular Update methods
    // have finished, so the hook does not lag one frame behind the player.
    private void LateUpdate()
    {
        // Show the water projection only during flight.
        if (hookShadow != null)
        {
            bool showShadow = State == FishingHookFlightState.Flying;

            if (hookShadow.activeSelf != showShadow)
            {
                hookShadow.SetActive(showShadow);
            }
        }

        if (State != FishingHookFlightState.Docked ||
            launchPoint == null)
        {
            return;
        }

        FollowLaunchPoint();
    }

    private void Update()
    {
        if (State == FishingHookFlightState.Flying)
        {
            UpdateFlight();
        }
    }

    #endregion

    #region Public Commands

// Return the hook to its ready position.
// This will also be used when a new fishing attempt begins.
    public void Dock()
    {
        State = FishingHookFlightState.Docked;
        flightElapsed = 0f;
        //Reset the visual offset when returning to the launch point.

        if (hookVisual != null)
        {
            hookVisual.localPosition = Vector3.zero;
        }

        if (launchPoint != null)
        {
            FollowLaunchPoint();
        }
    }

// Begin a cast using normalized power in the range [0, 1].
// This method prepares the trajectory; UpdateFlight will move the hook.
    public void Launch(float power, float launchSpeedMultiplier = 1f)
    {
        if (launchPoint == null || flightProfile == null || hookVisual == null)
        {
            Debug.LogError(
                "FishingHookController requires a launch point and flight profile.", this);
            return;
        }

        // Capture the current player position as this cast's origin.
        startPosition = launchPoint.position;

        // Equipment modifies this cost without changing the shared profile.
        float launchSpeed = flightProfile.EvaluateLaunchSpeed(power) * Mathf.Max(0.01f, launchSpeedMultiplier);

        float angleRadians = flightProfile.LaunchAngleDegrees * Mathf.Deg2Rad;

        // Forward speed moves along the water; upward speed controls height.
        float forwardSpeed = launchSpeed * Mathf.Cos(angleRadians);
        initialUpwardSpeed = launchSpeed * Mathf.Sin(angleRadians);
        flightGravity = flightProfile.Gravity;

        //The hook launches and lands at the same virtual water  height.
        flightDuration = 2f * initialUpwardSpeed / flightGravity;
        float travelDistance = forwardSpeed * flightDuration;

        targetPosition = startPosition + Vector3.up * travelDistance;

        transform.position = startPosition;
        flightElapsed = 0f;
        State = FishingHookFlightState.Flying;
    }

    public void CancelFlight()
    {
        if (State == FishingHookFlightState.Flying)
        {
            // Freeze at the current position without publishing a landing event.
            State = FishingHookFlightState.Landed;
        }
    }

    #endregion


    #region Flight and Visual Updates

// Snap the waiting hook to the player's launch point.
    private void FollowLaunchPoint()
    {
        transform.SetPositionAndRotation(
            launchPoint.position,
            launchPoint.rotation);
    }


    // Update the water position and the separate airborne visual.
    private void UpdateFlight()
    {
        flightElapsed = Mathf.Min(flightElapsed + Time.deltaTime, flightDuration);

 

        

        float progress =
            flightElapsed / flightDuration;
        
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        float height = initialUpwardSpeed * flightElapsed - 0.5f * flightGravity * flightElapsed * flightElapsed;

        hookVisual.position = transform.position + Vector3.up * Mathf.Max(0f, height);

        if (flightElapsed < flightDuration)
        {
            return;
        }
        
        transform.position = targetPosition;
        hookVisual.localPosition = Vector3.zero;
        
        //Change state first so landing is reported only once,
        State = FishingHookFlightState.Landed;
        Landed?.Invoke((Vector2)targetPosition);
    }

    #endregion
}
