# Fishing Loop Implementation — Teaching Plan for Codex

Date: 2026-08-29 (M5 and M6 rewritten 2026-09-01 for design revisions 6–8)
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

## Step 0 — Before teaching anything, write it down

Do this before M0's "think first" question, not after — before any teaching
happens at all, full stop. Read this plan and its
companion design doc in full, then repurpose this repo's existing
`task_plan.md` / `findings.md` / `progress.md` (root of `SevenSeas_AD_2d/` —
the same three files already used for the earlier gyroscope-HUD task) for this
fishing-loop implementation:

- **`task_plan.md`** — a checklist mirroring M0–M8 and their sub-steps from
  this document, each marked not-started / in-progress / complete as work
  actually happens. This is the big-picture anchor.
- **`findings.md`** — running discoveries made along the way (the
  `TriggerThreshold` re-tuning need flagged in M2 is the first entry that
  already belongs here).
- **`progress.md`** — a dated log of what actually got done, session by
  session.

**Why this matters, explicitly:** a long teaching conversation can drift —
getting absorbed in one milestone's details for many turns risks losing track
of the overall architecture, and the original design doc's guidance can quietly
lose weight against a growing chat history. These three files are an external
anchor that doesn't degrade the way conversation context does. Update them as
you go — don't write them once and let them go stale. If you (Codex) ever
notice the thread of a conversation has wandered from why a sub-decision
matters, re-read the design doc, this plan, and these three files before
re-reading chat history — they are the source of truth, the conversation is
not.

## Milestones

Work through these in order, starting from M0; each depends on the previous one
existing. Treat each as its own teaching session — don't chain them into one
long dump.

### M0 — Quick orientation (keep this short — the deep dives happen later, in M2 and M5)

The student doesn't yet fully understand the existing sensor prototypes this
plan reuses (Codex built the originals; the student wants to actually
understand them, not just inherit them) — but M1 right after this doesn't
touch that code at all, it's just an empty state machine. Don't front-load the
whole knowledge debt here; that would burn time before anything is even
running. M0 is deliberately small: just enough grounding for M1 to make sense.
The full deep dives on `AttitudeCircleController` and `CastGestureDetector` are
scheduled just-in-time, right before M2 and M5, the milestones that actually
build on them (see those sections below) — learning something right before
using it sticks better than learning it in the abstract, days before it's
needed.

- **Thinking prompt:** "Why does this project use two separate reader
  components — `AttitudeReader` for orientation, `GyroscopeReader` for angular
  velocity — instead of one?"
- **Architecture discussion (brief):** the reader/controller/view three-layer
  split, and the core position-vs-speed distinction between attitude
  (`Quaternion`, a state) and angular velocity (`Vector3` rad/s, a rate).
  Source: [`docs/reference/motion-input-guide.md`](../reference/motion-input-guide.md)
  §0–1 — this should be a short conversation grounded in that material, not a
  read-the-whole-doc assignment.
- **Checkpoint:** the student can state, in their own words, why attitude and
  angular velocity aren't interchangeable. That's the entire bar for M0 —
  nothing about thresholds, filtering, or the gesture state machine belongs
  here; those get their own checkpoints later, attached to M2 and M5.

**Time checkpoint after M3, not just at the end.** Revision 3 of the design
(Baiting's fish race, Striking's reversed flick, Reeling's tension/accelerate)
is more than a small one-day slice — this was flagged explicitly in the design
doc's §6 and confirmed again in review. M0–M3 (quick orientation, state
machine skeleton, cast lane/flick — including its `AttitudeCircleController`
deep-dive, flight-to-landing) are the stability floor: get those solid first.
Once M3 works end to end, stop and check the clock with the student together —
don't silently cut scope. If time is short, the two cheapest things to simplify
are M4 (drop the fish race to a single fish + instant bite, no timeout logic)
and M6 (drop the accelerate/tension mechanic, keep pure auto-reel + dodge). Any
simplification is the student's call, made explicitly, not an assumption Codex
makes on its own — and it should come back to Claude for a quick design-doc
update before being treated as final, not just quietly diverge from the spec.

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

