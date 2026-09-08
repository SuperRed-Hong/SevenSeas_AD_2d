# Fishing Loop Design — Cast, Fly, Reel (MVP)

Status: revision 9, pending user re-review
Date: 2026-08-29 (revision 5 added 2026-08-30; revisions 6, 7 and 8 added
2026-09-01; revision 9 added 2026-09-04)

**Revision 2 changes:** added a `Baiting` state between `Casting` and `Reeling`
(fish actively swim toward the bait, tilt can wiggle the bait, first fish to
arrive wins, times out to an empty reel if none arrive); brought back a
tension/line-snap mechanic in `Reeling`, but shaped differently from the
original — reeling is still auto constant-speed by default, with an optional
single-button "accelerate" that risks a snap the longer it's held, shown on a
tension bar. Both changes came from user review of revision 1 (see §5).

**Revision 3 changes (this pass):** renamed `Baited` → `Baiting` for naming
consistency with `Casting`/`Reeling` (both name the state after the ongoing
action, not a completed one). Added a new `Striking` state between a successful
bite in `Baiting` and `Reeling`: the player has a short reaction window to flick
the phone in the *opposite* rotational direction from the cast flick (a
"backward"/counter-clockwise wrist snap, mirroring the cast's outward/clockwise
snap) to set the hook. Missing the window fails the attempt and costs a hook
without ever entering `Reeling`. See §3 and §5.

**Revision 4 changes:** final consistency check, not a design change — fixed
the state diagram's `Casting → Baiting` transition label (was "flight time
elapsed," which contradicted the `Casting` section's own text; it's actually
the landing detection), and folded the `TriggerThreshold` re-tuning
requirement (added earlier in that pass to §3) into the Goals line and this
changelog for completeness.

**Revision 5 changes (this pass, found during M3 implementation):**
`CastBallController.cs`/`GyroscopeCastTest.unity` is **not** reused for the
production cast flight after all — that scene's flight is a horizontal
side-view parabola, but the production scene casts vertically on screen into
the water, a different orientation the existing physics don't transplant into.
`CastBallController` and its test scene stay untouched and isolated; a new
flight component gets built for the production scene instead, matching its
public shape (a `Launch(power)`-style entry point, a generic landing event)
without being the same component. `FishingLoopController` still owns the
`Casting → Baiting` transition off that generic event, unchanged. See §3 and
§4.

**Revision 6 changes (this pass, requested by the user during M5):** `Striking`
is no longer a bare "reverse-flick inside 0.7 seconds" reflex check. It becomes
a **shrinking ring / radial timing skill check** — a fixed full-ring target band
plus a moving ring that shrinks linearly from the outer edge to the centre
across the whole window. An attempt (reverse flick, or keyboard Space) succeeds
only if the moving ring is inside the band at that instant, and an attempt made
outside the band is **rejected rather than fatal**: it is ignored and starts a
short input cooldown, with a red ring flash and a light camera shake as
feedback. Only the window running out still costs a hook. Revision 6 also splits
`Striking` across three owners (`StrikeController` / `StrikeWindowHUD` /
`FishingLoopController`) and pins the strike input to
`CastGestureDetector.GestureTriggered` — the immediate threshold-crossing event
— rather than `CastDetected`, which only fires after the cast path's peak
sampling has finished and would therefore report a gesture that already
happened. See §3, §4, §5 and §7.

**Also in revision 6, and deliberately partial:** §2's "mobile-vs-desktop dual
input" non-goal could not survive this change, because the new `Striking` spec
names keyboard Space as a first-class strike attempt. That bullet is corrected
below **only as far as `Striking` requires**. The full reconciliation of the
shared `FishingInputSource` contract across every state — and the matching §8
follow-up entry — is still owed and was **not** attempted in this pass.

**Revision 7 changes (this pass, requested by the user during M6):** `Reeling`'s
tension is no longer driven by the accelerate button alone. It becomes a
**continuously-evaluated rate sum**, clamped to `0–1`: a **passive decay that
always applies**, an **accelerate rise rate** (greater than the decay, so
holding the button produces net growth), and a **lateral contribution scaled by
the hook's absolute actual lateral speed** — gentle dodging contributes little
enough that tension still falls, faster dodging progressively contributes enough
to overcome the decay. The accelerate and lateral contributions **stack**, so
accelerating while weaving hard is now the fastest way to snap a line. The snap
event fires **once** on reaching maximum. This makes dodging and accelerating
interact instead of being independent systems, which changes deviation §5.1's
claim that risk only appears when the player chooses it — see §3, §4 and §5.

**Revision 8 changes (this pass, requested by the user):** the feel/balance
numbers introduced in revisions 6 and 7 move out of scene-serialized fields and
into **`ScriptableObject` tuning profiles**, following the precedent
`CastTuningProfile` already set. The decisive reason is workflow, not tidiness:
values on a scene component **revert when Play Mode exits**, and every one of
these numbers can only be settled *by playing*. §4 gains a new subsection
stating where each kind of parameter lives and why, adds `StrikeWindowProfile`
and `ReelTuningProfile`, requires the profiles to validate their own invariants
rather than fail silently, and renames the strike detector's asset from revision
6's "StrikeTuningProfile" to **`StrikeGestureProfile`**, now that a second
strike-related asset exists and the old name no longer says which is which.

**Revision 9 changes (this pass, requested by the user on 2026-09-04) —
DEFERRED IMPLEMENTATION, captured now so nothing is lost.** None of this is part
of the current M6 work; the shipped `Striking` implements revision 6 as written,
and these four changes are queued behind M6/M7. See the "Implementation status"
box in §3's `Striking` section.

1. **The target band's position is randomized per attempt.** Its *width* still
   comes from the profile; the *radius it appears at* is drawn at random within
   a tunable range each time `Striking` begins. Turns the check from "learn one
   fixed rhythm" into "read and react."
