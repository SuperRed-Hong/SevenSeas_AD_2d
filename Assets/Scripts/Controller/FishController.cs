using UnityEngine;
using System.Collections.Generic;

public enum FishState
{
    Idle,
    Approaching,
    Hooked
}

public sealed class FishController : MonoBehaviour
{
    [SerializeField, Min(0f)]
    [Tooltip("The fish's movement speed in world units per second.")]
    private float swimSpeed = 2f;
    [SerializeField, Min(0)]
    [Tooltip("The score awarded when this fish is successfully caught.")]
    private int scoreValue = 1;

    private Transform approachTarget;
    private Transform hookedTarget;
    private BoxCollider2D movementArea;
    private FishMovementProfile movementProfile;
    private readonly FishNavigationGeometry geometry = new();
    private Vector3 localBitePoint;
    private float approachBiteRadius;
    private Quaternion originalLocalRotation;
    private Vector3 homePosition;
    private Vector3 idleTarget;
    private float idlePauseRemaining;
    private bool hasIdleTarget;
    private bool hasFinishedApproachTurn;
    private bool ambientMovementEnabled = true;
    private bool externalNavigation;
    private Transform externalTransformTarget;
    private bool externalUsesTransform;
    private Vector3 externalPositionTarget;
    private float externalStopRadius;
    private float externalSpeedMultiplier = 1f;
    private readonly List<Vector3> route = new();
    private int routeIndex;
    private Vector3 plannedTarget;
    private float retryRemaining;
    private bool recoveryRoute;
    private int recoveryAttempts;

    public FishState State { get; private set; } = FishState.Idle;
    public int ScoreValue => scoreValue;
    public Vector3 BitePosition => transform.TransformPoint(localBitePoint);
    public bool CanNavigate => isActiveAndEnabled && ambientMovementEnabled &&
        State != FishState.Hooked && movementProfile != null && movementArea != null && geometry.HasShape;
    public bool IsExternallyNavigating => externalNavigation;
    public bool HasReachedNavigationTarget { get; private set; }
    public bool IsFacingNavigationTarget => externalNavigation &&
        (!externalUsesTransform || externalTransformTarget != null) &&
        IsFacingTarget(externalUsesTransform ? externalTransformTarget.position : externalPositionTarget);
    public string NavigationStatus { get; private set; } = "Idle";
    public Vector3 NavigationWaypoint { get; private set; }
    public Collider2D NavigationBlocker { get; private set; }
    public bool CanClaimBait => isActiveAndEnabled && ambientMovementEnabled && !externalNavigation &&
        State == FishState.Approaching && approachTarget != null &&
        hasFinishedApproachTurn && IsFacingTarget(approachTarget.position);

    private void Awake()
    {
        originalLocalRotation = transform.localRotation;
    }

    public void ConfigureMovement(BoxCollider2D bounds, FishMovementProfile profile)
    {
        movementArea = bounds;
        movementProfile = profile;
        homePosition = transform.position;
        Physics2D.SyncTransforms();
        geometry.Capture(transform);
        CaptureBitePoint();
        ClearRoute();
        BeginIdlePause();
    }

    public bool IsNavigationPoseAllowed(Vector3 position, float padding = 0f)
    {
        return geometry.IsPoseAllowed(position, transform.rotation, movementArea, padding);
    }

    public void SetAmbientMovementEnabled(bool enabled)
    {
        ambientMovementEnabled = enabled;
        if (!enabled)
        {
            EndExternalNavigation();
            hasFinishedApproachTurn = false;
            hasIdleTarget = false;
            ClearRoute();
            NavigationStatus = "Disabled";
        }
    }

    public bool BeginExternalNavigation(Transform target, float stopRadius = 0f, float speedMultiplier = 1f)
    {
        if (target == null || !CanNavigate) return false;
        bool changedMode = !externalNavigation || !externalUsesTransform || externalTransformTarget != target;
        externalNavigation = true;
        externalTransformTarget = target;
        externalUsesTransform = true;
        ConfigureExternalNavigation(stopRadius, speedMultiplier, changedMode);
        return true;
    }

