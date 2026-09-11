using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ReelingButtonHUD : MonoBehaviour
{
    [SerializeField] private ReelingController reelingController;
    [SerializeField] private GameObject accelerateButton;
    [SerializeField] private MobileFishingInputSource fishingInputSource;

    private GameObject leftButton;
    private GameObject rightButton;
    private GameObject actionButton;

    private void Awake()
    {
        CreateTouchFallbackButtons();
    }

    private void Start()
    {
        RefreshVisibility();
    }


    private void LateUpdate()
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (reelingController == null ||
            accelerateButton == null ||
            fishingInputSource == null)
        {
            return;
        }

        bool shouldShow = fishingInputSource.isActiveAndEnabled &&
                          reelingController.IsActive &&
                          !fishingInputSource.UsesTouchFallback;

        if (accelerateButton.activeSelf == shouldShow)
        {
            RefreshTouchFallbackVisibility();
            return;
        }

        if (!shouldShow)
        {
            //hiding a held button must also relase its input

            fishingInputSource.ReleaseAccelerate();
        }
        
        accelerateButton.SetActive(shouldShow);
        RefreshTouchFallbackVisibility();
    }

    private void CreateTouchFallbackButtons()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || fishingInputSource == null)
        {
            return;
        }

        Sprite buttonSprite = accelerateButton != null
            ? accelerateButton.GetComponent<Image>()?.sprite
            : null;

        leftButton = CreateHoldButton(
            "TouchLeftButton", "<", canvas.transform,
            new Vector2(130f, 170f), false, buttonSprite,
            fishingInputSource.PressLeft, fishingInputSource.ReleaseLeft);

        rightButton = CreateHoldButton(
            "TouchRightButton", ">", canvas.transform,
            new Vector2(340f, 170f), false, buttonSprite,
            fishingInputSource.PressRight, fishingInputSource.ReleaseRight);

        actionButton = CreateHoldButton(
            "TouchActionButton", "ACTION", canvas.transform,
            new Vector2(-150f, 170f), true, buttonSprite,
            fishingInputSource.PressAccelerate, fishingInputSource.ReleaseAccelerate);
    }

    private void RefreshTouchFallbackVisibility()
    {
        bool fallbackActive = fishingInputSource != null &&
                              fishingInputSource.isActiveAndEnabled &&
                              fishingInputSource.UsesTouchFallback;

        SetActive(leftButton, fallbackActive && fishingInputSource.IsMoveAvailable);
        SetActive(rightButton, fallbackActive && fishingInputSource.IsMoveAvailable);
        SetActive(actionButton, fallbackActive && fishingInputSource.IsActionAvailable);

        if (!fallbackActive || !fishingInputSource.IsMoveAvailable)
        {
            fishingInputSource?.ReleaseLeft();
            fishingInputSource?.ReleaseRight();
        }

        if (!fallbackActive || !fishingInputSource.IsActionAvailable)
        {
            fishingInputSource?.ReleaseAccelerate();
        }
    }

    private static GameObject CreateHoldButton(
        string objectName,
        string labelText,
        Transform parent,
        Vector2 position,
        bool anchorRight,
        Sprite sprite,
        UnityEngine.Events.UnityAction press,
        UnityEngine.Events.UnityAction release)
    {
        GameObject root = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(EventTrigger));

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorRight ? new Vector2(1f, 0f) : Vector2.zero;
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(180f, 180f);

        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;

        EventTrigger trigger = root.GetComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerDown, press);
        AddTrigger(trigger, EventTriggerType.PointerUp, release);
        AddTrigger(trigger, EventTriggerType.PointerExit, release);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = labelText.Length > 1 ? 30f : 72f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.035f, 0.075f, 0.14f, 1f);
        label.raycastTarget = false;

        root.SetActive(false);
        return root;
    }

    private static void AddTrigger(
        EventTrigger trigger,
        EventTriggerType type,
        UnityEngine.Events.UnityAction action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null && target.activeSelf != value)
        {
            target.SetActive(value);
        }
    }
}