2. **A hidden forgiveness margin separates the drawn band from the judged
   band.** One profile parameter widens the success test by a fixed radius
   beyond what is drawn, absorbing the sensor's detection latency on the reverse
   flick. The margin is never drawn.
3. **The ring makes two passes, not one:** outer → centre, then centre → outer.
   The band is crossed once per pass, so the player gets **two chances** per
   `Striking` entry instead of one.
4. **A cooldown on returning to `ReadyToCast` after a completed attempt.**
   Tunable; the player stays in `ReadyToCast` and can still move along the
   shore, but cannot cast until it elapses. See §3's `ReadyToCast` section.

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
lives, no obstacles. This design specifies that loop. (For a full explainer of
how these two prototypes and their HUDs actually work, see
[`docs/reference/motion-input-guide.md`](../reference/motion-input-guide.md).)

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

**Goals:** a complete, playable cast → fly → wait-for-bite → strike → reel loop,
in a single test scene, using placeholder shapes (no final art), that runs end-to-end
in the Unity Editor using the existing gyroscope/attitude simulation workflow
already validated by the team (see `findings.md`, `progress.md` at the repo root).

**Non-goals (explicitly deferred, not part of this MVP):**

- Final sprite art, animation, retro visual style.
- Fish variety/rarity/size tiers (MVP fish are interchangeable, same score value).
- Multiple/randomized rock layouts, rock variety.
- Main menu integration, scene-flow polish.
- **Mobile-vs-desktop dual input (keyboard/D-pad fallback) — partly superseded
  in revision 6.** As originally written, this design used the sensor +
  single-button path only. During implementation a shared semantic input
  contract was built instead (`FishingInputSource`, putting the keyboard/mouse
  implementation and the sensor path behind the same Move / Cast / Strike /
  Accelerate intentions), and it is in use — so this document can no longer
  describe keyboard input as out of scope: §3's `Striking` below names keyboard
  Space as one of the two ways to make a strike attempt. Revision 6 corrects
  this bullet **only** as far as `Striking` needs. Whether keyboard support is
  permanent cross-platform scope or Editor-only scaffolding — and what that
  implies for `ReadyToCast`, `Baiting` and `Reeling` — remains open for a later
  revision.

## 3. State machine

```
ReadyToCast --(outward flick detected)--> Casting --(hook lands)--> Baiting
Baiting --(a fish reaches the bait)--> Striking
Baiting --(wait timeout, no fish arrived)--> Reeling (empty)
Striking --(attempt while the moving ring is inside the target band)--> Reeling (with fish)
Striking --(attempt outside the band)--> rejected: ignored + input cooldown, stay in Striking
Striking --(window elapses with no successful attempt)--> lose one hook --> ReadyToCast (or GameOver)
Reeling --(reaches shore, has fish)--> catch scored --> ReadyToCast*
Reeling --(reaches shore, no fish)--> empty reel, no score --> ReadyToCast*
Reeling --(hits rock)--> lose one hook --> ReadyToCast* (or GameOver if hooks == 0)
Reeling --(tension maxes out)--> line snaps, lose one hook --> ReadyToCast* (or GameOver)
(any state) --(timer reaches 0)--> GameOver

* every return from Reeling enters ReadyToCast with casting locked out for
  postAttemptCooldown (revision 9) — lane movement stays available
```

### ReadyToCast

- Tilt (`AttitudeReader`, horizontal axis) moves the on-shore angler character
  left/right. This repositions *where along the shore* the cast will originate —
  it is a lane choice, not an aim angle. Reuses the same tilt-to-normalized-value
  logic already proven in `AttitudeCircleController`.
