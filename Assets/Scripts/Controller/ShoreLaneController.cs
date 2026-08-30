using System;
using UnityEngine;
using UnityEngine.Serialization;

public class ShoreLaneController : MonoBehaviour
{
    private AttitudeReader attitudeReader;
    private AttitudeCalibrationService calibrationService;
    [SerializeField] private Transform leftLaneLimit;
    [SerializeField] private Transform rightLaneLimit;

    [SerializeField] private bool invertLane;
    [FormerlySerializedAs("deadZoneDegress")] [SerializeField, Min(0f)] private float deadZoneDegrees = 3f;


    [SerializeField, Range(1f, 90f)] [Tooltip("The maximum angle the object is allowed to tilt, in degrees.")]
    private float maxTiltDegrees = 25f;

    [SerializeField, Range(45f, 89f)] [Tooltip("The maximum tilt angle the object can support, in degrees.")]
    private float maxSupportedTiltDegrees = 75f;

    /// <summary>
    /// The smoothing rate applied to tilt changes.
    /// Higher values produce a faster response.
    /// </summary>
    [SerializeField, Min(0f)]
    [Tooltip("Controls how quickly tilt changes are smoothed. Higher values produce a faster response.")]
    private float smoothing = 30f;
    //[SerializeField, Min(0f)] private float autoCalibrationDelay = 0.5f;

    public float CurrentLaneX => transform.position.x;




    //private float calibrationReadyTime;


    public bool IsTiltWithinSupportedRange { get; private set; } = true;

    private void Awake()
    {
        if (AppRoot.Instance == null)
        {
            return;
        }

        attitudeReader = AppRoot.Instance.AttitudeReader;
        calibrationService = AppRoot.Instance.AttitudeCalibration;
    }


    private void Update()
    {
        if (attitudeReader == null || 
            !attitudeReader.IsEnabled || 
            !attitudeReader.HasSample ||
            calibrationService == null ||
            !calibrationService.IsCalibrated ||
            leftLaneLimit == null ||
            rightLaneLimit == null)
        {
            return;
        }

        // convert the absolute attitude into a rotation relative to the calibrated neutral pose.
        // Quaternion equivalent of "current - neutral" : inverse(neutral) * current
        Quaternion relativeAttitude =
            Quaternion.Inverse(calibrationService.NeutralAttitude) *
            attitudeReader.Attitude;

        // Apply the relative rotation to the phone's forward direction
        //This makes it easier to calculate the tilt angle from a direction vector.
        // Rotate the neutral forward vector by the relative attitude.
        // The resulting X and Z components can be used to calculate yaw.
        Vector3 relativeForward = relativeAttitude * Vector3.forward;


        // Replace the old Vector3.Angle check with this block.
        float tiltFromNeutral =
            Vector3.Angle(Vector3.forward, relativeForward);
        IsTiltWithinSupportedRange =
            tiltFromNeutral <= maxSupportedTiltDegrees;

        // Reject unsupported orientations to prevent flipped or discontinuous yaw.
        // Keep the last valid lane position and expose the state for UI feedback.
        if (!IsTiltWithinSupportedRange)
        {
            return;
        }

        // Atan2(x, z) calculates the Y-axis yaw in radians.
        // Convert the result from radians to degrees.
        float laneTiltDegrees =
            Mathf.Atan2(relativeForward.x, relativeForward.z) * Mathf.Rad2Deg;


        if (invertLane)
        {
            laneTiltDegrees = -laneTiltDegrees;
        }

        float normalizedTilt = NormalizeTilt(laneTiltDegrees);

        //Convert [-1,1] into [0,1]
        // -1 represents the left limit, 0 the center, 1 the right limit
        float laneposition01 = (normalizedTilt + 1f) * 0.5f;

        //Interpolate between the two world-space lane limits
        // to calculate the target x position
        float targetX = Mathf.Lerp(
            leftLaneLimit.position.x,
            rightLaneLimit.position.x,
            laneposition01);


        Vector3 targetPosition = transform.position;
        targetPosition.x = targetX;

// Use frame-rate-independent exponential smoothing.
// High frame rates use smaller steps, while low frame rates use larger steps,
// producing approximately the same response speed per second.
        float blend = smoothing > 0f
            ? 1f - Mathf.Exp(-smoothing * Time.deltaTime)
            : 1f;
        transform.position =
            Vector3.Lerp(transform.position, targetPosition, blend);
    }

    private float NormalizeTilt(float degrees)
    {
        // Separate the tilt magnitude from its direction.
        float magnitude = Mathf.Abs(degrees);

        // Treat small inputs as hand movement or sensor noise.
        if (magnitude <= deadZoneDegrees)
        {
            return 0f;
        }

        // The usable range starts after the dead zone and ends at max tilt.
        // The minimum value prevents division by zero.
        float usableRange =
            Mathf.Max(0.001f, maxTiltDegrees - deadZoneDegrees);

        // Map the usable tilt range to [0, 1].
        float normalized =
            (magnitude - deadZoneDegrees) / usableRange;

        // Restore the original direction and clamp the final value to [-1, 1].
        return Mathf.Sign(degrees) *
               Mathf.Clamp01(normalized);
    }


}