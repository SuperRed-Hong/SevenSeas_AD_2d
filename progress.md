# Fishing Loop Progress

## 2026-08-29

- Read `docs/design/2026-08-29-fishing-loop-design.md` in full, starting with its Process note.
- Read `docs/design/2026-08-29-fishing-loop-teaching-plan.md` in full.
- Read `docs/reference/motion-input-guide.md` in full, including the sections reserved for later M2 and M5 teaching.
- Confirmed the non-negotiable coaching boundary: Codex will not edit C# files or Unity scenes; the student types and wires everything.
- Started M0 and asked the orientation-versus-angular-velocity thinking prompt.
- Paused M0 before explanation when the student noticed Step 0 had not yet been performed.
- Repurposed `task_plan.md`, `findings.md`, and `progress.md` from the old gyroscope-HUD task into living fishing-loop records.
- Marked Step 0 complete and M0 in progress, awaiting the student's own answer to the thinking prompt.
- Completed M0 after the student correctly distinguished persistent attitude from angular velocity that returns to zero when rotation stops.
- Clarified the data direction (`sensor -> reader -> controller -> view`) and that the existing attitude prototype maps tilt to a target position rather than unbounded movement.
- Started M1 at its think-first stage; no implementation instructions or code have been given yet.
- M1 think-first attempt completed: the student proposed dispatching `Update()` through a `switch` on the current state.
- Read the existing `CastGestureDetector` and `CastTestController` as architecture precedent without modifying either file.
- Began M1 architecture discussion; implementation has not started.
- Completed the M1 architecture discussion: selected enum + switch with explicit state lifecycle methods and small delegated components, while preserving a mechanical extraction path to State Pattern if evidence justifies it.
- Created and indexed the Obsidian note `游戏开发-Unity-State Pattern与可演进状态机架构.md`; verified its metadata, related-note links, and both Unity/root index entries.
- Inspected existing enum placement conventions. Component-specific enums live beside their controller, so the fishing-loop enum will begin in `FishingLoopController.cs`.
- M1 implementation is ready to begin with the student typing the six-state enum; Codex has not edited any C# or scene file.
- Student created `FishingLoopController.cs` and correctly typed all six `FishingLoopState` values.
- Read-only inspection confirmed chat formatting artifacts were not present in the actual C# file.
- Treated the student's request for the next step as confirmation that Unity compiled the enum without red Console errors.
- Student completed the enum + switch controller skeleton with state-specific Enter/Update/Exit hooks and centralized transitions.
- `Assembly-CSharp.csproj` compiled with 0 errors; unrelated Unity package reference conflicts produced 3 warnings.
- Confirmed the script initially had no scene or prefab reference; the student then attached it in Unity for Play Mode verification.
- Console verification passed with ordered transitions from `ReadyToCast` through `GameOver`, one second apart, and no transition after `GameOver`.
- M1 has one cleanup step remaining: remove the temporary timed auto-advance harness before beginning M2.
- Student removed the temporary timed auto-advance fields, checks, and method after learning how `Time.unscaledTime` works.
- Verified the debug harness is absent, `Start()` only enters `ReadyToCast`, and `Assembly-CSharp.csproj` builds with 0 errors.
- Marked M1 complete and started M2 at the required attitude-code deep-dive stage.
- Completed the M2 conceptual checkpoint on dead zone, axis-lock hysteresis, and frame-rate-independent exponential smoothing.
- At the student's explicit request, deferred the `AttitudeControlTest.unity` phone/HUD observation until M2's control-tuning/debug pass; it remains an open M2 verification item.
- Advanced to M2's one-axis shore-lane controller thinking prompt before architecture or implementation guidance.
- The student reconsidered the deferral and resumed the planned `AttitudeControlTest.unity` Play Mode observation before lane-controller design.
- Play Mode observations confirmed dead-zone filtering, axis-switch hysteresis, and the visible response difference between low and high smoothing values.
- The student updated `GyroscopeDebugHUD` labels to show the device-axis/rotation-name mapping (`X-PITCH`, `Y-YAW`, `Z-ROLL`) in both online and offline display paths.
- Read-only inspection found the temporary test values persisted in the scene (`axisSwitchHysteresisDegrees = 10`, `smoothing = 30`), so restoration to 2 and 12 remains the immediate cleanup step.
- The student explicitly chose to retain hysteresis 10 and smoothing 30 as preferred tuning values; the M2 attitude deep dive and Play Mode observation checkpoint are complete.
- Started the one-axis shore-lane controller think-first stage.
- Completed the M2 lane think-first and architecture discussion. Selected a new `ShoreLaneController` plus a combined lane-X/power snapshot at the transition to `Casting`, without changing the generic cast detector event.
- Began learner-written implementation of the lane controller.

