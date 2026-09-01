using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class M4BaitingTestHarness : MonoBehaviour
{
    [SerializeField]
    private FishingSceneEntryGuard sceneEntryGuard;

    [SerializeField]
    private GameObject gameplayRoot;

    [SerializeField]
    private GameObject fishingLoopRoot;

    [SerializeField]
    private ShoreLaneController shoreLaneController;

    [SerializeField]
    private FishingHookController hookController;

    [SerializeField]
    private FishBiteRaceController biteRaceController;

    [SerializeField]
    [Tooltip("Hook position used when testing M4 directly in the Editor.")]
    private Vector2 testHookPosition = new Vector2(0f, 1.5f);
    
    
    
    private IEnumerator Start()
    {
#if UNITY_EDITOR
        // Normal Bootstrap entry already provides the required services.
        if (AppRoot.Instance != null)
        {
            yield break;
        }

        if (sceneEntryGuard == null ||
            gameplayRoot == null ||
            fishingLoopRoot == null ||
            shoreLaneController == null ||
            hookController == null ||
            biteRaceController == null)
        {
            Debug.LogError(
                "M4BaitingTestHarness is missing required references.",
                this);

            yield break;
        }

        // Prevent the regular entry guard from rejecting direct scene play.
        sceneEntryGuard.enabled = false;

        // Disable systems that require mobile sensor services.
        fishingLoopRoot.SetActive(false);
        shoreLaneController.enabled = false;
        hookController.enabled = false;

        Vector3 hookPosition = hookController.transform.position;
        hookController.transform.position = new Vector3(
            testHookPosition.x,
            testHookPosition.y,
            hookPosition.z);

        gameplayRoot.SetActive(true);

        // Allow FishSpawner.Start() to create all fish first.
        yield return null;

        biteRaceController.BeginRace();
#else
    yield break;
#endif
    }
}