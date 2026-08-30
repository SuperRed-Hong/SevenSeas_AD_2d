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
- [in-progress] Student implements and wires the lane controller.
- [complete] Student designs and implements a shared 3-second motion-calibration service that survives scene changes through `AppRoot`.
- [in-progress] Student adds explicit calibration entry points in the main menu and pause menu, with hold-still progress and retry feedback. Main-menu entry and mandatory fishing-scene fallback are complete; pause-menu reuse remains pending until pause UI exists.
- [not-started] Student implements a velocity-controlled lane variant B using the same calibration, dead-zone, and normalization pipeline.
- [not-started] Student runs an A/B playtest of position-target control versus tilt-as-velocity control and explicitly selects the final lane scheme.
- [not-started] Student subscribes to cast detection and captures lane + power.
- [not-started] Student verifies ordinary lane tilting does not cast accidentally.
- [not-started] Student measures the lane-tilt noise floor with `GyroscopeDebugHUD`.
- [not-started] Student empirically raises `TriggerThreshold` so only a deliberate flick casts.

## M3 — Casting: flight and landing transition

Status: **not-started**

- [not-started] Think first: reinterpret `CastBallController.Landed` for the new loop.
- [not-started] Discuss event decoupling and why the ball need not know about `Baiting`.
- [not-started] Student launches from the selected lane using cast power.
- [not-started] Student uses the existing `Landed` event to compute the landing point.
- [not-started] Student transitions from `Casting` to `Baiting` on landing.
- [not-started] Verify M0–M3 end to end.
- [not-started] Mandatory time checkpoint with the student before M4.

If time is short, only the student may choose to simplify M4 and/or M6. Any chosen
simplification must go back to Claude for a design-doc update before implementation diverges.

## M4 — Baiting: fish race + bait wiggle

Status: **not-started**

- [not-started] Think first: prevent two fish from claiming the same bite.
- [not-started] Discuss per-fish state and a single authority for first-writer-wins resolution.
- [not-started] Student implements `Fish` state and score data.
- [not-started] Student implements detection range and approach movement.
- [not-started] Student implements tilt-driven bait wiggle within a small radius.
- [not-started] Student implements deterministic winner resolution and loser reset.
- [not-started] Student implements the 8-second timeout.
- [not-started] Student transitions to `Striking` on a bite or empty `Reeling` on timeout.

## M5 — Striking: reversed flick + reaction window

Status: **not-started**

- [not-started] Coached deep dive: motion-input guide §5 and §9.2 plus the detector source.
- [not-started] Checkpoint: trigger/rearm hysteresis and filtered-versus-raw peak.
- [not-started] Think first: reuse `InvertAxis` for the opposite flick direction.
- [not-started] Discuss two detector/profile instances versus new detection logic.
- [not-started] Verify `OnEnable()` resets the detector in the actual source.
- [not-started] Student creates the inverted tuning profile and second detector.
- [not-started] Student enables each detector only in its relevant fishing-loop state.
- [not-started] Student implements the 0.7-second strike window.
- [not-started] Student handles strike success and timeout failure paths.

## M6 — Reeling: retrieve + dodge + tension

Status: **not-started**

- [not-started] Think first: model tension as a clamped per-frame accumulator.
- [not-started] Discuss simple accumulation and a shared attempt-failed path.
- [not-started] Student implements constant-speed retrieval.
- [not-started] Student implements tilt-driven lateral dodging.
- [not-started] Student adds rock collision handling.
- [not-started] Student adds button-held speed boost and tension rise/decay.
- [not-started] Student adds the live tension bar and snap-at-max behavior.
- [not-started] Student handles successful arrival at shore and scoring.

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

Run the pre-calibrated cross-scene entry test, then verify shared calibration drives `ShoreLaneController` in `FishingLoopTest`. If it passes, proceed to the velocity-controlled lane variant B and the planned A/B comparison.

## Errors

| Error | Attempts | Resolution |
|---|---:|---|
| A single patch tried to delete and re-add each planning file | 1 | Split replacement into one delete patch followed by one add patch. |
