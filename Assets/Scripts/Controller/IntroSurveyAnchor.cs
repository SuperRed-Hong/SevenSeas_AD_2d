using UnityEngine;

// Optional per-map framing for the intro's first survey stop. Put this on a map
// root and the intro parks the camera on the marker instead of deriving the stop
// from the shared fish movement area, which cannot differ between maps.
// A map without this component keeps the derived behaviour.
public sealed class IntroSurveyAnchor : MonoBehaviour
{
    [SerializeField, Tooltip("开场下降后停靠的位置。把它拖到这张图上你想展示的地方；相机的 z 不受影响。")]
    private Transform stop;

    [SerializeField, Tooltip("勾上则这张图用下面的正交尺寸，覆盖 Profile 里的 surveyOrthographicSize。")]
    private bool overrideOrthographicSize;

    [SerializeField, Min(1f), Tooltip("越大看到的范围越广。")]
    private float orthographicSize = 7f;

    [Header("每图独立的运镜节奏（不勾则沿用 Profile）")]
    [SerializeField, Tooltip("从开场高点下降到停靠点的时长。地图越高越需要调。")]
    private bool overrideEntrySeconds;

    [SerializeField, Min(0f)]
    private float entrySeconds = 2f;

    [SerializeField, Tooltip("停靠后向钓鱼人摇镜的世界速度（单位/秒），不是时长。")]
    private bool overrideSurveySpeed;

    [SerializeField, Min(0.01f)]
    private float surveySpeed = 4f;

    [SerializeField, Tooltip("摇到钓鱼人之后停留多久再交接给游戏镜头。")]
    private bool overridePlatformHoldSeconds;

    [SerializeField, Min(0f)]
    private float platformHoldSeconds = 0.6f;

    [Header("Scene 视图预览（不影响运行）")]
    [SerializeField, Min(0.05f), Tooltip("预览用的画面宽高比。竖屏 450x720 约为 0.625。")]
    private float previewAspect = 0.625f;

    [SerializeField, Range(0f, 0.3f), Tooltip("预览用的黑边高度，和 Profile 的 barHeight 保持一致才准。")]
    private float previewBarHeight = 0.2f;

    public bool TryGetStop(out Vector2 position)
    {
        position = stop != null ? (Vector2)stop.position : default;
        return stop != null;
    }

    public bool TryGetOrthographicSize(out float size)
    {
        size = orthographicSize;
        return overrideOrthographicSize;
    }

    public bool TryGetEntrySeconds(out float seconds)
    {
        seconds = entrySeconds;
        return overrideEntrySeconds;
    }

    public bool TryGetSurveySpeed(out float speed)
    {
        speed = surveySpeed;
        return overrideSurveySpeed;
    }

    public bool TryGetPlatformHoldSeconds(out float seconds)
    {
        seconds = platformHoldSeconds;
        return overridePlatformHoldSeconds;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (stop == null) return;
        Vector3 center = stop.position;
        float halfHeight = orthographicSize;
        float halfWidth = halfHeight * previewAspect;

        // Full sensor rect, then the band left visible between the two black bars.
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireCube(center, new Vector3(halfWidth * 2f, halfHeight * 2f, 0f));

        float visibleHalfHeight = halfHeight * (1f - 2f * previewBarHeight);
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.9f);
        Gizmos.DrawWireCube(center, new Vector3(halfWidth * 2f, visibleHalfHeight * 2f, 0f));

        Gizmos.DrawLine(center + Vector3.left * 0.5f, center + Vector3.right * 0.5f);
        Gizmos.DrawLine(center + Vector3.down * 0.5f, center + Vector3.up * 0.5f);
    }
#endif
}
