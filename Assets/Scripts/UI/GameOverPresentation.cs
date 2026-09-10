using TMPro;
using UnityEngine;

public sealed class GameOverPresentation : MonoBehaviour
{
    [SerializeField] private FishingLoopController loopController;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField, Min(0f)] private float leaderboardDelay = 5f;

    private float remaining;
    private bool waiting;
    private bool navigationStarted;

    private void OnEnable()
    {
        navigationStarted = false;
        waiting = loopController != null && loopController.CurrentState == FishingLoopState.GameOver;
        remaining = leaderboardDelay;
        RefreshCountdown();
    }

    private void LateUpdate()
    {
        if (!waiting || navigationStarted) return;
        if (loopController == null || loopController.CurrentState != FishingLoopState.GameOver)
        {
            waiting = false;
            return;
        }

        // Run after UI clicks so Play Again wins over a timeout in the same frame.
        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        RefreshCountdown();
        if (remaining <= 0f) Navigate(E_SceneID.Leaderboard);
    }

    public void PlayAgain()
    {
        if (waiting && !navigationStarted) Navigate(E_SceneID.GameScene);
    }

    private void Navigate(E_SceneID destination)
    {
        waiting = false;
        navigationStarted = true;
        Time.timeScale = 1f;
        if (AppRoot.Instance != null) AppRoot.Instance.SceneLoader.LoadScene(destination);
        else SceneLoader.LoadWithoutAppRoot(destination);
    }

    private void RefreshCountdown()
    {
        if (countdownText == null || !waiting) return;
        string saveWarning = loopController.FinalScoreSaved ? "" : "SCORE NOT SAVED\n";
        countdownText.text = $"{saveWarning}HIGH SCORES IN {Mathf.CeilToInt(remaining)}";
    }

    private void OnDisable()
    {
        waiting = false;
    }
}