- A flick (`CastGestureDetector.CastDetected`) reads the current character lane
  position, reads the flick's power value (already computed by
  `CastTuningProfile.EvaluatePower`), and transitions to Casting. This is the
  **outward** wrist snap (clockwise, per the user's description) — its
  direction matters now, because `Striking` (§below) uses the opposite
  direction on the same axis, and the two must not be confused with each other.
- **`TriggerThreshold` must be re-tuned higher for this combined context.** The
  existing `CastTuningProfile.TriggerThreshold` (1.25, per the current asset)
  was tuned in `GyroscopeCastTest.unity`, a scene where flicking was the *only*
  thing happening. In `ReadyToCast`, tilt-driven lane selection is running
  continuously at the same time, and ordinary quick tilt adjustments can
  produce angular-velocity spikes too. If the threshold isn't clearly above
  that everyday-tilt noise floor, casting will trigger by accident while the
  player is just choosing a lane. The threshold needs to sit high enough that
  only a deliberate, obvious flick crosses it — verify this empirically with
  the existing debug HUD tooling (`GyroscopeDebugHUD.cs`) while performing
  normal lane-selection tilting, not just by picking a number that feels right
  on paper.

#### Post-attempt cooldown (new in revision 9 — not yet implemented)

Every return from `Reeling` — scored catch, empty reel, rock hit or line snap —
enters `ReadyToCast` with **casting locked out for `postAttemptCooldown`**
(tunable, starting value **0.8 s**).

- **The player stays in `ReadyToCast`.** Lane movement remains available, so
  the cooldown is repositioning time rather than dead time. No seventh state is
  added: this is a temporary input restriction inside an existing state, the
  same shape `FishingSceneEntryGuard` already uses to hold calibration outside
  the six-state machine.
- **The cast detector stays disabled** for the duration, on top of the existing
  rule that it is only enabled during `ReadyToCast`.
- **The session timer keeps running.** The cooldown costs real session time,
  which is what stops it being free.

Three reasons it exists:

1. **The outcome needs a beat to read.** A catch that scores, a snapped line and
   a rock hit currently all dump the player straight back into a castable state,
   with no room for the score to land or the hook to visibly return to its dock.
2. **It stops re-cast spam** on a failed attempt, which otherwise rewards
   flailing over aiming.
3. **It absorbs the strike's recovery motion.** The reverse flick that sets the
   hook ends with the wrist travelling *forward* again — the cast direction. A
   cooldown covering that recovery removes a whole class of accidental casts.

**Open question, flagged rather than decided:** reason 3 applies just as much to
the `Striking`-timeout path, which also returns to `ReadyToCast` immediately and
also follows a reverse flick. This revision applies the cooldown only to the
four `Reeling` exits, as literally requested. Extending it to the
`Striking`-timeout return is a one-line change if wanted, but it makes losing a
hook cost time as well, so it is a balance decision, not a cleanup.

`postAttemptCooldown` is a loop-level feel value, so by §4's tuning rule it
belongs in a small **`FishingLoopProfile`** rather than on any of the existing
per-system profiles. M7's "5 hooks" and "90-second timer" are the same kind of
value and should join it there rather than each acquiring a home of its own.

### Casting

- Non-interactive. Player input has **no effect** during this phase — it is
  purely the flight animation playing out, exactly as the user specified.
- Ends when the hook lands. The landing point is a 2D position: (shore lane X
  from `ReadyToCast`, distance from cast power).
- **Not a direct reuse of `CastBallController`.** `GyroscopeCastTest.unity`'s
  flight is a horizontal side-view parabola (Rigidbody2D arcing across flat
  ground). The production fishing scene's camera/layout casts **vertically
  upward on screen into the water**, a different orientation entirely — the
  existing component's physics don't transplant as-is. `CastBallController`
  and `GyroscopeCastTest.unity` stay exactly as they are, untouched, as an
  isolated distance-test scene. A **new** flight component is built for the
  production scene, matching `CastBallController`'s public shape (a
  `Launch(power)`-style entry point, a generic landing event) but with
  velocity/trajectory physics appropriate to the vertical orientation.
  `CastTuningProfile.EvaluatePower(peak)` (peak angular velocity → 0–1 power)
  is orientation-agnostic and stays reusable as-is; whether
  `EvaluateLaunchVelocity(power)`'s current min/max velocity vectors carry over
  or need new values for the vertical layout is an open question for M3's
  architecture discussion, not resolved here.
- The landing event stays **generic** — `FishingLoopController` (the state
  machine) owns the transition to `Baiting` when it fires, and doesn't need to
  know or care which flight component raised it. This is the same
  decoupling-via-events principle already called out for `CastBallController`
  in §4 below, just reinforced now that two different flight implementations
  exist side by side.

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
- On a successful bite: enters `Striking` (below) — biting does not immediately
  start Reeling.

### Striking (reworked in revision 6)

Entered the instant a fish reaches the bait in `Baiting`. This is still the
"hook-set" reaction moment: in real fishing (and the original's press-a-button
bite response) you have to react to the bite, not just wait it out.

Revision 3's version of that moment was a bare reflex test — one reverse flick
inside a 0.7-second window, miss and lose a hook — with nothing on screen to
time against. Revision 6 replaces it with a **shrinking ring / radial timing
skill check**.

> **Implementation status.** The version shipped in M5 is revision 6's: one
> fixed band, one inward pass, no forgiveness margin. Revision 9's three changes
> to this state (randomized band position, hidden forgiveness margin, two-pass
> ring) are **specified but not implemented** — queued behind M6/M7. The text
> below describes the intended end state; the paragraphs marked *(revision 9)*
> are the parts not yet built.

**What the player sees**

- Two concentric rings, centred on the bait/hook.
- A **target band**: a complete ring band (an annulus) that holds still for the
  whole attempt. It is a **full 360° band, not a sector and not an arc** — the
  strike input carries no direction or aim component, so an arc would promise an
  aiming task the input cannot express. The band is the success region.
- **The band's radius is randomized per attempt *(revision 9)*.** Its width is
  fixed by `targetBandWidth` in the profile, but its centre radius is drawn
  uniformly at random from
  `[minBandCenterRadius, maxBandCenterRadius]` each time `Striking` begins.
  The point is to stop the check being memorized: with a fixed band, a player
  eventually strikes on a stopwatch and stops looking at the screen. With a
  random one, they have to actually read the ring every time.
  Two guards, both required rather than advisory:
  - The band must fit inside `[0, 1]`, so the drawn centre is clamped to
    `[targetBandWidth / 2, 1 - targetBandWidth / 2]`.
  - `maxBandCenterRadius` must stay meaningfully below `1`. A band sitting at
    the outer edge is entered the instant `Striking` starts, before the player
    has read anything — that is not a hard attempt, it is an unfair one.
- A **moving ring**. It starts at the outermost radius the moment `Striking`
  begins and travels **linearly**, sweeping the whole radius range across
  `windowDuration`.

**The ring makes two passes *(revision 9)*.** It runs outer → centre, then
turns around at the centre and runs centre → outer, with `windowDuration`
covering **both** legs (so each leg takes half of it). Its normalized radius
over the window is simply:

```
p          = elapsed / windowDuration        // 0 → 1
ringRadius = Abs(1 - 2p)                     // 1 → 0 → 1
```

The band is therefore crossed **twice**, giving the player **two chances per
`Striking` entry** instead of one. A first mistimed attempt no longer costs the
whole bite — it costs the first pass. This also takes most of the sting out of
`attemptCooldown`: a cooldown that swallows the first crossing usually still
leaves the second, which matters more now that the band's position is random and
the player can misjudge it honestly.

