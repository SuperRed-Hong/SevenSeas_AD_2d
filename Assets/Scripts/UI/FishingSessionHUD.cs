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

    [Header("Multiplier Feedback")]
    [SerializeField] private Color normalMultiplierColor = new Color(0.2f, 0.65f, 1f);
    [SerializeField] private Color doubleMultiplierColor = new Color(1f, 0.76f, 0.16f);
    [SerializeField] private Color maximumMultiplierColor = new Color(1f, 0.16f, 0.28f);

    private Vector3 multiplierBaseScale;
    private Color multiplierBaseColor;

    private void Awake()
    {
        if (multiplierText != null)
        {
            multiplierBaseScale = multiplierText.transform.localScale;
            multiplierBaseColor = multiplierText.color;
        }
    }

    private void OnEnable()
    {
        if (loopController != null)
        {
            loopController.DistanceMultiplierLocked += HandleDistanceMultiplierLocked;
        }
    }

    private void OnDisable()
    {
        if (loopController != null)
        {
            loopController.DistanceMultiplierLocked -= HandleDistanceMultiplierLocked;
        }
        ResetMultiplierFeedback();
    }

    private void HandleDistanceMultiplierLocked()
    {
        if (multiplierText == null)
        {
            return;
        }

        // Read the same multiplier used by gameplay during flight and after landing.
        multiplierText.text = $"x{loopController.CurrentDistanceMultiplier:F2}";
        float multiplier = loopController.CurrentDistanceMultiplier;
        multiplierText.color = multiplier <= 2f
            ? Color.Lerp(normalMultiplierColor, doubleMultiplierColor, Mathf.InverseLerp(1.5f, 2f, multiplier))
            : Color.Lerp(doubleMultiplierColor, maximumMultiplierColor, Mathf.InverseLerp(2.5f, 3f, multiplier));
        multiplierText.transform.localScale = multiplierBaseScale * multiplier;
    }

    private void LateUpdate()
    {
        // Read after gameplay Update so the value and scale match this frame's flight.
        FishingLoopState state = loopController.CurrentState;
        if (state == FishingLoopState.Casting ||
            state == FishingLoopState.Baiting ||
            state == FishingLoopState.Striking ||
            state == FishingLoopState.Reeling)
        {
            // Also restore the locked presentation if the HUD is re-enabled mid-attempt.
            HandleDistanceMultiplierLocked();
        }
        else
        {
            ResetMultiplierFeedback();
        }
    }

    private void ResetMultiplierFeedback()
    {
        if (multiplierText != null)
        {
            multiplierText.text = $"x{loopController.CurrentDistanceMultiplier:F2}";
            multiplierText.transform.localScale = multiplierBaseScale;
            multiplierText.color = multiplierBaseColor;
        }
    }

    private void Update()
    {
        int secondsRemaining =
            Mathf.CeilToInt(sessionTimer.TimeRemaining);

        statusText.text =
            $"Score: {scoreTracker.Score}\n" +
            $"Hooks: {hookTracker.HooksRemaining}\n" +
            $"Time: {secondsRemaining}";

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
