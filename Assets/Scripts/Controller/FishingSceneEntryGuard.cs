using UnityEngine;

public sealed class FishingSceneEntryGuard : MonoBehaviour
{
    [SerializeField] private GameObject gameplayRoot;

    [SerializeField]
    private AttitudeCalibrationPanel calibrationPanel;

    private AttitudeCalibrationService calibrationService;
    private bool isWaitingForCalibration;

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
    }

    private void Start()
    {
        if (AppRoot.Instance == null)
        {
            Debug.LogError(
                "The fishing scene must be entered through Bootstrap.",
                this);

            return;
        }

        calibrationService =
            AppRoot.Instance.AttitudeCalibration;

        if (calibrationService == null)
        {
            Debug.LogError(
                "Attitude calibration service is unavailable.",
                this);

            return;
        }

        if (calibrationService.IsCalibrated)
        {
            BeginGameplay();
            return;
        }

        if (calibrationPanel == null)
        {
            Debug.LogError(
                "Fishing scene entry requires a calibration panel.",
                this);

            return;
        }

        isWaitingForCalibration = true;
        calibrationPanel.OpenPanel();
    }

    private void Update()
    {
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

        if (calibrationPanel != null)
        {
            calibrationPanel.ClosePanel();
        }

        gameplayRoot.SetActive(true);

        // Entry validation is complete; no further polling is needed.
        enabled = false;
    }
}