- **Before anything else — `AttitudeReader`/`AttitudeCircleController` deep
  dive (coached, not a reading assignment):** this milestone's new component is
  a variant of exactly this existing code, so understand it for real first.
  Source: [`docs/reference/motion-input-guide.md`](../reference/motion-input-guide.md)
  §2–3. Ask before explaining: "what's the dead zone (`deadZoneDegrees`) for,
  and what would the control feel like without it?" / "why does axis-locking
  need hysteresis (`axisSwitchHysteresisDegrees`) instead of just picking
  whichever axis has the bigger value each frame?" / "the smoothing formula is
  `1 - e^(-k·dt)`, not a flat `Lerp(current, target, 0.1f)` — what goes wrong
  with the flat version?" Have the student restate the answers, then run
  `AttitudeControlTest.unity` in Play Mode and actually tilt the phone while
  watching the Circle and the debug HUD, connecting what moves on screen to
  what they just discussed. Don't move to the thinking prompts below until
  this is solid — treating `AttitudeCircleController` as a black box to copy
  from here will just produce cargo-culted code.
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
- **Don't skip this verification step:** once tilt-driven lane control and the
  flick detector are running at the same time, have the student actively play
  with lane selection — normal, maybe slightly quick tilting — and confirm it
  does *not* accidentally fire `CastDetected`. This combination never existed
  before (the two prototypes were tested in separate scenes), so the current
  `CastTuningProfile.TriggerThreshold` (1.25, tuned in isolation in
  `GyroscopeCastTest.unity`) is not trustworthy here without re-checking. Use
  the existing `GyroscopeDebugHUD.cs` to watch the live angular-velocity value
  while doing ordinary lane adjustments, and raise `TriggerThreshold` until
  only a clearly deliberate flick crosses it — this is a real user-review
  finding (see the design doc §3), not a hypothetical.

### M3 — Casting: a new flight for the production scene, generic "landed" event

**Design correction found while implementing this milestone (now reflected in
design doc revision 5, §3/§4):** `CastBallController`/`GyroscopeCastTest.unity`
is a horizontal side-view parabola; the production scene casts vertically on
screen into the water. That's a real orientation mismatch, not something to
force through — `CastBallController` and its test scene stay exactly as they
are, untouched and isolated. Build a **new** flight component for the
production scene instead.

- **Thinking prompts:** "`CastBallController` gives you a working example of a
  flight component's *public shape* — a `Launch(power)` entry point and a
  generic landing event — even though its actual trajectory physics don't fit
  the production scene's vertical orientation. What should the new component
  keep from that shape, and what has to be rebuilt from scratch for the
  vertical layout?" / "`CastTuningProfile.EvaluatePower(peak)` turns a flick's
  peak into a 0–1 power value, independent of any orientation — does that
  still apply here? What about `EvaluateLaunchVelocity(power)` and its current
  tuned min/max vectors — do those numbers mean anything in a vertical cast, or
  do they need to be re-derived for the new layout?"
- **Architecture discussion:** decoupling via events, same as originally
  planned — the *state machine* (`FishingLoopController`) reinterprets a
  generic landing event as "end `Casting`, start `Baiting`," and doesn't need
  to know or care which concrete flight component raised it. Reinforce this as
  a general principle: it's exactly this decoupling that makes swapping in a
  new flight implementation safe without touching the state machine at all.
  Also worth a beat on *why* to keep `CastBallController` isolated rather than
  editing it in place: it's still a working, useful distance-test scene on its
  own, and bending it to fit a second, incompatible use case would make both
  worse.
- **Implementation steps:** design the new flight component's trajectory
  physics for the vertical-cast orientation (working out the velocity/gravity
  mapping is real design work here, not a given — do it together, don't just
  hand over numbers), reuse `CastTuningProfile.EvaluatePower` for the power
  value, launch from the M2 lane position, raise a generic landing event,
  wire it in the state machine to compute the landing point (lane position +
  cast power) and transition to `Baiting`.

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

### M5 — Striking: the shrinking-ring timing check

