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
    [SerializeField] private Transform launchPoint;

    [SerializeField, Min(0f)]
    private float minimumCastDistance = 2f;

    [SerializeField, Min(0f)]
    private float maximumCastDistance = 6f;

    [SerializeField, Min(0.01f)]
    private float flightDuration = 0.75f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float flightElapsed;

    public event Action<Vector2> Landed;

    public FishingHookFlightState State { get; private set; } =
        FishingHookFlightState.Docked;
    
    
    // Follow the player's launch point after all regular Update methods
    // have finished, so the hook does not lag one frame behind the player.
    private void LateUpdate()
    {
        if (State != FishingHookFlightState.Docked ||
            launchPoint == null)
        {
            return;
        }

        FollowLaunchPoint();
    }

// Return the hook to its ready position.
// This will also be used when a new fishing attempt begins.
    public void Dock()
    {
        State = FishingHookFlightState.Docked;
        flightElapsed = 0f;

        if (launchPoint != null)
        {
            FollowLaunchPoint();
        }
    }

// Begin a cast using normalized power in the range [0, 1].
// This method prepares the trajectory; UpdateFlight will move the hook.
    public void Launch(float power)
    {
        if (launchPoint == null)
        {
            Debug.LogError(
                "FishingHookController requires a launch point.");
            return;
        }

        // Capture the current player position as this cast's origin.
        startPosition = launchPoint.position;

        // Convert normalized power [0, 1] into vertical travel distance.
        // Mathf.Max prevents an invalid maximum from becoming smaller
        // than the configured minimum distance.
        float castDistance = Mathf.Lerp(
            minimumCastDistance,
            Mathf.Max(minimumCastDistance, maximumCastDistance),
            Mathf.Clamp01(power));

        // The fishing scene casts upward on screen, along world-space +Y.
        targetPosition =
            startPosition + Vector3.up * castDistance;

        // Start exactly at the current launch point and reset flight time.
        transform.position = startPosition;
        flightElapsed = 0f;

        // LateUpdate stops following the player after entering Flying.
        State = FishingHookFlightState.Flying;
    }

// Snap the waiting hook to the player's launch point.
    private void FollowLaunchPoint()
    {
        transform.SetPositionAndRotation(
            launchPoint.position,
            launchPoint.rotation);
    }
    
    private void Update()
    {
        if (State == FishingHookFlightState.Flying)
        {
            UpdateFlight();
        }
    }
    // Move the hook from its captured start position to the target position.
// The final 2.5D parabolic presentation can replace this interpolation
// without changing Launch(), Landed, or the fishing-loop state machine.
    private void UpdateFlight()
    {
        flightElapsed += Time.deltaTime;

        // Protect the division even if serialized scene data contains zero.
        float safeDuration = Mathf.Max(0.01f, flightDuration);

        // Convert elapsed time into normalized flight progress [0, 1].
        float progress =
            Mathf.Clamp01(flightElapsed / safeDuration);

        // Temporary flat trajectory: move vertically at a constant rate.
        transform.position = Vector3.Lerp(
            startPosition,
            targetPosition,
            progress);

        if (progress < 1f)
        {
            return;
        }

        // Change state before publishing the event so landing is reported once.
        State = FishingHookFlightState.Landed;
        transform.position = targetPosition;

        // Report the physical landing position.
        // FishingLoopController decides what this means for the game flow.
        Landed?.Invoke((Vector2)targetPosition);
    }
}