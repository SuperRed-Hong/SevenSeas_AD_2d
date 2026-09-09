using UnityEngine;

[CreateAssetMenu(fileName = "FishMovementProfile", menuName = "Seven Seas/Fish Movement Profile")]
public sealed class FishMovementProfile : ScriptableObject
{
    [SerializeField, Min(0f)] private float idleSpeedMultiplier = 0.3f;
    [SerializeField, Min(0.1f)] private float idleRoamRadius = 2f;
    [SerializeField] private Vector2 idleTravelDistance = new Vector2(0.4f, 1.2f);
    [SerializeField] private Vector2 idlePauseDuration = new Vector2(0.8f, 2f);
    [SerializeField, Min(1f)] private float turnSpeedDegrees = 180f;
    [SerializeField, Range(0.1f, 10f)] private float facingToleranceDegrees = 2f;
    [SerializeField] [Tooltip("The fish sprite's forward direction in degrees: right = 0, up = 90.")]
    private float spriteForwardAngle = 180f;
    [SerializeField, Min(0f)] private float obstaclePadding = 0.02f;
    [SerializeField, Range(1, 32)] private int targetSelectionAttempts = 12;
    [Header("Local Navigation")]
    [SerializeField, Min(0.1f)] private float maximumDetourDistance = 6f;
    [SerializeField, Min(0.05f)] private float detourClearance = 0.15f;
    [SerializeField, Min(0.05f)] private float navigationRetryDelay = 0.5f;
    [SerializeField, Min(0.01f)] private float waypointTolerance = 0.08f;
    [SerializeField, Min(0.05f)] private float recoveryStepDistance = 0.4f;
    [SerializeField, Range(0.5f, 10f)] private float rotationCheckStepDegrees = 4f;
    [SerializeField, Min(0.1f)] private float routeTargetReplanDistance = 1f;

    public float IdleSpeedMultiplier => idleSpeedMultiplier;
    public float IdleRoamRadius => idleRoamRadius;
    public Vector2 IdleTravelDistance => idleTravelDistance;
    public Vector2 IdlePauseDuration => idlePauseDuration;
    public float TurnSpeedDegrees => turnSpeedDegrees;
    public float FacingToleranceDegrees => facingToleranceDegrees;
    public float SpriteForwardAngle => spriteForwardAngle;
    public float ObstaclePadding => obstaclePadding;
    public int TargetSelectionAttempts => targetSelectionAttempts;
    public float MaximumDetourDistance => maximumDetourDistance;
    public float DetourClearance => detourClearance;
    public float NavigationRetryDelay => navigationRetryDelay;
    public float WaypointTolerance => waypointTolerance;
    public float RecoveryStepDistance => recoveryStepDistance;
    public float RotationCheckStepDegrees => rotationCheckStepDegrees;
    public float RouteTargetReplanDistance => routeTargetReplanDistance;

    private void OnValidate()
    {
        idleSpeedMultiplier = Mathf.Max(0f, idleSpeedMultiplier);
        idleRoamRadius = Mathf.Max(0.1f, idleRoamRadius);
        idleTravelDistance.x = Mathf.Max(0.05f, idleTravelDistance.x);
        idleTravelDistance.y = Mathf.Max(idleTravelDistance.x, idleTravelDistance.y);
        idlePauseDuration.x = Mathf.Max(0.05f, idlePauseDuration.x);
        idlePauseDuration.y = Mathf.Max(idlePauseDuration.x, idlePauseDuration.y);
        turnSpeedDegrees = Mathf.Max(1f, turnSpeedDegrees);
        facingToleranceDegrees = Mathf.Clamp(facingToleranceDegrees, 0.1f, 10f);
        obstaclePadding = Mathf.Max(0f, obstaclePadding);
        targetSelectionAttempts = Mathf.Clamp(targetSelectionAttempts, 1, 32);
        maximumDetourDistance = Mathf.Max(0.1f, maximumDetourDistance);
        detourClearance = Mathf.Max(0.05f, detourClearance);
        navigationRetryDelay = Mathf.Max(0.05f, navigationRetryDelay);
        waypointTolerance = Mathf.Max(0.01f, waypointTolerance);
        recoveryStepDistance = Mathf.Max(0.05f, recoveryStepDistance);
        rotationCheckStepDegrees = Mathf.Clamp(rotationCheckStepDegrees, 0.5f, 10f);
        routeTargetReplanDistance = Mathf.Max(0.1f, routeTargetReplanDistance);
    }
}