> **The design changed under this milestone — read design doc revision 6
> (§3/§4/§5) before teaching any of it.** `Striking` is no longer "reverse-flick
> inside 0.7 s or lose a hook." It is a **shrinking ring / radial timing skill
> check**: a fixed full-ring target band, a moving ring that shrinks linearly to
> the centre over `windowDuration`, an attempt (reverse flick *or* keyboard
> Space) judged against where the ring is at that instant, and an out-of-band
> attempt that is **rejected with a short input cooldown** rather than fatal.
> The earlier version of this section described a different mechanic and is
> superseded.

- **Before anything else — `CastGestureDetector` deep dive (coached, not a
  reading assignment):** unchanged from the previous plan, and now load-bearing
  rather than background. Source:
  [`docs/reference/motion-input-guide.md`](../reference/motion-input-guide.md)
  §5 and §9.2. Ask before explaining: "a naive flick detector would be
  `if (angularVelocity > threshold) Launch();` — what breaks with that, and
  why does `CastGestureDetector` need a whole state machine instead?" / "what
  is `rearmThreshold` for, separate from `triggerThreshold` — what would
  happen with just one threshold?" / "why does `CastPower` get computed from
  `FilteredPeak` instead of `RawPeak`?" Have the student restate the
  hysteresis/Schmitt-trigger idea and the noise-filtering purpose of the
  exponential smoothing (same formula shape as M2's position smoothing — worth
  having them notice that on their own) before moving on. **Checkpoint before
  the rest of this milestone:** the student should be able to explain,
  unprompted, the trigger/rearm threshold pair and the filtered-vs-raw-peak
  choice.

- **Thinking prompt 1 — latency. Do this one first; its answer decides the
  architecture.** "`CastDetected` does not fire when you flick. It fires after
  `Sampling` has run for `SampleWindow`. Walk me through what that means for a
  check where the ring is moving every frame — where would the ring actually be
  by the time that event arrives? Now look at the detector again: is there any
  point in its state machine that *does* correspond to the moment of the flick?"
  The student has already added `GestureTriggered` for exactly this purpose, so
  the goal is not to inform them it exists — it is to have them re-derive *why*
  it has to exist. Let them find it in their own code; don't hand it over.

- **Thinking prompt 2 — the shape of the check.** "The ring shrinks linearly
  from 1 to 0 across the window, and the band is a fixed slice of that range. If
  the band is 0.16 wide and the window is 1.5 s, how long is the player actually
  inside it? What does that tell you about which numbers belong in the
  Inspector, and which should be computed from the others?"

- **Thinking prompt 3 — why reject instead of fail.** "The old design failed the
  whole attempt on a mistimed flick. The new one ignores it and starts a
  cooldown. What does that change about how a player learns this mechanic? And
  what stops someone from just mashing Space through the whole window?" (The
  answer to reach on their own: the cooldown *is* the cost — a badly-timed
  attempt can swallow the one moment the ring is inside the band.)

- **Thinking prompt 4 — ownership.** "Three things happen when an attempt
  misses: the rings flash red, the camera shakes, and the state machine does
  nothing at all. Which component should know about each? And where does the
  ring's position get computed — who is allowed to compute it?"

