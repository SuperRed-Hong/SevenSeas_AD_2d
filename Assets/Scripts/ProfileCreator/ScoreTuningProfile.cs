using UnityEngine;

[CreateAssetMenu(fileName = "ScoreTuningProfile", menuName = "Seven Seas/Score Tuning Profile")]
public sealed class ScoreTuningProfile : ScriptableObject
{
    [Header("Catch Distance")]
    [Tooltip("World-space distance from shore required for the maximum multiple.")]
    [SerializeField, Min(0.01f)]
    private float distanceForMaxMultiplier = 10f;
    
    [SerializeField, Min(1f)]
    private float maxDistanceMultiplier = 2f;
    
    public float GetDistanceMultiplier(float catchDistance)
    {
        float progress = Mathf.Clamp01(catchDistance /  distanceForMaxMultiplier);
        return Mathf.Lerp(1f, maxDistanceMultiplier, progress);
    }


    private void OnValidate()
    {
        distanceForMaxMultiplier =
            Mathf.Max(0.01f, distanceForMaxMultiplier);

        maxDistanceMultiplier =
            Mathf.Max(1f, maxDistanceMultiplier);
    }
}
