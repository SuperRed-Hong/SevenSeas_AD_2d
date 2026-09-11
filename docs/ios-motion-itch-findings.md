# iPhone Safari + itch.io WebGL motion input — findings

Investigated 2026-09-10. Unity 6000.3.23f1, project page
`https://sevenseasgames777.itch.io/sevenseas`, build id 19181529.

Evidence is tagged:
**[SOURCE]** = read directly from WebKit / Unity / itch.io itself ·
**[SPEC]** = W3C standard · **[COMMUNITY]** = forum report, unverified by me.

---

## TL;DR

The failure is **not** a permission-flow bug in our code, and it is not an iOS
Safari restriction on cross-origin iframes in general. It is one missing token
in itch.io's iframe `allow` attribute:

* itch.io delegates `gyroscope` and `accelerometer`. **It does not delegate `magnetometer`.**
* WebKit requires `gyroscope + accelerometer + **magnetometer**` for the
  `deviceorientation` event and for `DeviceOrientationEvent.requestPermission()`.
* WebKit requires only `gyroscope + accelerometer` for `devicemotion` /
  `DeviceMotionEvent.requestPermission()`.

Therefore, inside the itch.io iframe on iOS:

| API | Status |
| --- | --- |
| `DeviceMotionEvent.requestPermission()` | works, prompts the user |
| `devicemotion` events | **fire normally** |
| `DeviceOrientationEvent.requestPermission()` | resolves `"denied"` instantly, **no prompt** |
| `deviceorientation` events | **never fire** |

And because Unity's WebGL backend implements `AttitudeSensor` on Safari via the
`deviceorientation` event, **`AttitudeSensor` can never produce a sample inside
the itch.io iframe on iOS** — while `Gyroscope`, `Accelerometer`,
`LinearAccelerationSensor` and `GravitySensor` all can.

There *is* a stable, publishable motion path on iPhone inside the itch iframe.
It just cannot be `AttitudeSensor`.

---

## 1. What itch.io actually delegates  **[SOURCE]**

Read from the live game page DOM (`.iframe_placeholder[data-iframe]`):

```html
<iframe frameborder="0" allowfullscreen="true" scrolling="no"
  allow="autoplay; fullscreen *; geolocation; microphone; camera; midi;
         monetization; xr-spatial-tracking; gamepad; gyroscope; accelerometer;
         xr; cross-origin-isolated; web-share"
  id="game_drop"
  src="https://html-classic.itch.zone/html/19181529/index.html?v=1789053120">
</iframe>
```

`gyroscope` ✅ `accelerometer` ✅ `magnetometer` ❌

This list is global to itch.io, generated server-side per embed. Nothing in the
project's itch settings, and nothing in a Unity WebGL Template, can change it —
Permissions Policy delegation flows strictly parent → child. **[SPEC]**

## 2. What WebKit requires  **[SOURCE]**

From current `WebKit/Source/WebCore/page/LocalDOMWindow.cpp`:

```cpp
bool LocalDOMWindow::isAllowedToUseDeviceMotion(String& message) const
{
    if (!PermissionsPolicy::isFeatureEnabled(PermissionsPolicy::Feature::Gyroscope, document, ...)
        || !PermissionsPolicy::isFeatureEnabled(PermissionsPolicy::Feature::Accelerometer, document, ...)) {
        message = "Third-party iframes are not allowed access to device motion unless explicitly allowed via Feature-Policy (gyroscope & accelerometer)";
        return false;
    }
    return true;
}

bool LocalDOMWindow::isAllowedToUseDeviceOrientation(String& message) const
{
    if (!PermissionsPolicy::isFeatureEnabled(...Gyroscope...)
        || !PermissionsPolicy::isFeatureEnabled(...Accelerometer...)
        || !PermissionsPolicy::isFeatureEnabled(...Magnetometer...)) {
        message = "Third-party iframes are not allowed access to device orientation unless explicitly allowed via Feature-Policy (gyroscope & accelerometer & magnetometer)";
        return false;
    }
    return true;
}
```

And from `Source/WebCore/dom/DeviceOrientationEvent.cpp`:

```cpp
void DeviceOrientationEvent::requestPermission(Document& document, PermissionPromise&& promise)
{
    ...
    if (!window->isAllowedToUseDeviceOrientation(errorMessage)) {
        document.addConsoleMessage(..., "Call to requestPermission() failed, reason: " + errorMessage);
        return promise.resolve(PermissionState::Denied);   // <-- resolves "denied", never prompts
    }
    ...
}
```

Two things follow:

1. The denial is **silent and instant** — the promise *resolves* `"denied"`, it
   does not reject. From JS it is indistinguishable from the user tapping
   "Don't Allow", except by timing (< ~60 ms, no sheet shown) and by the Safari
   console warning.
2. Requiring `magnetometer` for *relative* `deviceorientation` is a
   **WebKit-and-Blink implementation choice that is stricter than the current
   spec**. The W3C DeviceOrientation spec now requires `magnetometer` only for
   `deviceorientationabsolute`. **[SPEC]** WebKit's 2021 commit states it was
   done "to match Blink". So this may relax in a future Safari, but we cannot
   ship on that hope.

