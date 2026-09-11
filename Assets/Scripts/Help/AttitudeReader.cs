using UnityEngine;
using UnityEngine.InputSystem;

public enum AttitudeSource
{
    None,
    Attitude,
    Gravity,
    Accelerometer
}

public sealed class AttitudeReader : MonoBehaviour
{
    [SerializeField, Min(1f)]
    private float requestedSamplingFrequency = 60f;

    [SerializeField, Range(0.01f, 1f)]
    private float gravityFilterAlpha = 0.12f;

    private Sensor activeSensor;
    private Vector3 smoothedGravity;
    private float lastSampleTime;
    private float nextConnectionAttemptTime;
    private int attemptsWithoutSample;
    private AttitudeSource lastReportedSource = AttitudeSource.None;
    private bool connectionWarningLogged;
    private WebMotionPermissionState lastMotionPermission;
    private WebMotionPermissionState lastOrientationPermission;

    public AttitudeSource ActiveSource { get; private set; } = AttitudeSource.None;
    public bool IsAvailable => activeSensor != null && activeSensor.added;
    public bool IsEnabled => IsAvailable && activeSensor.enabled;
    public bool HasSample { get; private set; }
    public float SamplingFrequency => IsAvailable ? activeSensor.samplingFrequency : 0f;
    public Quaternion Attitude { get; private set; } = Quaternion.identity;

    private void OnEnable()
    {
        attemptsWithoutSample = 0;
        lastMotionPermission = WebMotionPermission.MotionState;
        lastOrientationPermission = WebMotionPermission.OrientationState;
        TryConnect();
    }

    private void Update()
    {
        // An asynchronous permission result should not wait for the slow retry.
        if (lastMotionPermission != WebMotionPermission.MotionState ||
            lastOrientationPermission != WebMotionPermission.OrientationState)
        {
            lastMotionPermission = WebMotionPermission.MotionState;
            lastOrientationPermission = WebMotionPermission.OrientationState;
            attemptsWithoutSample = 0;
            nextConnectionAttemptTime = Time.unscaledTime;
        }
        bool orientationBlocked = ActiveSource == AttitudeSource.Attitude && !CanUseAttitude();
        if (orientationBlocked || !IsEnabled || Time.unscaledTime - lastSampleTime >= 1f)
        {
            HasSample = false;
            if (Time.unscaledTime >= nextConnectionAttemptTime)
            {
                // Skip a silent source for this attempt, otherwise an enabled but
                // non-reporting attitude device would starve the gravity fallback.
                TryConnect(IsEnabled ? activeSensor : null);
            }
        }

        if (!IsEnabled || (ActiveSource == AttitudeSource.Attitude && !CanUseAttitude()) ||
            !activeSensor.wasUpdatedThisFrame)
        {
            return;
        }

        switch (ActiveSource)
        {
            case AttitudeSource.Attitude:
                Quaternion sample = ((AttitudeSensor)activeSensor).attitude.ReadValue();
                float magnitude = Quaternion.Dot(sample, sample);
                if (!IsFinite(magnitude) || magnitude < 0.0001f) return;
                Attitude = Quaternion.Normalize(sample);
                break;
            case AttitudeSource.Gravity:
                Vector3 gravity = ((GravitySensor)activeSensor).gravity.ReadValue();
                if (!IsUsableVector(gravity)) return;
                ApplyGravity(gravity);
                break;
            case AttitudeSource.Accelerometer:
                Vector3 acceleration = ((Accelerometer)activeSensor).acceleration.ReadValue();
                if (!IsUsableVector(acceleration)) return;
                smoothedGravity = smoothedGravity == Vector3.zero
                    ? acceleration
                    : Vector3.Lerp(smoothedGravity, acceleration, gravityFilterAlpha);
                if (!IsUsableVector(smoothedGravity)) return;
                ApplyGravity(smoothedGravity);
                break;
            default:
                return;
        }

        HasSample = true;
        lastSampleTime = Time.unscaledTime;
        attemptsWithoutSample = 0;
        nextConnectionAttemptTime = Time.unscaledTime + 1f;
        if (lastReportedSource != ActiveSource)
        {
            Debug.Log($"Attitude source: {ActiveSource} (sample received)", this);
            lastReportedSource = ActiveSource;
        }
    }

