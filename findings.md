# Fishing Loop Findings

## Confirmed before implementation

- `CastTuningProfile.TriggerThreshold` is currently 1.25 rad/s, but that value was tuned with flick input in isolation. In M2 it must be re-measured while normal lane-selection tilting is active and raised as needed so ordinary tilt does not trigger a cast.
- The approved loop has six states: `ReadyToCast`, `Casting`, `Baiting`, `Striking`, `Reeling`, and `GameOver`.
- `AttitudeReader` supplies absolute rotational orientation as a `Quaternion`; `GyroscopeReader` supplies angular velocity as a signed `Vector3` in rad/s. They serve different gameplay intentions and are not interchangeable.
- The existing sensor prototypes follow a reader -> controller -> view/HUD split. New gameplay controls should preserve that separation where practical.
- `CastBallController.Landed` should remain a generic event. The fishing-loop state machine will reinterpret landing as the transition from `Casting` to `Baiting`.
- A reversed strike flick can likely reuse a second `CastGestureDetector` with a second profile whose `InvertAxis` value is flipped.
- Cast and strike detectors must only be enabled in their relevant fishing-loop states. Otherwise the strike detector could enter cooldown before `Striking`; `CastGestureDetector.OnEnable()` already resets its state.
- The M4 fish race needs one authority to accept the winning bite so two fish cannot independently claim it in the same frame.
- Rock collision and maximum tension intentionally share the same attempt-failure consequence: lose one hook and end the attempt.

## Scope and process constraints

- Codex coaches only. The student writes all C# and performs all Unity Editor and Inspector work.
- M0 stays limited to sensor orientation: reader/controller/view and orientation-versus-angular-velocity. Thresholds, filtering, and gesture-state details wait for M2/M5.
- M0–M3 are the stability floor. A mandatory time check happens after M3.
- M4 and M6 may be simplified only after the student explicitly chooses to do so.
- Any simplification must be returned to Claude for a design-document update before it becomes the implementation target.
- Placeholder content for the full MVP is two fish and one or two rocks.

## Implementation discoveries