    public bool BeginExternalNavigation(Vector3 target, float stopRadius = 0f, float speedMultiplier = 1f)
    {
        if (!CanNavigate) return false;
        bool changedMode = !externalNavigation || externalUsesTransform;
        externalNavigation = true;
        externalTransformTarget = null;
        externalUsesTransform = false;
        externalPositionTarget = target;
        ConfigureExternalNavigation(stopRadius, speedMultiplier, changedMode);
        return true;
    }

    private void ConfigureExternalNavigation(float stopRadius, float speedMultiplier, bool changedMode)
    {
        externalStopRadius = Mathf.Max(0f, stopRadius);
        externalSpeedMultiplier = Mathf.Max(0f, speedMultiplier);
        hasFinishedApproachTurn = false;
        hasIdleTarget = false;
        if (!changedMode) return;
        HasReachedNavigationTarget = false;
        recoveryAttempts = 0;
        approachTarget = null;
        State = FishState.Idle;
        ClearRoute();
    }

    public void EndExternalNavigation()
    {
        externalNavigation = false;
        externalTransformTarget = null;
        externalUsesTransform = false;
        HasReachedNavigationTarget = false;
        ClearRoute();
        BeginIdlePause();
    }

    public void BeginApproach(Transform target, float biteRadius = 0f)
    {
        if (target == null)
        {
            Debug.LogError($"{name} requires an approach target.");
            return;
        }
        if (State == FishState.Hooked || externalNavigation) return;
        approachTarget = target;
        approachBiteRadius = Mathf.Max(0f, biteRadius);
        hasIdleTarget = false;
        hasFinishedApproachTurn = movementProfile == null;
        State = FishState.Approaching;
        recoveryAttempts = 0;
        ClearRoute();
    }

    private void Update()
    {
        if (!ambientMovementEnabled || Time.deltaTime <= 0f) return;
        if (State == FishState.Hooked) return;
        retryRemaining = Mathf.Max(0f, retryRemaining - Time.deltaTime);
        if (externalNavigation)
        {
            if (externalUsesTransform && externalTransformTarget == null)
            {
                EndExternalNavigation();
                return;
            }
            Vector3 target = externalUsesTransform ? externalTransformTarget.position : externalPositionTarget;
            target.z = transform.position.z;
            HasReachedNavigationTarget = Vector2.Distance(transform.position, target) <= externalStopRadius;
            if (HasReachedNavigationTarget)
            {
                ClearRoute();
                // Contact distance alone is not permission to consume prey from the fish's side.
                bool facing = TryFaceTarget(target, out bool blocked);
                NavigationBlocker = blocked ? geometry.LastBlocker : null;
                NavigationStatus = blocked ? "Blocked turn" : facing ? "Arrived" : "Turning at target";
                return;
            }
            Vector3 destination = Vector3.MoveTowards(target, transform.position, externalStopRadius);
            Navigate(destination, swimSpeed * externalSpeedMultiplier, false);
            HasReachedNavigationTarget = Vector2.Distance(transform.position, target) <= externalStopRadius + 0.001f;
            return;
        }
        if (State == FishState.Idle)
        {
            UpdateIdle();
            return;
        }
        if (approachTarget == null)
        {
            ResetToIdle();
            return;
        }
        Vector3 bait = approachTarget.position;
        bait.z = transform.position.z;
        if (movementProfile == null)
        {
            hasFinishedApproachTurn = true;
            transform.position = Vector3.MoveTowards(transform.position, bait, swimSpeed * Time.deltaTime);
            return;
        }
        Quaternion finalRotation = FacingRotation(transform.position, bait);
        Vector3 mouthOffset = finalRotation * Vector3.Scale(localBitePoint, transform.lossyScale);
        Vector3 finalMouth = Vector3.MoveTowards(bait, transform.position + mouthOffset, approachBiteRadius * 0.9f);
        Vector3 finalCenter = finalMouth - mouthOffset;
        finalCenter.z = transform.position.z;
        Navigate(finalCenter, swimSpeed, false);
        hasFinishedApproachTurn = routeIndex >= route.Count && IsFacingTarget(bait);
    }

