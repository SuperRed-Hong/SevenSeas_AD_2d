using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AttitudeReader : MonoBehaviour
{
    [SerializeField, Min(1f)]
    private float requestedSamplingFrequency = 60f;

    private AttitudeSensor attitudeSensor;
    private float nextConnectionAttemptTime;

    public bool IsAvailable => attitudeSensor != null;
    public bool IsEnabled => attitudeSensor != null && attitudeSensor.enabled;
    public bool HasSample { get; private set; }
    public float SamplingFrequency => attitudeSensor != null ? attitudeSensor.samplingFrequency : 0f;
    public Quaternion Attitude { get; private set; } = Quaternion.identity;

    private void OnEnable()
    {
        TryConnect();
    }

    private void Update()
    {
        if (attitudeSensor == null)
        {
            HasSample = false;

            if (Time.unscaledTime >= nextConnectionAttemptTime)
            {
                TryConnect();
            }

            return;
        }

        if (!attitudeSensor.wasUpdatedThisFrame)
        {
            return;
        }

        Quaternion sample = attitudeSensor.attitude.ReadValue();

        if (Quaternion.Dot(sample, sample) < 0.0001f)
        {
            return;
        }

        Attitude = Quaternion.Normalize(sample);
        HasSample = true;
    }

    private void OnDisable()
    {
        if (attitudeSensor != null)
        {
            InputSystem.DisableDevice(attitudeSensor);
        }

        attitudeSensor = null;
        Attitude = Quaternion.identity;
        HasSample = false;
    }

    private void TryConnect()
    {
        nextConnectionAttemptTime = Time.unscaledTime + 1f;
        attitudeSensor = FindAttitudeSensor();

        if (attitudeSensor == null)
        {
            return;
        }

        InputSystem.EnableDevice(attitudeSensor);
        attitudeSensor.samplingFrequency = requestedSamplingFrequency;
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
