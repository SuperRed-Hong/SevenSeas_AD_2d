using UnityEngine;

public class ReelingButtonHUD : MonoBehaviour
{
    [SerializeField] private ReelingController reelingController;
    [SerializeField] private GameObject accelerateButton;
    [SerializeField] private MobileFishingInputSource fishingInputSource;

    [Header("Scene Touch Buttons")]
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;
    [SerializeField] private GameObject actionButton;

    [ContextMenu("Log Touch Fallback Status")]
    [UnityEngine.Scripting.Preserve]
    public void LogTouchFallbackStatus()
    {
        Debug.Log($"Touch fallback HUD: enabled={isActiveAndEnabled}, wired={leftButton != null && rightButton != null && actionButton != null}, " +
            $"fallback={fishingInputSource?.UsesTouchFallback}, inputEnabled={fishingInputSource?.isActiveAndEnabled}, " +
            $"move={fishingInputSource?.IsMoveAvailable}, action={fishingInputSource?.IsActionAvailable}", this);
        foreach (GameObject target in new[] { leftButton, rightButton, actionButton })
        {
            if (target == null) continue;
            CanvasRenderer renderer = target.GetComponent<CanvasRenderer>();
            Debug.Log($"{target.name}: active={target.activeInHierarchy}, layer={target.layer}, " +
                $"culled={renderer.cull}, alpha={renderer.GetAlpha()}, depth={renderer.absoluteDepth}, " +
                $"position={target.transform.position}", target);
        }
    }

    private void OnEnable()
    {
        RefreshVisibility();
    }


    private void LateUpdate()
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        RefreshTouchFallbackVisibility();
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
            return;
        }

        if (!shouldShow)
        {
            //hiding a held button must also relase its input

            fishingInputSource.ReleaseAccelerate();
        }
        
        accelerateButton.SetActive(shouldShow);
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

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null && target.activeSelf != value)
        {
            target.SetActive(value);
        }
    }
}
