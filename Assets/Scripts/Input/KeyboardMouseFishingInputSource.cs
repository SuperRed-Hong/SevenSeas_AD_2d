using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KeyboardMouseFishingInputSource : FishingInputSource
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference castAction;
    [SerializeField] private InputActionReference strikeAction;
    [SerializeField] private InputActionReference accelerateAction;

    [SerializeField, Range(0f, 1f)] [Tooltip("Cast power produced by keyboard and mouse input")]
    private float castPower = 1f;
    [SerializeField, Min(0f)]
    [Tooltip("How fast keyboard input moves the virtual position across the normalized range.")]
   
   
    private bool castEnabled;
    private bool strikeEnabled;
    private bool accelerateEnabled;
    private bool accelerateRequiresRelease;
    private bool moveEnabled;

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
        
        
        if (castEnabled &&
            castAction != null &&
            castAction.action.WasPressedThisFrame())
        {
            RaiseCastPerformed(castPower);
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