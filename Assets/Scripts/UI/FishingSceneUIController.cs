using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public sealed class FishingSceneUIController : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject gameplayCanvas;
    [SerializeField] private MenuCanvasFader menuFader;
    [SerializeField] private GameObject menuTitle;
    [SerializeField] private GameObject menuButtonList;
    [SerializeField] private GameObject[] menuOverlays;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private AttitudeCalibrationPanel menuCalibrationPanel;
    [SerializeField] private AttitudeCalibrationPanel gameplayCalibrationPanel;
    [SerializeField] private DeveloperPanel developerPanel;

    private bool initialized;
    public bool HasMenu => menuCanvas != null;

    private void Awake() => Initialize();

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        // Runtime state deliberately overrides Inspector preview visibility.
        SetVisible(gameplayCanvas, false);
        CloseOverlays();
        SetVisible(gameOverPanel, false);
        SetVisible(menuCanvas, true);
        SetVisible(menuTitle, true);
        SetVisible(menuButtonList, true);
    }

    public IEnumerator FadeMenuForGameplay()
    {
        SetVisible(gameplayCanvas, false);
        CloseOverlays();
        if (menuFader != null) yield return menuFader.FadeOut();
        SetVisible(menuCanvas, false);
    }

    public void ShowGameplay()
    {
        CloseOverlays();
        SetVisible(menuCanvas, false);
        SetVisible(gameOverPanel, false);
        SetVisible(gameplayCanvas, true);
    }

    public void ShowGameplayCalibration()
    {
        // Support older prototype scenes whose calibration lives in the HUD.
        SetVisible(gameplayCanvas, true);
    }

    public void OpenDeveloper()
    {
        if (developerPanel != null) developerPanel.Open();
        else Debug.LogError("Developer menu entry requires a DeveloperPanel reference.", this);
    }

    private void CloseOverlays()
    {
        if (menuOverlays != null)
            foreach (GameObject overlay in menuOverlays) SetVisible(overlay, false);
        menuCalibrationPanel?.ClosePanel();
        gameplayCalibrationPanel?.ClosePanel();
    }

    private static void SetVisible(GameObject target, bool visible)
    {
        if (target == null) return;
        target.SetActive(visible);
        if (!visible) return;
        if (target.TryGetComponent(out Canvas canvas)) canvas.enabled = true;
        if (target.TryGetComponent(out CanvasGroup group))
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }
}
