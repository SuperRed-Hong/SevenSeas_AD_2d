using UnityEngine;
using UnityEngine.UI;

public sealed class TensionBarHUD : MonoBehaviour
{
    [SerializeField]
    private ReelingController reelingController;

    [SerializeField]
    private Slider tensionSlider;

    private void Awake()
    {
        if (reelingController == null || tensionSlider == null)
        {
            Debug.LogError("TensionBarHUD requires a controller and Slider.", this);
            enabled = false;
            return;
        }

        tensionSlider.minValue = 0f;
        tensionSlider.maxValue = 1f;
        tensionSlider.transition = Selectable.Transition.None;
        tensionSlider.interactable = false;
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (reelingController == null ||
            tensionSlider == null)
        {
            return;
        }

        // Keep this controller enabled so it can show the child next retrieval.
        bool visible = reelingController.IsActive;
        if (tensionSlider.gameObject.activeSelf != visible)
        {
            tensionSlider.gameObject.SetActive(visible);
        }

        float tension = reelingController.Tension01;
        tensionSlider.SetValueWithoutNotify(tension);
        /*
        if (tensionSlider.fillRect != null &&
            tensionSlider.fillRect.TryGetComponent(out Image fill))
        {
            fill.color = tension <= 0.5f
                ? Color.Lerp(Color.green, Color.yellow, tension * 2f)
                : Color.Lerp(Color.yellow, Color.red, (tension - 0.5f) * 2f);
        }
        */
    }
}
