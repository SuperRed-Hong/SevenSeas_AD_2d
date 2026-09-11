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
    [SerializeField] private FishingIntroController intro;

    private AttitudeCalibrationService calibrationService;
    private bool isWaitingForCalibration;
    private bool isStarting;
    private bool transitionStarted;
    private bool isResolvingWebMotion;
    private bool webMotionResolved;
    public bool IsStartingGame => isStarting && enabled;

    private void Awake()
    {
        if (intro == null) intro = GetComponent<FishingIntroController>();
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
        bool requestedDeveloper = SceneLoader.ConsumeDeveloperRequest();
        if (sceneUI.HasMenu)
        {
            cameraController?.ShowMenu();
            if (requestedDeveloper && !requestedGameplay) sceneUI.OpenDeveloper();
            if (!requestedGameplay) return;
        }
        StartGame();
    }

    public void StartGame()
    {
        if (!isActiveAndEnabled || isStarting) return;
        WebMotionPermission.RequestIfNeeded();
        isStarting = true;

        if (RuntimeInputPlatform.IsWebMobilePlayer)
        {
            if (webMotionResolved)
            {
                ContinueWithTutorial();
                return;
            }

            if (AppRoot.Instance == null ||
                AppRoot.Instance.AttitudeCalibration == null)
            {
                Debug.LogError(
                    "The fishing scene requires motion services from Bootstrap.",
                    this);
                isStarting = false;
                return;
            }

            calibrationService = AppRoot.Instance.AttitudeCalibration;
            StartCoroutine(ResolveWebMotionInput());
            return;
        }

        ContinueWithTutorial();
    }

    private void ContinueWithTutorial()
    {
        if (RuntimeInputPlatform.UsesMobileControls &&
            !WebMotionPermission.TouchFallbackActive &&
            !LocalPlayerProgress.TutorialCompleted)
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
        if (!RuntimeInputPlatform.UsesMobileControls)
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

        if (WebMotionPermission.TouchFallbackActive)
        {
            BeginGameplay();
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

    private IEnumerator ResolveWebMotionInput()
    {
        if (isResolvingWebMotion || webMotionResolved)
        {
            yield break;
        }

        isResolvingWebMotion = true;
        float deadline = Time.unscaledTime + 3f;

        while (WebMotionPermission.State == WebMotionPermissionState.Requesting &&
               Time.unscaledTime < deadline)
        {
            yield return null;
        }

        if (WebMotionPermission.State == WebMotionPermissionState.Granted)
        {
            deadline = Time.unscaledTime + 3f;
            while (!calibrationService.IsSensorReady &&
                   Time.unscaledTime < deadline)
            {
                yield return null;
            }
        }

        isResolvingWebMotion = false;

        if (WebMotionPermission.State != WebMotionPermissionState.Granted ||
            !calibrationService.IsSensorReady)
        {
            WebMotionPermission.ActivateTouchFallback();
        }

        webMotionResolved = true;
        ContinueWithTutorial();
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
        if (transitionStarted) return;
        if (intro != null && (!intro.isActiveAndEnabled || !intro.IsPrepared))
        {
            Debug.LogError("Fishing intro is present but not ready. Check its scene references before starting.", this);
            isWaitingForCalibration = false;
            isStarting = false;
            return;
        }
        transitionStarted = true;
        isWaitingForCalibration = false;
        StartCoroutine(BeginAfterTransition());
    }

    private IEnumerator BeginAfterTransition()
    {

        Coroutine cameraTransition = intro != null && intro.IsPrepared
            ? intro.StartCoroutine(intro.Play())
            : cameraController != null ? StartCoroutine(cameraController.TransitionToOverview()) : null;
        yield return sceneUI.FadeMenuForGameplay();
        if (cameraTransition != null) yield return cameraTransition;

        if (intro != null && intro.IsPrepared && !intro.HasCompleted) yield break;

        loopController.enabled = true;
        LocalPlayerProgress.RecordGameStarted();
        sceneUI.ShowGameplay();

        // Entry validation is complete; no further polling is needed.
        enabled = false;
    }
}
