using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 纯显示：只读 SessionTimer，不推进时间，也不判定结束。
public sealed class TimerBarHUD : MonoBehaviour
{
    public enum DrainDirection
    {
        // 填充左端固定，右端随剩余时间回缩（怀表一侧先空）。
        TowardLeft,

        // 填充右端固定，左端随剩余时间回缩。
        TowardRight
    }

    [Header("Source")]
    [SerializeField]
    [Tooltip("提供剩余时间的局内计时器。")]
    private SessionTimer sessionTimer;

    [Header("Parts")]
    [SerializeField]
    [Tooltip("木框内部米色区里的填充条，锚点由本脚本驱动。")]
    private RectTransform fillRect;

    [SerializeField]
    [Tooltip("填充条的 Image，用来做低时间闪烁和加时闪光。")]
    private Image fillImage;

    [SerializeField]
    [Tooltip("金色怀表盘里的剩余秒数文字，可留空。")]
    private TMP_Text remainingText;

    [Header("Fill")]
    [SerializeField]
    [Tooltip("时间减少时填充向哪一侧收缩。")]
    private DrainDirection drainDirection = DrainDirection.TowardLeft;

    [SerializeField]
    [Tooltip("正常状态的填充颜色。")]
    private Color fillColor = new(0.62f, 0.40f, 0.18f);

    [SerializeField]
    [Tooltip("进入低时间警告后闪烁到的颜色。")]
    private Color lowTimeColor = new(0.80f, 0.20f, 0.16f);

    [SerializeField, Min(0f)]
    [Tooltip("剩余时间低于该秒数时开始闪烁；设 0 关闭警告。")]
    private float lowTimeThreshold = 10f;

    [SerializeField, Min(0f)]
    [Tooltip("低时间闪烁频率（次/秒）。")]
    private float lowTimeBlinkHz = 3f;

    [Header("Bonus Time")]
    [SerializeField]
    [Tooltip("检测到剩余时间被加长时闪一下的颜色。")]
    private Color bonusFlashColor = new(1f, 0.89f, 0.62f);

    [SerializeField, Min(0f)]
    [Tooltip("加时闪光持续秒数；设 0 关闭。")]
    private float bonusFlashDuration = 0.35f;

    [Header("Text")]
    [SerializeField]
    [Tooltip("勾选后显示 M:SS，否则只显示剩余整秒。")]
    private bool showMinutes;

    private float previousTimeRemaining;
    private float bonusFlashTimeLeft;
    private int displayedSeconds = -1;

    private void Awake()
    {
        if (sessionTimer == null || fillRect == null)
        {
            Debug.LogError("TimerBarHUD 需要 SessionTimer 和填充 RectTransform。", this);
            enabled = false;
            return;
        }

        if (fillImage == null)
        {
            fillImage = fillRect.GetComponent<Image>();
        }

        // 锚点驱动宽度，偏移必须清零，否则填充比例会被残留边距污染。
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        previousTimeRemaining = sessionTimer.TimeRemaining;
        Refresh();
    }

    // 与其他 HUD 一致：在玩法 Update 之后读取，显示的就是本帧的值。
    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        float timeRemaining = sessionTimer.TimeRemaining;

        if (bonusFlashDuration > 0f && timeRemaining > previousTimeRemaining + 0.001f)
        {
            bonusFlashTimeLeft = bonusFlashDuration;
        }

        previousTimeRemaining = timeRemaining;
        bonusFlashTimeLeft = Mathf.Max(0f, bonusFlashTimeLeft - Time.deltaTime);

        float duration = Mathf.Max(0.0001f, sessionTimer.SessionDuration);
        float progress = Mathf.Clamp01(timeRemaining / duration);

        ApplyFillProgress(progress);
        ApplyFillColor(timeRemaining);
        ApplyText(timeRemaining);
    }

    private void ApplyFillProgress(float progress)
    {
        if (drainDirection == DrainDirection.TowardLeft)
        {
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(progress, 1f);
        }
        else
        {
            fillRect.anchorMin = new Vector2(1f - progress, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
        }

        // 进度为 0 时留一个空矩形而不是负尺寸。
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private void ApplyFillColor(float timeRemaining)
    {
        if (fillImage == null)
        {
            return;
        }

        Color color = fillColor;

        bool isLow =
            lowTimeThreshold > 0f &&
            sessionTimer.IsRunning &&
            timeRemaining <= lowTimeThreshold;

        if (isLow && lowTimeBlinkHz > 0f)
        {
            // 用缩放时间：暂停时闪烁一起停下，读数才不会和玩法脱节。
            float blend = 0.5f + 0.5f * Mathf.Sin(
                Time.time * lowTimeBlinkHz * Mathf.PI * 2f);
            color = Color.Lerp(fillColor, lowTimeColor, blend);
        }

        if (bonusFlashTimeLeft > 0f && bonusFlashDuration > 0f)
        {
            float flash = bonusFlashTimeLeft / bonusFlashDuration;
            color = Color.Lerp(color, bonusFlashColor, flash);
        }

        fillImage.color = color;
    }

    private void ApplyText(float timeRemaining)
    {
        if (remainingText == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(timeRemaining);
        if (seconds == displayedSeconds)
        {
            return;
        }

        displayedSeconds = seconds;
        remainingText.text = showMinutes
            ? $"{seconds / 60}:{seconds % 60:00}"
            : seconds.ToString();
    }
}