Note that `ringRadius` is no longer monotonic, so `StrikeController` publishes
which leg is running (or the direction of travel) alongside the radius — the HUD
may want to show it, and a debugger definitely does.

**Making an attempt**

- One attempt is a **backward/counter-clockwise flick** on the phone (the same
  reversed wrist snap as revision 3 — direction still matters, so the fish is
  never hooked by the same motion that cast the line), **or** a keyboard
  **Space** press. Both express the same semantic intention and are judged
  identically.
- An attempt is judged at the instant it is made, against the moving ring's
  position at that instant.
- **Inside the band → success.** The hook is set; enter `Reeling` "with a fish
  attached." The first success ends `Striking` immediately — there is no second
  success to collect.

**The judged band is slightly larger than the drawn band *(revision 9)*.** A
single profile parameter, `hitForgivenessRadius`, widens the success test on
both sides of what the HUD draws:

```
effectiveInner = Clamp01(visualInner - hitForgivenessRadius)
effectiveOuter = Clamp01(visualOuter + hitForgivenessRadius)
```

**Why it is needed:** the moment the player *intends* the flick and the moment
the detector *reports* it are not the same. Between them sit the sensor's
sampling interval (up to ~17 ms at 60 Hz), the low-pass filter's lag, and the
time it takes a real wrist to cross `TriggerThreshold`. The ring keeps moving
through all of that, so a perfectly-judged flick registers slightly past where
the player saw the ring. Without a margin, the mechanic punishes latency rather
than timing.

**Why a symmetric margin is the right shape here, despite the lag being
one-directional:** on the inward leg, latency pushes the registered position
*inward*; on the outward leg it pushes it *outward*. With the two-pass ring
those two errors point opposite ways, so one symmetric widening covers both. It
is also simply the cheapest thing that works, which is what this parameter is
for.

Two rules about it:

- **Never draw the forgiveness margin.** The HUD draws the visual band only. If
  the real band is visible, players aim at the real band and the forgiveness is
  spent immediately — the effect only exists while it is invisible.
- `StrikeController` therefore publishes the **visual** bounds for the HUD and
  keeps the effective bounds private for judging. Two values that look
  interchangeable but must not be unified; keeping them apart is the whole
  mechanism.
- **Outside the band → rejected, not failed.** The attempt is ignored, costs
  nothing directly, and starts a short, tunable **input cooldown**; no further
  attempt is accepted while it runs. `Striking` continues.
- An attempt made *during* an active cooldown is ignored outright — it does not
  restart or extend the cooldown, so mashing cannot compound the penalty. The
  HUD still shows the cooldown, so the player can see why nothing happened.
- There is no attempt limit. The cooldown is the only limiter — and it is a real
  cost, because a cooldown started just before the band can swallow the player's
  one good moment.

**Feedback**

- A rejected attempt flashes the rings **red** and triggers a **light camera
  shake**. Both are feedback only; neither changes state.
- A successful attempt reads as the transition into `Reeling` itself.

**Failure**

- The only failure is **the window elapsing with no successful attempt** — both
  passes spent, including any crossing the player was locked out of by a
  cooldown. Lose one hook, return to `ReadyToCast` (or `GameOver` if that was
  the last hook). `Reeling` is never entered; there is nothing to reel back, the
  fish is simply gone.
- The consequence is unchanged from revision 3. Only the path to it changed.

**No other input applies** during this state — no tilt, no lane movement. It is
a single timing check, not a positioning task.

**Tunable parameters.** These live on a `StrikeWindowProfile` `ScriptableObject`
asset, not on the scene component — see §4's "Tuning data" subsection for why.
All are to be settled by Play Mode feel rather than on paper; the values below
are starting guesses only.

| Parameter | Starting value | Meaning |
|---|---|---|
| `windowDuration` | 2.0 s | Total time for **both** ring passes (1.0 s per leg) |
| `targetBandWidth` *(rev 9)* | 0.16 | Band thickness as a normalized radius; replaces the two fixed radii |
| `minBandCenterRadius` *(rev 9)* | 0.25 | Lower bound of the random band centre |
| `maxBandCenterRadius` *(rev 9)* | 0.70 | Upper bound of the random band centre — keep well below 1 |
| `hitForgivenessRadius` *(rev 9)* | 0.04 | Hidden widening of the judged band on each side |
| `attemptCooldown` | 0.35 s | Input lockout after a rejected attempt |
| shake amplitude / duration | ~0.12 units / 0.15 s | Rejected-attempt camera shake |

Revisions 6–8 used fixed `targetBandOuterRadius` / `targetBandInnerRadius`
(0.40 / 0.24) instead of the three revision 9 band parameters. That is what the
shipped M5 currently reads.

The moving ring's position is published as a **normalized radius** — `1` at the
outer edge, `0` at the centre — so the band, the forgiveness margin and the ring
are all expressed in the same units and the success test is one range check.

The property that makes this tunable at all: the ring travels **linearly** in
both radius and time, so a width in normalized radius converts directly into
seconds. With `windowDuration` covering two legs, each leg lasts
`windowDuration / 2`, and the in-band time per crossing is:

```
(targetBandWidth + 2 × hitForgivenessRadius) × windowDuration / 2
```

At the starting values: `(0.16 + 0.08) × 1.0 = 0.24 s` per crossing, **twice** —
about `0.48 s` of total forgiving time, against revision 6's single `0.24 s`.
Revision 9 is therefore meaningfully *more* generous overall even though the
band moved and each individual leg got shorter, which is the intended trade:
harder to memorize, easier to survive.

`windowDuration` rose from 1.5 s to 2.0 s precisely because it now buys two legs
instead of one; at 1.5 s each leg would be only 0.75 s and each crossing 0.18 s.
(Revision 3's original figure was 0.7 s, for a pure reflex check with no run-up
at all — the number has grown every time the state gained something to read.)

