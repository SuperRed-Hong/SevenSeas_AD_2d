# Fishing Loop Design — Cast, Fly, Reel (MVP)

Status: approved by user, ready for planning
Date: 2026-08-29

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
flick) instead of joystick + timed button, per one deliberate simplification noted
in §5.

## 2. Goals / Non-goals for this spec

**Goals:** a complete, playable cast → fly → reel loop, in a single test scene,
using placeholder shapes (no final art), that runs end-to-end in the Unity Editor
using the existing gyroscope/attitude simulation workflow already validated by the
team (see `findings.md`, `progress.md` at the repo root).

**Non-goals (explicitly deferred, not part of this MVP):**

- Final sprite art, animation, retro visual style.
- Multiple simultaneous fish, fish variety/rarity, fish AI/movement.
- Multiple/randomized rock layouts, rock variety.
- Main menu integration, scene-flow polish.
- Any manual "don't reel too hard" tension mechanic (see §5 for why).
- Mobile-vs-desktop dual input (keyboard/D-pad fallback). This design uses the
  sensor path only; a keyboard/D-pad fallback is a known follow-up required by
  the project rules but out of scope for today's build.

## 3. State machine

```
ReadyToCast --(flick detected)--> Casting --(flight time elapsed)--> Reeling
Reeling --(reaches shore, has fish)--> catch scored --> ReadyToCast
Reeling --(reaches shore, no fish)--> empty reel, no score --> ReadyToCast
Reeling --(hits rock)--> lose one hook --> ReadyToCast (or GameOver if hooks == 0)
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

### Bite check (happens at the instant Casting ends)

- The landing point is a 2D position: (shore lane X from `ReadyToCast`, distance
  from cast power). Each fish for this attempt occupies one fixed 2D position in
  the water.
- If the landing point is within a small tolerance radius of the fish's position,
  the fish bites immediately (no dwell/wait — see §5 for why this differs from an
  earlier draft of this idea) and Reeling starts "with a fish attached."
- Otherwise Reeling starts "empty" — the hook still needs to be reeled back in
  (so rocks are still a hazard), but landing it will not score.

### Reeling

- The hook auto-retrieves at a constant speed toward the shore (no button, no
  tension meter — see §5).
- Tilt (same `AttitudeReader` axis) now controls the hook's left/right position,
  used to dodge rocks placed along the retrieval path.
- Colliding with a rock: lose one hook immediately, this attempt ends (whether or
  not a fish was attached — it gets away too), return to `ReadyToCast` (or
  `GameOver` if that was the last hook).
- Reaching the shore without hitting a rock: if a fish was attached, award its
  score; return to `ReadyToCast` either way.

### Lives, timer, game over

- Start with **5 hooks**. Each rock collision costs one. `GameOver` when hooks
  reach 0.
- **90-second** session timer, counting down regardless of state. `GameOver` when
  it reaches 0.
- `GameOver` shows total score and stops accepting input.

*(5 hooks / 90 seconds are starting defaults, easy to retune later — flag during
implementation if they feel wrong in playtesting.)*

## 4. Components

### Reused as-is

- `AttitudeReader.cs` — tilt input source for both the shore-lane control in
  `ReadyToCast` and the dodge control in `Reeling`.
- `GyroscopeReader.cs` + `CastGestureDetector.cs` — flick detection and power
  value for `ReadyToCast`.
- `CastTuningProfile.cs` — power curve and launch-velocity curve.

### Reused with changes

- `CastBallController.cs` — keep the parabolic `Launch(power)` flight and
  ground-contact detection, but its role changes: today it treats "landed" as
  the end of the test. In the new loop, "landed" is the trigger for the bite
  check and the *start* of Reeling, not the end of the attempt.

### New (conceptual — not implemented by Claude)

- A fishing-loop state machine component, replacing/expanding
  `CastTestController.cs`, owning the `ReadyToCast → Casting → Reeling` flow
  above and the transitions to `GameOver`.
- Shore-lane character control (reads `AttitudeReader`, outputs a lane position;
  same shape as `AttitudeCircleController` but constrained to one axis and to the
  shore, not the full screen).
- Reeling dodge control (reads `AttitudeReader` during `Reeling`, moves the hook
  left/right along the retrieval path).
- A `Fish` representation for this attempt: a 2D position + a score value.
- `Rock` obstacles: fixed positions along the retrieval path with a collider.
- Hook count (lives) tracker.
- Session timer.
- Score tracker + minimal `GameOver` UI (reuse the existing debug-HUD patterns
  from `CastDebugHUD.cs`/`GyroscopeDebugHUD.cs` for consistency).

## 5. Deliberate deviations from the original, and why

These were explicit decisions made during design review, called out here so they
don't get "corrected" back by accident during implementation:

1. **No manual reel-tension/mash mechanic.** The original's "don't reel too hard
   or the line snaps" was initially going to be kept, but once the state machine
   was pinned down, reeling was defined as automatic constant-speed retrieval —
   the dodge-the-rocks part carries the challenge instead. Rocks still cost a
   hook on collision, matching the original's hazard/lives coupling.
2. **Bite is instantaneous at landing, not a dwell/chase.** An earlier draft of
   this design had the player tilt-steer the hook underwater *after* landing to
   reach a fish, with a short dwell time before auto-catching. That was replaced
   by folding aim into the cast itself (tilt = lane, flick = power), so the bite
   check now happens once, at the moment the hook lands, by comparing landing
   position to fish position.
3. **Aim happens before/during the cast, not after.** Tilt controls the shore
   character's lane *before* the flick, and flick power is read at the moment of
   the flick — together these replace the original's joystick-aim +
   timed-power-bar with the project's two existing sensor mechanics, reusing
   both without inventing a third input scheme.

## 6. MVP content for today

- **1 fish** per attempt, placed at a position reachable by some combination of
  lane + power (not placed out of reach).
- **1–2 rocks** on the retrieval path, enough to make dodging meaningful without
  making it unfair.
- Placeholder shapes only (circles/squares) — no final art. Richer content
  (multiple fish, fish variety, randomized rocks, real art, menu integration) is
  explicitly deferred (§2).

## 7. Verification plan

- Playable end-to-end in the Unity Editor using the same gyroscope/attitude
  simulation workflow already used for the existing prototypes (per
  `findings.md`: Unity Editor's remote input simulation, no device build
  required for a first pass).
- Manual test checklist (to be run by the user, not automated today):
  1. Tilt moves the shore character; flick casts from the current lane.
  2. Landing near the fish starts Reeling "with a fish attached"; landing away
     from it starts Reeling "empty."
  3. During Reeling, tilt moves the hook; touching a rock costs a hook and ends
     the attempt.
  4. Reaching shore with a fish attached adds score; without one, no score.
  5. Hooks reaching 0 or the timer reaching 0 ends the game and shows the score.

## 8. Follow-ups (explicitly not today)

- Keyboard/D-pad input fallback for non-mobile play (required by project rules
  before shipping, not required for today's playable slice).
- Multiple fish, fish variety/size tiers feeding into score.
- Randomized/varied rock layouts.
- Real sprite art and animation.
- MainMenu → gameplay → results scene flow integration.
