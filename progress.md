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
- The student deferred M4 bait wiggle and requested M6 Reeling next. The one-dimensional bait-wiggle proposal remains blocked on a Claude design-doc update; no implementation divergence was made.
- The student immediately corrected the requested order before M6 implementation began: proceed with M5 Striking first, then M6 Reeling.
- Started the mandatory M5 detector deep dive using motion-input guide §5 and §9.2 plus the current `CastGestureDetector` and `CastTuningProfile` sources.
- Completed the M5 detector checkpoint: trigger gates deliberate intent, rearm hysteresis supports repeated use, and filtered peak protects Cast power from raw one-frame sensor spikes.
- Refined the Strike requirement during discussion: it should react immediately to the first valid opposite-direction threshold crossing, publish once, and then disable on the transition to Reeling rather than waiting for a Cast-style power peak.
- Added a generic immediate `GestureTriggered` event at the detector's Ready-to-Sampling boundary while preserving the existing delayed `CastDetected(power)` event for Cast power.
- Created the learner-written `StrikeController` skeleton direction and began separating Strike input/window rules from `FishingLoopController` orchestration.
- Paused further Strike-window implementation after the student replaced the simple reaction window with a shrinking concentric-ring skill check, including retry cooldown, red invalid-input flash, and camera shake. The revision is waiting for Claude to update the approved design and teaching plan.
- Re-read design revision 6, the rewritten M5 teaching plan, and motion-input guide §5/§9.2 after Claude's update. The radial Strike design is now approved and the design-update blocker is cleared.
- Read-only source review confirmed the immediate `GestureTriggered` event, inverted Strike profile, second state-gated detector, keyboard Strike event, and initial `StrikeController` skeleton already exist. The skeleton still reflects the superseded 0.7-second design, and the Strike profile still carries the cast detector's long sample/cooldown values; revised M5 resumes before either is expanded.
- Retuned and verified the saved Strike-only detector path: `SampleWindow = 0.05`, profile `CooldownDuration = 0`, and Strike detector display duration `0`; the original Cast detector retains its `0.2` display duration.
- Chose the M5 clock semantics: Striking freezes with gameplay pause, so its window/ring/cooldown use scaled delta time and paused sensor events must not be judged.
- Implemented the learner-written Strike timing core and transferred mobile/keyboard attempt ownership from `FishingLoopController` into `StrikeController`. Success enters Reeling; timeout releases the fish and returns to ReadyToCast, with hook deduction explicitly awaiting M7's tracker.
- Built a world-space `StrikeWindowHUD` with a thick LineRenderer target annulus and a thin shrinking ring anchored to the hook. After assigning a URP 2D sprite-unlit material so vertex colors render correctly, the base ring display passed Play Mode testing.
- Completed and tuned the rejected-attempt feedback: the HUD remains red for the controller-owned cooldown and a Cinemachine Impulse Source produces a short Rumble through a listener on `HookFollowCamera`.
- Preserved presentation separation by routing `StrikeController.AttemptRejected` through `FishingLoopController` to `FishingCameraController.PlayStrikeRejectedShake()`.
- Accepted the current camera presentation settings: HookFollow-to-Overview uses the student's preferred Hard Out 0.4-second blend, while rejected Strike attempts use a 0.15-second impulse with default velocity `(0.20, 0.12, 0)`.
- M5 Striking is complete. The next teaching checkpoint is M6's tension accumulator thinking prompt.
- Began M6 think-first discussion. The student defined the boundary behavior and requested speed-sensitive lateral-dodge tension in addition to Accelerate tension.
- Paused M6 code implementation pending a Claude design update because the current approved document explicitly says only Accelerate creates tension risk.
- Fully re-read fishing-loop design revision 8 and the rewritten M6 teaching-plan section in the required order.
- Cleared the former M6 movement-driven-tension design blocker and rewrote its living checklist to mirror all five think-first prompts, six architecture topics, and ten learner-written implementation steps.
- Reopened M5 only for the revision 8 data-location back-migration: its scene-component tuning values must move into a dedicated `StrikeWindowProfile` before M6 proceeds.
- The student confirmed the revision 8 profile rationale was already understood and explicitly requested a faster path without repeating the think-first questions. Marked that checkpoint complete, confirmed the data/runtime/presentation boundary, and advanced to the first learner-written migration step.
- Created and scene-wired `StrikeWindowProfile`; `StrikeController` now reads window duration, band radii, and attempt cooldown from the asset. At the student's explicit rapid-prototype time decision, wiring the profile's shake amplitude/duration and the final M5 regression were deferred without removing them from the approved scope.
- Resumed the main vertical slice at M6. No M6 mechanic has been silently removed; the teaching cadence will be compressed where prior reasoning already covers a prompt, while implementation remains learner-written and stepwise.
- M6 Step 1 complete: the learner added a scene-wired `ReelingController`; entering Reeling now moves the landed hook at a temporary constant speed toward `HookLaunchPoint`, stops at its Y coordinate, and leaves the formal shore-success transition for step 9.
- M6 Step 2 complete: Editor `MoveInput` drives frame-rate-independent lateral hook velocity within the shared lane limits while auto-retrieval continues. `ShoreLaneController` is disabled outside `ReadyToCast`, preventing the shore player from consuming the same enabled input during Reeling.
- Fixed the attached-fish presentation gap discovered during the Step 2 test: `FishController` now retains the winning hook transform and snaps to it in `LateUpdate()`, while reset clears the attachment without changing scene hierarchy.
- Diagnosed the premature Casting reset after adding hook physics: the newly active Rigidbody caused the hook's obsolete `PlayerHazard` component to receive the `Sea` object's `Hazard` trigger and reload the scene. The new `ReelingController.AttemptFailed` path was not responsible; `ReelingObstacle.cs` and its marker were not yet present in the saved project.
- Removed the obsolete `PlayerHazard` component from `FishingHook` and verified Casting no longer reloads the scene when the hook enters the `Sea` trigger. The new Reeling-only obstacle path remains to be completed and tested.
- M6 Step 3 complete: a marker-based `ReelingObstacle` path now reports one `AttemptFailed` event only while retrieval is active. Regression checks passed: Casting contact is ignored, Reeling contact releases the fish and returns to `ReadyToCast`, and dodging around the rock continues retrieval.
- M6 Step 4 complete: the learner measured actual post-clamp lateral movement rather than input intent. Logs show about `4 units/s` during full keyboard movement and `0` while continuing to push against a lane boundary.
- At the student's explicit request, Codex made a one-time implementation exception for the mechanical M6 profile migration: created `ReelTuningProfile` plus its asset, preserved retrieval `2` and dodge `4`, initialized tension thresholds from measured speed, replaced both prototype constants, and serialized the scene reference. The first command-line build could not see the new type because Unity had not yet refreshed its generated `.csproj`; Unity Refresh and a fresh build/Play test remain before Step 5 is complete.
- After Unity Refresh, the generated project includes `ReelTuningProfile.cs`; a fresh `Assembly-CSharp.csproj` build completed with 0 errors and the same 3 pre-existing assembly-version warnings. Scene GUID verification confirmed `ReelingController` references the new asset, so M6 Step 5 is complete.
- The student explicitly reprioritized the rapid prototype toward an end-to-end repeatable loop. M6's tension UI, snap outcome, and full tuning verification remain deferred rather than removed; shore arrival and the return to `ReadyToCast` are now the immediate target, with score storage still reserved for M7's `ScoreTracker`.

