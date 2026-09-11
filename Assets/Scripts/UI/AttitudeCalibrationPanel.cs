using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AttitudeCalibrationPanel : MonoBehaviour
{
    [SerializeField] private Button calibrateButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text statusText;

    private AttitudeCalibrationService calibrationService;
    private bool startedCalibration;

    private void OnEnable()
    {
        calibrationService = AppRoot.Instance != null
            ? AppRoot.Instance.AttitudeCalibration
            : null;

        startedCalibration = false;
        if (calibrationService != null) calibrationService.Completed += HandleCompleted;
        RefreshView();
    }

    private void OnDisable()
    {
        if (calibrationService != null) calibrationService.Completed -= HandleCompleted;
        startedCalibration = false;
        if (calibrationService != null &&
            calibrationService.State ==
            AttitudeCalibrationState.Calibrating)
        {
            calibrationService.CancelCalibration();
        }
    }

    private void Update()
    {
        RefreshView();
    }
    public void OpenPanel()
    {
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    public void BeginCalibration()
    {
        if (calibrationService == null)
        {
            return;
        }

        startedCalibration = calibrationService.BeginCalibration();
        RefreshView();
    }

    private void HandleCompleted()
    {
        // An existing calibration must not close a newly opened panel.
        if (startedCalibration) ClosePanel();
    }

    private void RefreshView()
    {
        if (calibrationService == null)
        {
            calibrateButton.interactable = false;
            progressSlider.SetValueWithoutNotify(0f);
            statusText.text = "MOTION CONTROLS UNAVAILABLE\nReturn to the menu to continue.";
            return;
        }

        calibrateButton.interactable =
            calibrationService.IsSensorReady &&
            calibrationService.State !=
            AttitudeCalibrationState.Calibrating;

        progressSlider.SetValueWithoutNotify(
            calibrationService.Progress01);

        if (!calibrationService.IsSensorReady)
        {
            if (WebMotionPermission.MotionState == WebMotionPermissionState.Granted)
            {
                statusText.text = "CONNECTING MOTION SENSORS\nWaiting for your device's first sample.";
                return;
            }
            statusText.text = WebMotionPermission.State switch
            {
                WebMotionPermissionState.Requesting =>
                    "ALLOW MOTION ACCESS\nWaiting for browser permission.",
                WebMotionPermissionState.Denied =>
                    "MOTION ACCESS BLOCKED\nEnable motion access in browser settings.",
                WebMotionPermissionState.Unsupported =>
                    "MOTION CONTROLS UNAVAILABLE\nThis browser did not expose motion sensors.",
                _ => "WAITING FOR YOUR DEVICE\nMotion sensor not detected."
            };
            return;
        }

        switch (calibrationService.State)
        {
            case AttitudeCalibrationState.Uncalibrated:
                statusText.text =
                    "READY WHEN YOU ARE\nPress SET to save your neutral pose.";
                break;

            case AttitudeCalibrationState.Calibrating:
                statusText.text =
                    $"HOLD STEADY\n{calibrationService.Progress01:P0}";
                break;

            case AttitudeCalibrationState.Calibrated:
                statusText.text =
                    "YOUR POSE IS SAVED\nPress SET to adjust it again.";
                break;
        }
    }
}
