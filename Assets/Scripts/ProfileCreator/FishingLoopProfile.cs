using UnityEngine;

[CreateAssetMenu(fileName = "FishingLoopProfile", menuName = "Seven Seas/Fishing Loop Profile")]
public sealed class FishingLoopProfile : ScriptableObject
{
    [Header("Attempt Recovery")]
    [SerializeField, Min(0f)]
    [Tooltip("Cast lockout duration after returning from Striking or Reeling")]
    private float postAttemptCooldown = 0.8f;
    
    // Store configuration only. The controller owns the remaining time.
    
    public float PostAttemptCooldown => postAttemptCooldown;

    private void OnValidate()
    {
        // Zero allows immediate costing; negative durations are invalid.
        postAttemptCooldown = Mathf.Max(0f, postAttemptCooldown);
    }
    
}
