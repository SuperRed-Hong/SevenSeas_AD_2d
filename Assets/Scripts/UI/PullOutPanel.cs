using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 贴屏幕边缘的抽屉式面板：点击露出的部分拉出或收回，也可由外部临时拉出后自动收回。
// 本组件挂在带 RectMask2D 的根节点上，遮罩既裁掉推出边缘之外的画面，
// 也把点击范围限制在露出的那一截，收起时只有标签可点。
[RequireComponent(typeof(RectMask2D))]
public sealed class PullOutPanel : MonoBehaviour, IPointerClickHandler
{
    public enum AnchorEdge
    {
        // 贴左缘，收起时向左滑出遮罩。
        Left,

        // 贴右缘，收起时向右滑出遮罩。
        Right
    }

    [Header("Parts")]
    [SerializeField]
    [Tooltip("会滑动的面板本体，必须是本节点的子物体。")]
    private RectTransform panelRect;

    [SerializeField]
    [Tooltip("面板本体的 Image；它同时是点击热区，raycastTarget 需要开着。")]
    private Image panelImage;

    [SerializeField]
    [Tooltip("随开合度淡入淡出的内容，例如分数文字。计时器读数不要放这里。")]
    private CanvasGroup[] fadeWithOpen;

    [Header("Layout")]
    [SerializeField]
    [Tooltip("面板贴在画布的哪一侧。")]
    private AnchorEdge anchorEdge = AnchorEdge.Right;

    [SerializeField, Min(1f)]
    [Tooltip("收起时露出的宽度。")]
    private float collapsedWidth = 150f;

    [SerializeField, Min(1f)]
    [Tooltip("完全拉出时的宽度。")]
    private float expandedWidth = 610f;

    [Header("Sprites")]
    [SerializeField]
    [Tooltip("完全收起时换用的窄贴图；留空则始终用拉出贴图，只靠位移收起。")]
    private Sprite collapsedSprite;

    [SerializeField]
    [Tooltip("拉出状态的贴图。")]
    private Sprite expandedSprite;

    [Header("Motion")]
    [SerializeField]
    [Tooltip("关掉后只能由代码控制，点击不再收放。")]
    private bool clickToToggle = true;

    [SerializeField]
    [Tooltip("初始是否保持拉出。")]
    private bool startExpanded;

    [SerializeField, Min(0.01f)]
    [Tooltip("拉出用时（秒）。")]
    private float expandDuration = 0.22f;

    [SerializeField, Min(0.01f)]
    [Tooltip("收回用时（秒）。")]
    private float collapseDuration = 0.3f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("开合度超过该值后淡入内容。")]
    private float fadeStart = 0.45f;

    private float openAmount;
    private float holdTimeLeft;
    private bool pinnedOpen;

    // 0 = 完全收起，1 = 完全拉出。
    public float OpenAmount => openAmount;

    // 被点击钉住、不会自动收回的状态。
    public bool IsPinnedOpen => pinnedOpen;

    // 玩家点击导致的开合翻转，参数是翻转后的目标状态。
    public event Action<bool> ToggledByPointer;

    private void Awake()
    {
        if (panelRect == null || panelImage == null)
        {
            Debug.LogError("PullOutPanel 需要面板 RectTransform 和 Image。", this);
            enabled = false;
            return;
        }

        pinnedOpen = startExpanded;
        openAmount = startExpanded ? 1f : 0f;
        ApplyOpenAmount(openAmount);
    }

    // 钉住拉出，直到被收回。
    public void Open()
    {
        pinnedOpen = true;
    }

    // 立刻取消钉住并清掉停留时间，避免刚收回又被自动弹出。
    public void Close()
    {
        pinnedOpen = false;
        holdTimeLeft = 0f;
    }

    // 自动弹出期间点击视为「钉住」而不是关闭，玩家想细看时不会被收走。
    public void Toggle()
    {
        if (pinnedOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    // 临时拉出，停留结束后自动收回；已钉住时不受影响。
    public void OpenTemporarily(float holdSeconds)
    {
        holdTimeLeft = Mathf.Max(holdTimeLeft, holdSeconds);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!clickToToggle || !isActiveAndEnabled)
        {
            return;
        }

        Toggle();
        ToggledByPointer?.Invoke(pinnedOpen);
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        holdTimeLeft = Mathf.Max(0f, holdTimeLeft - deltaTime);

        bool wantOpen = pinnedOpen || holdTimeLeft > 0f;
        float duration = wantOpen ? expandDuration : collapseDuration;
        openAmount = Mathf.MoveTowards(
            openAmount,
            wantOpen ? 1f : 0f,
            deltaTime / Mathf.Max(0.01f, duration));

        ApplyOpenAmount(openAmount);
    }

    private void ApplyOpenAmount(float amount)
    {
        bool isOpen = amount > 0.0001f;
        bool hasCollapsedSprite = collapsedSprite != null;

        // 完全收起时换回窄贴图，静止状态才是像素对齐的原图而不是被拉伸的长图。
        Sprite wantedSprite = isOpen || !hasCollapsedSprite
            ? expandedSprite
            : collapsedSprite;
        if (wantedSprite != null && panelImage.sprite != wantedSprite)
        {
            panelImage.sprite = wantedSprite;
        }

        float width = isOpen || !hasCollapsedSprite
            ? expandedWidth
            : collapsedWidth;
        panelRect.sizeDelta = new Vector2(width, panelRect.sizeDelta.y);

        float hiddenWidth = Mathf.Max(0f, expandedWidth - collapsedWidth);
        float slide;
        if (isOpen)
        {
            slide = Mathf.Lerp(hiddenWidth, 0f, Mathf.SmoothStep(0f, 1f, amount));
        }
        else
        {
            // 没有窄贴图时收起完全靠位移，此时不能把位移归零。
            slide = hasCollapsedSprite ? 0f : hiddenWidth;
        }

        float direction = anchorEdge == AnchorEdge.Right ? 1f : -1f;
        panelRect.anchoredPosition = new Vector2(
            direction * slide, panelRect.anchoredPosition.y);

        if (fadeWithOpen == null)
        {
            return;
        }

        float alpha = Mathf.InverseLerp(fadeStart, 1f, amount);
        foreach (CanvasGroup group in fadeWithOpen)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }
    }
}
