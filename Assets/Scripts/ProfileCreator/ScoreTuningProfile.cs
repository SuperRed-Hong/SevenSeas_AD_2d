using UnityEngine;

[CreateAssetMenu(fileName = "ScoreTuningProfile", menuName = "Seven Seas/Score Tuning Profile")]
public sealed class ScoreTuningProfile : ScriptableObject
{
    [Header("Catch Distance")]
    [Tooltip("World-space distance from shore required for the maximum multiple.")]
    [SerializeField, Min(0.01f)]
    private float distanceForMaxMultiplier = 10f;
    
    [SerializeField, Range(1f, 3f)]
    private float maxDistanceMultiplier = 3f;

    [Header("Acceleration Reward")]
    [SerializeField, Min(0f)]
    [Tooltip("Bonus points per world unit retrieved toward shore while accelerating with a fish.")]
    private float accelerationPointsPerUnit = 1f;

    public float AccelerationPointsPerUnit => accelerationPointsPerUnit;
    
    public float GetDistanceMultiplier(float catchDistance)
    {
        float progress = Mathf.Clamp01(catchDistance /  distanceForMaxMultiplier);
        return Mathf.Lerp(1f, Mathf.Clamp(maxDistanceMultiplier, 1f, 3f), progress);
    }


    private void OnValidate()
    {
        distanceForMaxMultiplier =
            Mathf.Max(0.01f, distanceForMaxMultiplier);

        maxDistanceMultiplier =
            Mathf.Clamp(maxDistanceMultiplier, 1f, 3f);
        accelerationPointsPerUnit = Mathf.Max(0f, accelerationPointsPerUnit);
    }
}
