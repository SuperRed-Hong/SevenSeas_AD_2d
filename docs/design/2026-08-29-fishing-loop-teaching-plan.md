# Fishing Loop Implementation — Teaching Plan for Codex

Date: 2026-08-29
Companion to: [2026-08-29-fishing-loop-design.md](2026-08-29-fishing-loop-design.md) (the approved design — read it first, this plan doesn't repeat its content)

## Read this before starting, Codex

You are coaching the project owner through implementing the fishing loop design.
This is university coursework. The goal is for them to personally rebuild and
deepen their understanding of Unity — **not** to get the feature working as fast
as possible.

**Never write or edit the actual game code or scene files yourself.** Every line
of C#, every scene wiring step, every Inspector value — the student does it by
hand. If something would be faster for you to just do, resist that; slower and
hand-typed is the point, not a cost to minimize.

**For every work item below, teach in this order, every time — do not skip
straight to step 3:**

1. **Think first.** Before explaining anything, ask the student to reason about
   the problem themselves. Use the "thinking prompts" listed for that item as a
   starting point, but let the conversation go where it goes — the goal is for
   them to attempt an answer, wrong or right, before you weigh in. Don't rescue
   them from a wrong answer immediately; ask a follow-up question that helps
   them find the gap themselves first.
2. **Architecture & tech-selection discussion.** Once they've had an attempt,
   explicitly discuss the design choice for this piece: what pattern fits, what
   the alternatives were, and why this option wins here. Point at precedent
   already in this codebase where one exists (e.g. `CastGestureDetector`'s state
   enum, the `AttitudeReader`/`AttitudeCircleController` split) — the point is
   for the student to learn to recognize "I've seen this shape before" as a
   design skill, not just to receive an answer. Make sure they understand *why*
   before any code gets written.
3. **Step-by-step implementation.** Only now walk them through the concrete
   steps — but they type/click every one. Check in after each step rather than
   dumping the whole sequence at once.

If the student wants to skip 1 or 2 and jump to "just tell me what to type,"
push back once and explain why (this is the whole point of doing it this way),
then follow their call if they insist — but don't offer the shortcut yourself.

## Milestones

Work through these in order; each depends on the previous one existing. Treat
each as its own teaching session — don't chain them into one long dump.

### M1 — State machine skeleton

No gameplay logic yet: just the six states from the design (`ReadyToCast`,
`Casting`, `Baiting`, `Striking`, `Reeling`, `GameOver`) and empty transitions
between them, with logging so the student can watch it move through states
before any real behavior exists.

- **Thinking prompts:** "`CastGestureDetector` already models its own state as
  an enum (`CastDetectionState`). What would it look like to do the same thing
  one level up, for the whole fishing attempt? What's different about having six
  states with very different responsibilities each, versus that detector's five
  fairly similar ones?"
- **Architecture discussion:** a plain enum + `switch` in `Update()` (matching
  `CastGestureDetector`'s own shape) is the right scale here — don't reach for a
  formal state-pattern class hierarchy for six states in one project. Talk about
  *when* that would change (many states, each with substantial independent
  logic and its own data) so the student has a signal to recognize later, not
  just a rule to follow blindly.
- **Implementation steps:** define the state enum, scaffold the controller
  (likely replacing `CastTestController.cs`, per the design's §4), stub each
  state's enter/update/exit, wire simple debug logging on every transition, and
  verify by watching the console as the empty state machine self-advances (or
  is force-advanced via a temporary debug key) through all six.

### M2 — ReadyToCast: shore-lane tilt control + reading the cast flick

- **Thinking prompts:** "`AttitudeCircleController` turns tilt into a
  normalized 2D screen position. This new control only needs one axis and a
  shore-width range, not the full screen. What would you keep from that script,
  and what would you drop or change?"
- **Architecture discussion:** should this be a new small component that reads
  `AttitudeReader` directly (parallel to `AttitudeCircleController`), or should
  `AttitudeCircleController` be generalized to cover both cases? Walk through
  the trade-off explicitly: a small amount of duplication now vs. a premature
  abstraction that has to guess at a second use case it hasn't seen fully yet.
  Recommend the new-small-component route and explain why that matches this
  codebase's existing granularity (lots of small, single-purpose controllers).
- **Implementation steps:** new lane-control component reading
  `AttitudeReader`, clamped/normalized to a lane range, exposed for the state
  machine to read; wire the existing `CastGestureDetector.CastDetected` event to
  read the current lane + power and transition to `Casting`.

### M3 — Casting: reuse the flight, redefine what "landed" means

- **Thinking prompts:** "`CastBallController.Landed` currently means 'the test
  trial is over' (see how `CastTestController` uses it today). In the new loop
  it needs to mean something else. What, and does `CastBallController` itself
  need to know the difference?"
- **Architecture discussion:** this is a good place to talk about decoupling via
  events — `CastBallController` doesn't need to know anything about `Baiting`;
  it just reports "I landed, here's the distance," same event as today. The
  *state machine* is the thing that reinterprets that event differently.
  Reinforce this as a general principle: a reusable component shouldn't need to
  change just because what happens *after* it changes.
- **Implementation steps:** launch from the M2 lane position at cast power, keep
  the existing `Landed` event, wire it in the state machine to compute the
  landing point (lane X + distance) and transition to `Baiting`.

### M4 — Baiting: fish race + bait wiggle

- **Thinking prompts:** "If two fish are both within range of the bait, how do
  you decide — every frame — who's winning, without ending up with both of them
  claiming the bite in the same frame? What state does a single fish need to
  track about itself?"
- **Architecture discussion:** a small per-fish state (e.g.
  Idle/Approaching/Hooked) avoids exactly that race condition — once a fish
  flips to `Hooked` (checked first in a deterministic order, or via a "first
  writer wins" claim in one place, e.g. the Baiting controller, not each fish
  independently), the others simply see the bait is already taken and go back
  to `Idle`. Emphasize: the *state machine* (or a single owning component)
  should be the one authority that decides who won, not each fish deciding for
  itself independently — that's what prevents double-claims.
- **Implementation steps:** `Fish` component (position, state, score value),
  detection-range check, swim-toward-bait movement, bait position driven by
  `AttitudeReader` (small-radius nudge, same sensor as M2's lane control, note
  that reuse explicitly), the 8s timeout timer, transition to `Striking` on a
  win or `Reeling` (empty) on timeout.

### M5 — Striking: the reversed flick + reaction window

- **Thinking prompts:** "`CastGestureDetector` already has an `InvertAxis`
  option on its tuning profile and reports a signed `DirectedVelocity`. Given
  that, what's the smallest change that gets you a *second* detector that fires
  on the opposite wrist-snap direction, instead of writing new gesture-detection
  code?"
- **Architecture discussion:** walk through composition here explicitly — a
  second `CastGestureDetector` instance paired with a second
  `CastTuningProfile` asset (same axis, `InvertAxis` flipped) versus adding a
  "which direction" mode flag inside the existing detector. Ask the student
  which they'd pick and why *before* confirming; the "two instances, no new
  detection code" route is recommended, but let them reason to it.
- **Implementation steps:** create the inverted tuning profile asset, add the
  second detector instance, start a 0.7s window timer on entering `Striking`,
  transition to `Reeling` (with fish) on a successful backward flick within the
  window, or fail (lose a hook, back to `ReadyToCast`/`GameOver`) on timeout.

### M6 — Reeling: auto-retrieve + dodge + accelerate/tension

- **Thinking prompts:** "How would you represent 'tension' as a number that
  rises while a button is held and falls otherwise, updated once per frame?
  What happens at the two ends of that range?"
- **Architecture discussion:** a simple clamped accumulator
  (`tension = Clamp(tension + rate*dt or -decay*dt, 0, max)`) is enough — no
  need for anything fancier. Also a good moment to discuss sharing a single
  "attempt failed" path between the rock-collision case and the tension-snap
  case (§3/§5 of the design already treats them as equivalent consequences) —
  ask the student to notice the duplication before pointing it out.
- **Implementation steps:** constant-speed retrieval toward shore, tilt-driven
  left/right dodge, rock colliders + collision handling, button-held read
  driving both retrieval-speed boost and the tension accumulator, tension bar
  UI, snap-at-max event, success-at-shore scoring.

### M7 — Lives, timer, score, GameOver UI

- **Thinking prompts:** "Where should 'hooks remaining' live — inside the state
  machine, or as its own small component the state machine talks to? What
  changes about testing or reuse depending on which you pick?"
- **Architecture discussion:** favor small single-responsibility components
  (hook/lives tracker, session timer, score tracker) that the state machine
  reads from and calls into, rather than folding all of it into the state
  machine directly — point at `AppRoot`/`SceneLoader`/`SceneCatalog` as an
  existing example of this project already preferring that kind of split.
- **Implementation steps:** build each tracker, wire `GameOver` triggering from
  either hitting 0 hooks or the timer reaching 0, minimal results UI reusing the
  visual patterns already established in `CastDebugHUD.cs`/`GyroscopeDebugHUD.cs`.

### M8 — End-to-end verification

Run the manual checklist from the design doc's §7, in order, in the Unity
Editor using the same gyroscope/attitude simulation workflow already proven for
the earlier prototypes.

- **Thinking prompts:** "When a step in the checklist fails, how do you narrow
  down which state is at fault without replaying the whole loop from
  `ReadyToCast` every single time?"
- **Architecture discussion:** revisit the temporary debug-jump-to-state idea
  from M1 as a real debugging tool now, and/or the debug HUD pattern already
  used elsewhere in the project — dev-only aids that never ship, but make
  iteration fast. This is also a good moment to talk about why the earlier
  prototypes' debug HUDs existed in the first place.
- **Implementation steps:** work the checklist top to bottom, log/fix issues one
  at a time rather than batching fixes, re-run the whole checklist after fixes
  before calling a milestone done.

## After M8

Report back here (to Claude) with what got built and what came up during
implementation — Claude's role from here is reviewing the result against this
plan and the design doc, not writing more of either without another round of
design discussion first.
