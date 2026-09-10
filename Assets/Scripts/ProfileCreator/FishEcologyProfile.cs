using UnityEngine;

[CreateAssetMenu(fileName = "FishEcologyProfile", menuName = "Seven Seas/Fish Ecology Profile")]
public sealed class FishEcologyProfile : ScriptableObject
{
    [SerializeField, Min(0f)] private float fleeEnterRadius = 3f;
    [SerializeField, Min(0f)] private float fleeExitRadius = 4f;
    [SerializeField, Min(0f)] private float fleeSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float predatorDetectionRadius = 5f;
    [SerializeField, Range(0f, 1f)] private float predationProbability = 0.3f;
    [SerializeField, Min(0f)] private float predatorSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float mouthContactTolerance = 0.1f;
    [SerializeField] private bool largeCanPredate = true;
    [SerializeField] private bool specialCanPredate = true;

    public float FleeEnterRadius => fleeEnterRadius;
    public float FleeExitRadius => fleeExitRadius;
    public float FleeSpeedMultiplier => fleeSpeedMultiplier;
    public float PredatorDetectionRadius => predatorDetectionRadius;
    public float PredationProbability => predationProbability;
    public float PredatorSpeedMultiplier => predatorSpeedMultiplier;
    public float MouthContactTolerance => mouthContactTolerance;
    public bool LargeCanPredate => largeCanPredate;
    public bool SpecialCanPredate => specialCanPredate;

    public bool IsPredator(FishAppearanceCategory category)
    {
        return (largeCanPredate && category == FishAppearanceCategory.Large) ||
            (specialCanPredate && category == FishAppearanceCategory.Special);
    }

    private void OnValidate()
    {
        fleeEnterRadius = Mathf.Max(0f, fleeEnterRadius);
        fleeExitRadius = Mathf.Max(fleeEnterRadius + 0.1f, fleeExitRadius);
        fleeSpeedMultiplier = Mathf.Max(0f, fleeSpeedMultiplier);
        predatorDetectionRadius = Mathf.Max(0f, predatorDetectionRadius);
        predationProbability = Mathf.Clamp01(predationProbability);
        predatorSpeedMultiplier = Mathf.Max(0f, predatorSpeedMultiplier);
        mouthContactTolerance = Mathf.Max(0f, mouthContactTolerance);
    }
}
