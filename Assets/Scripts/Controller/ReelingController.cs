using UnityEngine;
using System;
public sealed class ReelingController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The shore position that the hook retrieves toward.")]
    private Transform shoreTarget;

    
    [SerializeField]
    private FishingInputSource inputSource;

    [SerializeField]
    private Transform leftLaneLimit;

    [SerializeField]
    private Transform rightLaneLimit;

    [SerializeField, Tooltip("收线时鱼钩左右移动范围，引用 FishMovementArea。纵向仍向岸边收线，避免被矩形下边界拦住而无法上岸。未配置时兼容原岸边限位。")]
    private BoxCollider2D movementArea;

    [SerializeField]
    private ReelTuningProfile tuningProfile;

    [SerializeField]
    [Tooltip("Logs the hook's measured lateral speed during Reeling.")]
    private bool logActualLateralSpeed;

    private const float LateralSpeedLogInterval = 0.25f;

    private float nextLateralSpeedLogTime;
    private float catchTensionMultiplier = 1f;

    public float ActualLateralSpeed { get; private set; }
    public bool IsActive { get; private set; }
    public float Tension01 { get; private set; }
    public event Action AttemptFailed;
    public event Action RetrievalCompleted;
    public event Action<float> AcceleratedDistanceMoved;
    
    public float DistanceToShore => Mathf.Abs(shoreTarget.position.y - transform.position.y);
    private bool attemptFailureReported;

    public void BeginRetrieval()
    {
        if (shoreTarget == null ||
            inputSource == null ||
            (movementArea == null && (leftLaneLimit == null || rightLaneLimit == null)) ||
            tuningProfile == null)
        {
            Debug.LogError(
                $"{nameof(ReelingController)} has missing references.",
                this);

            return;
        }
        attemptFailureReported = false;
        
        Tension01 = 0f;
        ActualLateralSpeed = 0f;
        nextLateralSpeedLogTime = Time.unscaledTime;
        IsActive = true;
    }

    public void CancelRetrieval()
    {
        Tension01 = 0f;
        ActualLateralSpeed = 0f;
        IsActive = false;
        catchTensionMultiplier = 1f;
    }

    public void SetCatchTensionMultiplier(float multiplier)
    {
        // Changing the hooked fish must not clear accumulated tension.
        catchTensionMultiplier =
            multiplier >= 0f && !float.IsNaN(multiplier) && !float.IsInfinity(multiplier)
                ? multiplier
                : 1f;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsActive ||
            !ReelingObstacle.IsBlockingCollider(other))
        {
            return;
        }
        
        ReportAttemptFailed();
    }

    private void ReportAttemptFailed()
    {
        if (!IsActive || attemptFailureReported)
        {
            return;
        }

        attemptFailureReported = true;
        IsActive = false;
        ActualLateralSpeed = 0f;
        AttemptFailed?.Invoke();
    }
    private void Update()
    {
        if (!IsActive)
        {
            return;
        }
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;
        
        bool accelerateHeld = inputSource.AccelerateHeld;

        float retrievalSpeed =
            accelerateHeld
                ? tuningProfile.AcceleratedRetrievalSpeed
                : tuningProfile.RetrievalSpeed;
        
        float previousX = transform.position.x;
        Vector3 position = transform.position;
        float previousDistanceToShore = DistanceToShore;
        // Convert semantic movement input into lateral hook movement.
        float moveInput =
            Mathf.Clamp(inputSource.MoveInput.x, -1f, 1f);

        position.x +=
            moveInput *
            tuningProfile.DodgeSpeed *
            deltaTime;

// Reeling uses the fish area; the player's shore lane remains independent.
        float minimumX = movementArea != null ? movementArea.bounds.min.x : Mathf.Min(
            leftLaneLimit.position.x,
            rightLaneLimit.position.x);

        float maximumX = movementArea != null ? movementArea.bounds.max.x : Mathf.Max(
            leftLaneLimit.position.x,
            rightLaneLimit.position.x);

        position.x = Mathf.Clamp(
            position.x,
            minimumX,
            maximumX);
        // Continue to shore even when it lies outside the swimming rectangle.
        position.y = Mathf.MoveTowards(
            position.y,
            shoreTarget.position.y,
            retrievalSpeed * deltaTime);

        transform.position = position;
// Measure the movement that actually survived lane clamping.
        ActualLateralSpeed =
            deltaTime > 0f
                ? Mathf.Abs(position.x - previousX) / deltaTime
                : 0f;
// Convert actual lateral speed into a normalized danger level.
// At or below the safe speed it is 0; at maximum evaluated speed it is 1.
        float lateral01 = Mathf.InverseLerp(
            tuningProfile.SafeLateralSpeed,
            tuningProfile.MaxEvaluatedLateralSpeed,
            ActualLateralSpeed);

// Convert the normalized danger level into tension added per second.
        float lateralRate =
            lateral01 *
            tuningProfile.MaxLateralTensionRate;

// Combine passive decay, acceleration, and lateral movement.
// Acceleration and fast dodging can increase tension at the same time.
        float netRate =
            -tuningProfile.DecayRate +
            (accelerateHeld
                ? tuningProfile.AccelerateRiseRate * catchTensionMultiplier
                : 0f) +
            lateralRate;

// Apply the per-second rate over this frame and keep tension within [0, 1].
        Tension01 = Mathf.Clamp01(
            Tension01 + netRate * deltaTime);
        
        if (Tension01 >= 1f)
        {
            Debug.LogWarning("Fishing line snapped.");

            ReportAttemptFailed();
            return;
        } 
        
        // Publish actual forward progress after clamping and failure checks.
        float distanceRetrieved = Mathf.Max(0f, previousDistanceToShore - DistanceToShore);
        if (accelerateHeld && distanceRetrieved > 0f)
        {
            AcceleratedDistanceMoved?.Invoke(distanceRetrieved);
        }

        if (logActualLateralSpeed &&
            Time.unscaledTime >= nextLateralSpeedLogTime)
        {
            Debug.Log(
                $"Lateral speed: {ActualLateralSpeed:F2}, " +
                $"tension: {Tension01:F2}");

            nextLateralSpeedLogTime =
                Time.unscaledTime + LateralSpeedLogInterval;
        }
        if (!Mathf.Approximately(
                transform.position.y,
                shoreTarget.position.y))
        {
            return;
        }
        
        ActualLateralSpeed = 0f;
        IsActive = false;

        Debug.Log("Hook reached the shore.");
        RetrievalCompleted?.Invoke();
    }
}
