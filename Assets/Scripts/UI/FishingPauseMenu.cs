using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FishingPauseMenu : MonoBehaviour
{
    [SerializeField] private FishingPauseController pauseController;
    [SerializeField] private CatchInventoryView inventoryView;
    [SerializeField] private FishingHaptics haptics;
    [SerializeField] private FishingAudioFeedback audioFeedback;
    [SerializeField, Tooltip("暂停时可随时重新校准体感中性点。留空则不显示该按钮。")]
    private AttitudeCalibrationPanel calibrationPanel;
    [SerializeField, Tooltip("暂停时可重看操作教程。留空则不显示该按钮。")]
    private TutorialPanelController tutorial;
    [Header("Menu Artwork")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite closeSprite;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private TMP_FontAsset font;

    private static readonly Color Ink = new(0.23f, 0.16f, 0.10f);
    private static readonly Color Cream = new(0.94f, 0.86f, 0.66f);
    private const float ActionHeight = 0.082f;
    private const float SettingHeight = 0.072f;
    private GameObject panel;
    private RectTransform safeArea;
    private Rect lastSafeArea;
    private TMP_Text vibrationLabel;
    private TMP_Text soundLabel;
    private Button calibrateButton;
    private GameObject hint;
    private bool isOpen;
    private bool waitingForSubPanel;
    private float cursor;

    private readonly System.Collections.Generic.List<(RectTransform rect, float height, float gap, float inset)>
        rows = new();
    private float lastHeight, lastGap, lastInset;

    // Normalised rows laid out downwards from the last one placed.
    private Rect StackRow(float height, float gap = 0.016f, float inset = 0.16f)
    {
        lastHeight = height;
        lastGap = gap;
        lastInset = inset;
        float bottom = cursor - height;
        cursor = bottom - gap;
        return new Rect(inset, bottom, 1f - 2f * inset, height);
    }

    // Remember the row so a hidden button can be closed up instead of leaving a hole.
    private T Track<T>(T made) where T : Component
    {
        rows.Add((made.GetComponent<RectTransform>(), lastHeight, lastGap, lastInset));
        return made;
    }

    private void LayoutRows()
    {
        float y = 0.94f;
        foreach (var row in rows)
        {
            if (row.rect == null || !row.rect.gameObject.activeSelf) continue;
            float bottom = y - row.height;
            row.rect.anchorMin = new Vector2(row.inset, bottom);
            row.rect.anchorMax = new Vector2(1f - row.inset, y);
            row.rect.offsetMin = Vector2.zero;
            row.rect.offsetMax = Vector2.zero;
            y = bottom - row.gap;
        }
    }

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
        shade.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.075f, 0.068f, 1f);
        safeArea = MakeRect("SafeArea", shade, Vector2.zero, Vector2.one);
        UpdateSafeArea();
        RectTransform bounds = MakeRect("BoardBounds", safeArea, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
        RectTransform board = MakeRect("PauseBoard", bounds, Vector2.zero, Vector2.one);
        Image paper = board.gameObject.AddComponent<Image>();
        paper.sprite = panelSprite;
        paper.color = new Color(0.055f, 0.14f, 0.12f);
        paper.raycastTarget = false;
        AspectRatioFitter fitter = board.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 0.65f;

        // Rows are stacked from a cursor because the optional buttons make the
        // count vary; fixed anchors would leave holes or overlap.
        cursor = 0.94f;
        Track(MakeText("PAUSED", board, StackRow(0.075f, 0.012f, 0.12f), 68f));
        Track(MakeText("SEVEN SEAS", board, StackRow(0.035f, 0.026f, 0.18f), 25f));
        Track(MakeButton("RESUME", board, StackRow(ActionHeight), Resume, true));
        if (inventoryView != null)
            Track(MakeButton("CATCHES", board, StackRow(ActionHeight), OpenInventory));
        if (calibrationPanel != null)
        {
            calibrateButton = Track(MakeButton("RECALIBRATE", board, StackRow(ActionHeight), OpenCalibration));
            // Availability is only known once entry has resolved motion input.
            calibrateButton.gameObject.SetActive(false);
        }
        if (tutorial != null)
            Track(MakeButton("HOW TO PLAY", board, StackRow(ActionHeight), OpenTutorial));
        RectTransform divider = Track(MakeRect("Divider", board, StackRow(0.003f, 0.02f, 0.18f)));
        Image rule = divider.gameObject.AddComponent<Image>();
        rule.color = new Color(Ink.r, Ink.g, Ink.b, 0.45f);
        rule.raycastTarget = false;
        if (haptics != null)
        {
            Button vibrationButton = Track(MakeButton("VIBRATION: ON", board, StackRow(SettingHeight), () =>
                {
                    haptics.VibrationEnabled = !haptics.VibrationEnabled;
                    RefreshSettings();
                }));
            vibrationLabel = vibrationButton.GetComponentInChildren<TMP_Text>();
        }
        if (audioFeedback != null)
        {
            Button soundButton = Track(MakeButton("SOUND: ON", board, StackRow(SettingHeight), () =>
                {
                    audioFeedback.SoundEnabled = !audioFeedback.SoundEnabled;
                    RefreshSettings();
                }));
            soundLabel = soundButton.GetComponentInChildren<TMP_Text>();
        }
        // Pinned to the bottom rather than stacked, so a short list cannot float it.
        MakeButton("MAIN MENU", board, new Rect(0.16f, 0.100f, 0.68f, 0.082f),
            ReturnToMenu, false, UiButtonSound.Back);
        MakeText("RETURNING TO MENU\nENDS THIS RUN", board,
            new Vector2(0.14f, 0.030f), new Vector2(0.86f, 0.088f), 23f);
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
        BuildHint();
    }

    // A drifting neutral pose is invisible to the player: the boat simply will
    // not hold still. Surface it where they are looking, not only in the menu.
    private void BuildHint()
    {
        if (calibrationPanel == null) return;
        hint = new GameObject("RecalibrateHintCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        hint.transform.SetParent(transform, false);
        Canvas canvas = hint.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1050;
        CanvasScaler scaler = hint.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440f, 2304f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform banner = MakeRect("Banner", hint.transform,
            new Vector2(0.08f, 0.795f), new Vector2(0.92f, 0.875f));
        Image background = banner.gameObject.AddComponent<Image>();
        background.sprite = buttonSprite;
        background.color = new Color(0.94f, 0.86f, 0.66f, 0.96f);
        Button accept = banner.gameObject.AddComponent<Button>();
        accept.targetGraphic = background;
        accept.onClick.AddListener(AcceptHint);
        UiAudioRouter.RegisterButton(accept, UiButtonSound.Confirm);
        TMP_Text label = MakeText("TILT DRIFTING?\nTAP TO RECALIBRATE", banner,
            new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), 30f);
        label.color = Ink;
        hint.SetActive(false);
    }

    private void AcceptHint()
    {
        AppRoot.Instance?.PostureWatcher?.Acknowledge();
        if (hint != null) hint.SetActive(false);
        Open();
        OpenCalibration();
    }

    private void RefreshHint()
    {
        if (hint == null) return;
        AttitudePostureWatcher watcher = AppRoot.Instance != null ? AppRoot.Instance.PostureWatcher : null;
        // Never over the pause board; the menu has its own button for this.
        bool show = !isOpen && watcher != null && watcher.NeedsRecalibration;
        if (hint.activeSelf != show) hint.SetActive(show);
    }

    private void LateUpdate()
    {
        RefreshHint();
        if (!isOpen) return;
        UpdateSafeArea();
        // Neither sub-panel reports its own close, so bring the board back once
        // whichever one we opened has gone away.
        if (!waitingForSubPanel) return;
        bool stillOpen = (calibrationPanel != null && calibrationPanel.gameObject.activeInHierarchy) ||
                         (tutorial != null && tutorial.IsOpen);
        if (stillOpen) return;
        waitingForSubPanel = false;
        if (panel != null) panel.SetActive(true);
    }

    private void OpenCalibration()
    {
        if (!isOpen || calibrationPanel == null) return;
        panel.SetActive(false);
        waitingForSubPanel = true;
        calibrationPanel.OpenPanel();
    }

    private void OpenTutorial()
    {
        if (!isOpen || tutorial == null) return;
        panel.SetActive(false);
        waitingForSubPanel = true;
        tutorial.Open();
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
        // Recalibrating means nothing on a keyboard or in the touch fallback.
        if (calibrateButton != null)
            calibrateButton.gameObject.SetActive(RuntimeInputPlatform.UsesMobileControls &&
                                                 !WebMotionPermission.TouchFallbackActive);
        // Close the gap a hidden row would otherwise leave.
        LayoutRows();
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

    private static RectTransform MakeRect(string name, Transform parent, Rect area) =>
        MakeRect(name, parent, new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMax));

    private TMP_Text MakeText(string text, Transform parent, Rect area, float size) =>
        MakeText(text, parent, new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMax), size);

    private Button MakeButton(string text, Transform parent, Rect area,
        UnityEngine.Events.UnityAction action, bool primary = false,
        UiButtonSound sound = UiButtonSound.Confirm) =>
        MakeButton(text, parent, new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMax),
            action, primary, sound);

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
        label.color = Cream;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private Button MakeButton(string text, Transform parent, Vector2 minimum, Vector2 maximum,
        UnityEngine.Events.UnityAction action, bool primary = false, UiButtonSound sound = UiButtonSound.Confirm)
    {
        RectTransform rect = MakeRect(text, parent, minimum, maximum);
        Image border = rect.gameObject.AddComponent<Image>();
        border.color = Color.clear;
        Image fill = MakeRect("Fill", rect, new Vector2(0.012f, 0.065f), new Vector2(0.988f, 0.935f))
            .gameObject.AddComponent<Image>();
        fill.sprite = buttonSprite;
        fill.color = Color.white;
        fill.raycastTarget = false;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.96f, 0.83f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.58f, 0.43f, 0.25f);
        colors.disabledColor = new Color(0.5f, 0.46f, 0.38f, 0.65f);
        colors.fadeDuration = 0f;
        button.colors = colors;
        button.onClick.AddListener(action);
        UiAudioRouter.RegisterButton(button, sound);
        TMP_Text label = MakeText(text, rect, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), 38f);
        label.color = Ink;
        return button;
    }
}