### Reeling (tension reworked in revision 7)

- The hook auto-retrieves at a constant speed toward the shore by default.
- Tilt (same `AttitudeReader` axis) controls the hook's left/right position,
  used to dodge rocks placed along the retrieval path.
- Holding the single button **accelerates** the retrieval rate above the default
  constant speed.
- Colliding with a rock: lose one hook immediately, this attempt ends (whether or
  not a fish was attached — it gets away too), return to `ReadyToCast` (or
  `GameOver` if that was the last hook).
- Line snapping from tension: same consequence as a rock hit.
- Reaching the shore without hitting a rock or snapping: if a fish was attached,
  award its score; return to `ReadyToCast` either way.

#### Tension

`tension` is a single value **clamped to `0–1`** and shown live on a **tension
bar UI**. It is updated once per frame from a **sum of rates**, not from a
branch:

```
netRate = -decayRate
        + (accelerateHeld ? accelerateRiseRate : 0)
        + lateralRate(|hookLateralSpeed|)

tension = Clamp01(tension + netRate * deltaTime)
```

**Passive decay always applies.** `decayRate` is subtracted every frame
unconditionally — there is no "idle" branch. A player doing nothing is always
recovering.

**Accelerate adds a positive rate greater than the decay rate.** So holding the
button produces net growth on its own, and releasing it immediately returns the
line to net decay. Pulsing the button for bursts of speed remains the intended
skilful play.

**Lateral dodging also contributes, scaled by the hook's absolute actual lateral
speed:**

```
t           = InverseLerp(safeLateralSpeed, maxEvaluatedLateralSpeed, |hookLateralSpeed|)
lateralRate = t * maxLateralTensionRate
```

- Below `safeLateralSpeed` the contribution is zero, so gentle repositioning
  leaves tension falling.
- Just above it the contribution is small — small enough that tension **may
  still decrease**. There is a crossover speed at which dodging alone exactly
  cancels the decay:
  `Lerp(safeLateralSpeed, maxEvaluatedLateralSpeed, decayRate / maxLateralTensionRate)`.
  Below that speed, dodging is free; above it, dodging alone grows tension.
- At and above `maxEvaluatedLateralSpeed` the contribution is capped at
  `maxLateralTensionRate`, which must be **greater than `decayRate`** for fast
  weaving to produce net growth at all.

**`hookLateralSpeed` is the hook's *actual* movement, not the input value** —
measured from how far the hook really moved this frame, divided by the frame
time, absolute value taken (direction is irrelevant). This matters concretely:
holding the dodge control hard against a lane boundary produces a large input
but zero movement, and must therefore cost nothing. It also keeps this rule
independent of how the dodge control is implemented, including any smoothing
between input and position.

**The two contributions stack.** Accelerating *while* weaving hard is the
fastest possible fill and the most likely way to snap a line — that interaction
is the point, not a side effect (see §5.6).

**Snapping.** When `tension` reaches its maximum the line **snaps, and the snap
event fires exactly once** — not once per frame while pinned at the top. Same
consequence as a rock hit: lose one hook, the attempt ends.

**Tunable parameters.** These live on a `ReelTuningProfile` `ScriptableObject`
asset alongside the retrieval speeds — see §4's "Tuning data" subsection. The
values below are starting values only; settle them in Play Mode.

| Parameter | Starting value | Meaning |
|---|---|---|
| `decayRate` | 0.8 /s | Always-on passive drain; full bar empties in ~1.25 s |
| `accelerateRiseRate` | 1.2 /s | Added while the button is held |
| `safeLateralSpeed` | 2.0 units/s | At or below this, dodging contributes nothing |
| `maxEvaluatedLateralSpeed` | 6.0 units/s | Speed at which the lateral contribution is capped |
| `maxLateralTensionRate` | 1.4 /s | The capped lateral contribution |

What those starting values imply, so the tuning pass has something to compare
against:

| Situation | Net rate | Time from empty to snap |
|---|---|---|
| Idle / gentle dodging | −0.8 /s | never (drains) |
| Accelerate only | +0.4 /s | ~2.5 s |
| Max-speed dodging only | +0.6 /s | ~1.7 s |
| Accelerate + max-speed dodging | **+1.8 /s** | **~0.6 s** |
| Dodging break-even speed | 0 | ~4.3 units/s |

The two speed thresholds are in **world units per second and depend entirely on
how fast the dodge control can actually move the hook** — they cannot be guessed
on paper. Measure the hook's real maximum lateral speed in Play Mode first, then
set `maxEvaluatedLateralSpeed` at or near it and `safeLateralSpeed` at roughly a
third of it.

### Lives, timer, game over

- Start with **5 hooks**. Each rock collision or line snap costs one. `GameOver`
  when hooks reach 0.
- **90-second** session timer, counting down regardless of state. `GameOver` when
  it reaches 0.
- `GameOver` shows total score and stops accepting input.

*(5 hooks / 90 seconds / 8s bait-wait timeout / 1.5s strike window are starting
defaults, easy to retune later — flag during implementation if they feel wrong
in playtesting. `Striking`'s and `Reeling`-tension's full parameter sets have
their own tables above.)*

## 4. Components

### Reused as-is

- `AttitudeReader.cs` — tilt input source for the shore-lane control in
  `ReadyToCast`, the bait-wiggle control in `Baiting`, and the dodge control in
  `Reeling`.
