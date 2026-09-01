# Fishing Loop Teaching Plan

## Goal

Coach the project owner through hand-implementing the approved fishing loop:
`ReadyToCast -> Casting -> Baiting -> Striking -> Reeling -> GameOver`.

The project owner types every line of C# and performs every Unity Editor step.
Codex may inspect files and give feedback, but must never write or edit game code
or Unity scenes.

## Sources of truth

1. `docs/design/2026-08-29-fishing-loop-design.md`
2. `docs/design/2026-08-29-fishing-loop-teaching-plan.md`
3. `docs/reference/motion-input-guide.md`
4. This checklist, followed by `findings.md` and `progress.md`

If the conversation drifts, re-read these files before relying on chat history.

## Teaching protocol for every milestone

For each milestone, follow this order independently:

1. Think first: ask the student to reason from the milestone's thinking prompts.
2. After a real attempt, discuss architecture, alternatives, and codebase precedent.
3. Give one implementation step at a time; wait for the student to type/click and report back.

## Step 0 — Living project memory

- [complete] Read the design spec, teaching plan, and motion-input reference in full and in order.
- [complete] Repurpose `task_plan.md` as the M0–M8 checklist.
- [complete] Repurpose `findings.md` as the running discovery log.
- [complete] Repurpose `progress.md` as the dated session log.
- [in-progress] Keep all three files current as milestones advance.

## M0 — Quick orientation

Status: **complete**

- [complete] Ask why orientation and angular velocity use separate reader components.
- [complete] Wait for the student's own explanation before teaching.
- [complete] Briefly discuss the reader/controller/view split.
- [complete] Discuss attitude as rotational state (`Quaternion`) versus angular velocity as rotational rate (`Vector3`, rad/s).
- [complete] Checkpoint: student can explain why the two measurements are not interchangeable.

Scope guard: do not teach thresholds, filtering, or the gesture detector state machine here.

## M1 — State machine skeleton

Status: **complete**

- [complete] Think first: compare a fishing-attempt enum with `CastDetectionState`.
- [complete] Discuss enum + `switch` versus a formal state-pattern hierarchy.
- [complete] Student defines the six-state enum.
- [complete] Student scaffolds the fishing-loop controller.
- [complete] Student stubs enter/update/exit behavior for each state.
- [complete] Student adds transition logging.
- [complete] Student verifies all six empty-state transitions in the Console.
- [complete] Student removes the temporary timed auto-advance code before M2.

## M2 — ReadyToCast: lane tilt + cast flick

Status: **in-progress**

- [complete] Coached deep dive: motion-input guide §2–3 and the existing attitude scripts.
- [complete] Checkpoint: dead zone, axis hysteresis, and frame-rate-independent smoothing.
- [complete] Run `AttitudeControlTest.unity` and connect phone/HUD observations to the code.
- [complete] Think first: identify what a one-axis shore controller keeps and drops.
- [complete] Discuss a new small component versus generalizing `AttitudeCircleController`.
- [complete] Student implements and wires the position-target lane controller (variant A).
- [complete] Student designs and implements a shared 3-second motion-calibration service that survives scene changes through `AppRoot`.
- [in-progress] Student adds explicit calibration entry points in the main menu and pause menu, with hold-still progress and retry feedback. Main-menu entry and mandatory fishing-scene fallback are complete; pause-menu reuse remains pending until pause UI exists.
- [complete] Student replaces position-target lane mapping with one velocity-controlled scheme shared by keyboard direction and normalized phone tilt; neutral input means immediate stop.
- [complete] Student explicitly selects velocity control after the absolute-target keyboard experiment proved difficult to stop precisely.
- [in-progress] Verify the selected velocity scheme on Android: greater tilt increases speed, dead-zone/neutral stops immediately, and lane bounds still clamp correctly.
- [complete] Student subscribes to cast detection and captures lane + power.
- [complete] Student verifies ordinary lane tilting does not cast accidentally.
- [complete] Student checks lane-motion input against the cast detector and confirms direction, rather than threshold magnitude, caused the observed backswing false trigger.
- [complete] Student corrects the detection direction with `InvertAxis` and verifies the existing `TriggerThreshold` accepts deliberate forward flicks without ordinary-tilt false positives.

## M3 — Casting: flight and landing transition

Status: **core state flow verified — final parabola deferred**

- [complete] Think first: reinterpret `CastBallController.Landed` for the new loop.
- [complete] Discuss event decoupling and why the flight object need not know about `Baiting`.
- [complete] Separate the formal vertical gameplay flight from the horizontal gyroscope test projectile.
- [complete] Student builds a minimal vertical flat-flight controller with Docked/Flying/Landed lifecycle and a landing event.
- [deferred] Replace the minimal flat trajectory with the final vertical, 2D-simulated-3D parabolic presentation.
- [complete] Student launches from the selected lane using cast power.
- [complete] Student uses the new vertical flight's `Landed(Vector2)` event to preserve the landing point.
- [complete] Student transitions from `Casting` to `Baiting` on landing.
- [complete] Verify the M0–M3 core state flow end to end using the temporary flat trajectory.
- [complete] Mandatory time checkpoint: student chose to continue without simplifying M4 or M6.

