using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public sealed class FishingSceneEntryGuard : MonoBehaviour
{
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private FishingLoopController loopController;

    [SerializeField]
    private AttitudeCalibrationPanel calibrationPanel;

    [SerializeField] private FishingSceneUIController sceneUI;
    [SerializeField] private AttitudeCalibrationPanel menuCalibrationPanel;
    [SerializeField] private FishingCameraController cameraController;
    [SerializeField] private Button exitButton;
    [SerializeField] private TutorialPanelController tutorial;

    private AttitudeCalibrationService calibrationService;
    private bool isWaitingForCalibration;
    private bool isStarting;
    public bool IsStartingGame => isStarting && enabled;

    private void Awake()
    {
        if (sceneUI == null)
        {
            Debug.LogError("Fishing scene entry requires a FishingSceneUIController.", this);
            enabled = false;
            return;
        }
        sceneUI.Initialize();

        if (gameplayRoot == null)
        {
            Debug.LogError(
                "Fishing scene entry requires a GameplayRoot.",
                this);

            enabled = false;
            return;
        }

        if (loopController == null)
            loopController = gameplayRoot.GetComponentInChildren<FishingLoopController>(true);
        if (loopController == null)
        {
            Debug.LogError("Fishing scene entry requires a FishingLoopController.", this);
            enabled = false;
            return;
        }

        // Spawn fish and show actors immediately; only gameplay control waits.
        loopController.PrepareForEntry();
        gameplayRoot.SetActive(true);
    }

    private void Start()
    {
        if (exitButton != null && Application.platform == RuntimePlatform.WebGLPlayer)
            exitButton.interactable = false;
        bool requestedGameplay = SceneLoader.ConsumeGameplayRequest();
        if (sceneUI.HasMenu)
        {
            cameraController?.ShowMenu();
            if (!requestedGameplay) return;
        }
        StartGame();
    }

    public void StartGame()
    {
        if (isStarting) return;
        isStarting = true;
        if (!LocalPlayerProgress.TutorialCompleted)
        {
            if (tutorial == null || !tutorial.OpenForFirstGame(ContinueStart, CancelStart))
            {
                Debug.LogError("First-game tutorial references are incomplete.", this);
                isStarting = false;
            }
            return;
        }
        ContinueStart();
    }

    private void CancelStart() => isStarting = false;

    private void ContinueStart()
    {
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
        if (menuCalibrationPanel == null) sceneUI.ShowGameplayCalibration();
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

        Coroutine cameraTransition = cameraController != null
            ? StartCoroutine(cameraController.TransitionToOverview()) : null;
        yield return sceneUI.FadeMenuForGameplay();
        if (cameraTransition != null) yield return cameraTransition;

        loopController.enabled = true;
        LocalPlayerProgress.RecordGameStarted();
        sceneUI.ShowGameplay();

        // Entry validation is complete; no further polling is needed.
        enabled = false;
    }
}
