using UnityEngine;

[CreateAssetMenu(
    fileName = "ReelTuningProfile",
    menuName = "Seven Seas/Reel Tuning Profile")]
public sealed class ReelTuningProfile : ScriptableObject
{
    private const float MinimumDifference = 0.01f;

    [Header("Movement")]
    [SerializeField, Min(0.01f)]
    [Tooltip("Default vertical retrieval speed in world units per second.")]
    private float retrievalSpeed = 2f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Vertical retrieval speed while Accelerate is held.")]
    private float acceleratedRetrievalSpeed = 4f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Maximum lateral dodge speed in world units per second.")]
    private float dodgeSpeed = 4f;

    [Header("Tension")]
    [SerializeField, Min(0f)]
    [Tooltip("Tension removed per second at all times.")]
    private float decayRate = 0.8f;

    [SerializeField, Min(0f)]
    [Tooltip("Tension added per second while Accelerate is held.")]
    private float accelerateRiseRate = 1.2f;

    [SerializeField, Min(0f)]
    [Tooltip("Actual lateral speed at or below which dodging adds no tension.")]
    private float safeLateralSpeed = 1.33f;

    [SerializeField, Min(0f)]
    [Tooltip("Actual lateral speed at which the lateral tension contribution reaches its maximum.")]
    private float maxEvaluatedLateralSpeed = 4f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum tension added per second by lateral movement.")]
    private float maxLateralTensionRate = 1.4f;

    public float RetrievalSpeed => retrievalSpeed;
    public float AcceleratedRetrievalSpeed => acceleratedRetrievalSpeed;
    public float DodgeSpeed => dodgeSpeed;
    public float DecayRate => decayRate;
    public float AccelerateRiseRate => accelerateRiseRate;
    public float SafeLateralSpeed => safeLateralSpeed;
    public float MaxEvaluatedLateralSpeed => maxEvaluatedLateralSpeed;
    public float MaxLateralTensionRate => maxLateralTensionRate;

    private void OnValidate()
    {
        retrievalSpeed = Mathf.Max(0.01f, retrievalSpeed);
        acceleratedRetrievalSpeed = Mathf.Max(
            retrievalSpeed + MinimumDifference,
            acceleratedRetrievalSpeed);
        dodgeSpeed = Mathf.Max(0.01f, dodgeSpeed);

        decayRate = Mathf.Max(0f, decayRate);
        accelerateRiseRate = Mathf.Max(
            decayRate + MinimumDifference,
            accelerateRiseRate);
        safeLateralSpeed = Mathf.Max(0f, safeLateralSpeed);
        maxEvaluatedLateralSpeed = Mathf.Max(
            safeLateralSpeed + MinimumDifference,
            maxEvaluatedLateralSpeed);
        maxLateralTensionRate = Mathf.Max(
            decayRate + MinimumDifference,
            maxLateralTensionRate);
    }
}
