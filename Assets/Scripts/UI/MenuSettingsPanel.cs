using UnityEngine;

public sealed class MenuSettingsPanel : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TutorialPanelController tutorial;
    [SerializeField] private AttitudeCalibrationPanel calibration;
    [SerializeField] private FishingSceneEntryGuard entryGuard;
    [SerializeField] private StrikeTuningPanel strikeTuning;

    public void Open()
    {
        if (entryGuard != null && entryGuard.IsStartingGame) return;
        settingsPanel.transform.SetAsLastSibling();
        settingsPanel.SetActive(true);
    }

    public void Close() => settingsPanel.SetActive(false);

    public void OpenTutorial()
    {
        tutorial.Open();
    }

    public void OpenStrikeTuning()
    {
        strikeTuning?.Open();
    }

    public void OpenCalibration()
    {
        if (calibration == null) return;
        calibration.transform.SetAsLastSibling();
        calibration.OpenPanel();
    }
}
