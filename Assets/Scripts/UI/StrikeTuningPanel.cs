using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StrikeTuningPanel : MonoBehaviour
{
    [SerializeField] private StrikeController strikeController;
    [SerializeField] private FishingPauseController pauseController;

    private readonly string[] labels =
    {
        "Window duration (s)", "Attempt cooldown (s)", "Shake amplitude", "Shake duration (s)",
        "Target band width", "Minimum band center", "Maximum band center", "Hit forgiveness"
    };
    private readonly float[] minimums = { 0.01f, 0f, 0f, 0f, 0.01f, 0f, 0f, 0f };
    private readonly float[] maximums = { 10f, 2f, 1f, 2f, 0.99f, 1f, 1f, 0.5f };
    private Slider[] sliders;
    private TMP_Text[] valueLabels;
    private TMP_Text message;
    private GameObject panel;
    private GameObject canvasObject;
    private bool isOpen;
    private Button openButton;

    private void Start()
    {
        canvasObject = new GameObject("StrikeTuningCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440f, 2304f);
        scaler.matchWidthOrHeight = 0.5f;

        openButton = MakeButton("STRIKE TUNING", canvas.transform, new Vector2(0.02f, 0.95f),
            new Vector2(0.31f, 0.985f), Open);
        RectTransform shade = MakeRect("StrikeTuningPanel", canvas.transform, Vector2.zero, Vector2.one);
        panel = shade.gameObject;
        shade.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.035f, 0.06f, 0.98f);
        MakeText("STRIKE TUNING / PAUSED", shade, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.96f), 48f);
        MakeText("Changes apply to the next strike check", shade,
            new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.9f), 30f);

        sliders = new Slider[labels.Length];
        valueLabels = new TMP_Text[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            float top = 0.83f - i * 0.082f;
            valueLabels[i] = MakeText(labels[i], shade, new Vector2(0.07f, top - 0.034f),
                new Vector2(0.93f, top), 34f);
            RectTransform track = MakeRect(labels[i], shade, new Vector2(0.08f, top - 0.065f),
                new Vector2(0.92f, top - 0.043f));
            track.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.22f, 0.28f);
            RectTransform fill = MakeRect("Fill", track, Vector2.zero, Vector2.one);
            fill.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.65f, 0.85f);
            RectTransform handle = MakeRect("Handle", track, Vector2.zero, new Vector2(0f, 1f));
            handle.sizeDelta = new Vector2(34f, 12f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(1f, 0.8f, 0.3f);
            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = minimums[i];
            slider.maxValue = maximums[i];
            slider.onValueChanged.AddListener(value => UpdateLabel(index, value));
            sliders[i] = slider;
        }

        message = MakeText("", shade, new Vector2(0.06f, 0.115f), new Vector2(0.94f, 0.17f), 28f);
        MakeButton("APPLY", shade, new Vector2(0.06f, 0.04f), new Vector2(0.32f, 0.10f), Apply);
        MakeButton("DEFAULTS", shade, new Vector2(0.37f, 0.04f), new Vector2(0.63f, 0.10f), ResetDefaults);
        MakeButton("CLOSE", shade, new Vector2(0.68f, 0.04f), new Vector2(0.94f, 0.10f), Close);
        panel.SetActive(false);
    }

    public void Open()
    {
        if (isOpen || panel == null || strikeController == null ||
            strikeController.RuntimeProfile == null || pauseController == null ||
            !pauseController.TryPause(this)) return;
        isOpen = true;
        RefreshValues();
        message.text = "Runtime only. The project Profile asset stays unchanged.";
        panel.SetActive(true);
    }

    private void RefreshValues()
    {
        float[] values = strikeController.RuntimeProfile.GetValues();
        for (int i = 0; i < values.Length; i++)
        {
            sliders[i].maxValue = Mathf.Max(maximums[i], values[i]);
            sliders[i].SetValueWithoutNotify(values[i]);
            UpdateLabel(i, values[i]);
        }
    }

    private void UpdateLabel(int index, float value) => valueLabels[index].text = $"{labels[index]}: {value:F3}";

    private void Apply()
    {
        float[] values = new float[sliders.Length];
        for (int i = 0; i < values.Length; i++) values[i] = sliders[i].value;
        strikeController.RuntimeProfile.ApplyRuntimeValues(values);
        RefreshValues();
        message.text = "Applied for the next check. Band limits are kept inside the ring.";
    }

    private void ResetDefaults()
    {
        strikeController.ResetRuntimeTuning();
        RefreshValues();
        message.text = "Profile defaults restored for the next check.";
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false);
        pauseController.Resume(this);
    }

    private void Update()
    {
        if (openButton != null)
            openButton.interactable = pauseController != null && pauseController.CanPause;
    }

    private void OnDisable()
    {
        Close();
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
