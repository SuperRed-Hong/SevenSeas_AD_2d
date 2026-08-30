using UnityEngine;

public class WaterRippleSpawner : MonoBehaviour
{
    public GameObject ripplePrefab;

    public float spawnInterval = 0.7f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnRipple();
            timer = 0f;
        }
    }

    void SpawnRipple()
    {
        Instantiate(
            ripplePrefab,
            transform.position,
            Quaternion.identity
        );
    }
}