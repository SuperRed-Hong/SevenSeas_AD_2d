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

    [SerializeField, Min(0f)]
    [Tooltip("Extra clearance around the fish's spawn footprint, in world units.")]
    private float obstacleSpawnPadding = 0.05f;
    
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
        Physics2D.SyncTransforms();
        Bounds bounds = spawnArea.bounds;

        for (int i = 0; i < FishCount; i++)
        {   
            FishController selectedPrefab = GetWeightedRandomFish();
            if (selectedPrefab == null)
            {
                Debug.LogWarning("FishSpawner has no valid positive-weight prefabs.", this);
                break;
            }

            // Measure the instantiated variant so inherited scale and sprite settings are respected.
            FishController fish = Instantiate(selectedPrefab, transform.position,
                Quaternion.identity, transform);
            Physics2D.SyncTransforms();
            Bounds footprint = GetSpawnFootprint(fish);
            footprint.Expand(obstacleSpawnPadding * 2f);
            Vector3 footprintOffset = footprint.center - fish.transform.position;
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

                Bounds candidateBounds = footprint;
                candidateBounds.center = new Vector3(candidatePosition.x,
                    candidatePosition.y, transform.position.z) + footprintOffset;
                if (!FitsSpawnArea(candidateBounds) || ReelingObstacle.OverlapsBounds(candidateBounds))
                {
                    continue;
                }
                spawnPosition = candidatePosition;
                positionFound = true;
                break;
            }
            if (!positionFound)
            {
                fish.gameObject.SetActive(false);
                Destroy(fish.gameObject);
                continue;
            }

            fish.transform.position = new Vector3(spawnPosition.x,
                spawnPosition.y, transform.position.z);

            spawnedPositions.Add(spawnPosition);
        }

        if (spawnedPositions.Count < FishCount)
        {
            Debug.LogWarning($"FishSpawner placed {spawnedPositions.Count} of {FishCount} fish; " +
                "some fish had no valid spawn position within the attempt limit.", this);
        }
    }

    private static Bounds GetSpawnFootprint(FishController fish)
    {
        Bounds footprint = new Bounds(fish.transform.position, Vector3.zero);
        if (fish.TryGetComponent<SpriteRenderer>(out var renderer) && renderer.sprite != null)
        {
            footprint.Encapsulate(renderer.bounds);
        }

        // Ignore child splash/ripple renderers; only the fish body defines visual clearance.
        foreach (Collider2D body in fish.GetComponentsInChildren<Collider2D>())
        {
            if (body.enabled && body.gameObject.activeInHierarchy)
            {
                footprint.Encapsulate(body.bounds);
            }
        }

        return footprint;
    }

    private bool FitsSpawnArea(Bounds footprint)
    {
        // All corners must fit, including when the spawn box is rotated.
        Vector2 halfSize = spawnArea.size * 0.5f;
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        {
            Vector3 corner = new Vector3(x == 0 ? footprint.min.x : footprint.max.x,
                y == 0 ? footprint.min.y : footprint.max.y, transform.position.z);
            Vector2 local = (Vector2)spawnArea.transform.InverseTransformPoint(corner) - spawnArea.offset;
            if (Mathf.Abs(local.x) > halfSize.x || Mathf.Abs(local.y) > halfSize.y)
            {
                return false;
            }
        }

        return true;
    }
    private FishController GetWeightedRandomFish()
    {
        float totalWeight = 0f;
        FishController fallback = null;

        foreach (var fish in fishPrefabs)
        {
            if (fish.prefab == null || fish.weight <= 0f) continue;
            totalWeight += fish.weight;
            fallback = fish.prefab;
        }

        float randomValue = Random.value * totalWeight;

        foreach (var fish in fishPrefabs)
        {
            if (fish.prefab == null || fish.weight <= 0f) continue;
            if (randomValue < fish.weight)
                return fish.prefab;

            randomValue -= fish.weight;
        }

        return fallback;
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
