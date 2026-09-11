using UnityEngine;

// Tilt drives velocity, not position, so a stale neutral pose makes the player
// drift forever instead of merely sitting off-centre. Telling intent from a
// posture change needs motion, not angle: someone steering holds a tilt for a
// moment, while someone who sat back holds the new pose indefinitely. A device
// that has stopped moving yet still reads as deflected is therefore a posture
// change, and that is the only case this raises.
[RequireComponent(typeof(AttitudeReader))]
[RequireComponent(typeof(AttitudeCalibrationService))]
public sealed class AttitudePostureWatcher : MonoBehaviour
{
    [SerializeField, Min(0f)]
    [Tooltip("低于这个角速度（度/秒）才算设备静止。手持的自然抖动通常在 2~5 度/秒之间。")]
    private float stillThresholdDegreesPerSecond = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("偏离中性点超过这个角度才算在漂移。要比移动死区大，否则传感器噪声会误触发。")]
    private float driftThresholdDegrees = 6f;

    [SerializeField, Min(0.1f)]
    [Tooltip("设备静止且持续漂移多久之后，判定玩家换了姿势（秒）。")]
    private float holdSeconds = 3f;

    [SerializeField]
    [Tooltip("勾上则直接把当前姿态设为新中性点，不打扰玩家。默认只提示，由玩家决定。")]
    private bool autoRecenter;

    private AttitudeReader reader;
    private AttitudeCalibrationService calibration;
    private Quaternion previousAttitude = Quaternion.identity;
    private bool hasPrevious;
    private float stillElapsed;

    public bool NeedsRecalibration { get; private set; }
    public event System.Action RecalibrationSuggested;

    // Call once the player has been told, or has recalibrated another way.
    public void Acknowledge()
    {
        NeedsRecalibration = false;
        stillElapsed = 0f;
    }

    private void Awake()
    {
        reader = GetComponent<AttitudeReader>();
        calibration = GetComponent<AttitudeCalibrationService>();
    }

    private void OnEnable()
    {
        calibration.Completed += Acknowledge;
        hasPrevious = false;
        Acknowledge();
    }

    private void OnDisable()
    {
        calibration.Completed -= Acknowledge;
    }

    private void Update()
    {
        if (!calibration.IsCalibrated || !reader.HasSample ||
            calibration.State == AttitudeCalibrationState.Calibrating)
        {
            hasPrevious = false;
            stillElapsed = 0f;
            return;
        }

        Quaternion current = reader.Attitude;
        float delta = Time.unscaledDeltaTime;
        if (!hasPrevious || delta <= 0f)
        {
            previousAttitude = current;
            hasPrevious = true;
            return;
        }

        // Derived from the attitude itself rather than the gyroscope device, so
        // this still works on the gravity and accelerometer fallback sources.
        float speed = Quaternion.Angle(previousAttitude, current) / delta;
        previousAttitude = current;

        float drift = Quaternion.Angle(calibration.NeutralAttitude, current);
        if (speed > stillThresholdDegreesPerSecond || drift <= driftThresholdDegrees)
        {
            stillElapsed = 0f;
            return;
        }

        if (NeedsRecalibration) return;

        stillElapsed += delta;
        if (stillElapsed < holdSeconds) return;

        if (autoRecenter)
        {
            calibration.AdoptCurrentAsNeutral();
            return;
        }

        NeedsRecalibration = true;
        RecalibrationSuggested?.Invoke();
    }
}