- **Architecture discussion:**
  - **Two events, two consumers.** `CastDetected` (delayed, carries power) stays
    the cast path's event; `GestureTriggered` (immediate, parameterless) is the
    strike path's. Point out this is the same decoupling principle already used
    for `CastBallController.Landed` and the production hook's `Landed(Vector2)`
    — one producer, several consumers with different needs — and that adding a
    second event was cheaper and safer than bending one event to serve both.
  - **Two cooldowns, one authority.** The detector has its own
    `Sampling → CastDetected → Cooldown` lockout; `StrikeController` has
    `attemptCooldown`. Have the student add up the detector's lockout from the
    cast profile's values (`0.35 + 0.2 + 0.75 ≈ 1.3 s`) and compare it against a
    1.5 s strike window *before* you say anything. Let them discover that a
    single rejected attempt would otherwise guarantee a timeout. Then decide
    together: `StrikeGestureProfile` gets a minimal `SampleWindow` and
    `CooldownDuration`, the strike detector instance gets
    `castDetectedDisplayDuration = 0`, and `StrikeController.attemptCooldown`
    becomes the only thing pacing attempts.
  - **Normalized contract between logic and view.** `StrikeController` publishes
    a normalized ring radius and normalized band bounds; `StrikeWindowHUD` draws
    them and computes nothing. Point at the motion-input guide's §0 rule — "the
    HUD never calculates anything" — as existing project precedent, and at the
    concrete bug it prevents here: a HUD running its own `elapsed / duration`
    clock will drift from the one the controller judges against, and the player
    will lose on an attempt that looked correct on screen. This is the single
    most likely real bug in this milestone; say so plainly.
  - **Why a full ring band and not an arc.** The strike input has no direction or
    aim component, so an arc would promise an aiming task the input cannot
    express. Worth one beat — a good small example of visual language making a
    promise the mechanic then has to keep.
  - **Where the hook loss lives.** `StrikeController` reports "timed out";
    `FishingLoopController` decides that this means losing a hook. Same split
    the student already built in M4, where `FishBiteRaceController` picks the
    winner and the state machine decides what winning means. Ask them to notice
    the repeat before you name it.
  - **Where the tuning numbers live — ask this before they write any of them
    down.** "You are about to add five numbers that the design doc explicitly
    says can only be settled by playing. Where are you going to put them? Now
    walk me through what happens to a value you type into the Inspector *during*
    Play Mode when you press stop." Most students have to be bitten once to
    believe this, so if they aren't sure, have them try it — type a value on a
    component in Play Mode, stop, watch it revert. Then ask what is different
    about `CastTuningProfile`, which they have already been reading all
    milestone. The project contains both outcomes: `CastTuningProfile` is a
    `ScriptableObject` and survives; `AttitudeCircleController`'s tuning lives on
    the scene component and produced M2's "were 10 and 30 test values or real
    ones?" confusion, still recorded in `findings.md`. Let them reach the rule —
    feel/balance numbers go in a profile asset, object references and
    per-instance placement stay on the component — and only then confirm it
    against design §4's "Tuning data" subsection.
    Second half of the same discussion: `Striking` needs **two** assets, not
    one. Ask why the ring parameters can't just be added to `CastTuningProfile`.
    (Because it is shared with the cast path and the isolated `GyroscopeCastTest`
    scene, and neither has any idea what a ring is. Bloating a shared asset to
    serve one new consumer is the same mistake as bending `CastBallController`
    to serve the vertical cast in M3 — a comparison worth drawing out loud.)
  - **Profiles that check themselves.** `windowDuration`, the two band radii and
    `attemptCooldown` have relationships between them, and breaking one produces
    silent nonsense rather than an error — an inside-out band simply can never be
    hit, with nothing in the Console to say so. Ask what the profile could do
    about that before naming `OnValidate`, and point at how
    `CastTuningProfile.EvaluatePower` already guards its own divide-by-zero as
    the in-house precedent.

- **Known risk to carry over from the previous plan (still true, still verified
  against the source):** don't leave either detector always-enabled. Ordinary
  hand jitter can trip the strike detector's threshold during `ReadyToCast` or
  `Baiting`, drop it into `Cooldown`, and leave it deaf right when `Striking`
  needs it — a false negative on a real strike attempt. Toggle each detector's
  `enabled` to match its relevant state. `CastGestureDetector.OnEnable()`
  already calls `ResetDetector()`, so enabling it fresh at the right moment *is*
  the reset; no new reset code is needed. Have the student confirm that in the
  actual script rather than taking it from this plan.

- **One thing to decide explicitly, not by accident:** the detector runs on
  `Time.unscaledTime`, while the rest of the fishing loop runs on scaled
  gameplay time. Ask the student which clock `StrikeController` should use for
  the window and the cooldown, and make sure the HUD and the judgement end up on
  the same one either way.

