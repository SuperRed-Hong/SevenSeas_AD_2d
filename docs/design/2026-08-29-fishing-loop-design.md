# Fishing Loop Design — Cast, Fly, Reel (MVP)

Status: revision 2, pending user re-review
Date: 2026-08-29

**Revision 2 changes (this pass):** added a `Baiting` state between `Casting` and
`Reeling` (fish actively swim toward the bait, tilt can wiggle the bait, first
fish to arrive wins, times out to an empty reel if none arrive); brought back a
tension/line-snap mechanic in `Reeling`, but shaped differently from the
original — reeling is still auto constant-speed by default, with an optional
single-button "accelerate" that risks a snap the longer it's held, shown on a
tension bar. Both changes came from user review of revision 1 (see §5, updated).

## Process note (read this first)

**Claude's role on this document is design and later review only.** Claude did not
and will not write the implementation. The implementation is done by hand by the
project owner, coached step-by-step by Codex. Any implementation plan derived from
this spec must be written as **teaching steps for Codex to walk the user through**,
not as tasks for an AI to execute autonomously. The user needs to personally type
the code and wire the scene to build their own Unity skills — this is coursework,
not a delivery.

## 1. Context

This project ("Seven Seas") is a university rapid-prototyping assignment: iterate
on a pre-1984 coin-operated arcade game. The chosen base game is **Angler Dangler
(1982, Data East)**. Rules from [README.md](../../README.md): single-player,
no bombs, directional-pad + single-button controls, one mobile-specific mechanic,
HD retro sprite art, ship as HTML5/WebGL on itch.io.

Two sensor-driven prototypes already exist and are tested in the Unity Editor:

- **Tilt-drag** (`AttitudeReader.cs`, `AttitudeCircleController.cs`) — reads the
  phone's absolute orientation (`AttitudeSensor`) and moves a sprite based on tilt.
- **Flick-cast** (`GyroscopeReader.cs`, `CastGestureDetector.cs`,
  `CastBallController.cs`, `CastTuningProfile.cs`) — detects a flick gesture from
  gyroscope angular velocity, converts its peak into a "power" value via a tunable
  curve, and launches a `Rigidbody2D` with matching velocity.

Neither prototype is wired into an actual game loop yet — no fish, no scoring, no
lives, no obstacles. This design specifies that loop.

### Original game reference

Researched via web search (archive.org/details/arcade_cadanglr and other sources,
since this is an obscure 1982 title outside reliable built-in knowledge):

- Cast: aim with a joystick, hold a button to set power off a timed power bar.
- Reel: press a button to reel in; reeling too hard snaps the line.
- Hazard: rocks in the water; hitting one costs a hook.
- Hooks are lives; game over when they run out.
- Score is fish size; goal is the biggest catch before a timer runs out.

This design reinterprets that loop using the project's existing sensors (tilt +
flick) instead of joystick + timed button, and keeps a version of the line-snap
tension hazard, reshaped to fit the auto-reel structure (see §5).

## 2. Goals / Non-goals for this spec

**Goals:** a complete, playable cast → fly → wait-for-bite → reel loop, in a
single test scene, using placeholder shapes (no final art), that runs end-to-end
in the Unity Editor using the existing gyroscope/attitude simulation workflow
already validated by the team (see `findings.md`, `progress.md` at the repo root).

**Non-goals (explicitly deferred, not part of this MVP):**

- Final sprite art, animation, retro visual style.
- Fish variety/rarity/size tiers (MVP fish are interchangeable, same score value).
- Multiple/randomized rock layouts, rock variety.
- Main menu integration, scene-flow polish.
- Mobile-vs-desktop dual input (keyboard/D-pad fallback). This design uses the
  sensor + single-button path only; a keyboard/D-pad fallback is a known
  follow-up required by the project rules but out of scope for today's build.

## 3. State machine

```
ReadyToCast --(flick detected)--> Casting --(flight time elapsed)--> Baiting
Baiting --(a fish reaches the bait)--> Reeling (with fish)
Baiting --(wait timeout, no fish arrived)--> Reeling (empty)
Reeling --(reaches shore, has fish)--> catch scored --> ReadyToCast
Reeling --(reaches shore, no fish)--> empty reel, no score --> ReadyToCast
Reeling --(hits rock)--> lose one hook --> ReadyToCast (or GameOver if hooks == 0)
Reeling --(tension maxes out)--> line snaps, lose one hook --> ReadyToCast (or GameOver)
(any state) --(timer reaches 0)--> GameOver
```

