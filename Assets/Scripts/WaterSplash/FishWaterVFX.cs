using UnityEngine;

public class FishWaterVFX : MonoBehaviour
{
    public GameObject waterRipplePrefab;
    public GameObject waterSplashPrefab;

    public float rippleSpawnInterval = 0.7f;
    public float splashSpawnInterval = 0.9f;

    private float timer;
    private FishController fishController;

    void Awake()
    {
        fishController = GetComponent<FishController>();
    }

    void Start()
    {
        timer = Random.Range(0f, rippleSpawnInterval);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (fishController != null &&
            fishController.State == FishState.Hooked)
        {
            if (timer >= splashSpawnInterval)
            {
                SpawnVFX(waterSplashPrefab);
                timer = 0f;
            }
        }
        else
        {
            if (timer >= rippleSpawnInterval)
            {
                SpawnVFX(waterRipplePrefab);
                timer = 0f;
            }
        }
    }

    void SpawnVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
        {
            return;
        }

        Instantiate(
            vfxPrefab,
            transform.position,
            Quaternion.identity
        );
    }
}