- M1 architecture decision: use one `FishingLoopState` enum, centralized `switch` dispatch, and explicit Enter/Update/Exit method boundaries. Keep the controller as an orchestrator that delegates detailed gameplay to small components. Extract state classes later only if real state-owned complexity, reuse, replacement, or isolated-test requirements appear.
- State Pattern does not automatically make Unity code testable; testability depends on isolating rules from `MonoBehaviour`, `Time`, Transform, collision, and sensor dependencies.
- Project convention keeps component-specific enums beside their controller (`CastDetectionState`, `GyroscopeAxis`, `CastSensorAxis`), while broadly shared scene identity uses the separate `Enums` directory.
- M1 runtime verification succeeded: the controller entered `ReadyToCast`, then produced ordered Exit/Enter pairs through `Casting`, `Baiting`, `Striking`, `Reeling`, and finally `GameOver`, where automatic advancement stopped.
- Rider's “expensive method invocation” marker propagated from `Update()` through the debug transition call chain because `EnterState()`/`ExitState()` call `Debug.Log`; this is informational and acceptable for infrequent state transitions.
- `Time.unscaledTime` was used only by the temporary M1 harness to compare an absolute, time-scale-independent deadline; the harness was removed after verification.
- M2 concept checkpoint passed: the student connected dead zone to sensor noise, axis hysteresis to preventing horizontal/vertical flapping, and time-based exponential smoothing to frame-rate-independent response.
- The student explicitly deferred the `AttitudeControlTest.unity` phone/HUD observation until the M2 control-tuning/debug pass to prioritize infrastructure. This is a scheduling change, not a design simplification; the verification remains required before M2 is complete.
- The resumed Play Mode test made all three effects observable: a 10-degree dead zone delayed movement, 10-degree axis hysteresis held the current axis, and smoothing values 2 versus 30 produced slow versus near-immediate following.
- `GyroscopeDebugHUD` is an independent angular-velocity display, not a display of the attitude angles driving the Circle. The student relabeled its rows as `X-PITCH`, `Y-YAW`, and `Z-ROLL` while preserving the underlying x/y/z data mapping.
- After comparing the temporary values with the defaults, the student explicitly chose to keep `axisSwitchHysteresisDegrees: 10` and `smoothing: 30` because that response felt better. These are now intentional tuning values, not unrestored test overrides.
- M2 lane architecture decision: create a new single-purpose `ShoreLaneController` rather than generalizing the 2D screen-oriented `AttitudeCircleController`. It keeps calibration, Yaw extraction, dead zone, normalization, smoothing, and shore-range mapping while dropping Pitch/Roll, dominant-axis switching, and camera-screen mapping.
- Preserve the generic `CastGestureDetector.CastDetected(power)` contract. When it fires, the fishing-loop orchestrator combines the current lane X with power and passes/snapshots both at the `Casting` boundary; the detector itself does not gain a dependency on the shore character.
- The student requested a formal M2 A/B comparison instead of assuming the existing position-mapped control is best. Variant A maps normalized tilt to an absolute target lane position; variant B will map normalized tilt to signed movement velocity, so neutral input stops at the current position. Both variants must share the same calibration, dead-zone, supported-angle, inversion, and normalization behavior; only their final movement mapping should differ, and only one may be enabled at a time.
- `Mathf.Lerp(leftX, rightX, t)` in variant A is a stateless range conversion, not temporal smoothing. The later `Vector3.Lerp(current, target, blend)` is the part that adds response lag. Setting smoothing to zero selects immediate absolute-position mapping, not constant-speed movement.
- Read-only review of variant A found its Yaw-to-position, dead-zone, supported-angle, range-mapping, and frame-rate-independent smoothing pipeline complete and compiling. The functional blocker is that `Calibrate()` is private and never called internally, so `IsCalibrated` can never become true; it must be public for the planned UI button and should reset `IsTiltWithinSupportedRange` on success.
- `ShoreLaneController` currently has no scene or prefab reference, so variant A still requires attachment to the shore player, left/right limit objects and Inspector references, an explicit calibration button/status UI, and Play Mode verification. Its only code-specific build warning is the deprecated `FindObjectOfType<T>()`; `FindFirstObjectByType<T>()` is the current project-compatible replacement.
- Calibration scope expanded by explicit student request: both the main menu and an in-game pause menu must be able to start calibration. The player holds a neutral pose for three stable seconds; movement beyond tolerance resets the countdown and UI explains that the player must hold still.
- Calibration survives scene changes and new fishing attempts during the current application run, but intentionally resets after the application closes. This is session-lifetime state, not disk-saved player data.
- Existing `AppRoot` is the project precedent and composition root for cross-scene services because it already uses `DontDestroyOnLoad`. A focused motion-calibration service should own the neutral attitude; scene UI and gameplay controllers consume it instead of keeping competing copies.
- Future cross-scene data should be separated by lifecycle and responsibility rather than accumulated in one generic `GameManager` data bag: session services, per-run state, and disk-persisted settings/progress have different reset and storage rules.
- For the tightly clustered samples collected during a stable three-second hold, the planned practical quaternion average is: align sample signs against a reference quaternion using `Quaternion.Dot`, sum components, then normalize. Movement/spread is rejected before averaging. The full Markley eigenvector solution is unnecessary for this coursework-sized input cluster.
- Pause-menu calibration must use unscaled time because gameplay may set `Time.timeScale` to zero.
- The reusable `MotionCalibrationPanel` prefab is scene-local UI backed by the persistent service. MainMenu exposes optional manual calibration/recalibration; `FishingLoopTest` contains the same prefab as a mandatory fallback with its close button disabled only on that scene instance.
- `FishingSceneEntryGuard` owns the pre-game invariant rather than `SceneNavigationButton` or `FishingLoopController`: it disables `GameplayRoot`, opens calibration when required, and enables gameplay only after calibration. This protects every route into the gameplay scene without adding a non-gameplay state to the six-state fishing loop.
- `ShoreLaneController` no longer owns `neutralAttitude`, `IsCalibrated`, or `Calibrate()`. It reads the persistent `AttitudeReader` and `AttitudeCalibrationService` through `AppRoot`, freezes during calibration, and consumes the shared averaged neutral attitude afterward.
- `FishingLoopTest.unity` is now the single end-to-end placeholder gameplay scene and `SceneCatalog` maps `GameScene` to `FishingLoopTest` instead of the missing legacy `Scene1`.
- Runtime tuning selected `movementToleranceDegrees = 8` for the three-second neutral-pose hold. The original 3-degree default reset too easily during natural hand movement; 8 degrees felt reliable without making deliberate movement appear stable.