- `GyroscopeReader.cs` + `CastGestureDetector.cs` — flick detection and power
  value for `ReadyToCast`; flick detection *only* for `Striking`. The reuse
  route sketched in revision 3 is confirmed: a **second detector instance**
  paired with a **second tuning-profile asset (`StrikeGestureProfile`)** whose
  `InvertAxis` is the opposite of the cast profile's fires on the reversed wrist
  snap, with no new gesture-detection code written.

  **Revision 6 pins which event the strike path uses.** `CastDetected` is the
  wrong one — it fires only after the detector's `Sampling` state has run for
  `SampleWindow`, so it reports a gesture that already finished. In a check
  where the ring moves every frame, that latency is fatal. `Striking` subscribes
  to **`GestureTriggered`** instead: the parameterless event raised on the frame
  the threshold is crossed, in the `Ready → Sampling` transition. The cast path
  keeps using `CastDetected` and does not change at all.

  Two consequences to respect:

  - `StrikeGestureProfile`'s `Power` and `Launch` sections are **unused** by the
    strike path. No power value is read and no launch velocity is evaluated;
    only the `Sensor` section matters.
  - After `GestureTriggered` fires, the detector still walks
    `Sampling → CastDetected → Cooldown` before it can trigger again, so it is
    deaf for `SampleWindow + castDetectedDisplayDuration + CooldownDuration`.
    With the cast profile's values that is roughly **1.3 s — longer than the
    whole strike window**, which would silently turn every rejected attempt into
    a guaranteed timeout. `StrikeGestureProfile` therefore needs a minimal
    `SampleWindow` and `CooldownDuration`, and the strike detector instance a
    zero `castDetectedDisplayDuration`, so that `StrikeController.attemptCooldown`
    is the *only* thing pacing attempts.
- `CastTuningProfile.cs` — `EvaluatePower(peak)` reused as-is (orientation-
  agnostic). `EvaluateLaunchVelocity(power)`'s current values are tuned for the
  horizontal test layout; whether they carry over to the vertical production
  cast is open, see the new flight component note below.

### Not reused — stays isolated

- `CastBallController.cs` and `GyroscopeCastTest.unity` — **unchanged**. This
  was originally planned as a direct reuse for the production cast flight, but
  it's a horizontal side-view parabola and the production scene casts
  vertically on screen — the orientations don't match, so the component isn't
  transplantable as-is. It stays exactly as it is, as its own isolated
  distance-test scene; nothing here should be edited for this feature.

### New (conceptual — not implemented by Claude)

- A gameplay hook-flight component for the production scene: vertical-cast
  trajectory physics, matching `CastBallController`'s public shape (a
  `Launch(power)`-style entry point, a generic landing event) without being
  the same component. See the `Casting` section above for what's already
  decided versus still open here.
- A fishing-loop state machine component (`FishingLoopController` or similar),
  replacing/expanding `CastTestController.cs`, owning the
  `ReadyToCast → Casting → Baiting → Striking → Reeling` flow above and the
  transitions to `GameOver`. Subscribes to the landing event generically —
  doesn't need to know which flight component raised it.
- Shore-lane character control (reads `AttitudeReader`, outputs a lane position;
  same shape as `AttitudeCircleController` but constrained to one axis and to the
  shore, not the full screen).
- Bait-wiggle control for `Baiting` (reads `AttitudeReader`, nudges the bait within
  a small radius — same sensor as the lane control, different scale/clamp).
- Fish "race to the bait" behavior: detection range, swim-toward-bait steering,
  first-arrival-wins resolution, losers return to idle.
- `StrikeController` — the logic owner for `Striking`. It owns the window timer,
  the moving ring's **normalized** radius, the target band bounds, the input
  cooldown, and the outcome events: strike succeeded, window timed out, plus an
  attempt-rejected event purely for feedback. It takes attempts from both input
  paths — `CastGestureDetector.GestureTriggered` and the semantic
  `StrikePerformed` intention — and judges them. It knows nothing about how any
  of this is drawn.
- `StrikeWindowHUD` — **display only.** It reads the normalized ring radius and
  band bounds off `StrikeController` and draws the two rings; it flashes red on
  the attempt-rejected event. It computes no timing of its own — in particular
  it must not run its own `elapsed / duration` clock, or the ring the player
  sees will drift from the ring the controller judges against, and the player
  will lose on an attempt that looked correct on screen. This is the same rule
  the existing debug HUDs already follow (`CastDebugHUD` / `GyroscopeDebugHUD`
  format values; they never compute them).
- A small camera-shake component, driven by that same attempt-rejected event, so
  `StrikeController` never holds a camera reference.
- `FishingLoopController`'s part in `Striking` stays minimal: enter the state,
  start `StrikeController`, enable the strike detector, and on a success or
  timeout event switch the global state (`Reeling` with a fish attached, or lose
  a hook and return to `ReadyToCast`/`GameOver`). It does not judge attempts and
  does not know what a ring is. Same split already used for
  `FishBiteRaceController`, which picks the winning fish while the state machine
  decides what winning means.
- Reeling dodge control (reads `AttitudeReader` during `Reeling`, moves the hook
  left/right along the retrieval path).
- Reel-accelerate + tension component. Reads the single button and raises the
  retrieval speed while held; separately, evaluates the `0–1` tension value each
  frame as the rate sum described under `Reeling` above, and fires a "snapped"
  event **once** on reaching maximum. Its two inputs are the accelerate-held
  flag and the hook's **actual** lateral speed — it reads the latter from the
  hook's real per-frame movement, never from the dodge control's input value, so
  it stays correct when the hook is clamped at a lane boundary and stays
  independent of how the dodge control is implemented. The speed → rate mapping
  is the same dead-zone / ramp / clamp shape as
  `CastTuningProfile.EvaluatePower(peak)` and `AttitudeCircleController`'s
  `NormalizeTilt`; reuse the shape, and take the `Mathf.InverseLerp` +
  `Mathf.Clamp01` idiom from them rather than reinventing it.
- Tension bar UI (shows the live `0–1` tension value during `Reeling`).
- A `Fish` representation: a 2D position, idle/approaching/hooked state, and a
  score value (flat for all fish in this MVP — see §2).
