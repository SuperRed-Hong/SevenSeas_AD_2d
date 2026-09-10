using UnityEngine;

public sealed class GameOverPresentation : MonoBehaviour
{
    [SerializeField] private FishingLoopController loopController;
    [SerializeField] private CatchInventoryView inventoryView;
    [SerializeField] private GameObject immediateActions;
    [SerializeField, Min(0f)] private float actionsDelay = 1f;

    private float remaining;
    private bool waiting;
    private bool navigationStarted;
    private bool actionsShown;

    private void OnEnable()
    {
        navigationStarted = false;
        actionsShown = false;
        waiting = loopController != null && loopController.CurrentState == FishingLoopState.GameOver;
        remaining = actionsDelay;
        if (immediateActions != null) immediateActions.SetActive(false);
        if (!waiting) return;
        // Open synchronously when the HUD enters GameOver, before the next rendered frame.
        if (inventoryView != null)
        {
            inventoryView.OpenResults(ShowLeaderboard, PlayAgain, loopController.FinalScoreSaved);
            if (remaining <= 0f) ShowActions();
        }
        else if (immediateActions != null) immediateActions.SetActive(true);
    }

    private void LateUpdate()
    {
        if (!waiting || navigationStarted || actionsShown) return;
        if (loopController == null || loopController.CurrentState != FishingLoopState.GameOver)
        {
            waiting = false;
            return;
        }

        // UI timing continues while gameplay is stopped.
        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        if (remaining <= 0f) ShowActions();
    }

    private void ShowActions()
    {
        actionsShown = true;
        if (inventoryView != null) inventoryView.ShowResultActions();
    }

    public void PlayAgain()
    {
        if (waiting && !navigationStarted) Navigate(E_SceneID.GameScene);
    }

    public void ShowLeaderboard()
    {
        if (waiting && !navigationStarted) Navigate(E_SceneID.Leaderboard);
    }

    private void Navigate(E_SceneID destination)
    {
        waiting = false;
        navigationStarted = true;
        Time.timeScale = 1f;
        if (AppRoot.Instance != null) AppRoot.Instance.SceneLoader.LoadScene(destination);
        else SceneLoader.LoadWithoutAppRoot(destination);
    }

    private void OnDisable()
    {
        waiting = false;
    }
}
