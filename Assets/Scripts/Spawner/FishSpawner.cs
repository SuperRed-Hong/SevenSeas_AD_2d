using UnityEngine;
using System.Collections.Generic;
[RequireComponent(typeof(BoxCollider2D))]
public class FishSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct WeightedFish
    {
        public FishController prefab;
        public float weight;
    }
    /// <summary>
    /// Above I added a small data structure so we can emulate rarity when it comes to fish. Instead of it being randomly chosen, we can have a weight assigned. Like a % chance.
    /// </summary>
    [SerializeField] [Tooltip("Fish Prefabs with % chance. The higher the number, the more common the chance of the fish spawning is.")]
    private WeightedFish[] fishPrefabs;

    [SerializeField, Range(0, 50)] [Tooltip("Minimum number of fish spawned when the scene starts.")]
    private int FishCount = 20;
    
    
    [SerializeField, Min(0f)]
    [Tooltip("Minimum allowed distance between the centers of two spawned fish.")]
    private float minimumSpawnSpacing = 0.75f;
    
    [SerializeField, Min(1)]
    [Tooltip("Maximum placement attempts before a fish is skipped.")]
    private int maximumPlacementAttempts = 50;
    
    private readonly List<Vector2> spawnedPositions = new();
    
    private BoxCollider2D spawnArea;

    private void Awake()
    {
        spawnArea = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        SpawnFish();
    }

    private void SpawnFish()
    {
        if (fishPrefabs == null || fishPrefabs.Length == 0)
        {
            Debug.LogError("FishSpawner Requires at least one fish prefab.");
            return;
        }

        spawnedPositions.Clear();
        Bounds bounds = spawnArea.bounds;

        for (int i = 0; i < FishCount; i++)
        {   
            bool positionFound = false;
            Vector2 spawnPosition = default;

            for (int attempt = 0;
                 attempt < maximumPlacementAttempts;
                 attempt++)
            {
                Vector2 candidatePosition = new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));

                if (!IsPositionAvailable(candidatePosition))
                {
                    continue;
                }
                spawnPosition = candidatePosition;
                positionFound = true;
                break;
            }
            if (!positionFound)
            {
                Debug.LogWarning(
                    $"FishSpawner could only place " +
                    $"{spawnedPositions.Count} of {FishCount} fish " +
                    "without overlap.");

                break;
            }

            FishController selectedPrefab = GetWeightedRandomFish();
            
            Instantiate(
                selectedPrefab,
                new Vector3(
                    spawnPosition.x,
                    spawnPosition.y,
                    transform.position.z),
                Quaternion.identity,transform);

            spawnedPositions.Add(spawnPosition);
        }
        
    }
    private FishController GetWeightedRandomFish()
    {
        float totalWeight = 0f;

        foreach (var fish in fishPrefabs)
            totalWeight += fish.weight;

        float randomValue = Random.value * totalWeight;

        foreach (var fish in fishPrefabs)
        {
            if (randomValue < fish.weight)
                return fish.prefab;

            randomValue -= fish.weight;
        }

        return fishPrefabs[0].prefab; // Just in case.
    }
        
    private bool IsPositionAvailable(Vector2 candidatePosition)
    {
        float minimumDistanceSquared =
            minimumSpawnSpacing * minimumSpawnSpacing;

        foreach (Vector2 existingPosition in spawnedPositions)
        {
            if ((candidatePosition - existingPosition).sqrMagnitude <
                minimumDistanceSquared)
            {
                return false;
            }
        }

        return true;
    }
}
