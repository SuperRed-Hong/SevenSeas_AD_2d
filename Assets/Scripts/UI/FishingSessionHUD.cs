using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class FishingSessionHUD : MonoBehaviour
{
    [SerializeField] private FishingLoopController loopController;
    [SerializeField] private ScoreTracker scoreTracker;
    [SerializeField] private HookTracker hookTracker;
    [SerializeField] private SessionTimer sessionTimer;
    [SerializeField]
    private KeyboardMouseFishingInputSource keyboardMouseFishingInputSource;

    [SerializeField] private TMP_Text castChargeText;
    [SerializeField] private Slider castChargeSlider;
    [SerializeField] private TMP_Text statusText;
    
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private TMP_Text castDistanceText;
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

        multiplierText.text =
            $"x{loopController.CurrentDistanceMultiplier:F2}";
        castDistanceText.text =
            $"DISTANCE {loopController.CurrentCastDistance:F1}";
        
        bool isGameOver =
            loopController.CurrentState ==
            FishingLoopState.GameOver;

        if (gameOverPanel.activeSelf != isGameOver)
        {
            gameOverPanel.SetActive(isGameOver);
        }



        bool showCastCharge =
            keyboardMouseFishingInputSource != null &&
            keyboardMouseFishingInputSource.isActiveAndEnabled &&
            keyboardMouseFishingInputSource.IsChargingCast;

        if (!castChargeText.gameObject.activeSelf)
        {
            castChargeText.gameObject.SetActive(true);
            
        }

        if (castChargeSlider.gameObject.activeSelf != showCastCharge )
        {
            castChargeSlider.gameObject.SetActive(showCastCharge);
        }

        
        
        if (showCastCharge)
        {
            float charge01 = keyboardMouseFishingInputSource.CastCharge01;
            castChargeText.text = $"POWER {charge01 * 100f:F0}";
            castChargeSlider.value =charge01;
        }
        
        if (isGameOver)
        {
            gameOverText.text =
                $"GAME OVER\nScore: {scoreTracker.Score}";
        }
    }
}