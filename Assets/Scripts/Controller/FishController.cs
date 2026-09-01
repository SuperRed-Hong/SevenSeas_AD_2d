using UnityEngine;

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

    public FishState State { get; private set; } =
        FishState.Idle;

    public int ScoreValue => scoreValue;
    public void BeginApproach(Transform target)
    {
        if (target == null)
        {
            Debug.LogError($"{name} requires an approach target.");
            return;
        }

        approachTarget = target;
        State = FishState.Approaching;
    }

    private void Update()
    {
        if (State != FishState.Approaching ||
            approachTarget == null)
        {
            return;
        }

        // Move toward the bait at a frame-rate-independent speed.
        transform.position = Vector3.MoveTowards(
            transform.position,
            approachTarget.position,
            swimSpeed * Time.deltaTime);
    }
    public void MarkHooked()
    {
        approachTarget = null;
        State = FishState.Hooked;
    }

    public void ResetToIdle()
    {
        approachTarget = null;
        State = FishState.Idle;
    }
}