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
- Runtime verification also passed for the pre-calibrated entry path: calibration performed in MainMenu survived the scene transition, the mandatory panel stayed closed, and lane control used the shared neutral attitude.
- The student pushed the completed calibration and lane-controller work to the remote repository.
- At the student's request, lane variant B and its A/B feel comparison were deferred until a later tuning pass without removing them from M2.
- Advanced to the next M2 slice: connect the existing gyroscope flick detector to `ReadyToCast`, capture lane X and cast power, then enter `Casting`.
- Added a persistent `GyroscopeReader` to `AppRoot` and runtime-injected it into the fishing-scene `CastGestureDetector` while keeping its tuning profile as scene-assigned data.
- Student subscribed `FishingLoopController` to `CastDetected`, captured `CurrentLaneX` plus cast power, transitioned to `Casting`, and disabled the detector outside `ReadyToCast`.
- Installed and configured Unity Android Logcat, filtered to package `com.DefaultCompany.urp_2d`, and observed the device log `Cast selected: lane X = -0.37, power = 1.00`.
- Combined-input regression passed: ordinary lane tilting did not cast, a deliberate forward flick cast once, and subsequent flicks in `Casting` were ignored.
- Corrected cast direction with `InvertAxis`; retained the existing 1.25 rad/s trigger threshold because runtime testing showed no ordinary-tilt false positives after the direction fix.
- M2's playable lane-plus-cast path is complete. Variant B and the formal A/B feel comparison remain explicitly deferred; began M3 at its required think-first stage.
- M3 think-first and event-ownership discussion completed: the flight component reports landing, while `FishingLoopController` owns the transition to `Baiting`.
- Paused the planned reuse after the student identified a real orientation mismatch: the prototype flies horizontally, but the production fishing scene must cast vertically into the water. Recorded the need for a separate gameplay flight component and a quick Claude design-doc correction before treating the revised M3 as final.
- Student specified the future production flight as a vertical 2D simulation of a 3D parabola and explicitly deferred its implementation to prioritize the remaining state-machine infrastructure. M3 remains open; started M4's think-first stage out of sequence by explicit student choice.
- Student clarified that M3 itself must still be completed now with a minimal representative flight. Returned from the premature M4 prompt to M3: build a separate vertical flat-flight controller, complete `Casting -> Baiting`, and defer only the final 2.5D parabolic trajectory presentation.
- Student implemented and wired the formal `FishingHookFlightController` with Docked/Flying/Landed lifecycle, vertical power-to-distance mapping, temporary linear interpolation, and `Landed(Vector2)`.
- Android device integration verified the full core sequence: ReadyToCast -> cast at lane X -0.05/power 1.00 -> Casting -> landing at (-0.05, 2.42) -> Baiting. The final 2.5D parabolic visual remains deferred without changing the established event contract.
- Completed the mandatory post-M3 checkpoint: the student chose to proceed to the next milestone without simplifying M4 or M6. Started M4 at its required think-first stage.
- Completed M4 think-first and architecture discussion: individual fish own only Idle/Approaching/Hooked movement state, while one `FishBiteRaceController` owns the candidate race, deterministic winner, loser reset, and timeout.
- Student implemented `FishController`, runtime attraction-range discovery, approach movement, deterministic first-winner resolution, loser reset, and the eight-second timeout infrastructure.
- Student added `FishSpawner` for a configurable fish count at random non-overlapping positions inside a BoxCollider2D area, plus a `BasicFish` prefab on a dedicated `Fish` physics layer.
- Added and verified an Editor-only M4 Test Harness path that runs the current scene without AppRoot or phone sensors. The initial stationary-fish failure was diagnosed from serialized data: both `BasicFish` and the race LayerMask excluded the `Fish` layer. After correcting both, range candidates were discovered and fish approached the bait.

## 2026-09-01

- Began a second keyboard/mouse control path using Unity's Input System without duplicating the fishing state machine. `FishingInputSource` carries semantic Move, Cast, Strike, and Accelerate intentions; the keyboard implementation references the `Fishing` action map.
- Bound A/D through the Move action and Space to state-gated Cast, Strike, and Accelerate actions. Cast uses a one-frame press event, while later acceleration will use held state with a release-before-accelerate guard after Strike.
- Repurposed the M4-only direct-play Harness into a full-loop Editor entry: it now bypasses only Bootstrap/calibration prerequisites and no longer disables gameplay controllers, repositions the hook, or calls `BeginRace()` directly.
- Made `FishingLoopController.Start()` tolerate missing `AppRoot` during direct Editor play while preserving gyroscope reader injection when Bootstrap is present.
- Diagnosed failed Editor A/D movement: `ShoreLaneController.Update()` returned when both lane limits were valid because the null check was inverted; the scene also lacked the saved input-source reference.
- Rejected the attempted absolute/virtual target keyboard scheme after playtesting showed poor stopping control. The student explicitly chose velocity control for both keyboard and mobile attitude: input magnitude controls speed, neutral input means immediate stop.
- Replaced the final lane mapping with frame-rate-independent velocity integration plus world-space boundary clamping. Editor A/D verification passed: releasing the key stops accurately at the current position.
- Moved the non-mobile calibration bypass into `FishingSceneEntryGuard`, where entry prerequisites belong. Editor/desktop now start gameplay without posture calibration; the mobile path still requires the persistent calibration service.
- The former M4 Harness is no longer required for full-loop Editor testing because the normal scene entry and state machine now support the keyboard path directly.
- Full Editor integration passed: A/D moved and stopped precisely, Space cast once, the hook landed, nearby fish responded in Baiting, and the bite path advanced to Striking.
