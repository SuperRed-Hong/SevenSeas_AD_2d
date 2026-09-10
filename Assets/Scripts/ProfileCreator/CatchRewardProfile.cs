using UnityEngine;

[CreateAssetMenu(fileName = "CatchRewardProfile", menuName = "Seven Seas/Catch Reward Profile")]
public sealed class CatchRewardProfile : ScriptableObject
{
    [Header("Time Reward (Seconds)")]
    [SerializeField, Min(0f)] private float smallTimeReward = 2f;
    [SerializeField, Min(0f)] private float mediumTimeReward = 4f;
    [SerializeField, Min(0f)] private float largeTimeReward = 6f;
    [SerializeField, Min(0f)] private float specialTimeReward = 6f;

    [Header("Acceleration Tension Multiplier")]
    [SerializeField, Min(0f)] private float smallAccelerationTensionMultiplier = 0.8f;
    [SerializeField, Min(0f)] private float mediumAccelerationTensionMultiplier = 1f;
    [SerializeField, Min(0f)] private float largeAccelerationTensionMultiplier = 1.3f;
    [SerializeField, Min(0f)] private float specialAccelerationTensionMultiplier = 1.3f;

    public float GetTimeReward(FishAppearanceCategory category)
    {
        switch (category)
        {
            case FishAppearanceCategory.Small: return NonNegativeFinite(smallTimeReward, 2f);
            case FishAppearanceCategory.Medium: return NonNegativeFinite(mediumTimeReward, 4f);
            case FishAppearanceCategory.Large: return NonNegativeFinite(largeTimeReward, 6f);
            case FishAppearanceCategory.Special: return NonNegativeFinite(specialTimeReward, 6f);
            default: return 0f;
        }
    }

    public float GetAccelerationTensionMultiplier(FishAppearanceCategory category)
    {
        switch (category)
        {
            case FishAppearanceCategory.Small: return NonNegativeFinite(smallAccelerationTensionMultiplier, 0.8f);
            case FishAppearanceCategory.Medium: return NonNegativeFinite(mediumAccelerationTensionMultiplier, 1f);
            case FishAppearanceCategory.Large: return NonNegativeFinite(largeAccelerationTensionMultiplier, 1.3f);
            case FishAppearanceCategory.Special: return NonNegativeFinite(specialAccelerationTensionMultiplier, 1.3f);
            default: return 1f;
        }
    }

    private void OnValidate()
    {
        smallTimeReward = NonNegativeFinite(smallTimeReward, 2f);
        mediumTimeReward = NonNegativeFinite(mediumTimeReward, 4f);
        largeTimeReward = NonNegativeFinite(largeTimeReward, 6f);
        specialTimeReward = NonNegativeFinite(specialTimeReward, 6f);
        smallAccelerationTensionMultiplier = NonNegativeFinite(smallAccelerationTensionMultiplier, 0.8f);
        mediumAccelerationTensionMultiplier = NonNegativeFinite(mediumAccelerationTensionMultiplier, 1f);
        largeAccelerationTensionMultiplier = NonNegativeFinite(largeAccelerationTensionMultiplier, 1.3f);
        specialAccelerationTensionMultiplier = NonNegativeFinite(specialAccelerationTensionMultiplier, 1.3f);
    }

    private static float NonNegativeFinite(float value, float fallback)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
    }
}
