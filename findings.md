# Findings

- Project uses Unity 6000.3.23f1 and Input System 1.20.0.
- Scene1 already contains user changes and must be inspected before wiring.
- A user-created `Assets/Scripts/Help/GyroscopeReader.cs` exists.
- Existing motion-sensor test code previously mixed gyroscope and linear acceleration; this task remains gyroscope-only.
- `GyroscopeReader.OnEnbale()` is misspelled, so Unity never enables or initializes the sensor.
- The separate duplicate `MotionSensorTest` object has been removed from Scene1, but Circle still carries the old `MotionSensorTest` component.
- Circle is currently parented to Main Camera at local Z 10; setup should detach it while preserving world position.
- Unity 6000.3.23f1 is open on Scene1, so scene wiring can be performed through the Editor instead of hand-editing YAML.