## Verification log

- Verified `task_plan.md` contains Step 0 plus M0–M8 with M0 in progress and M1–M8 not started.
- Verified no stale August 27, gyroscope-driven Circle, or Scene1 task markers remain in the three planning files.
- Verified `git diff --check` reports no whitespace errors in the three planning files.

## 2026-08-30

- Continued learner-written implementation of `ShoreLaneController` variant A and worked through the quaternion-relative-attitude, rotated-forward-vector, supported-angle guard, normalization, range mapping, and exponential-smoothing math.
- Changed calibration UX by student request: calibration will be an explicit player action via UI rather than an implicit delayed `OnEnable()` capture; the calibrated neutral pose persists across fishing-state re-enables and can be replaced by a later recalibration action.
- Added an observable supported-tilt flag so the future UI can explain when extreme orientation pauses lane input instead of failing silently.
- Added a planned variant B and A/B test: compare absolute position targeting against tilt-controlled velocity before choosing the final M2 lane behavior.
- Read-only reviewed the learner-written `ShoreLaneController` variant A and built `Assembly-CSharp.csproj`: 0 errors and 4 warnings (three existing Unity/package reference conflicts plus one deprecated `FindObjectOfType<T>()` call in the new controller).
- Confirmed the movement algorithm is present, but manual calibration is currently unreachable because `Calibrate()` is private and has no caller; the script is also not yet referenced by any scene or prefab.
- Student expanded calibration UX to two entry points: main menu and pause menu. Calibration now requires a three-second stable hold and an averaged neutral attitude rather than a one-frame snapshot.
- Confirmed lifecycle choices: calibration persists across scenes and new attempts for the current application run, resets on application restart, and restarts its countdown with `Hold still` feedback if movement exceeds tolerance.
- Identified the existing persistent `AppRoot` as the composition root for `AttitudeReader` and a focused motion-calibration service. `ShoreLaneController` will consume shared calibration rather than own it.
- Student implemented `AttitudeCalibrationService`: three-second unscaled-time hold, fixed-rate sample collection, movement-triggered restart, hemisphere-aligned quaternion component averaging, normalization, cancellation, and session-lifetime neutral attitude.
- Added persistent `AttitudeReader` and `AttitudeCalibrationService` components to `AppRoot`; verified they survive the Bootstrap-to-MainMenu scene transition without duplication.
- Student created a reusable `MotionCalibrationPanel` prefab with progress/status UI, manual calibrate/recalibrate, open/close controls, and a MainMenu entry button.
- Student created `FishingLoopTest.unity`, mapped `GameScene` to it, built the placeholder shore lane, attached the fishing loop and lane controller, and replaced the lane controller's local calibration state with the shared service.
- Student implemented and wired `FishingSceneEntryGuard`, keeping calibration entry outside the six-state fishing loop and preventing `GameplayRoot` from starting until requirements are satisfied.
- Runtime verification passed for the uncalibrated entry path: Play loaded `FishingLoopTest`, the mandatory panel opened with gameplay disabled, three-second calibration completed, the panel closed, gameplay activated, `ReadyToCast` began, and tilt movement worked smoothly.
- Student tuned the calibration movement tolerance from 3 degrees to 8 degrees after device testing; the three-second hold now feels reliable and was accepted as the current value.