### ReadyToCast

- Tilt (`AttitudeReader`, horizontal axis) moves the on-shore angler character
  left/right. This repositions *where along the shore* the cast will originate —
  it is a lane choice, not an aim angle. Reuses the same tilt-to-normalized-value
  logic already proven in `AttitudeCircleController`.
- A flick (`CastGestureDetector.CastDetected`) reads the current character lane
  position, reads the flick's power value (already computed by
  `CastTuningProfile.EvaluatePower`), and transitions to Casting.

### Casting

- Non-interactive. The hook/lure launches from the chosen shore lane using the
  existing `CastBallController` parabolic flight (reuse `Launch(power)` /
  `CastTuningProfile.EvaluateLaunchVelocity(power)`).
- Player input has **no effect** during this phase — it is purely the flight
  animation playing out, exactly as the user specified.
- Ends when the hook lands (existing ground-contact detection in
  `CastBallController`, currently used for the distance-test's "landed" event).
  The landing point is a 2D position: (shore lane X from `ReadyToCast`, distance
  from cast power).

### Baiting (new in this revision)

- Entered the instant the hook/bait lands in the water at the landing point
  above.
- **Player input:** tilt (same `AttitudeReader` axis) wiggles the bait's position
  within a small radius around the landing point. This is the same sensor as the
  lane control, just re-scoped to a small local nudge instead of the full shore
  range.
- **Fish behavior:** every fish within some detection range of the bait starts
  swimming toward it. This is a race — whichever fish's collider reaches the
  bait's hook radius first wins, hooks itself, and the rest turn away / go back
  to idle. (MVP needs at least 2 fish present for this race to mean anything —
  see §6.)
- **Timeout:** if no fish reaches the bait within **8 seconds** (default,
  tunable), the bait is retrieved with nothing hooked and Reeling starts empty.
- On a successful bite: Reeling starts "with a fish attached."

### Reeling

- The hook auto-retrieves at a constant speed toward the shore by default.
- Tilt (same `AttitudeReader` axis) controls the hook's left/right position,
  used to dodge rocks placed along the retrieval path.
- **Accelerate + tension (new in this revision):** holding the single button
  speeds up the retrieval rate above the default constant speed. While held, a
  tension value fills; a **tension bar UI** shows it live. If tension reaches
  its max, the line **snaps** — same consequence as a rock hit (lose one hook,
  attempt ends). Releasing the button lets tension drain back down, so the
  player can pulse the button for bursts of speed instead of holding it to a
  snap. Suggested defaults (tunable): reaches max after ~2.5s of continuous
  hold; drains to 0 in ~1s once released.
- Colliding with a rock: lose one hook immediately, this attempt ends (whether or
  not a fish was attached — it gets away too), return to `ReadyToCast` (or
  `GameOver` if that was the last hook).
- Line snapping from tension: same consequence as a rock hit.
- Reaching the shore without hitting a rock or snapping: if a fish was attached,
  award its score; return to `ReadyToCast` either way.

### Lives, timer, game over

- Start with **5 hooks**. Each rock collision or line snap costs one. `GameOver`
  when hooks reach 0.
- **90-second** session timer, counting down regardless of state. `GameOver` when
  it reaches 0.
- `GameOver` shows total score and stops accepting input.

*(5 hooks / 90 seconds / 8s bait-wait timeout / ~2.5s to snap / ~1s tension decay
are starting defaults, easy to retune later — flag during implementation if they
feel wrong in playtesting.)*

## 4. Components

### Reused as-is

- `AttitudeReader.cs` — tilt input source for the shore-lane control in
  `ReadyToCast`, the bait-wiggle control in `Baiting`, and the dodge control in
  `Reeling`.
- `GyroscopeReader.cs` + `CastGestureDetector.cs` — flick detection and power
  value for `ReadyToCast`.
- `CastTuningProfile.cs` — power curve and launch-velocity curve.

### Reused with changes

- `CastBallController.cs` — keep the parabolic `Launch(power)` flight and
  ground-contact detection, but its role changes: today it treats "landed" as
  the end of the test. In the new loop, "landed" is the trigger that ends
  `Casting` and starts `Baiting`, not the end of the attempt.

### New (conceptual — not implemented by Claude)

- A fishing-loop state machine component, replacing/expanding
  `CastTestController.cs`, owning the `ReadyToCast → Casting → Baiting → Reeling`
  flow above and the transitions to `GameOver`.
- Shore-lane character control (reads `AttitudeReader`, outputs a lane position;
  same shape as `AttitudeCircleController` but constrained to one axis and to the
  shore, not the full screen).
- Bait-wiggle control for `Baiting` (reads `AttitudeReader`, nudges the bait within
  a small radius — same sensor as the lane control, different scale/clamp).
- Fish "race to the bait" behavior: detection range, swim-toward-bait steering,
  first-arrival-wins resolution, losers return to idle.
- Reeling dodge control (reads `AttitudeReader` during `Reeling`, moves the hook
  left/right along the retrieval path).
- Reel-accelerate + tension component (reads the single button, raises retrieval
  speed while held, accumulates/decays a tension value, fires a "snapped" event
  at max).
- Tension bar UI (shows the live tension value during `Reeling`).
- A `Fish` representation: a 2D position, idle/approaching/hooked state, and a
  score value (flat for all fish in this MVP — see §2).
- `Rock` obstacles: fixed positions along the retrieval path with a collider.
- Hook count (lives) tracker.
- Session timer.
- Score tracker + minimal `GameOver` UI (reuse the existing debug-HUD patterns
  from `CastDebugHUD.cs`/`GyroscopeDebugHUD.cs` for consistency).

## 5. Deliberate deviations from the original, and why

These were explicit decisions made during design review, called out here so they
don't get "corrected" back by accident during implementation:

1. **Reel-tension is optional and player-triggered, not the default state.** The
   original always risks a snap while reeling. Here, the default retrieval is a
   safe constant speed; risk only appears if the player chooses to hold the
   accelerate button for speed. (Revision 1 had dropped the tension mechanic
   entirely — that was wrong; the user confirmed during review that a
   button-driven accelerate-with-snap-risk mechanic, plus a tension bar UI, was
   intended all along.)
2. **Aim happens before/during the cast, not after.** Tilt controls the shore
   character's lane *before* the flick, and flick power is read at the moment of
   the flick — together these replace the original's joystick-aim +
   timed-power-bar with the project's two existing sensor mechanics, reusing
   both without inventing a third input scheme.
3. **Biting is an active fish race, not an instant proximity check.** (Revision 1
   had this as an instant check at the landing point with no separate waiting
   state — that missed a state the user had already been picturing. Corrected in
   this revision: see the new `Baiting` state in §3.)

## 6. MVP content for today

- **2 fish** per attempt, positioned so the `Baiting` race is meaningful (both
  within reach of at least some lane/power combinations, at slightly different
  distances from likely landing points so the race has a real outcome instead of
  always being a tie).
- **1–2 rocks** on the retrieval path, enough to make dodging meaningful without
  making it unfair.
- Placeholder shapes only (circles/squares) — no final art. Richer content (more
  fish, fish variety, randomized rocks, real art, menu integration) is explicitly
  deferred (§2).

This revision is more work than revision 1's MVP (adds the `Baiting` state, fish
race behavior, and the tension/accelerate mechanic) — flagging that plainly since
it changes today's scope, not just the design's fidelity to the original.

