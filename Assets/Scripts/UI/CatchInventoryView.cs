using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CatchInventoryView : MonoBehaviour
{
    [SerializeField] private CatchInventory inventory;
    [SerializeField] private Sprite inventorySprite;
    [SerializeField] private Sprite closeSprite;
    [SerializeField] private Sprite previousPageSprite;
    [SerializeField] private Sprite nextPageSprite;
    [SerializeField] private TMP_FontAsset font;

    private const int PageSize = 12;
    private static readonly Color Ink = new(0.20f, 0.14f, 0.09f);
    private readonly Image[] fishImages = new Image[PageSize];
    private readonly TMP_Text[] scores = new TMP_Text[PageSize];
    private GameObject panel;
    private TMP_Text summary;
    private TMP_Text pageLabel;
    private TMP_Text emptyLabel;
    private Button previousButton;
    private Button nextButton;
    private Action onClosed;
    private int page;
    public bool IsOpen => panel != null && panel.activeSelf;

    private void OnEnable()
    {
        if (inventory != null) inventory.Changed += Refresh;
    }

    public void Open(Action closed = null)
    {
        if (inventory == null || inventorySprite == null)
        {
            Debug.LogError("Catch inventory requires a tracker and the existing inventory sprite.", this);
            closed?.Invoke();
            return;
        }

        if (panel == null) BuildPanel();
        onClosed = closed;
        page = 0;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);
        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.Changed -= Refresh;
        // Scene unloading must not reopen a menu or claim ownership of pause.
        if (panel != null) panel.SetActive(false);
        onClosed = null;
    }

    private void BuildPanel()
    {
        panel = new GameObject("CatchInventoryCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        panel.transform.SetParent(transform, false);
        Canvas canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1110;
        CanvasScaler scaler = panel.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440f, 2304f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform shade = Rect("Shade", panel.transform, Vector2.zero, Vector2.one);
        shade.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.025f, 0.015f, 0.96f);
        summary = Label("Summary", shade, new Vector2(0.08f, 0.87f), new Vector2(0.92f, 0.97f), 48f);
        summary.color = new Color(0.91f, 0.81f, 0.59f);

        RectTransform bounds = Rect("BoardBounds", shade, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.86f));
        RectTransform board = Rect("InventoryBoard", bounds, Vector2.zero, Vector2.one);
        Image background = board.gameObject.AddComponent<Image>();
        background.sprite = inventorySprite;
        background.raycastTarget = false;
        AspectRatioFitter fitter = board.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = inventorySprite.rect.width / inventorySprite.rect.height;

        for (int i = 0; i < PageSize; i++)
        {
            int column = i % 3;
            int row = i / 3;
            // Match the painted cells in the existing 73 x 102 artwork.
            float left = (9f + column * 20f) / 73f;
            float bottom = (72f - row * 20f) / 102f;
            RectTransform cell = Rect($"Catch{i + 1}", board,
                new Vector2(left, bottom), new Vector2(left + 15f / 73f, bottom + 15f / 102f));
            fishImages[i] = Rect("Fish", cell, new Vector2(0.05f, 0.31f), new Vector2(0.95f, 0.96f))
                .gameObject.AddComponent<Image>();
            fishImages[i].preserveAspect = true;
            fishImages[i].raycastTarget = false;
            scores[i] = Label("Score", cell, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.32f), 30f);
            scores[i].enableAutoSizing = true;
            scores[i].fontSizeMin = 12f;
            scores[i].fontSizeMax = 30f;
        }

        emptyLabel = Label("Empty", shade, new Vector2(0.12f, 0.11f), new Vector2(0.88f, 0.15f), 28f);
        emptyLabel.color = summary.color;
        previousButton = Button("<", previousPageSprite, shade,
            new Vector2(0.16f, 0.04f), new Vector2(0.29f, 0.10f), PreviousPage);
        nextButton = Button(">", nextPageSprite, shade,
            new Vector2(0.71f, 0.04f), new Vector2(0.84f, 0.10f), NextPage);
        pageLabel = Label("Page", shade, new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.10f), 36f);
        pageLabel.color = summary.color;
        Button("X", closeSprite, shade, new Vector2(0.89f, 0.92f), new Vector2(0.98f, 0.98f), Close);
    }

    private void PreviousPage()
    {
        page--;
        Refresh();
    }

    private void NextPage()
    {
        page++;
        Refresh();
    }

    private void Refresh()
    {
        if (!IsOpen || inventory == null) return;
        int pages = Mathf.Max(1, (inventory.Count + PageSize - 1) / PageSize);
        page = Mathf.Clamp(page, 0, pages - 1);
        summary.text = $"THIS RUN\n{inventory.Count} FISH   {inventory.TotalScore:N0} PTS";
        pageLabel.text = $"{page + 1} / {pages}";
        emptyLabel.text = inventory.Count == 0 ? "NO FISH CAUGHT YET" : "ACTUAL POINTS PER CATCH";
        previousButton.interactable = page > 0;
        nextButton.interactable = page + 1 < pages;
        for (int i = 0; i < PageSize; i++)
        {
            int index = page * PageSize + i;
            bool occupied = index < inventory.Count;
            fishImages[i].sprite = occupied ? inventory.Records[index].RevealedSprite : null;
            fishImages[i].enabled = occupied && fishImages[i].sprite != null;
            scores[i].text = occupied ? inventory.Records[index].ActualScore.ToString("N0") : "";
        }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 minimum, Vector2 maximum)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = minimum;
        rect.anchorMax = maximum;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private TMP_Text Label(string name, Transform parent, Vector2 minimum, Vector2 maximum, float size)
    {
        var label = Rect(name, parent, minimum, maximum).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.fontSize = size;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private Button Button(string text, Sprite sprite, Transform parent, Vector2 minimum, Vector2 maximum,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = Rect(text, parent, minimum, maximum);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : new Color(0.68f, 0.55f, 0.34f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        UiAudioRouter.RegisterButton(button, text == "X" ? UiButtonSound.Back : UiButtonSound.Confirm);
        if (sprite == null) Label(text, rect, Vector2.zero, Vector2.one, 36f).text = text;
        return button;
    }
}