    private void OnDisable()
    {
        if (IsEnabled)
        {
            InputSystem.DisableDevice(activeSensor);
        }

        activeSensor = null;
        ActiveSource = AttitudeSource.None;
        smoothedGravity = Vector3.zero;
        Attitude = Quaternion.identity;
        HasSample = false;
        attemptsWithoutSample = 0;
        lastReportedSource = AttitudeSource.None;
        connectionWarningLogged = false;
    }

    private void TryConnect(Sensor silentSensor = null)
    {
        attemptsWithoutSample++;
        // Try all three sources promptly, then leave time for a late sample.
        // Keep a bounded retry so sensors can recover after focus/permission changes.
        nextConnectionAttemptTime = Time.unscaledTime + (attemptsWithoutSample >= 3 ? 5f : 1f);
        // Give the last fallback a chance before retrying a silent attitude
        // source, otherwise silent attitude/gravity devices could alternate forever.
        if (silentSensor is GravitySensor &&
            TryUse(Accelerometer.current, AttitudeSource.Accelerometer, silentSensor)) return;
        if (CanUseAttitude() && TryUse(FindAttitudeSensor(), AttitudeSource.Attitude, silentSensor)) return;
        if (TryUse(GravitySensor.current, AttitudeSource.Gravity, silentSensor)) return;
        if (TryUse(Accelerometer.current, AttitudeSource.Accelerometer, silentSensor)) return;
        // Keep retrying when permission is still pending or hardware appears later.
    }

    private bool TryUse(Sensor sensor, AttitudeSource source, Sensor silentSensor)
    {
        if (sensor == null || !sensor.added || sensor == silentSensor) return false;
        try
        {
            InputSystem.EnableDevice(sensor);
            if (!sensor.enabled) return false;
            sensor.samplingFrequency = requestedSamplingFrequency;
        }
        catch (System.Exception exception)
        {
            if (!connectionWarningLogged)
            {
                Debug.LogWarning($"Cannot enable {source} motion source: {exception.Message}", this);
                connectionWarningLogged = true;
            }
            return false;
        }

        if (activeSensor != sensor && IsEnabled) InputSystem.DisableDevice(activeSensor);
        activeSensor = sensor;
        ActiveSource = source;
        smoothedGravity = Vector3.zero;
        HasSample = false;
        lastSampleTime = Time.unscaledTime;
        return true;
    }

    private static bool CanUseAttitude()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return WebMotionPermission.OrientationState != WebMotionPermissionState.Denied &&
            WebMotionPermission.OrientationState != WebMotionPermissionState.Unsupported;
#else
        return true;
#endif
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsUsableVector(Vector3 value) =>
        IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) &&
        IsFinite(value.sqrMagnitude) && value.sqrMagnitude > 0.01f;

    private void ApplyGravity(Vector3 gravity)
    {
        // Gravity gives tilt, not a unique yaw. GyroscopeAxis.Z (Euler roll) in
        // AttitudeCircleController is unreliable with this source; keep the
        // default horizontal=Y / vertical=X mapping. Verify inversion on device.
        Attitude = Quaternion.FromToRotation(gravity.normalized, Vector3.down);
    }

    private static AttitudeSensor FindAttitudeSensor()
    {
#if UNITY_EDITOR
        foreach (InputDevice device in InputSystem.devices)
        {
            if (device.remote && device is AttitudeSensor remoteAttitudeSensor)
            {
                return remoteAttitudeSensor;
            }
        }
#endif

        return AttitudeSensor.current;
    }
}
