using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class MenuCanvasFader : MonoBehaviour
{
    [SerializeField, Min(0f)]
    [Tooltip("Time in seconds for the menu to fade out when starting a run.")]
    private float fadeOutDuration = 0.35f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public IEnumerator FadeOut()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            // Menu presentation should not depend on the gameplay clock.
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