## 7. Verification plan

- Playable end-to-end in the Unity Editor using the same gyroscope/attitude
  simulation workflow already used for the existing prototypes (per
  `findings.md`: Unity Editor's remote input simulation, no device build
  required for a first pass).
- Manual test checklist (to be run by the user, not automated today):
  1. Tilt moves the shore character; flick casts from the current lane.
  2. After landing, tilt wiggles the bait; whichever of the 2 fish reaches it
     first hooks itself and Reeling starts "with a fish attached."
  3. Letting the 8-second wait expire with no fish arriving starts Reeling
     "empty."
  4. During Reeling, tilt moves the hook; touching a rock costs a hook and ends
     the attempt.
  5. Holding the accelerate button speeds up retrieval and fills the tension
     bar; releasing before max lets it drain; holding to max snaps the line and
     costs a hook, same as a rock hit.
  6. Reaching shore with a fish attached adds score; without one, no score.
  7. Hooks reaching 0 or the timer reaching 0 ends the game and shows the score.

## 8. Follow-ups (explicitly not today)

- Keyboard/D-pad input fallback for non-mobile play (required by project rules
  before shipping, not required for today's playable slice).
- More fish, fish variety/size tiers feeding into score.
- Randomized/varied rock layouts.
- Real sprite art and animation.
- MainMenu → gameplay → results scene flow integration.