- **Implementation steps** (the student types and clicks every one; check in
  after each, don't dump the sequence):
  1. Create the `StrikeGestureProfile` asset — a `CastTuningProfile` instance:
     same axis as the cast profile, `InvertAxis` set to the opposite of whatever
     the cast profile currently uses, and the minimal
     `SampleWindow`/`CooldownDuration` decided above. Say out loud that its
     `Power` and `Launch` sections are dead weight on this path, and why that's
     acceptable rather than a smell.
  2. Add the second `CastGestureDetector` instance, disabled by default, wired
     to that profile.
  3. Confirm `GestureTriggered` fires on the frame of the flick — a temporary
     `Debug.Log` with `Time.unscaledTime` beside one on `CastDetected` makes the
     `SampleWindow` gap visible. Have them *see* the gap, not just believe it.
  4. Write the `StrikeWindowProfile` `ScriptableObject` and create its asset:
     `windowDuration`, both band radii, `attemptCooldown`, shake
     amplitude/duration, plus the `OnValidate` checks discussed above. Match
     `CastTuningProfile`'s house style exactly — `[Header]`, a `[Tooltip]` per
     field, `[Min]`/`[Range]` bounds, private `[SerializeField]` backing fields
     behind read-only properties, `[CreateAssetMenu]` under `Seven Seas/`.
     Having them mirror an existing file field-for-field is the point here.
  5. Write `StrikeController`: window timer, normalized ring radius, band
     bounds, attempt intake from both input paths, the in-band test, the
     cooldown, and the three events (succeeded / timed out / attempt rejected).
     It holds a reference to the profile and reads every tunable from it; the
     only `[SerializeField]`s on the component itself are references.
  6. Check the keyboard side against the current source together rather than
     re-adding it: `FishingInputSource.StrikePerformed` and `SetStrikeEnabled`
     already exist, and the keyboard implementation already raises Strike on
     Space. Reading their own earlier code as an API is a skill worth practising
     here.
  7. Write `StrikeWindowHUD`: draw the fixed band and the moving ring from the
     controller's normalized values only, plus the red flash on rejection.
  8. Add the camera-shake component subscribing to the same rejection event.
  9. Wire `FishingLoopController`: on entering `Striking`, start the controller
     and enable the strike detector; on success go to `Reeling` with a fish; on
     timeout lose a hook and return to `ReadyToCast`/`GameOver`; on exit disable
     the detector and hide the HUD.
  10. **Play Mode tuning pass — a required step, not polish.** `windowDuration`,
     the two band radii, `attemptCooldown` and the shake amplitude are all
     starting guesses in the design doc, explicitly flagged as such. Sit with
     the student and tune them by feel, the same way the calibration tolerance
     and the smoothing values were settled in M2.

### M6 — Reeling: auto-retrieve + dodge + accelerate/tension

> **The tension model changed — read design doc revision 7 (§3 `Reeling`, §4,
> §5.1 and §5.6) before teaching this.** Tension is no longer "rises while a
> button is held, falls otherwise." It is a **sum of rates**, clamped `0–1`:
> an always-on passive decay, an accelerate rise rate larger than that decay,
> and a lateral contribution scaled by the hook's **actual** lateral speed. The
> last two stack.

- **Thinking prompt 1 — the shape of the update.** "Tension now has three
  contributions and only one of them is always on. Write me the single line that
  updates it once per frame. Would you rather write that as an if/else chain or
  as one sum? What does each version do when two contributions apply at once?"
  (Where they should land: sum the rates first, apply once, clamp once —
  stacking then falls out for free instead of needing a branch per combination.)

- **Thinking prompt 2 — measuring the right thing. This is the one that
  actually bites.** "The dodge control has an input value, and the hook has a
  position. Which of the two should feed tension? Now imagine the player is
  holding the dodge control hard against the left lane boundary — what does each
  choice charge them?" Let them find the answer themselves; it is a good one:
  full input with zero movement must cost nothing, so tension has to read the
  hook's real per-frame movement, not the intent behind it. Follow up with "what
  else does that choice protect you from?" (any smoothing, any clamping, and any
  future change to how the dodge control works).