## 2026-09-03

- Read-only inspected the saved tension implementation and scene. `ReelingController` exposes `Tension01`, resets it at retrieval start/cancel, and already routes maximum tension through guarded `ReportAttemptFailed()`. No runtime verification was performed in this review.
- The student requested complete step-by-step teaching for the tension UI. Resumed M6 Step 7; asked the student to map tension 0.25 onto endpoints -100 and 100. Awaiting their answer before the first Unity setup step.
- The student explicitly requested continued maintenance of `task_plan.md`; confirmed ongoing synchronization with `progress.md` and `findings.md`. Updated M6's stale heading and snap status without claiming runtime completion.
- Inspected the student's saved Slider setup. Bottom-to-top direction, `0..1` range, disabled interaction, Fill/Handle Rect assignments, and foreground Frame ordering are present. The frame uses the intended attention-bar sprite. Before binding the HUD script, the renamed EventSystem parent must be separated back into an EventSystem and a plain `TensionBarHUD`; the Handle still uses Unity's default sprite.

## 2026-09-06 — Project collaboration agreement

- Created root AGENTS.md with project roles, design handoff, architecture boundaries, Unity editing discipline, and verification requirements.
- User explicitly selected collaborative implementation: Codex may directly edit code and scene wiring within requested tasks; important gameplay and design changes are discussed with the user first. Claude remains the primary design-document author and design reviewer.
- This replaces the historical learner-written-only default; teaching is now available on request. No gameplay code or scenes changed in this task.

