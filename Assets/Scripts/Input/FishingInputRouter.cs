using UnityEngine;

public sealed class FishingInputRouter : FishingInputSource
{
    [SerializeField]
    private FishingInputSource desktopSource;

    [SerializeField]
    private FishingInputSource mobileSource;

    [Header("Touch Controls")]
    [Tooltip("Force on-screen controls in Editor and on any device. Off restores automatic platform selection.")]
    [SerializeField] private bool forceTouchControls;
    private bool moveEnabled, castEnabled, strikeEnabled, accelerateEnabled;
    public bool ForceTouchControls => forceTouchControls;

    private FishingInputSource activeSource;

    public FishingInputSource ActiveSource =>
        activeSource;

    public override Vector2 MoveInput =>
        activeSource != null
            ? activeSource.MoveInput
            : Vector2.zero;

    public override bool AccelerateHeld =>
        activeSource != null &&
        activeSource.AccelerateHeld;

    private void Awake()
    {
        forceTouchControls |= WebMotionPermission.ForceTouchControls;
        WebMotionPermission.SetForceTouchControls(forceTouchControls);
        activeSource =
            (forceTouchControls || RuntimeInputPlatform.UsesMobileControls)
                ? mobileSource
                : desktopSource;

        if (activeSource == null)
        {
            Debug.LogError(
                $"{nameof(FishingInputRouter)} could not select " +
                "a valid input source.",
                this);

            enabled = false;
            return;
        }

        if (desktopSource != null)
        {
            desktopSource.enabled =
                desktopSource == activeSource;
        }

        if (mobileSource != null)
        {
            mobileSource.enabled =
                mobileSource == activeSource;
        }

        Debug.Log(
            $"Fishing input selected: " +
            $"{activeSource.GetType().Name}");
    }

    private void OnEnable()
    {
        if (activeSource == null)
        {
            return;
        }

        activeSource.CastPerformed +=
            HandleCastPerformed;

        activeSource.StrikePerformed +=
            HandleStrikePerformed;
    }

    private void OnDisable()
    {
        if (activeSource == null)
        {
            return;
        }

        activeSource.CastPerformed -=
            HandleCastPerformed;

        activeSource.StrikePerformed -=
            HandleStrikePerformed;
    }

    public override void SetMoveEnabled(bool value)
    {
        moveEnabled = value;
        activeSource?.SetMoveEnabled(value);
    }

    public override void SetCastEnabled(bool value)
    {
        castEnabled = value;
        activeSource?.SetCastEnabled(value);
    }

    public override void SetStrikeEnabled(bool value)
    {
        strikeEnabled = value;
        activeSource?.SetStrikeEnabled(value);
    }

    public override void SetAccelerateEnabled(bool value)
    {
        accelerateEnabled = value;
        activeSource?.SetAccelerateEnabled(value);
    }

    private void Update()
    {
        // Also apply Inspector changes during Play Mode.
        if (forceTouchControls != WebMotionPermission.ForceTouchControls)
            SetForceTouchControls(forceTouchControls);
    }

    public void SetForceTouchControls(bool value)
    {
        FishingInputSource next = value || RuntimeInputPlatform.UsesMobileControls
            ? mobileSource : desktopSource;
        if (next == null) return;
        bool wasEnabled = isActiveAndEnabled;
        if (wasEnabled) OnDisable();
        // Cancel held input before changing modes, including mobile-to-mobile changes.
        if (activeSource != null) activeSource.enabled = false;
        forceTouchControls = value;
        WebMotionPermission.SetForceTouchControls(value);
        activeSource = next;
        if (desktopSource != null) desktopSource.enabled = desktopSource == next;
        if (mobileSource != null) mobileSource.enabled = mobileSource == next;
        activeSource.SetMoveEnabled(moveEnabled);
        activeSource.SetCastEnabled(castEnabled);
        activeSource.SetStrikeEnabled(strikeEnabled);
        activeSource.SetAccelerateEnabled(accelerateEnabled);
        if (wasEnabled) OnEnable();
    }

    private void HandleCastPerformed(float power)
    {
        RaiseCastPerformed(power);
    }

    private void HandleStrikePerformed()
    {
        RaiseStrikePerformed();
    }
}
