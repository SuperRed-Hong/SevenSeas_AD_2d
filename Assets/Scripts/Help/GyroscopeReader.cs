using UnityEngine;
using UnityEngine.InputSystem;

using InputSystemGyroscope = UnityEngine.InputSystem.Gyroscope;

public sealed class GyroscopeReader : MonoBehaviour
{
    [SerializeField, Min(1f)]
    private float requestedSamplingFrequency = 60f;

    private InputSystemGyroscope gyroscope;
    private float nextConnectionAttemptTime;

    public bool IsAvailable => gyroscope != null;
    public bool IsEnabled => gyroscope != null && gyroscope.enabled;
    public float SamplingFrequency => gyroscope != null ? gyroscope.samplingFrequency : 0f;
    public string DeviceName => gyroscope != null ? gyroscope.displayName : "Unavailable";
    public Vector3 AngularVelocity { get; private set; }
    public float AngularSpeed => AngularVelocity.magnitude;
    public bool WasUpdatedThisFrame { get; private set; }

    private void OnEnable()
    {
        TryConnect();
    }

    private void Update()
    {
        if (gyroscope == null)
        {
            AngularVelocity = Vector3.zero;
            WasUpdatedThisFrame = false;

            if (Time.unscaledTime >= nextConnectionAttemptTime)
            {
                TryConnect();
            }

            return;
        }

        WasUpdatedThisFrame = gyroscope.wasUpdatedThisFrame;
        AngularVelocity = gyroscope.angularVelocity.ReadValue();
    }

    private void OnDisable()
    {
        if (gyroscope != null)
        {
            InputSystem.DisableDevice(gyroscope);
        }

        gyroscope = null;
        AngularVelocity = Vector3.zero;
        WasUpdatedThisFrame = false;
    }

    private void TryConnect()
    {
        nextConnectionAttemptTime = Time.unscaledTime + 1f;
        gyroscope = InputSystemGyroscope.current;

        if (gyroscope == null)
        {
            return;
        }

        InputSystem.EnableDevice(gyroscope);
        gyroscope.samplingFrequency = requestedSamplingFrequency;
    }
}
