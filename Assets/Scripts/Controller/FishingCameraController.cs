using Unity.Cinemachine;
using UnityEngine;

public sealed class FishingCameraController : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera overviewCamera;

    [SerializeField]
    private CinemachineCamera hookFollowCamera;

    
    [SerializeField]
    private CinemachineImpulseSource strikeRejectedImpulseSource;

    private Transform suspendedFollowTarget;
    private bool isFollowSuspended;
    
    public void ShowOverview()
    {
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
