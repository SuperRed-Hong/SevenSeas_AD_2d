using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AttitudeCalibrationPanel : MonoBehaviour
{
    [SerializeField] private Button calibrateButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text statusText;

    private AttitudeCalibrationService calibrationService;

    private void OnEnable()
    {
        calibrationService = AppRoot.Instance != null
            ? AppRoot.Instance.AttitudeCalibration
            : null;

        RefreshView();
    }

    private void OnDisable()
    {
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

        calibrationService.BeginCalibration();
        RefreshView();
    }

    private void RefreshView()
    {
        if (calibrationService == null)
        {
            calibrateButton.interactable = false;
            progressSlider.SetValueWithoutNotify(0f);
            statusText.text = "Calibration service unavailable.";
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
            statusText.text = "Motion sensor unavailable.";
            return;
        }

        switch (calibrationService.State)
        {
            case AttitudeCalibrationState.Uncalibrated:
                statusText.text =
                    "Set a neutral pose, then press Calibrate.";
                break;

            case AttitudeCalibrationState.Calibrating:
                statusText.text =
                    $"Hold still... {calibrationService.Progress01:P0}";
                break;

            case AttitudeCalibrationState.Calibrated:
                statusText.text =
                    "Calibration complete. You can recalibrate at any time.";
                break;
        }
    }
}