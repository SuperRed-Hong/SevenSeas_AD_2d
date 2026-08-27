using UnityEngine;
using UnityEngine.InputSystem;

using InputGryroscepe = UnityEngine.InputSystem.Gyroscope;
public class MotionSensorTest : MonoBehaviour
{
    private void OnEnable()
    {
        if (InputGryroscepe.current != null)
        {
            InputSystem.EnableDevice(InputGryroscepe.current);
            InputGryroscepe.current.samplingFrequency = 60;
        }

        if (LinearAccelerationSensor.current != null)
        {
            InputSystem.EnableDevice(LinearAccelerationSensor.current);
            LinearAccelerationSensor.current.samplingFrequency = 60;
        }
    }

    private void Update()
    {
        if (InputGryroscepe.current != null)
        {
            Vector3 rotationSpeed =
                InputGryroscepe.current.angularVelocity.ReadValue();

            Debug.Log($"Gyroscope: {rotationSpeed}");
        }

        if (LinearAccelerationSensor.current != null)
        {
            Vector3 acceleration =
                LinearAccelerationSensor.current.acceleration.ReadValue();

            Debug.Log($"Acceleration: {acceleration}");
        }
    }

    private void OnDisable()
    {
        if (InputGryroscepe.current != null)
            InputSystem.DisableDevice(InputGryroscepe.current);

        if (LinearAccelerationSensor.current != null)
            InputSystem.DisableDevice(LinearAccelerationSensor.current);
    }
}