## 2026-09-06 — Tension HUD and session cleanup

- Implemented and wired TensionBarHUD, retrieval-only display, continuous green/yellow/red fill, noninteractive value updates, and the existing pointer sprite.
- Added silent flight/race cancellation at GameOver and explicit Strike cancellation. No gameplay balance parameters changed.
- Validation: dotnet build Assembly-CSharp.csproj passed with 0 errors and 2 CS0649 warnings in SceneCatalog. Scene file IDs checked for uniqueness and target references; diff reviewed. Unity Play Mode, Android and WebGL were not run.
- Next: playtest HUD/reset, snap hook loss and timer expiration during flight/baiting, then verify complete M7 outcomes.

## 2026-09-06 — Teaching and prototype priorities

- User returned to step-by-step teaching and confirmed tension pointer/fill alignment works in play. User adjusted aspect/scaling/placement; saved lateral tension rate is 0.9.
- User explicitly deferred the proposed systematic regression pass, preferring current playability and remaining feature development. Tests remain deferred, not passed.
- Read-only backlog check: mobile accelerate methods lack serialized UI bindings in searched scenes/prefabs; PausePanelButton navigates to MainMenu; gameplay-specific restart flow not found; linear cast and shake-profile integration remain unfinished. FishingLoopTest2 exists but catalog/build still target FishingLoopTest.


## 2026-09-06 — Two-day implementation planning

- Created the Android playtest implementation proposal and a separate Claude design-change brief covering all current requests, Profile ownership, cooldown paths, implementation order, deferred scope and open decisions.
- Kept Claude's canonical revision 9 design and teaching plan unchanged. No gameplay/scene edits or runtime tests in this planning task. Brief prepared for user handoff, not sent to Claude.

## 2026-09-06 — Strike revision 9 user checkpoint

- User explicitly reported the timing-ring migration complete and its functional test passed. This is user-reported verification, not an additional Codex test run.
- Continue teaching post-attempt cast cooldown next. Add a dedicated FishingLoopProfile for cooldown first; starting-hooks/session-duration migration will be a separate coordinated change to avoid duplicate live configuration sources.

## 2026-09-07 — 本轮教学进度与重新规划

- 用户继续亲自编写代码、英文注释、一次一个适量步骤；不反复检查。时机圈按用户报告通过，随后 post-attempt cooldown 完成并获用户试玩确认。
- 已教学并在保存源码中看到 ScoreTuningProfile、实时距离/倍率、PC 按住空格蓄力和竖条 HUD。显示统一归 FishingSessionHUD；用户确认 Slider 手动及运行时 Value 都会增长。
- 用户确认倍率在飞行中变化、落水锁定，距离显示在 Reeling 中继续变化。检查发现落水最终倍率重算、上岸倍率结算和 scoreTuningProfile 缺失检查尚未接入；不能标记计分闭环完成。
- 当前蓄力条显隐仍依赖旧文字对象的显隐条件，记录为下一小步收尾项。PC 蓄力字段仍在输入组件，飞行距离/固定时长仍在 FishingHookController；Profile 迁移尚未执行。
- 用户希望飞行时间来自模拟；初速度/角度/重力与虚拟高度为讨论建议，尚未确认可见弧线范围，没有实现。
- 更新 task_plan.md、findings.md、本日志和旧入口提示，新增 docs/reference/2026-09-07-progress-and-plan.md，重新安排收尾、飞行方案、反馈、技巧奖励、鱼行为及设备交付顺序。
- 本次只更新文档并进行局部源码/保存场景读取，没有修改代码或场景，没有运行编译、Play Mode、Android、WebGL 或完整回归，也未提交/推送。没有改写 Claude 主导设计或发送外部消息。
