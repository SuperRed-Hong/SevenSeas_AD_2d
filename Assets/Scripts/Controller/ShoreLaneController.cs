using System;
using UnityEngine;
using UnityEngine.Serialization;

public class ShoreLaneController : MonoBehaviour
{
    private AttitudeReader attitudeReader;
    private AttitudeCalibrationService calibrationService;
    [SerializeField] private Transform leftLaneLimit;
    [SerializeField] private Transform rightLaneLimit;
    [SerializeField] private FishingInputSource inputSource;
    [SerializeField] private bool invertLane;
    [FormerlySerializedAs("deadZoneDegress")] [SerializeField, Min(0f)] private float deadZoneDegrees = 3f;


    [SerializeField, Range(1f, 90f)] [Tooltip("The maximum angle the object is allowed to tilt, in degrees.")]
    private float maxTiltDegrees = 25f;

    [SerializeField, Range(45f, 89f)] [Tooltip("The maximum tilt angle the object can support, in degrees.")]
    private float maxSupportedTiltDegrees = 75f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum horizontal movement speed in world units per second.")]
    private float maxLaneSpeed = 4f;
    


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
        if (leftLaneLimit == null && rightLaneLimit == null)
        {
            Debug.unityLogger.Log("Left Lane limit is null or empty");
            return;
        }
        
        // Platform-specific movement is resolved by FishingInputRouter.
        // This also lets mobile WebGL fall back from tilt to touch buttons.
        if (inputSource != null)
        {
            IsTiltWithinSupportedRange = true;
            ApplyLaneVelocity(inputSource.MoveInput.x);
            return;
        }

        // Legacy fallback for scenes that have no input source assigned.
        if (attitudeReader == null ||
            !attitudeReader.IsEnabled ||
            !attitudeReader.HasSample ||
            calibrationService == null ||
            !calibrationService.IsCalibrated)
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

        ApplyLaneVelocity(normalizedTilt);
    }
    private void ApplyLaneVelocity(float normalizedInput)
    {
        //Treat the normalized input as a velocity multiplier.
        // -1 is full speed left , 0 stops, and 1 is full speed right.
        
        float ClampedVelocity = Mathf.Clamp(normalizedInput, -1, 1);
        
        Vector3 nextPosition = transform.position;
        nextPosition.x += ClampedVelocity * maxLaneSpeed * Time.deltaTime;
        
        // Keep the player inside the configured lane Limits.
        float minimumX = Mathf.Min(leftLaneLimit.position.x, rightLaneLimit.position.x);
        
        float maximumX = Mathf.Max(leftLaneLimit.position.x, rightLaneLimit.position.x);
        
        nextPosition.x = Mathf.Clamp(nextPosition.x, minimumX, maximumX);
        
        transform.position = nextPosition;
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
