using System;
using UnityEngine;

public class HookLandingVFX : MonoBehaviour
{
    [SerializeField] private FishingHookController hookController;
    [SerializeField] private GameObject ripplePrefab;

    private void OnEnable()
    {
        if (hookController != null)
        {
            hookController.Landed += HandleLanded;
        }
    }

    private void OnDisable()
    {
        if (hookController != null)
        {
            hookController.Landed -= HandleLanded;
        }
    }
    private void HandleLanded(Vector2 landingPosition)
    {
        if (ripplePrefab == null)
        {
            return;
        }
        
        //Keep the ripple at the Landing point as the hook moves away.
        
        Vector3 position = new Vector3(landingPosition.x, landingPosition.y, hookController.transform.position.z);
        
        Instantiate(ripplePrefab, position, Quaternion.identity);
    }
}
