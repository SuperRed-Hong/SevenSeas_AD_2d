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
    
    public void ShowOverview()
    {
        if (overviewCamera != null)
        {
            overviewCamera.gameObject.SetActive(true);
        }

        if (hookFollowCamera != null)
        {
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