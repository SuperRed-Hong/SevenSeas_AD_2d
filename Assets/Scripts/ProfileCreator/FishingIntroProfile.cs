using UnityEngine;

// Shared defaults for the fishing intro. A map can override the marked fields
// for itself with an IntroSurveyAnchor on its root.
[CreateAssetMenu(menuName = "Seven Seas/Fishing Intro Profile")]
public sealed class FishingIntroProfile : ScriptableObject
{
    [Min(0f), Tooltip("第一段：从开场高点下降到停靠点的时长（秒）。全程用 SmoothStep 缓入缓出，" +
        "所以这是总时长，不是速度。可被地图上的 IntroSurveyAnchor 覆盖。")]
    public float entrySeconds = 2f;

    [Min(0.01f), Tooltip("第二段：停靠后向钓鱼人摇镜的速度，单位是「世界单位/秒」，不是时长。" +
        "距离越远耗时越久，但速度感恒定，中段不会加速。数值越小摇得越慢。" +
        "可被地图上的 IntroSurveyAnchor 覆盖。")]
    public float surveySpeed = 2f;

    [Min(0f), Tooltip("第三段：摇到钓鱼人之后停留多久，再交接给游戏镜头（秒）。" +
        "可被地图上的 IntroSurveyAnchor 覆盖。")]
    public float platformHoldSeconds = 0.6f;

    [Min(0f), Tooltip("第四段：正常播完时，交接到游戏镜头的时长（秒）。黑边也在这段时间内收起。")]
    public float handoffSeconds = 0.8f;

    [Min(0f), Tooltip("玩家点 SKIP 时，交接到游戏镜头的时长（秒）。" +
        "通常比 Handoff Seconds 短，因为玩家已经表示不想看了。")]
    public float skipSeconds = 0.35f;

    [Min(0f), Tooltip("上下黑边展开的时长（秒）。发生在第一段结束、第二段摇镜开始之前。")]
    public float barSeconds = 0.35f;

    [Range(0f, 0.3f), Tooltip("每条黑边的高度，占屏幕高度的比例。0.1 = 上 10% + 下 10%，共遮挡 20%。" +
        "注意它还会缩小可视范围，从而抬高自动推算的停靠点。")]
    public float barHeight = 0.1f;

    [Min(1f), Tooltip("停靠时的正交尺寸，即画面高度的一半（世界单位）。越大看到的范围越广。" +
        "可被地图上的 IntroSurveyAnchor 覆盖。")]
    public float surveyOrthographicSize = 7f;

    [Min(0f), Tooltip("菜单地图放在游戏地图上方多远（世界单位），也就是开场要下降的高度。" +
        "这是「最小值」：实际取值是它和「两图高度 + 2×正交尺寸 + Scenery Gap」中的较大者，" +
        "所以调到自动算出的值以下不会有任何效果。想缩短开场下落距离，先改 Scenery Gap。")]
    public float menuVerticalOffset = 55f;

    [Min(0f), Tooltip("菜单地图和游戏地图之间保留的额外空隙（世界单位），防止两张图在镜头里同时出现。" +
        "它参与上面那个自动高度的计算。")]
    public float sceneryGap = 4f;

    private void OnValidate()
    {
        entrySeconds = Mathf.Max(0f, entrySeconds);
        surveySpeed = Mathf.Max(0.01f, surveySpeed);
        platformHoldSeconds = Mathf.Max(0f, platformHoldSeconds);
        handoffSeconds = Mathf.Max(0f, handoffSeconds);
        skipSeconds = Mathf.Max(0f, skipSeconds);
        barSeconds = Mathf.Max(0f, barSeconds);
        barHeight = Mathf.Clamp(barHeight, 0f, 0.3f);
        surveyOrthographicSize = Mathf.Max(1f, surveyOrthographicSize);
        menuVerticalOffset = Mathf.Max(0f, menuVerticalOffset);
        sceneryGap = Mathf.Max(0f, sceneryGap);
    }
}
