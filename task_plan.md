# Task Plan

## Goal

Use gyroscope angular velocity to drive the Circle in Scene1 and add a reusable runtime debug HUD for sensor tuning.

## Phases

- [complete] Inspect the current reader, Scene1 wiring, and user changes.
- [complete] Implement configurable gyroscope-driven Circle movement.
- [complete] Implement the runtime debug HUD with numeric values, bipolar bars, peak tracking, and rolling history.
- [complete] Wire components into Scene1 without overwriting unrelated scene changes.
- [complete] Compile and verify the smallest relevant behavior.
- [complete] Record durable learning points in Obsidian.

## Constraints

- C# identifiers, logs, exceptions, and comments must be English only.
- Do not add button or cast-state gameplay yet.
- Preserve existing user changes.
- Keep raw sensor reading separate from movement and visualization.

## Errors

| Error | Attempts | Resolution |
|---|---:|---|
| `CS1073` in signed numeric interpolation format | 1 | Replaced alignment comma with the required format colon. |
| Obsolete TMP word-wrapping property warning | 1 | Replaced it with `TextWrappingModes.NoWrap`. |
