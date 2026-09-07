using UnityEngine;

public class ReelingButtonHUD : MonoBehaviour
{
    [SerializeField] private ReelingController reelingController;
    [SerializeField] private GameObject accelerateButton;
    [SerializeField] private MobileFishingInputSource fishingInputSource;

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

        bool shouldShow = reelingController.IsActive;

        if (accelerateButton.activeSelf == shouldShow)
        {
            return;
        }

        if (!shouldShow)
        {
            //hiding a held button must also relase its input

            fishingInputSource.ReleaseAccelerate();
        }
        
        accelerateButton.SetActive(shouldShow);
    }
}