- **Thinking prompt 3 — the mapping.** "You have a safe speed, a maximum
  evaluated speed, and a maximum contribution. What turns a raw speed into a
  0–1 position between the first two? Have you written this shape before in
  this project?" They have — **twice**: `CastTuningProfile.EvaluatePower(peak)`
  (`minimumPeak` → `maximumPeak` via `Mathf.InverseLerp`) and
  `AttitudeCircleController.NormalizeTilt` (dead zone → max tilt). Getting them
  to *recognise* the third instance of a shape they already built is worth more
  here than the code itself. Name it once they see it: dead zone, ramp, clamp.

- **Thinking prompt 4 — the two balance conditions.** "Decay is subtracted every
  frame no matter what. What has to be true of `accelerateRiseRate` for holding
  the button to do anything at all? And of `maxLateralTensionRate` for fast
  dodging to do anything? Is there a speed at which dodging exactly cancels the
  decay — and would a player be able to feel where it is?"

- **Thinking prompt 5 — firing once.** "The snap fires when tension hits
  maximum. Tension is clamped, so it *stays* at maximum. What stops the event
  firing again every frame after that?"

- **Architecture discussion:**
  - **One accumulator, summed rates, one clamp.** No branchy per-case logic;
    stacking should be arithmetic, not a special case. Contrast the two versions
    if it helps — the sum version is also the one that stays correct when a
    fourth contribution is added later.
  - **Actual vs. intended, as a general principle.** Tension reads *result* (the
    hook moved) rather than *intent* (the player pushed). Tie it back to M5's
    HUD rule: there, the display had to read the controller's value instead of
    recomputing it; here, tension has to read the hook's movement instead of
    re-deriving it from input. Same failure mode both times — two places
    computing what should only be computed once.
  - **Frame ordering.** Lateral speed must be sampled *after* the dodge has
    moved the hook this frame, or tension trails the movement by a frame. Worth
    making them say out loud where in the frame each step happens.
  - **The `maxEvaluatedLateralSpeed` clamp is also the spike guard.** A single
    frame hitch can produce an enormous `Δx / deltaTime`; because the mapping
    clamps, the worst that can do is one frame at maximum contribution. Point
    out that the clamp is load-bearing for two different reasons, so nobody
    removes it later thinking it is only about tuning.
  - **Where tension lives.** Its own component that the reeling controller reads
    and calls into, versus fields inside the reeling controller. Same question
    shape as M7's "where should hooks remaining live" — ask it here first, then
    let M7 be the easy repeat.
  - **The shared "attempt failed" path** between rock collision and tension snap
    (§3/§5 of the design already treat them as identical consequences). Ask the
    student to notice the duplication before you point it out — this beat
    predates revision 7 and is still worth having.

- **Implementation steps** (the student types and clicks every one):
  1. Constant-speed retrieval toward the shore.
  2. Tilt/`MoveInput`-driven left/right dodge, clamped to the lane range.
  3. Rock colliders and collision handling, routed through a single
     attempt-failed path.
  4. Measure the hook's actual lateral speed from its per-frame movement, taking
     the absolute value. Have them log it and watch it while dodging *before*
     wiring it to anything — including watching it read zero while shoving
     against a lane boundary.
  5. The `ReelTuningProfile` `ScriptableObject` and its asset: retrieval speed,
     accelerated speed, and all five tension parameters, plus `OnValidate`
     checks for the two "greater than `decayRate`" invariants and
     `maxEvaluatedLateralSpeed > safeLateralSpeed`. Same house style as M5's
     profile — by now this should be a short step, and that is the point.
  6. The tension accumulator: three rates, summed, clamped `0–1`, all values
     read from the profile.
  7. The tension bar UI, reading the `0–1` value and computing nothing.
  8. The snap-at-max event, fired exactly once.
  9. Success-at-shore scoring.
  10. **Play Mode tuning pass — required, and it has a mandatory first step.**
     `safeLateralSpeed` and `maxEvaluatedLateralSpeed` are in world units per
     second, so they are meaningless until the hook's real maximum lateral speed
     is known. Measure that number first (step 4's log already shows it), then
     set the thresholds from it, then tune the three rates against the table in
     design §3. Walk design §7 item 6's sub-checks a–f one at a time rather than
     judging the whole system by feel at once.

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