    public bool CanReachBait(Vector3 baitPosition, float radius)
    {
        if (!CanClaimBait || Vector2.Distance(BitePosition, baitPosition) > radius) return false;
        return !ReelingObstacle.BlocksSegment(BitePosition, baitPosition);
    }

    private void UpdateIdle()
    {
        if (!CanNavigate) return;
        if (!hasIdleTarget)
        {
            idlePauseRemaining -= Time.deltaTime;
            if (idlePauseRemaining > 0f) return;
            if (!TrySelectIdleTarget())
            {
                BeginIdlePause();
                return;
            }
        }
        Navigate(idleTarget, swimSpeed * movementProfile.IdleSpeedMultiplier, true);
        if (Vector2.Distance(transform.position, idleTarget) <= movementProfile.WaypointTolerance ||
            NavigationStatus == "Unreachable") BeginIdlePause();
    }

    private bool TrySelectIdleTarget()
    {
        Vector2 distance = movementProfile.IdleTravelDistance;
        float radius = movementProfile.IdleRoamRadius;
        Vector2 toHome = homePosition - transform.position;
        if (toHome.sqrMagnitude > radius * radius)
        {
            idleTarget = Vector3.MoveTowards(transform.position, homePosition, distance.y);
            hasIdleTarget = IsNavigationPoseAllowed(idleTarget, movementProfile.ObstaclePadding);
            return hasIdleTarget;
        }
        for (int i = 0; i < movementProfile.TargetSelectionAttempts; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 target = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Random.Range(distance.x, distance.y);
            if (((Vector2)(target - homePosition)).sqrMagnitude > radius * radius ||
                !IsNavigationPoseAllowed(target, movementProfile.ObstaclePadding)) continue;
            idleTarget = target;
            hasIdleTarget = true;
            return true;
        }
        return false;
    }

    private void Navigate(Vector3 target, float speed, bool constrainHome)
    {
        NavigationBlocker = null;
        if (!CanNavigate || speed <= 0f) return;
        // Retain committed waypoints even while a moving target updates every frame.
        bool followingRoute = routeIndex < route.Count;
        if (followingRoute && Vector2.Distance(transform.position, route[routeIndex]) <= movementProfile.WaypointTolerance)
        {
            routeIndex++;
            if (routeIndex >= route.Count)
            {
                ClearRoute();
                followingRoute = false;
            }
        }
        if (followingRoute && !recoveryRoute &&
            Vector2.Distance(plannedTarget, target) > movementProfile.RouteTargetReplanDistance && retryRemaining <= 0f)
        {
            // Replan only at a committed waypoint, not on each target update.
            if (routeIndex > 0)
            {
                ClearRoute();
                followingRoute = false;
            }
        }
        if (!followingRoute && retryRemaining > 0f)
        {
            NavigationStatus = "Unreachable";
            return;
        }
        if (!followingRoute && !IsLegAllowed(transform.position, transform.rotation, target, out _))
        {
            Collider2D blocker = geometry.LastBlocker;
            NavigationBlocker = blocker;
            if (!TryPlanDetour(target, blocker, constrainHome) && !TryPlanRecovery(constrainHome))
            {
                retryRemaining = movementProfile.NavigationRetryDelay;
                NavigationStatus = "Unreachable";
                return;
            }
            followingRoute = true;
        }
        Vector3 waypoint = followingRoute ? route[routeIndex] : target;
        NavigationWaypoint = waypoint;
        bool facing = TryFaceTarget(waypoint, out bool blocked);
        if (blocked)
        {
            NavigationBlocker = geometry.LastBlocker;
            ClearRoute();
            retryRemaining = movementProfile.NavigationRetryDelay;
            NavigationStatus = "Blocked turn";
            return;
        }
        if (!facing)
        {
            NavigationStatus = "Turning";
            return;
        }
        if (!TryMoveTowards(waypoint, speed))
        {
            NavigationBlocker = geometry.LastBlocker;
            ClearRoute();
            retryRemaining = movementProfile.NavigationRetryDelay;
            NavigationStatus = "Blocked move";
            return;
        }
        NavigationStatus = recoveryRoute ? "Clearing turn" : followingRoute ? "Detouring" : "Direct";
        if (!recoveryRoute) recoveryAttempts = 0;
    }