- `Rock` obstacles: fixed positions along the retrieval path with a collider.
- Hook count (lives) tracker.
- Session timer.
- Score tracker + minimal `GameOver` UI (reuse the existing debug-HUD patterns
  from `CastDebugHUD.cs`/`GyroscopeDebugHUD.cs` for consistency).

### Tuning data — where parameters live (new in revision 8)

**The rule.** A number goes in a `ScriptableObject` **tuning profile** when it is
a *feel or balance* value settled by playing. It stays a scene `[SerializeField]`
when it is a *wiring or placement* fact about one particular object.

**Why the split matters, concretely:** values edited on a scene component during
Play Mode are **thrown away when Play Mode exits**. Every number in revisions 6
and 7 is explicitly labelled "starting value, settle it in Play Mode" — so
leaving them on components would mean tuning them, watching the good values
vanish, and re-typing them from memory. `ScriptableObject` values **persist**
through Play Mode, which is exactly why `CastTuningProfile` was made one in the
first place (see the motion-input guide §6.1). This project also has the
counter-example: `AttitudeCircleController`'s tuning lives on the scene
component, and that produced both the guide's §3.8 warning about fields never
written to the scene file *and* M2's "were 10 and 30 test overrides or
intentional values?" episode in `findings.md`. One pattern has already worked
here and the other has already cost time.

Two secondary benefits, same as `CastTuningProfile`'s: one asset shared by
several consumers cannot drift out of sync, and difficulty presets become a
matter of swapping an asset reference rather than editing code.

**Profiles this design needs:**

| Profile | Holds |
|---|---|
| `CastTuningProfile` (existing) | Cast gesture detection, power curve, launch velocity |
| `StrikeGestureProfile` (asset) | A `CastTuningProfile` instance for the reversed detector — `Sensor` section only |
| `StrikeWindowProfile` (new class) | `windowDuration`, the band parameters, `hitForgivenessRadius`, `attemptCooldown`, shake amplitude/duration |
| `ReelTuningProfile` (new class) | Retrieval speed, accelerated speed, and all five tension parameters from §3 |
| `FishingLoopProfile` (rev 9, new class) | `postAttemptCooldown`, plus M7's starting hooks and session length |

Note that `Striking` needs **two** assets, not one. The reversed detector is
configured by a `CastTuningProfile` because that is genuinely what it is; the
ring parameters must **not** be bolted onto `CastTuningProfile`, because that
class is shared with the cast path and the isolated `GyroscopeCastTest` scene,
neither of which has any concept of a ring.

**Stays on the scene component**, and should not be moved into a profile: object
references (`launchPoint`, the lane-limit transforms, the camera), per-instance
positions and extents that only mean anything in one scene, and anything that
legitimately differs between two instances of the same prefab.

**Profiles validate their own invariants.** Several of these parameters are only
meaningful in relation to each other, and a violated relationship produces
silent nonsense rather than an error — "holding accelerate does nothing" or "the
band can never be hit" with no clue why. Each profile enforces its own rules
(via `OnValidate` and/or clamping in its accessors, matching how
`CastTuningProfile.EvaluatePower` already guards its own divide):

- `accelerateRiseRate > decayRate` — otherwise holding the button cannot raise
  tension at all.
- `maxLateralTensionRate > decayRate` — otherwise fast dodging cannot either.
- `maxEvaluatedLateralSpeed > safeLateralSpeed` — otherwise the mapping divides
  by zero or inverts.
- `0 < targetBandWidth < 1` — otherwise the band is empty or swallows the whole
  radius. *(Revisions 6–8 used the pair `0 ≤ targetBandInnerRadius <
  targetBandOuterRadius ≤ 1` instead, which is what the shipped M5 validates.)*
- `minBandCenterRadius < maxBandCenterRadius`, and both clamped into
  `[targetBandWidth / 2, 1 - targetBandWidth / 2]` so a randomly drawn band
  always fits — a silently out-of-range band is simply unhittable, with nothing
  in the Console to say why.
- `hitForgivenessRadius ≥ 0`, and small relative to `targetBandWidth`. If it
  approaches the band's own width, the drawn band stops meaning anything and the
  player is being lied to rather than helped — worth an explicit warning bound
  rather than a silent clamp.
- `windowDuration > 0`, `attemptCooldown ≥ 0`, `postAttemptCooldown ≥ 0`.

**Follow the existing house style** set by `CastTuningProfile`: `[Header]`
grouping, a `[Tooltip]` on every field, `[Min]`/`[Range]` where a bound exists,
private `[SerializeField]` backing fields exposed as read-only properties so
consumers cannot write to shared data, and `[CreateAssetMenu]` under the same
`Seven Seas/` menu.

**One honest caveat.** The same persistence that makes this worth doing also
means a wild experimental value survives silently — there is no "exit Play Mode
to undo." Once a number is settled, record it and its reasoning in
`findings.md`, the way M2's calibration tolerance and smoothing values were
recorded.

## 5. Deliberate deviations from the original, and why

These were explicit decisions made during design review, called out here so they
don't get "corrected" back by accident during implementation:

1. **Reel-tension is opt-in, not the default state.** The original always risks
   a snap while reeling. Here the default retrieval is a safe constant speed and
   decay always applies, so a player who is neither accelerating nor weaving
   hard is always recovering. (Revision 1 had dropped the tension mechanic
   entirely — that was wrong; the user confirmed during review that a
   button-driven accelerate-with-snap-risk mechanic, plus a tension bar UI, was
   intended all along.) **Amended in revision 7:** "risk only appears if the
   player chooses it" is no longer strictly true — fast lateral dodging now
   contributes too, so risk can also arrive from a rock the player had to avoid.
   That is deliberate; see §5.6.
