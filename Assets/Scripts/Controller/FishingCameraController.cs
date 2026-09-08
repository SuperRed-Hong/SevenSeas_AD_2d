using Unity.Cinemachine;
using System.Collections;
using UnityEngine;

public sealed class FishingCameraController : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera overviewCamera;

    [SerializeField]
    private CinemachineCamera hookFollowCamera;

    [SerializeField] private CinemachineCamera menuCamera;
    [SerializeField] private CinemachineBrain brain;

    
    [SerializeField]
    private CinemachineImpulseSource strikeRejectedImpulseSource;

    private Transform suspendedFollowTarget;
    private bool isFollowSuspended;
    
    public void ShowOverview()
    {
        if (menuCamera != null) menuCamera.gameObject.SetActive(false);
        if (overviewCamera != null)
        {
            overviewCamera.gameObject.SetActive(true);
        }

        if (hookFollowCamera != null)
        {
            // The brain can update the outgoing camera before taking its blend snapshot.
            // Detach before the hook docks so that update keeps the current camera pose.
            if (!isFollowSuspended)
            {
                suspendedFollowTarget = hookFollowCamera.Follow;
                hookFollowCamera.Follow = null;
                isFollowSuspended = true;
            }

            hookFollowCamera.gameObject.SetActive(false);
        }
    }

    public void ShowMenu()
    {
        if (menuCamera == null) return;
        ShowOverview();
        if (overviewCamera != null) overviewCamera.gameObject.SetActive(false);
        menuCamera.gameObject.SetActive(true);
    }

    public IEnumerator TransitionToOverview()
    {
        ShowOverview();
        // Let the brain create the blend before deciding whether it has finished.
        yield return null;
        while (brain != null && brain.IsBlending) yield return null;
    }

    public void FollowHook()
    {
        if (overviewCamera != null)
        {
            overviewCamera.gameObject.SetActive(true);
        }

        if (hookFollowCamera != null)
        {
            if (isFollowSuspended)
            {
                hookFollowCamera.Follow = suspendedFollowTarget;
                suspendedFollowTarget = null;
                isFollowSuspended = false;
            }

            hookFollowCamera.gameObject.SetActive(true);
        }
    }
    
    
    public void PlayStrikeRejectedShake()
    {
        if (strikeRejectedImpulseSource != null)
        {
            strikeRejectedImpulseSource.GenerateImpulse();
        }
    }
    
    
}
