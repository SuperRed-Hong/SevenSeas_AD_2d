using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KeyboardMouseFishingInputSource : FishingInputSource
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference castAction;
    [SerializeField] private InputActionReference strikeAction;
    [SerializeField] private InputActionReference accelerateAction;

    [SerializeField, Min(0.01f)]
    [Tooltip("Hold duration in seconds required to reach full cast power.")]
    private float fullChargeDuration = 2f;

    private float castChargeElapsed;
    private bool isChargingCast;
    
    
   
    private bool castEnabled;
    private bool strikeEnabled;
    private bool accelerateEnabled;
    private bool accelerateRequiresRelease;
    private bool moveEnabled;
    
    public bool IsChargingCast => isChargingCast;
    public float CastCharge01 => Mathf.Clamp01(castChargeElapsed / Mathf.Max(0.01f,fullChargeDuration));

    public override Vector2 MoveInput => moveEnabled &&
                                         moveAction != null &&
                                         moveAction.action.enabled
        ? moveAction.action.ReadValue<Vector2>()
        : Vector2.zero;
    
    public override bool AccelerateHeld => accelerateEnabled && !accelerateRequiresRelease &&
                                           accelerateAction != null&&
                                           accelerateAction.action.IsPressed();
    public override void SetMoveEnabled(bool value)
    {
        moveEnabled = value;
    }
    private void OnEnable()
    {
        SetActionEnabled(moveAction, true);
        SetActionEnabled(castAction, true);
        SetActionEnabled(strikeAction, true);
        SetActionEnabled(accelerateAction, true);
    }
    private void OnDisable()
    {
        
        isChargingCast = false;
        castChargeElapsed = 0f;
        SetActionEnabled(moveAction, false);
        SetActionEnabled(castAction, false);
        SetActionEnabled(strikeAction, false);
        SetActionEnabled(accelerateAction, false);
    }
    
    private void Update()
    {
        
       
        if (accelerateRequiresRelease &&
            accelerateAction != null &&
            !accelerateAction.action.IsPressed())
        {
            accelerateRequiresRelease = false;
        }


        if (castEnabled && castAction != null)
        {
            if (castAction.action.WasPressedThisFrame())
            {
                isChargingCast = true;
                castChargeElapsed = 0f;
                
            }

            if (isChargingCast)
            {
                float safeDuration = Mathf.Max(0.01f, fullChargeDuration);
                
                castChargeElapsed = Mathf.Min(castChargeElapsed + Time.deltaTime, safeDuration);

                if (castAction.action.WasReleasedThisFrame())
                {
                    float power = Mathf.Clamp01(castChargeElapsed / safeDuration);
                    
                    // Clear charging before the event changes the game state.
                    isChargingCast = false;
                    castChargeElapsed = 0f;
                    
                    RaiseCastPerformed(power);
                }
            }
        }
        
        
        if (strikeEnabled &&
            strikeAction != null &&
            strikeAction.action.WasPressedThisFrame())
        {
            RaiseStrikePerformed();
        }
        
        
    }
    public override void SetCastEnabled(bool value)
    {
        castEnabled = value;

        if (!value)
        {
            isChargingCast = false;
            castChargeElapsed = 0f;
        }
    }

    public override void SetStrikeEnabled(bool value)
    {
        strikeEnabled = value;
    }

    public override void SetAccelerateEnabled(bool value)
    {
        accelerateEnabled = value;

        // Entering Reeling while Space is still held must not
        // immediately activate acceleration.
        accelerateRequiresRelease =
            value &&
            accelerateAction != null &&
            accelerateAction.action.IsPressed();
        
    }
    private static void SetActionEnabled(
        InputActionReference actionReference,
        bool value)
    {
        if (actionReference == null)
        {
            return;
        }

        if (value)
        {
            actionReference.action.Enable();
        }
        else
        {
            actionReference.action.Disable();
        }
    }


 
}