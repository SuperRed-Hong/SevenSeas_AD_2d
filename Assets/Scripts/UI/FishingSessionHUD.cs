using TMPro;
using UnityEngine;

public sealed class FishingSessionHUD : MonoBehaviour
{
    [SerializeField] private FishingLoopController loopController;
    [SerializeField] private ScoreTracker scoreTracker;
    [SerializeField] private HookTracker hookTracker;
    [SerializeField] private SessionTimer sessionTimer;

    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;

    private void Update()
    {
        int secondsRemaining =
            Mathf.CeilToInt(sessionTimer.TimeRemaining);

        statusText.text =
            $"Score: {scoreTracker.Score}\n" +
            $"Hooks: {hookTracker.HooksRemaining}\n" +
            $"Time: {secondsRemaining}";

        bool isGameOver =
            loopController.CurrentState ==
            FishingLoopState.GameOver;

        if (gameOverPanel.activeSelf != isGameOver)
        {
            gameOverPanel.SetActive(isGameOver);
        }

        if (isGameOver)
        {
            gameOverText.text =
                $"GAME OVER\nScore: {scoreTracker.Score}";
        }
    }
}