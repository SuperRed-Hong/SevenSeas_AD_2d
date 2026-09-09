using UnityEngine;

public sealed class FishingPauseController : MonoBehaviour
{
    [SerializeField] private Behaviour[] pausedBehaviours;
    [SerializeField] private FishingSceneEntryGuard entryGuard;
    [SerializeField] private FishingAudioFeedback audioFeedback;
    private Object owner;
    private bool[] previousStates;
    private float previousTimeScale;
    public bool CanPause => owner == null && (entryGuard == null || !entryGuard.IsStartingGame);

    public bool TryPause(Object requester)
    {
        if (!CanPause || requester == null) return false;
        owner = requester;
        previousTimeScale = Time.timeScale;
        previousStates = new bool[pausedBehaviours.Length];
        for (int i = 0; i < pausedBehaviours.Length; i++)
        {
            if (pausedBehaviours[i] == null) continue;
            previousStates[i] = pausedBehaviours[i].enabled;
            pausedBehaviours[i].enabled = false;
        }
        Time.timeScale = 0f;
        audioFeedback?.SetPaused(true);
        return true;
    }

    public void Resume(Object requester)
    {
        if (owner == null || owner != requester) return;
        owner = null;
        Time.timeScale = previousTimeScale;
        audioFeedback?.SetPaused(false);
        for (int i = 0; i < pausedBehaviours.Length; i++)
            if (pausedBehaviours[i] != null) pausedBehaviours[i].enabled = previousStates[i];
    }

    private void OnDisable()
    {
        // Scene unloading must never leave global time frozen.
        if (owner != null)
        {
            owner = null;
            Time.timeScale = previousTimeScale;
        }
    }
}
