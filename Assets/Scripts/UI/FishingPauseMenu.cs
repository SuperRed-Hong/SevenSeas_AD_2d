using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FishingPauseMenu : MonoBehaviour
{
    [SerializeField] private FishingPauseController pauseController;
    private GameObject panel;
    private bool isOpen;

    private void Start()
    {
        panel = new GameObject("PauseCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        panel.transform.SetParent(transform, false);
        Canvas canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;
        CanvasScaler scaler = panel.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440f, 2304f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform shade = MakeRect("Shade", panel.transform, Vector2.zero, Vector2.one);
        shade.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.035f, 0.06f, 0.85f);
        TMP_Text title = MakeText("PAUSED", shade, new Vector2(0.1f, 0.64f), new Vector2(0.9f, 0.73f), 64f);
        title.alignment = TextAlignmentOptions.Center;
        MakeButton("RESUME", shade, new Vector2(0.22f, 0.49f), new Vector2(0.78f, 0.57f), Resume);
        MakeButton("MAIN MENU", shade, new Vector2(0.22f, 0.35f), new Vector2(0.78f, 0.43f), ReturnToMenu);
        TMP_Text hint = MakeText("Returning to the menu ends this run.", shade,
            new Vector2(0.1f, 0.27f), new Vector2(0.9f, 0.33f), 28f);
        hint.alignment = TextAlignmentOptions.Center;
        panel.SetActive(false);
    }

    public void Open()
    {
        if (isOpen || panel == null || pauseController == null || !pauseController.TryPause(this)) return;
        isOpen = true;
        panel.SetActive(true);
    }

    public void Resume()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false);
        pauseController.Resume(this);
    }

    private void ReturnToMenu()
    {
        // Keep gameplay components suspended while the scene is loading.
        if (AppRoot.Instance != null) AppRoot.Instance.SceneLoader.LoadScene(E_SceneID.MainMenu);
        else SceneLoader.LoadWithoutAppRoot(E_SceneID.MainMenu);
    }

    private void OnDisable()
    {
        Resume();
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 minimum, Vector2 maximum)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = minimum;
        rect.anchorMax = maximum;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text MakeText(string text, Transform parent, Vector2 minimum, Vector2 maximum, float size)
    {
        TextMeshProUGUI label = MakeRect(text, parent, minimum, maximum).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = new Color(0.92f, 0.96f, 1f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    private static Button MakeButton(string text, Transform parent, Vector2 minimum, Vector2 maximum,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = MakeRect(text, parent, minimum, maximum);
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.1f, 0.32f, 0.48f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);
        TMP_Text label = MakeText(text, rect, Vector2.zero, Vector2.one, 30f);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }
}
