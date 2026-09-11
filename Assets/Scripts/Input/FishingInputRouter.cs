using UnityEngine;

public sealed class FishingInputRouter : FishingInputSource
{
    [SerializeField]
    private FishingInputSource desktopSource;

    [SerializeField]
    private FishingInputSource mobileSource;

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
        activeSource =
            RuntimeInputPlatform.UsesMobileControls
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
        activeSource?.SetMoveEnabled(value);
    }

    public override void SetCastEnabled(bool value)
    {
        activeSource?.SetCastEnabled(value);
    }

    public override void SetStrikeEnabled(bool value)
    {
        activeSource?.SetStrikeEnabled(value);
    }

    public override void SetAccelerateEnabled(bool value)
    {
        activeSource?.SetAccelerateEnabled(value);
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
