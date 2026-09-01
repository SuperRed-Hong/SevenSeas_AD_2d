using UnityEngine;

public class FishWaterVFX : MonoBehaviour
{
    public GameObject waterRipplePrefab;

    public float spawnInterval = 0.7f;

    private float timer;

    void Start()
    {
        timer = Random.Range(0f, spawnInterval);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnWaterRipple();
            timer = 0f;
        }
    }

    void SpawnWaterRipple()
    {
        Instantiate(
            waterRipplePrefab,
            transform.position,
            Quaternion.identity
        );
    }
}