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
        tensionSlider.minValue = 0f;
        tensionSlider.maxValue = 1f;
        tensionSlider.interactable = false;
    }

    private void Update()
    {
        if (reelingController == null ||
            tensionSlider == null)
        {
            return;
        }

        tensionSlider.value = reelingController.Tension01;
    }
}