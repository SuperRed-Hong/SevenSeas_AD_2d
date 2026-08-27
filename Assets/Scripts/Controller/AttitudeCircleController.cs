using UnityEngine;

public enum GyroscopeAxis
{
    X,
    Y,
    Z
}

public sealed class AttitudeCircleController : MonoBehaviour
{
    private enum DominantAxis
    {
        None,
        Horizontal,
        Vertical
    }

    [SerializeField] private AttitudeReader attitudeReader;
    [SerializeField] private GyroscopeAxis horizontalAxis = GyroscopeAxis.Y;
    [SerializeField] private GyroscopeAxis verticalAxis = GyroscopeAxis.X;
    [SerializeField] private bool invertHorizontal;
    [SerializeField] private bool invertVertical = true;
    [SerializeField, Min(0f)] private float deadZoneDegrees = 1.5f;
    [SerializeField, Range(1f, 90f)] private float maxTiltDegrees = 25f;
    [SerializeField, Range(45f, 89f)] private float maxSupportedTiltDegrees = 75f;
    [SerializeField, Range(0f, 10f)] private float axisSwitchHysteresisDegrees = 2f;
    [SerializeField, Range(0.1f, 1f)] private float movementRange = 0.85f;
    [SerializeField, Min(0f)] private float smoothing = 12f;
    [SerializeField, Min(0f)] private float autoCalibrationDelay = 0.5f;

    private Camera targetCamera;
    private SpriteRenderer spriteRenderer;
    private Quaternion neutralAttitude = Quaternion.identity;
    private Vector3 fallbackCenter;
    private float calibrationReadyTime;
    private DominantAxis dominantAxis;

    public bool IsCalibrated { get; private set; }
    public Vector2 TiltDegrees { get; private set; }

    private void Awake()
    {
        targetCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        fallbackCenter = transform.position;

        if (attitudeReader == null)
        {
            attitudeReader = FindFirstObjectByType<AttitudeReader>();
        }
    }

    private void OnEnable()
    {
        IsCalibrated = false;
        TiltDegrees = Vector2.zero;
        dominantAxis = DominantAxis.None;
        calibrationReadyTime = Time.unscaledTime + autoCalibrationDelay;
    }

    private void Update()
    {
        if (attitudeReader == null || !attitudeReader.IsEnabled || !attitudeReader.HasSample)
        {
            MoveTo(Vector2.zero);
            return;
        }

        if (!IsCalibrated)
        {
            if (Time.unscaledTime < calibrationReadyTime)
            {
                MoveTo(Vector2.zero);
                return;
            }

            Calibrate();
        }

        Quaternion relativeAttitude = Quaternion.Inverse(neutralAttitude) * attitudeReader.Attitude;
        Vector3 relativeForward = relativeAttitude * Vector3.forward;

        if (Vector3.Angle(Vector3.forward, relativeForward) > maxSupportedTiltDegrees)
        {
            TiltDegrees = Vector2.zero;
            dominantAxis = DominantAxis.None;
            MoveTo(Vector2.zero);
            return;
        }

        Vector3 tiltAngles = GetTiltAngles(relativeAttitude, relativeForward);
        TiltDegrees = new Vector2(
            ReadAxis(tiltAngles, horizontalAxis),
            ReadAxis(tiltAngles, verticalAxis));

        TiltDegrees = new Vector2(
            ApplyDirection(TiltDegrees.x, invertHorizontal),
            ApplyDirection(TiltDegrees.y, invertVertical));

        Vector2 normalizedTilt = new Vector2(
            NormalizeTilt(TiltDegrees.x),
            NormalizeTilt(TiltDegrees.y));
        normalizedTilt = SelectDominantAxis(normalizedTilt, TiltDegrees);
        MoveTo(normalizedTilt);
    }

    public void Calibrate()
    {
        if (attitudeReader == null || !attitudeReader.HasSample)
        {
            return;
        }

        neutralAttitude = attitudeReader.Attitude;
        TiltDegrees = Vector2.zero;
        dominantAxis = DominantAxis.None;
        IsCalibrated = true;
    }

    private static float ReadAxis(Vector3 value, GyroscopeAxis axis)
    {
        return axis switch
        {
            GyroscopeAxis.X => value.x,
            GyroscopeAxis.Y => value.y,
            GyroscopeAxis.Z => value.z,
            _ => 0f
        };
    }

    private float NormalizeTilt(float degrees)
    {
        float magnitude = Mathf.Abs(degrees);

        if (magnitude <= deadZoneDegrees)
        {
            return 0f;
        }

        float usableRange = Mathf.Max(0.001f, maxTiltDegrees - deadZoneDegrees);
        float normalized = (magnitude - deadZoneDegrees) / usableRange;
        return Mathf.Sign(degrees) * Mathf.Clamp01(normalized);
    }

    private static float ApplyDirection(float value, bool invert)
    {
        return invert ? -value : value;
    }

    private Vector2 SelectDominantAxis(Vector2 normalizedTilt, Vector2 tiltDegrees)
    {
        float horizontal = Mathf.Abs(tiltDegrees.x);
        float vertical = Mathf.Abs(tiltDegrees.y);

        if (normalizedTilt == Vector2.zero)
        {
            dominantAxis = DominantAxis.None;
            return Vector2.zero;
        }

        if (dominantAxis == DominantAxis.Horizontal &&
            vertical <= horizontal + axisSwitchHysteresisDegrees)
        {
            return new Vector2(normalizedTilt.x, 0f);
        }

        if (dominantAxis == DominantAxis.Vertical &&
            horizontal <= vertical + axisSwitchHysteresisDegrees)
        {
            return new Vector2(0f, normalizedTilt.y);
        }

        dominantAxis = horizontal >= vertical
            ? DominantAxis.Horizontal
            : DominantAxis.Vertical;

        return dominantAxis == DominantAxis.Horizontal
            ? new Vector2(normalizedTilt.x, 0f)
            : new Vector2(0f, normalizedTilt.y);
    }

    private static Vector3 GetTiltAngles(Quaternion relativeAttitude, Vector3 relativeForward)
    {
        float x = Mathf.Atan2(-relativeForward.y, relativeForward.z) * Mathf.Rad2Deg;
        float y = Mathf.Atan2(relativeForward.x, relativeForward.z) * Mathf.Rad2Deg;
        float z = Mathf.DeltaAngle(0f, relativeAttitude.eulerAngles.z);
        return new Vector3(x, y, z);
    }

    private void MoveTo(Vector2 normalizedTilt)
    {
        Vector3 center = fallbackCenter;
        Vector2 availableRange = Vector2.one;

        if (targetCamera == null || !targetCamera.orthographic)
        {
            ApplySmoothedPosition(center + (Vector3)normalizedTilt);
            return;
        }

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;
        Vector3 cameraPosition = targetCamera.transform.position;
        Vector3 extents = spriteRenderer != null ? spriteRenderer.bounds.extents : Vector3.zero;
        center = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
        availableRange = new Vector2(
            Mathf.Max(0f, halfWidth - extents.x),
            Mathf.Max(0f, halfHeight - extents.y)) * movementRange;

        Vector3 targetPosition = center + new Vector3(
            normalizedTilt.x * availableRange.x,
            normalizedTilt.y * availableRange.y,
            0f);
        ApplySmoothedPosition(targetPosition);
    }

    private void ApplySmoothedPosition(Vector3 targetPosition)
    {
        float blend = smoothing > 0f
            ? 1f - Mathf.Exp(-smoothing * Time.deltaTime)
            : 1f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, blend);
    }
}
