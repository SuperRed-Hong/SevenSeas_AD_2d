using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public sealed class MenuSettingsPanel : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TutorialPanelController tutorial;
    [SerializeField] private AttitudeCalibrationPanel calibration;
    [SerializeField] private FishingSceneEntryGuard entryGuard;
    [SerializeField] private StrikeTuningPanel strikeTuning;
    [SerializeField] private DeveloperPanel developerPanel;

    [Header("Settings Colors")]
    [SerializeField] private Color backgroundColor = new(0.035f, 0.075f, 0.14f, 1f);
    [SerializeField] private Color headingColor = new(0.92f, 0.97f, 1f, 1f);
    [SerializeField] private Color buttonColor = new(0.42f, 0.87f, 1f, 1f);
    [SerializeField] private Color buttonTextColor = new(0.035f, 0.075f, 0.14f, 1f);

    private void OnEnable()
    {
#if UNITY_EDITOR
        QueueAppearance();
#else
        ApplyAppearance();
#endif
    }

#if UNITY_EDITOR
    private void OnValidate() => QueueAppearance();
    private void OnDisable() => UnityEditor.EditorApplication.delayCall -= ApplyAppearance;

    private void QueueAppearance()
    {
        UnityEditor.EditorApplication.delayCall -= ApplyAppearance;
        UnityEditor.EditorApplication.delayCall += ApplyAppearance;
    }
#endif

    private void ApplyAppearance()
    {
        if (this == null || settingsPanel == null) return;
        SetColor(settingsPanel.GetComponent<Image>(), backgroundColor);
        foreach (Button button in settingsPanel.GetComponentsInChildren<Button>(true))
            SetColor(button.targetGraphic, buttonColor);
        foreach (TMP_Text label in settingsPanel.GetComponentsInChildren<TMP_Text>(true))
            SetColor(label, label.GetComponentInParent<Button>(true) != null ? buttonTextColor : headingColor);
    }

    private static void SetColor(Graphic graphic, Color color)
    {
        if (graphic == null || graphic.color == color) return;
        graphic.color = color;
#if UNITY_EDITOR
        if (!Application.IsPlaying(graphic.gameObject))
        {
            UnityEditor.EditorUtility.SetDirty(graphic);
            if (graphic.gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(graphic.gameObject.scene);
        }
#endif
    }

    public void OpenDeveloper() => developerPanel?.Open();

    public void Open()
    {
        if (entryGuard != null && entryGuard.IsStartingGame) return;
        ApplyAppearance();
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