2. **Aim happens before/during the cast, not after.** Tilt controls the shore
   character's lane *before* the flick, and flick power is read at the moment of
   the flick — together these replace the original's joystick-aim +
   timed-power-bar with the project's two existing sensor mechanics, reusing
   both without inventing a third input scheme.
3. **Biting is an active fish race, not an instant proximity check.** (Revision 1
   had this as an instant check at the landing point with no separate waiting
   state — that missed a state the user had already been picturing. Corrected in
   revision 2: see the `Baiting` state in §3.)
4. **A bite must be reacted to, not just accepted.** (Revision 2 went straight
   from a successful bite into `Reeling`. That missed another state the user had
   pictured: a short reflex window — `Striking` — where the player must flick
   backward to set the hook, or lose the hook entirely without ever reeling.
   Added in revision 3, see §3.) The directional pairing — outward flick to
   cast, backward flick to strike — is a deliberate physical echo of the
   original's separate "cast" and "reel" button presses, done with gesture
   direction instead of two different buttons.
5. **The hook-set is a readable timing check, not a hidden reflex test.**
   (Revision 3's `Striking` was one 0.7-second window in which a flick either
   landed or didn't, with nothing on screen to time against — in practice a coin
   flip that cost a hook. Revision 6's shrinking ring makes the moment *visible
   and approachable*: the player can see it coming, learn it, and get measurably
   better at it. Two further consequences are deliberate and should not be
   "corrected" back: an out-of-band attempt is **rejected with a cooldown rather
   than fatal**, so a mistimed input costs tempo instead of a hook; and the check
   is now **a timed indicator you act on at the right moment**, which is a
   considerably closer echo of the original game's timed power bar than a bare
   reflex window ever was.)
6. **Dodging and accelerating are coupled through tension, not independent.**
   (Through revision 6, `Reeling` was two unrelated systems sharing a state:
   dodge rocks with tilt, optionally hold a button for speed. Nothing connected
   them, so the optimal play was simply "dodge freely, and accelerate whenever
   no rock is near." Revision 7 makes the hook's own lateral speed feed the same
   tension value the accelerate button feeds. The consequences are the point:
   a panicked last-moment swerve now costs something; accelerating commits you
   to a calmer line; and the worst moment in the state — a rock arriving while
   you are already accelerating — forces a real choice between eating the rock,
   dumping speed, or risking the snap. Gentle repositioning stays free, so the
   cost lands on panic rather than on ordinary play.)

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
     first hooks itself and enters `Striking`.
  3. Letting the 8-second `Baiting` wait expire with no fish arriving skips
     `Striking` entirely and starts Reeling "empty."
  4. In `Striking`, two rings appear: a fixed target band and a moving ring
     shrinking to the centre over `windowDuration`. An attempt (backward flick,
     or Space) made while the moving ring is inside the band enters `Reeling`
     "with a fish attached." An attempt made outside the band is ignored,
     flashes the rings red, shakes the camera lightly and starts the input
     cooldown — it does **not** end the attempt, and a further input during that
     cooldown does nothing at all. Only letting the window elapse with no
     successful attempt fails: it costs a hook and returns to `ReadyToCast`
     without ever entering `Reeling`. Confirm specifically that one rejected
     attempt does not leave the detector deaf for the rest of the window (see
     §4's note on `StrikeGestureProfile`), and that the ring the HUD draws matches
     the position the controller judged against.
  5. During Reeling, tilt moves the hook; touching a rock costs a hook and ends
     the attempt.
  6. Tension behaves as a rate sum. Check each contribution separately before
     checking them together:
     a. Doing nothing drains the bar to empty and holds it there.
     b. Holding accelerate speeds up retrieval and fills the bar; releasing
        before max lets it drain again.
     c. Gentle left/right repositioning does **not** stop the bar draining;
        fast continuous weaving does fill it, with a findable break-even speed
        between the two.
     d. Holding the dodge control hard against a lane boundary — full input,
        no movement — adds **nothing**.
     e. Accelerating while weaving hard fills the bar visibly faster than either
        alone.
     f. Reaching max snaps the line, costs a hook and ends the attempt exactly
        as a rock hit does — and the snap fires once, not repeatedly.
  7. Reaching shore with a fish attached adds score; without one, no score.
  8. Hooks reaching 0 or the timer reaching 0 ends the game and shows the score.

  *Revision 9 adds these, to be run once its four changes are implemented:*

  9. The target band appears at a **different radius on successive attempts**,
     and never so far out that it is already entered on the first frame, nor
     clipped by the outer or inner edge.
  10. The ring **reverses at the centre** and returns outward, and an attempt
      timed on the **second** crossing succeeds exactly as one on the first.
  11. A rejected attempt whose cooldown swallows the first crossing still leaves
      the second crossing usable.
  12. A flick timed *slightly* outside the drawn band still succeeds
      (`hitForgivenessRadius` doing its job), while the drawn band is visibly
      unchanged — confirm the margin is not rendered.
  13. After every one of the four `Reeling` exits, casting is refused for
      `postAttemptCooldown` while lane movement still works, and the session
      timer keeps counting down throughout.

## 8. Follow-ups (explicitly not today)

- Full reconciliation of the cross-platform input architecture. The shared
  `FishingInputSource` contract and its keyboard/mouse implementation already
  exist and are in use (see the corrected §2 bullet), so this is no longer
  "add a fallback later" — it is "decide whether that contract is permanent
  scope, and if so document it across every state." Still owed; revision 6
  settled only what `Striking` needed.
- More fish, fish variety/size tiers feeding into score.
- Randomized/varied rock layouts.
- Real sprite art and animation.
- MainMenu → gameplay → results scene flow integration.
