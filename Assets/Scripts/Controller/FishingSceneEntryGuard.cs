using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public sealed class FishingSceneEntryGuard : MonoBehaviour
{
    [SerializeField] private GameObject gameplayRoot;

    [SerializeField]
    private AttitudeCalibrationPanel calibrationPanel;

    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject gameplayCanvas;
    [SerializeField] private AttitudeCalibrationPanel menuCalibrationPanel;
    [SerializeField] private FishingCameraController cameraController;
    [SerializeField] private Button exitButton;

    private AttitudeCalibrationService calibrationService;
    private bool isWaitingForCalibration;
    private bool isStarting;

    private void Awake()
    {
        if (gameplayRoot == null)
        {
            Debug.LogError(
                "Fishing scene entry requires a GameplayRoot.",
                this);

            enabled = false;
            return;
        }

        // Prevent gameplay Start methods from running
        // before all entry requirements are satisfied.
        gameplayRoot.SetActive(false);
        if (gameplayCanvas != null) gameplayCanvas.SetActive(false);
    }

    private void Start()
    {
        if (exitButton != null && Application.platform == RuntimePlatform.WebGLPlayer)
            exitButton.interactable = false;
        bool requestedGameplay = SceneLoader.ConsumeGameplayRequest();
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
            menuCalibrationPanel?.ClosePanel();
            cameraController?.ShowMenu();
            if (!requestedGameplay) return;
        }
        StartGame();
    }

    public void StartGame()
    {
        if (isStarting) return;
        isStarting = true;
        // Desktop and Editor use non-motion controls,
        // so attitude calibration is not required.
        if (!Application.isMobilePlatform)
        {
            BeginGameplay();
            return;
        }
        
        if (AppRoot.Instance == null)
        {
            Debug.LogError(
                "The fishing scene must be entered through Bootstrap.",
                this);

            isStarting = false;
            return;
        }

        calibrationService =
            AppRoot.Instance.AttitudeCalibration;

        if (calibrationService == null)
        {
            Debug.LogError(
                "Attitude calibration service is unavailable.",
                this);

            isStarting = false;
            return;
        }

        if (calibrationService.IsCalibrated)
        {
            BeginGameplay();
            return;
        }

        AttitudeCalibrationPanel entryPanel = menuCalibrationPanel != null
            ? menuCalibrationPanel : calibrationPanel;
        if (entryPanel == null)
        {
            Debug.LogError(
                "Fishing scene entry requires a calibration panel.",
                this);

            isStarting = false;
            return;
        }

        isWaitingForCalibration = true;
        if (menuCalibrationPanel == null && gameplayCanvas != null) gameplayCanvas.SetActive(true);
        entryPanel.OpenPanel();
    }

    public void OpenSettings()
    {
        if (!isStarting) menuCalibrationPanel?.OpenPanel();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_WEBGL
        Application.Quit();
#endif
    }

    private void Update()
    {
        AttitudeCalibrationPanel entryPanel = menuCalibrationPanel != null
            ? menuCalibrationPanel : calibrationPanel;
        if (isWaitingForCalibration && entryPanel != null &&
            !entryPanel.gameObject.activeInHierarchy && !calibrationService.IsCalibrated)
        {
            isWaitingForCalibration = false;
            isStarting = false;
            return;
        }

        if (!isWaitingForCalibration ||
            calibrationService == null ||
            !calibrationService.IsCalibrated)
        {
            return;
        }

        BeginGameplay();
    }

    private void BeginGameplay()
    {
        isWaitingForCalibration = false;
        StartCoroutine(BeginAfterTransition());
    }

    private IEnumerator BeginAfterTransition()
    {

        if (calibrationPanel != null)
        {
            calibrationPanel.ClosePanel();
        }
        menuCalibrationPanel?.ClosePanel();
        if (menuCanvas != null) menuCanvas.SetActive(false);
        if (gameplayCanvas != null) gameplayCanvas.SetActive(false);

        if (cameraController != null)
            yield return cameraController.TransitionToOverview();

        gameplayRoot.SetActive(true);
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);

        // Entry validation is complete; no further polling is needed.
        enabled = false;
    }
}
