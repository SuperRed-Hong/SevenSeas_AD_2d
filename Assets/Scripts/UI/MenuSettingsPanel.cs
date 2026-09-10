using UnityEngine;

public sealed class MenuSettingsPanel : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TutorialPanelController tutorial;
    [SerializeField] private AttitudeCalibrationPanel calibration;
    [SerializeField] private FishingSceneEntryGuard entryGuard;
    [SerializeField] private StrikeTuningPanel strikeTuning;
    [SerializeField] private DeveloperPanel developerPanel;

    public void OpenDeveloper() => developerPanel?.Open();

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

    public void OpenGyroTest() => OpenTest(E_SceneID.GyroscopeTest);
    public void OpenAttitudeTest() => OpenTest(E_SceneID.AttitudeControlTest);

    private void OpenTest(E_SceneID scene)
    {
        if (entryGuard != null && entryGuard.IsStartingGame) return;
        if (AppRoot.Instance != null) AppRoot.Instance.SceneLoader.LoadScene(scene);
        else SceneLoader.LoadWithoutAppRoot(scene);
    }

    public void OpenCalibration()
    {
        if (calibration == null) return;
        calibration.transform.SetAsLastSibling();
        calibration.OpenPanel();
    }
}