Historical note: WebKit bug 221399 ("Device motion/orientation events not
working in third-party iframes despite Feature-Policy allowing it") was
**RESOLVED FIXED in Feb 2021**. So the old blanket ban on `requestPermission()`
in cross-origin iframes is gone. Our problem is the narrower `magnetometer` gap,
not that iframe. **[SOURCE]**

## 3. Why Unity's `AttitudeSensor` specifically dies  **[SOURCE]**

From the installed editor,
`6000.3.23f1/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/lib/Sensor.js`:

```js
JS_OrientationSensor_Start: function(callback, frequency) {
    // If we don't have new sensor API, fallback to old DeviceOrientationEvent
    if (typeof RelativeOrientationSensor === 'undefined') {
        JS_RequestDeviceSensorPermissions(1 /*DeviceOrientationEvent permission*/);
        window.addEventListener('deviceorientation', JS_DeviceOrientation_eventHandler);
        return;
    }
    ...
}
```

versus every other sensor:

```js
$JS_DeviceMotion_add: function() {
    JS_RequestDeviceSensorPermissions(2 /*DeviceMotionEvent permission*/);
    window.addEventListener('devicemotion', JS_DeviceMotion_eventHandler);
}
```

`Accelerometer`, `Gyroscope`, `LinearAccelerationSensor` and `GravitySensor` all
route through `devicemotion`. Only `AttitudeSensor` routes through
`deviceorientation`.

Safari has no generic Sensor API (`RelativeOrientationSensor` is undefined), so
iOS always takes the legacy path. Chrome on Android *does* have the generic
Sensor API, which needs only `accelerometer` + `gyroscope` — exactly what itch
delegates. **That is why Android works and iPhone does not.**

Unity's sensor support on WebGL is official since 2021.2 and `AttitudeSensor` is
listed as supported — the platform table does not carry the iframe caveat. **[SOURCE]**

## 4. Two bugs in our current bridge

`Assets/Plugins/WebGL/SevenSeasWebMotion.jslib` does:

```js
Promise.all([DeviceMotionEvent.requestPermission(),
             DeviceOrientationEvent.requestPermission()])
  .then(results => finish(results.every(r => r === "granted") ? "granted" : "denied"));
```

1. The orientation request always resolves `"denied"` in the itch iframe, so
   `every(...)` collapses the *whole* result to `"denied"` — even though motion
   was granted. This is what produces the `MOTION ACCESS BLOCKED` screen. The
   two results must be tracked **separately**.
2. Unity's own `Sensor.js` issues its *own* `requestPermission()` calls when a
   sensor is first enabled. Ours runs in parallel with Unity's. They do not
   conflict (WebKit caches the grant per origin), but ours is the one that must
   happen inside the tap gesture, so ours should run first and Unity's sensor
   should only be enabled after it resolves.

## 5. Answers to the specific questions

**Q1 — Does itch delegate gyroscope/accelerometer to iOS Safari?**
Yes, both. Not `magnetometer`. Verified against the live page. **[SOURCE]**

**Q2 — Does iOS Safari still block `requestPermission()` in cross-origin iframes?**
No, not as a blanket rule — fixed in WebKit r273444 (Feb 2021). It blocks only
when the required Permissions Policy features are missing, which for
`DeviceOrientationEvent` includes `magnetometer`. **[SOURCE]**

**Q3 — Does "Click to launch in fullscreen" help?**
No. It is the same `<iframe id="game_drop">` put into the Fullscreen API; the
document, its origin and its Permissions Policy are unchanged. Fullscreen is
delegated (`fullscreen *`), sensors are not. **[SOURCE for the markup;
reasoned from spec for the conclusion]**

**Q4 — Can a custom WebGL Template / iframe config fix it?**
No. A document cannot grant itself a policy-controlled feature its embedder
withheld. **[SPEC]** The only in-iframe escape is a user-activated
`target="_blank"` link out to a top-level page.

**Q5 — Does opening the CDN URL top-level work?**
`https://html-classic.itch.zone/html/19181529/index.html` loads fine as a
top-level document (verified). At top level there is no Permissions Policy
restriction, ITP/Storage Access does not gate sensors, and both
`requestPermission()` calls should prompt normally. **[SOURCE for the load;
the sensor behaviour still needs the on-device test in §6.]**
Caveat: the numeric build id changes on every upload, so a hardcoded link rots.
Generate it at runtime instead — inside the iframe, `location.href` already *is*
the CDN URL, so `window.open(location.href, '_blank')` from a tap is self-updating.

**Q6 — Known-good iPhone + itch.io gyro projects?**
I found no verified case. The consistent community report is the opposite:
sensors work locally over HTTPS and stop working once embedded on itch, and the
workaround people land on is "use the actual URL instead of the embedded
version on itch". **[COMMUNITY]** That is consistent with everything above.

**Q7 — Should the iframe be button-only and motion be reserved for the standalone page?**
That is the safe shipping stance, but per §3 it is stricter than necessary.
`devicemotion` *is* available inside the itch iframe on iOS. Recommendation:

* **iframe + iOS** → motion via `GravitySensor` / `Gyroscope` (devicemotion path),
  touch fallback if the motion permission is refused.
* **iframe + Android** → unchanged, `AttitudeSensor` works.
* Offer an "Open in a new tab for the best tilt controls" button as an upgrade
  path, not as the only path.
* Only if the on-device test in §6 contradicts this, fall back to
  "iframe = buttons, standalone = motion".

**Q8 — Test page.** See `docs/motion-diagnostics/index.html` and §6.

## 6. On-device test procedure

`docs/motion-diagnostics/index.html` is a dependency-free page that separately
reports: context (top-level vs iframe, secure context, delegated policy),
API presence, each `requestPermission()` result *with timing*, live
`devicemotion` and `deviceorientation` counters and values, and a side-by-side
of two nested iframes — one with itch's exact `allow` string, one with
`magnetometer` added.

Deploy it the same way the game is deployed, so it lands in the real environment:

1. Zip it: the file must be at the root of the zip as `index.html`.
2. Upload as a **new, restricted/draft** itch.io project, kind "HTML", "This
   file will be played in the browser".
3. On the iPhone, run these five passes and record each:

| # | Where | Expected if the magnetometer theory holds |
| --- | --- | --- |
| 1 | itch project page, inside the embed | motion `granted` + events; orientation instant `denied`, 0 events |
| 2 | Same, after tapping fullscreen | identical to 1 |
| 3 | The page's "Open in a new TOP-LEVEL tab" link | both `granted`, both event streams flowing |
| 4 | Direct `html-classic.itch.zone/html/<id>/index.html`, pasted into Safari | same as 3 |
| 5 | In pass 3/4, compare nested frame **A** (no magnetometer) vs **B** (with magnetometer) | A: orientation denied · B: orientation granted |

Pass 5 is the decisive one — same origin, same page, same tap, one token
different.

Before running: check **Settings → Apps → Safari → Motion & Orientation Access**
is ON. If that global toggle is off, *both* requests return `denied` everywhere,
including top-level, which would mimic the same symptom for an unrelated reason.

Then repeat 1 and 3 with the actual game build and watch the Unity side:
`AttitudeSensor.current`, `Gyroscope.current`, `GravitySensor.current` — whether
the device exists at all, and whether `wasUpdatedThisFrame` ever goes true.
Safari's Web Inspector (Mac + cable, Develop → iPhone) will also show the
WebKit console warning quoted in §2 verbatim, which is direct confirmation.

## 7. Recommended changes, in order

1. **Track the two permissions separately** in `SevenSeasWebMotion.jslib`
   (return e.g. `"motion:granted|orientation:denied"`), and stop treating an
   orientation denial as total failure.
2. **Add a gravity-based attitude source.** `AttitudeReader` currently exposes a
   `Quaternion` consumed by `AttitudeCalibrationService` and
   `AttitudeCircleController`. A tilt-equivalent quaternion can be synthesised
   from `GravitySensor.current.gravity` (`Quaternion.FromToRotation` against the
   device-down axis). Yaw is lost, but nothing in the fishing loop uses yaw —
   the pipeline calibrates a neutral pose and reads pitch/roll deltas. As a
   bonus this is immune to compass drift.
   Priority: `AttitudeSensor` → `GravitySensor` → `Accelerometer` → touch.
3. **Add an "open in new tab" upgrade button** on the iOS-in-iframe path, using
   `window.open(location.href, '_blank')` from a tap.
4. **Ask itch.io to add `magnetometer`** to the global allow list. There is
   precedent: in the April 2021 thread `itch.io/t/1305008`, leafo added
   `gyroscope`/`accelerometer` within days of being asked. If granted, the
   existing `AttitudeSensor` code would start working with no client change.
   **[COMMUNITY]**
5. Keep the touch fallback exactly as it is.

---

## Sources

* WebKit source, current `main`:
  `Source/WebCore/page/LocalDOMWindow.cpp`,
  `Source/WebCore/dom/DeviceOrientationEvent.cpp`,
  `Source/WebCore/dom/DeviceMotionEvent.cpp`
* WebKit bug 221399 — <https://bugs.webkit.org/show_bug.cgi?id=221399>
* WebKit commit e8fcdbc — <https://github.com/WebKit/WebKit/commit/e8fcdbcc2d499879de1d8812003942b8b91d6bb1>
* W3C Device Orientation and Motion — <https://www.w3.org/TR/orientation-event/>
* MDN `DeviceOrientationEvent.requestPermission()` — <https://developer.mozilla.org/en-US/docs/Web/API/DeviceOrientationEvent/requestPermission_static>
* Unity Input System sensor platform support — <https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/Sensors.html>
* Unity 6000.3.23f1 WebGL module `BuildTools/lib/Sensor.js` (local install)
* itch.io thread on iframe sensor permissions — <https://itch.io/t/1305008/im-trying-to-use-a-deviceorientation-event-handler-and-running-into-iframe-problems-i-think>
* Unity Discussions, sensors on itch.io / CrazyGames — <https://discussions.unity.com/t/gyroscope-and-accelerometer-sensors-on-webgl-using-itch-io-or-crazygames/1555386>