    private bool TryPlanDetour(Vector3 target, Collider2D blocker, bool constrainHome)
    {
        if (blocker == null) return false;
        Vector2 forward = (Vector2)(target - transform.position);
        if (forward.sqrMagnitude <= 0.000001f) return false;
        forward.Normalize();
        Vector2 side = new Vector2(-forward.y, forward.x);
        Bounds bounds = blocker.bounds;
        float near = float.PositiveInfinity, far = float.NegativeInfinity;
        float left = float.PositiveInfinity, right = float.NegativeInfinity;
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        {
            Vector2 relative = new Vector2(x == 0 ? bounds.min.x : bounds.max.x,
                y == 0 ? bounds.min.y : bounds.max.y) - (Vector2)transform.position;
            float along = Vector2.Dot(relative, forward), lateral = Vector2.Dot(relative, side);
            near = Mathf.Min(near, along); far = Mathf.Max(far, along);
            left = Mathf.Min(left, lateral); right = Mathf.Max(right, lateral);
        }
        float clearance = geometry.BoundingRadius + movementProfile.ObstaclePadding + movementProfile.DetourClearance;
        float bestLength = float.PositiveInfinity;
        Vector3 bestA = default, bestB = default;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            float lateral = sign < 0 ? left - clearance : right + clearance;
            Vector3 a = transform.position + (Vector3)(forward * Mathf.Max(0f, near - clearance) + side * lateral);
            Vector3 b = transform.position + (Vector3)(forward * (far + clearance) + side * lateral);
            a.z = b.z = transform.position.z;
            if (Vector2.Distance(transform.position, a) > movementProfile.MaximumDetourDistance ||
                Vector2.Distance(a, b) > movementProfile.MaximumDetourDistance ||
                !WithinHome(a, constrainHome) || !WithinHome(b, constrainHome)) continue;
            if (!IsLegAllowed(transform.position, transform.rotation, a, out Quaternion atA) ||
                !IsLegAllowed(a, atA, b, out Quaternion atB) ||
                !IsLegAllowed(b, atB, target, out _)) continue;
            float length = Vector2.Distance(transform.position, a) + Vector2.Distance(a, b) + Vector2.Distance(b, target);
            if (length >= bestLength) continue;
            bestLength = length;
            bestA = a; bestB = b;
        }
        if (float.IsPositiveInfinity(bestLength)) return false;
        route.Clear();
        route.Add(bestA); route.Add(bestB);
        routeIndex = 0;
        plannedTarget = target;
        recoveryRoute = false;
        retryRemaining = movementProfile.NavigationRetryDelay;
        return true;
    }

    private bool TryPlanRecovery(bool constrainHome)
    {
        if (recoveryAttempts >= 2) return false;
        // If turning is blocked, try translating along the existing heading before turning again.
        float radians = (transform.eulerAngles.z + movementProfile.SpriteForwardAngle) * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        for (int step = 2; step >= 1; step--)
        {
            Vector3 target = transform.position + forward * (movementProfile.RecoveryStepDistance * step * 0.5f);
            if (!WithinHome(target, constrainHome) ||
                !geometry.IsTranslationAllowed(transform.position, target, transform.rotation, movementArea, movementProfile.ObstaclePadding)) continue;
            route.Clear(); route.Add(target); routeIndex = 0;
            recoveryRoute = true;
            recoveryAttempts++;
            return true;
        }
        return false;
    }

    private bool WithinHome(Vector3 position, bool constrainHome)
    {
        if (!constrainHome) return true;
        float radius = Mathf.Max(movementProfile.IdleRoamRadius, Vector2.Distance(transform.position, homePosition));
        return Vector2.Distance(position, homePosition) <= radius;
    }

    private bool IsLegAllowed(Vector3 start, Quaternion rotation, Vector3 end, out Quaternion finalRotation)
    {
        finalRotation = FacingRotation(start, end);
        return geometry.IsRotationAllowed(start, rotation, finalRotation, movementArea, movementProfile.ObstaclePadding,
            movementProfile.RotationCheckStepDegrees) &&
            geometry.IsTranslationAllowed(start, end, finalRotation, movementArea, movementProfile.ObstaclePadding);
    }

    private Quaternion FacingRotation(Vector3 start, Vector3 target)
    {
        Vector2 direction = target - start;
        if (direction.sqrMagnitude <= 0.000001f) return transform.rotation;
        return Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - movementProfile.SpriteForwardAngle);
    }

    private bool IsFacingTarget(Vector3 target)
    {
        if (movementProfile == null) return true;
        if (((Vector2)(target - transform.position)).sqrMagnitude <= 0.000001f) return true;
        return Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z,
            FacingRotation(transform.position, target).eulerAngles.z)) <= movementProfile.FacingToleranceDegrees;
    }

    private bool TryFaceTarget(Vector3 target, out bool blocked)
    {
        blocked = false;
        if (movementProfile == null) return true;
        float targetAngle = FacingRotation(transform.position, target).eulerAngles.z;
        float nextAngle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle,
            movementProfile.TurnSpeedDegrees * Time.deltaTime);
        Quaternion rotation = Quaternion.Euler(0f, 0f, nextAngle);
        if (!geometry.IsRotationAllowed(transform.position, transform.rotation, rotation, movementArea,
            movementProfile.ObstaclePadding, movementProfile.RotationCheckStepDegrees))
        {
            blocked = true;
            return false;
        }
        transform.rotation = rotation;
        return Mathf.Abs(Mathf.DeltaAngle(nextAngle, targetAngle)) <= movementProfile.FacingToleranceDegrees;
    }

    private bool TryMoveTowards(Vector3 target, float speed)
    {
        Vector3 next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (!geometry.IsTranslationAllowed(transform.position, next, transform.rotation, movementArea,
            movementProfile.ObstaclePadding)) return false;
        transform.position = next;
        return true;
    }

    private void ClearRoute()
    {
        route.Clear();
        routeIndex = 0;
        recoveryRoute = false;
        retryRemaining = 0f;
    }

    private void BeginIdlePause()
    {
        hasIdleTarget = false;
        recoveryAttempts = 0;
        ClearRoute();
        if (movementProfile == null) return;
        Vector2 duration = movementProfile.IdlePauseDuration;
        idlePauseRemaining = Random.Range(duration.x, duration.y);
    }

    private void CaptureBitePoint()
    {
        localBitePoint = Vector3.zero;
        if (movementProfile == null || !TryGetComponent<SpriteRenderer>(out var renderer) || renderer.sprite == null) return;
        Bounds bounds = renderer.localBounds;
        float angle = movementProfile.SpriteForwardAngle * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        float distanceX = Mathf.Abs(forward.x) > 0.0001f ? bounds.extents.x / Mathf.Abs(forward.x) : float.PositiveInfinity;
        float distanceY = Mathf.Abs(forward.y) > 0.0001f ? bounds.extents.y / Mathf.Abs(forward.y) : float.PositiveInfinity;
        localBitePoint = bounds.center + forward * Mathf.Min(distanceX, distanceY);
    }

    public void MarkHooked(Transform target)
    {
        if (target == null)
        {
            Debug.LogError($"{name} requires a hook target.");
            return;
        }
        EndExternalNavigation();
        approachTarget = null;
        hasFinishedApproachTurn = false;
        hasIdleTarget = false;
        hookedTarget = target;
        State = FishState.Hooked;
        NavigationStatus = "Hooked";
        transform.localRotation = originalLocalRotation;
        transform.position = target.position;
    }

    private void LateUpdate()
    {
        if (State == FishState.Hooked && hookedTarget != null) transform.position = hookedTarget.position;
    }

    public void ResetToIdle()
    {
        if (TryGetComponent<FishAppearance>(out var appearance)) appearance.RestoreHiddenAppearance();
        EndExternalNavigation();
        hookedTarget = null;
        approachTarget = null;
        hasFinishedApproachTurn = false;
        State = FishState.Idle;
        NavigationStatus = "Idle";
        BeginIdlePause();
    }
}
