using UnityEngine;

public sealed class MobileFishingInputSource : FishingInputSource
{
    [Header("Gesture Input")]
    [SerializeField]
    private CastGestureDetector castGestureDetector;

    [SerializeField]
    private CastGestureDetector strikeGestureDetector;

    [Header("Tilt Movement")]
    [SerializeField]
    private bool invertMove;

    [SerializeField, Min(0f)]
    private float deadZoneDegrees = 3f;

    [SerializeField, Range(1f, 90f)]
    private float maxTiltDegrees = 25f;

    [SerializeField, Range(45f, 89f)]
    private float maxSupportedTiltDegrees = 75f;

    private AttitudeReader attitudeReader;
    private AttitudeCalibrationService calibrationService;

    private Vector2 moveInput;

    private bool moveEnabled;
    private bool castEnabled;
    private bool strikeEnabled;
    private bool accelerateEnabled;
    private bool accelerateHeld;
    private bool accelerateRequiresRelease;

    public override Vector2 MoveInput =>
        moveEnabled ? moveInput : Vector2.zero;

    public override bool AccelerateHeld =>
        accelerateEnabled &&
        !accelerateRequiresRelease &&
        accelerateHeld;

    public bool IsTiltWithinSupportedRange
    {
        get;
        private set;
    } = true;

    private void OnEnable()
    {
        if (castGestureDetector != null)
        {
            castGestureDetector.CastDetected +=
                HandleCastDetected;
        }

        if (strikeGestureDetector != null)
        {
            strikeGestureDetector.GestureTriggered +=
                HandleStrikeTriggered;
        }
    }

    private void OnDisable()
    {
        if (castGestureDetector != null)
        {
            castGestureDetector.CastDetected -=
                HandleCastDetected;
        }

        if (strikeGestureDetector != null)
        {
            strikeGestureDetector.GestureTriggered -=
                HandleStrikeTriggered;
        }

        moveInput = Vector2.zero;
        accelerateHeld = false;
    }

    private void Update()
    {
        moveInput = Vector2.zero;

        if (!moveEnabled ||
            !TryResolveMotionServices() ||
            !attitudeReader.IsEnabled ||
            !attitudeReader.HasSample ||
            !calibrationService.IsCalibrated)
        {
            return;
        }

        Quaternion relativeAttitude =
            Quaternion.Inverse(
                calibrationService.NeutralAttitude) *
            attitudeReader.Attitude;

        Vector3 relativeForward =
            relativeAttitude * Vector3.forward;

        float tiltFromNeutral =
            Vector3.Angle(
                Vector3.forward,
                relativeForward);

        IsTiltWithinSupportedRange =
            tiltFromNeutral <=
            maxSupportedTiltDegrees;

        if (!IsTiltWithinSupportedRange)
        {
            return;
        }

        float tiltDegrees =
            Mathf.Atan2(
                relativeForward.x,
                relativeForward.z) *
            Mathf.Rad2Deg;

        if (invertMove)
        {
            tiltDegrees = -tiltDegrees;
        }

        moveInput = new Vector2(
            NormalizeTilt(tiltDegrees),
            0f);
    }

    public override void SetMoveEnabled(bool value)
    {
        moveEnabled = value;

        if (!value)
        {
            moveInput = Vector2.zero;
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

        accelerateRequiresRelease =
            value && accelerateHeld;

        if (!value)
        {
            accelerateHeld = false;
        }
    }

    // Connect this to a UI EventTrigger Pointer Down event.
    public void PressAccelerate()
    {
        accelerateHeld = true;
    }

    // Connect this to a UI EventTrigger Pointer Up event.
    public void ReleaseAccelerate()
    {
        accelerateHeld = false;
        accelerateRequiresRelease = false;
    }

    private void HandleCastDetected(float power)
    {
        if (castEnabled)
        {
            RaiseCastPerformed(power);
        }
    }

    private void HandleStrikeTriggered()
    {
        if (strikeEnabled)
        {
            RaiseStrikePerformed();
        }
    }

    private bool TryResolveMotionServices()
    {
        if (attitudeReader != null &&
            calibrationService != null)
        {
            return true;
        }

        if (AppRoot.Instance == null)
        {
            return false;
        }

        attitudeReader =
            AppRoot.Instance.AttitudeReader;

        calibrationService =
            AppRoot.Instance.AttitudeCalibration;

        return attitudeReader != null &&
               calibrationService != null;
    }

    private float NormalizeTilt(float degrees)
    {
        float magnitude = Mathf.Abs(degrees);

        if (magnitude <= deadZoneDegrees)
        {
            return 0f;
        }

        float usableRange =
            Mathf.Max(
                0.001f,
                maxTiltDegrees - deadZoneDegrees);

        float normalized =
            (magnitude - deadZoneDegrees) /
            usableRange;

        return Mathf.Sign(degrees) *
               Mathf.Clamp01(normalized);
    }
}