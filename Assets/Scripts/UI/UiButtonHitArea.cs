using UnityEngine;

// Keep padded/animated artwork unchanged while defining its actual interactive area.
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class UiButtonHitArea : MonoBehaviour, ICanvasRaycastFilter
{
    [SerializeField, Tooltip("Clickable rectangle relative to the full artwork: X/Y start at bottom-left, values are 0..1.")]
    private Rect normalizedRect = new Rect(0f, 0f, 1f, 1f);

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!isActiveAndEnabled) return true;
        var rectTransform = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint,
            eventCamera, out Vector2 local)) return false;
        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f) return false;
        Vector2 normalized = new Vector2((local.x - rect.xMin) / rect.width,
            (local.y - rect.yMin) / rect.height);
        return normalizedRect.Contains(normalized);
    }

    private void OnValidate()
    {
        float x = Mathf.Clamp01(normalizedRect.x);
        float y = Mathf.Clamp01(normalizedRect.y);
        normalizedRect = new Rect(x, y, Mathf.Clamp(normalizedRect.width, 0f, 1f - x),
            Mathf.Clamp(normalizedRect.height, 0f, 1f - y));
    }
}