If time is short, only the student may choose to simplify M4 and/or M6. Any chosen
simplification must go back to Claude for a design-doc update before implementation diverges.

## M4 — Baiting: fish race + bait wiggle

Status: **in-progress — fish race implementation and integration**

- [complete] Think first: prevent two fish from claiming the same bite.
- [complete] Discuss per-fish state and a single authority for first-writer-wins resolution.
- [complete] Student implements `Fish` state and score data.
- [complete] Student implements detection range and approach movement.
- [deferred] Student postpones bait wiggle. A proposed one-dimensional left/right version is waiting for Claude to update the design before implementation.
- [complete] Student implements and verifies deterministic winner resolution and loser reset through the full Editor loop.
- [in-progress] Student implements the 8-second timeout; code exists, formal state-transition verification remains.
- [in-progress] Student verifies the bite path transitions to `Striking`; the empty `Reeling` timeout path still needs an isolated/full-loop verification.

## Cross-platform input test infrastructure

Status: **in-progress**

- [complete] Student creates a common semantic `FishingInputSource` contract and a keyboard/mouse implementation using Unity's Input System.
- [complete] `ReadyToCast` gates Move and Cast input; Space emits one Cast event and A/D emits signed velocity input.
- [complete] `FishingSceneEntryGuard` treats calibration as a mobile-only prerequisite, so Editor/desktop full-loop play starts without a motion-calibration panel.
- [complete] Editor A/D test verifies release produces zero velocity and stops accurately at the current lane position.
- [complete] Verify Space drives `ReadyToCast -> Casting`, hook landing, Baiting, fish response, and the bite transition in the full Editor loop.
- [in-progress] Add keyboard Strike and Accelerate behavior when M5/M6 reaches those states. Keyboard Strike is verified through the M5 timing check; Accelerate remains for M6.

## M5 — Striking: shrinking-ring timing check

Status: **prototype-complete — revision 8 shake-profile wiring and regression check deferred**

- [complete] Coached deep dive: motion-input guide §5 and §9.2 plus the detector source.
- [complete] Checkpoint: trigger/rearm hysteresis and filtered-versus-raw peak.
- [complete] Think first: reuse `InvertAxis` for the opposite flick direction and use immediate threshold crossing rather than Cast power/peak completion.
- [complete] Discuss two detector/profile instances versus new detection logic.
- [complete] Verify `OnEnable()` resets the detector in the actual source.
- [complete] Student creates the inverted tuning profile and second detector.
- [complete] Student adds `GestureTriggered` at the Ready-to-Sampling boundary while keeping `CastDetected(power)` unchanged for casting.
- [complete] Student enables each detector only in its relevant fishing-loop state.
- [complete] Claude design revision 6 approves the shrinking-ring timing check and rewrites the M5 teaching sequence.
- [complete] Think through ring timing, rejected-attempt cooldown, ownership, and scaled-versus-unscaled clock choice before continuing implementation.
- [complete] Retune the Strike detector's own sample/display/cooldown lockout so `StrikeController` is the attempt-pacing authority.
- [complete] Student implements `StrikeController`: timer, normalized ring radius, band bounds, cooldown, and succeeded/timed-out/rejected events.
- [complete] Student verifies the existing keyboard Strike path against the semantic input contract.
- [complete] Student implements `StrikeWindowHUD` as display-only logic reading the controller's normalized values.
- [complete] Student implements rejected-attempt red feedback and an event-driven Cinemachine Impulse shake.
- [complete] Student wires success and timeout outcomes through `FishingLoopController`.
- [complete] Student completes the required Play Mode tuning pass.
- [complete] Revision 8 think first: student confirmed the Play-Mode persistence and single-responsibility reasons were already understood and requested no repeated questioning.
- [complete] Revision 8 architecture discussion: keep `StrikeController` as runtime judgement authority, move only feel/balance data into a dedicated `StrikeWindowProfile`, and keep scene wiring on components.
- [complete] Student creates and wires `StrikeWindowProfile`, migrates window duration, inner/outer band radii, and attempt cooldown into it, and adds revision 8 validation rules.
- [deferred] Route rejected-attempt shake amplitude/duration from `StrikeWindowProfile` into `FishingCameraController`; the existing accepted Cinemachine settings remain active meanwhile.
- [deferred] Verify Play-Mode profile persistence and rerun the complete Strike success/reject/timeout regression after the remaining shake wiring.

## M6 — Reeling: retrieve + dodge + tension

Status: **in-progress — revision 8 approved and re-read; think-first stage next**

- [complete] Revision 8 design update received; movement-driven tension is approved and the former design blocker is cleared.

### Think first — answer before architecture or implementation

