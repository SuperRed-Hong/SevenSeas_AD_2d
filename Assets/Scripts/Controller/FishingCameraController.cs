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
    [SerializeField] private CinemachineCamera introCamera;
    [SerializeField] private CinemachineBrain brain;

    
    [SerializeField]
    private CinemachineImpulseSource strikeRejectedImpulseSource;

    private Transform suspendedFollowTarget;
    private bool isFollowSuspended;

    public CinemachineCamera MenuCamera => menuCamera;
    public CinemachineCamera IntroCamera => introCamera;
    public CinemachineCamera OverviewCamera => overviewCamera;

    // Intro has already interpolated to the exact gameplay pose and lens.
    // Invalidate the incoming camera's cached state so its old pose cannot leak into a blend.
    public void CompleteIntro()
    {
        if (overviewCamera != null) overviewCamera.PreviousStateIsValid = false;
        ShowOverview();
    }

    public IEnumerator BeginIntro()
    {
        if (introCamera == null || menuCamera == null) yield break;
        introCamera.transform.SetPositionAndRotation(menuCamera.transform.position, menuCamera.transform.rotation);
        introCamera.Lens = menuCamera.Lens;
        introCamera.PreviousStateIsValid = false;
        var previousBlend = brain != null ? brain.DefaultBlend : default;
        var previousCustomBlends = brain != null ? brain.CustomBlends : null;
        try
        {
            // Identical poses need a cut, not an extra stationary blend before the entry movement.
            if (brain != null)
            {
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
                brain.CustomBlends = null;
            }
            introCamera.gameObject.SetActive(true);
            menuCamera.gameObject.SetActive(false);
            yield return null;
        }
        finally
        {
            if (brain != null)
            {
                brain.DefaultBlend = previousBlend;
                brain.CustomBlends = previousCustomBlends;
            }
        }
    }
    
    public void ShowOverview()
    {
        if (introCamera != null) introCamera.gameObject.SetActive(false);
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

    public void PlayStrikeRejectedShake(float amplitude, float duration)
    {
        if (strikeRejectedImpulseSource == null || amplitude <= 0f || duration <= 0f) return;
        float previousDuration = strikeRejectedImpulseSource.ImpulseDefinition.ImpulseDuration;
        Vector3 direction = strikeRejectedImpulseSource.DefaultVelocity.normalized;
        strikeRejectedImpulseSource.ImpulseDefinition.ImpulseDuration = duration;
        strikeRejectedImpulseSource.GenerateImpulseWithVelocity(direction * amplitude);
        strikeRejectedImpulseSource.ImpulseDefinition.ImpulseDuration = previousDuration;
    }
    
    
}
