using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FishingPauseMenu : MonoBehaviour
{
    [SerializeField] private FishingPauseController pauseController;
    [SerializeField] private CatchInventoryView inventoryView;
    [SerializeField] private FishingHaptics haptics;
    [SerializeField] private FishingAudioFeedback audioFeedback;
    [Header("Menu Artwork")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite closeSprite;
    [SerializeField] private TMP_FontAsset font;

    private static readonly Color Ink = new(0.23f, 0.16f, 0.10f);
    private static readonly Color Cream = new(0.94f, 0.86f, 0.66f);
    private GameObject panel;
    private RectTransform safeArea;
    private Rect lastSafeArea;
    private TMP_Text vibrationLabel;
    private TMP_Text soundLabel;
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
        shade.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.025f, 0.015f, 0.78f);
        safeArea = MakeRect("SafeArea", shade, Vector2.zero, Vector2.one);
        UpdateSafeArea();
        RectTransform bounds = MakeRect("BoardBounds", safeArea, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
        RectTransform board = MakeRect("PauseBoard", bounds, Vector2.zero, Vector2.one);
        Image paper = board.gameObject.AddComponent<Image>();
        paper.sprite = panelSprite;
        paper.color = panelSprite != null ? Color.white : Cream;
        paper.raycastTarget = false;
        AspectRatioFitter fitter = board.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 0.65f;

        MakeText("PAUSED", board, new Vector2(0.12f, 0.84f), new Vector2(0.88f, 0.92f), 68f);
        MakeText("SEVEN SEAS", board, new Vector2(0.18f, 0.795f), new Vector2(0.82f, 0.835f), 25f);
        MakeButton("RESUME", board, new Vector2(0.16f, 0.66f), new Vector2(0.84f, 0.755f), Resume, true);
        if (inventoryView != null)
            MakeButton("CATCHES", board, new Vector2(0.16f, 0.535f), new Vector2(0.84f, 0.63f), OpenInventory);
        RectTransform divider = MakeRect("Divider", board, new Vector2(0.18f, 0.493f), new Vector2(0.82f, 0.496f));
        Image rule = divider.gameObject.AddComponent<Image>();
        rule.color = new Color(Ink.r, Ink.g, Ink.b, 0.45f);
        rule.raycastTarget = false;
        if (haptics != null)
        {
            Button vibrationButton = MakeButton("VIBRATION: ON", board,
                new Vector2(0.16f, 0.385f), new Vector2(0.84f, 0.46f), () =>
                {
                    haptics.VibrationEnabled = !haptics.VibrationEnabled;
                    RefreshSettings();
                });
            vibrationLabel = vibrationButton.GetComponentInChildren<TMP_Text>();
        }
        if (audioFeedback != null)
        {
            Button soundButton = MakeButton("SOUND: ON", board,
                new Vector2(0.16f, 0.285f), new Vector2(0.84f, 0.36f), () =>
                {
                    audioFeedback.SoundEnabled = !audioFeedback.SoundEnabled;
                    RefreshSettings();
                });
            soundLabel = soundButton.GetComponentInChildren<TMP_Text>();
        }
        MakeButton("MAIN MENU", board, new Vector2(0.16f, 0.145f), new Vector2(0.84f, 0.235f),
            ReturnToMenu, false, UiButtonSound.Back);
        MakeText("RETURNING TO MENU\nENDS THIS RUN", board,
            new Vector2(0.14f, 0.065f), new Vector2(0.86f, 0.12f), 23f);
        if (closeSprite != null)
        {
            RectTransform rect = MakeRect("Close", board, new Vector2(0.80f, 0.88f), new Vector2(0.96f, 0.99f));
            rect.gameObject.AddComponent<Image>().color = Color.clear;
            // Keep the pixel icon small while giving touch input a larger hit target.
            Image image = MakeRect("Icon", rect, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f))
                .gameObject.AddComponent<Image>();
            image.sprite = closeSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Button close = rect.gameObject.AddComponent<Button>();
            close.targetGraphic = image;
            close.onClick.AddListener(Resume);
            UiAudioRouter.RegisterButton(close, UiButtonSound.Back);
        }
        RefreshSettings();
        panel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (isOpen) UpdateSafeArea();
    }

    private void UpdateSafeArea()
    {
        Rect area = Screen.safeArea;
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0 || area == lastSafeArea) return;
        lastSafeArea = area;
        safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
    }

    private void RefreshSettings()
    {
        if (vibrationLabel != null) vibrationLabel.text = haptics.VibrationEnabled ? "VIBRATION: ON" : "VIBRATION: OFF";
        if (soundLabel != null) soundLabel.text = audioFeedback.SoundEnabled ? "SOUND: ON" : "SOUND: OFF";
    }

    private void OpenInventory()
    {
        if (!isOpen || inventoryView == null) return;
        panel.SetActive(false);
        inventoryView.Open(() =>
        {
            if (isOpen && panel != null) panel.SetActive(true);
        });
    }

    public void Open()
    {
        if (isOpen || panel == null || pauseController == null || !pauseController.TryPause(this)) return;
        isOpen = true;
        UpdateSafeArea();
        RefreshSettings();
        panel.SetActive(true);
    }

    public void Resume()
    {
        if (!isOpen) return;
        isOpen = false;
        inventoryView?.Close();
        panel.SetActive(false);
        pauseController.Resume(this);
    }

    private void ReturnToMenu()
    {
        // Keep gameplay suspended until the new scene has loaded.
        if (AppRoot.Instance != null) AppRoot.Instance.SceneLoader.LoadScene(E_SceneID.MainMenu);
        else SceneLoader.LoadWithoutAppRoot(E_SceneID.MainMenu);
    }

    private void OnDisable() => Resume();

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

    private TMP_Text MakeText(string text, Transform parent, Vector2 minimum, Vector2 maximum, float size)
    {
        TextMeshProUGUI label = MakeRect(text, parent, minimum, maximum).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = size;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private Button MakeButton(string text, Transform parent, Vector2 minimum, Vector2 maximum,
        UnityEngine.Events.UnityAction action, bool primary = false, UiButtonSound sound = UiButtonSound.Confirm)
    {
        RectTransform rect = MakeRect(text, parent, minimum, maximum);
        Image border = rect.gameObject.AddComponent<Image>();
        border.color = Ink;
        Image fill = MakeRect("Fill", rect, new Vector2(0.012f, 0.065f), new Vector2(0.988f, 0.935f))
            .gameObject.AddComponent<Image>();
        fill.color = Color.white;
        fill.raycastTarget = false;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;
        ColorBlock colors = button.colors;
        colors.normalColor = primary ? new Color(0.34f, 0.23f, 0.14f) : new Color(0.76f, 0.65f, 0.44f);
        colors.highlightedColor = primary ? new Color(0.48f, 0.34f, 0.20f) : new Color(0.9f, 0.79f, 0.56f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.58f, 0.43f, 0.25f);
        colors.disabledColor = new Color(0.5f, 0.46f, 0.38f, 0.65f);
        colors.fadeDuration = 0f;
        button.colors = colors;
        button.onClick.AddListener(action);
        UiAudioRouter.RegisterButton(button, sound);
        TMP_Text label = MakeText(text, rect, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), 38f);
        label.color = primary ? Cream : Ink;
        return button;
    }
}