- [complete] Prompt 1: express each frame's tension change as one summed net-rate line so passive decay, Accelerate, and fast lateral movement can stack.
- [complete] Prompt 2: choose intended input or actual hook movement as the lateral-risk measurement, including the full-input-at-lane-boundary case.
- [complete] Prompt 3: map actual lateral speed from a safe threshold to a maximum evaluated threshold into a normalized `0..1` contribution, relating it to the existing normalization pattern.
- [complete] Prompt 4: identify the two profile invariants that guarantee Accelerate alone and maximum lateral movement alone can each overpower passive decay, and reason about the lateral crossover speed.
- [complete] Prompt 5: prevent repeated line-snap events while tension remains clamped at its maximum.

### Architecture discussion — only after all five thinking attempts

- [complete] Discuss one accumulator, summed rates, and one final clamp rather than mutually exclusive branches.
- [complete] Discuss actual movement versus intended input as a general gameplay-measurement principle.
- [complete] Discuss frame ordering: move/clamp the hook first, then measure actual lateral speed for that frame.
- [complete] Discuss `maxEvaluatedLateralSpeed` as both the top of the tuning range and protection against frame-hitch/position-spike values.
- [complete] Decide that movement and tension state live together in `ReelingController`, while its eventual `ReelTuningProfile` remains data-only.
- [complete] Discuss one shared attempt-failed path for rock collision and line snap.

### Learner-written implementation — one checked step at a time

- [complete] Step 1: student implements constant-speed retrieval.
- [complete] Step 2: student implements `MoveInput`-driven lateral dodge clamped to the lane; the attached fish follows the hook in `LateUpdate()`.
- [complete] Step 3: student adds rocks/collision and routes collision through the shared attempt-failed path; Casting ignores rocks and Reeling failure fires once.
- [complete] Step 4: student measures and logs actual lateral speed after movement: keyboard maximum is approximately `4 units/s`, and sustained input at a boundary measures `0`.
- [complete] Step 5: by the student's explicit one-time exception to learner-written mode, Codex created and wired `ReelTuningProfile`, migrated retrieval/dodge speeds, and added tension parameters plus validation; Unity refreshed and `Assembly-CSharp` builds with 0 errors.
- [in-progress] Step 6: student has implemented the tension accumulator; concise logging/play verification remains.
- [deferred] Step 7: student adds a display-only live tension bar. Explicitly deferred by the student to prioritize the complete gameplay loop.
- [deferred] Step 8: student makes maximum tension fire the line-snap outcome exactly once. Explicitly deferred by the student to prioritize the complete gameplay loop.
- [in-progress] Step 9: prioritize the shore-arrival event and return-to-`ReadyToCast` loop now; final score accumulation remains owned by M7's `ScoreTracker`.
- [not-started] Step 10: student measures real maximum lateral speed first, tunes thresholds before rates, and verifies design §7 checklist 6a–f separately.

## M7 — Hooks, timer, score, GameOver

Status: **not-started**

- [not-started] Think first: decide where hooks remaining should live.
- [not-started] Discuss small trackers versus folding all data into the state machine.
- [not-started] Student implements the hook/lives tracker with 5 starting hooks.
- [not-started] Student implements the 90-second session timer.
- [not-started] Student implements the score tracker.
- [not-started] Student wires both game-over conditions.
- [not-started] Student builds and wires the minimal results UI.

## M8 — End-to-end verification

Status: **not-started**

- [not-started] Think first: isolate a failing state without replaying the whole loop.
- [not-started] Discuss temporary debug state jumps and HUD-based observability.
- [not-started] Run design §7 checklist item 1: lane + cast.
- [not-started] Run checklist item 2: bait wiggle + fish race + strike entry.
- [not-started] Run checklist item 3: bait timeout -> empty reel.
- [not-started] Run checklist item 4: correct/wrong/missed strike behavior.
- [not-started] Run checklist item 5: reeling dodge + rock failure.
- [not-started] Run checklist item 6: accelerate + tension rise/drain/snap.
- [not-started] Run checklist item 7: shore arrival scoring rules.
- [not-started] Run checklist item 8: hooks/timer GameOver and final score.
- [not-started] Re-run the entire checklist after fixes.
- [not-started] Report the implementation outcome to Claude for review.

## Current next action

Prioritize the rapid-prototype vertical slice by proceeding with M6 while preserving its approved behavior. Complete the concise M6 think-first checkpoint, then implement the ten steps one at a time, focusing first on retrieval, dodge, failure, tension, and shore completion. M5 shake-profile wiring, M4 bait wiggle, and the final 2D-simulated-3D parabola remain explicitly deferred rather than removed.

## Errors

| Error | Attempts | Resolution |
|---|---:|---|
| A single patch tried to delete and re-add each planning file | 1 | Split replacement into one delete patch followed by one add patch. |
| A broad status patch matched the wrong milestone heading | 1 | Re-read the exact M5/M6 lines and applied a heading-scoped correction. |
