mergeInto(LibraryManager.library, {
  SevenSeasIsMobileBrowser: function () {
    var nav = window.navigator || {};
    if (nav.userAgentData && nav.userAgentData.mobile === true) {
      return 1;
    }

    var userAgent = nav.userAgent || "";
    var reportsMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile/i.test(userAgent);
    var reportsIPadAsMac = nav.platform === "MacIntel" && nav.maxTouchPoints > 1;
    return reportsMobile || reportsIPadAsMac ? 1 : 0;
  },

  SevenSeasRequestMotionPermission: function (receiverNamePointer) {
    var receiverName = UTF8ToString(receiverNamePointer);
    var state = { motion: "unsupported", orientation: "unsupported" };
    var finish = function () {
      SendMessage(receiverName, "ReceivePermissionResult",
        "motion=" + state.motion + ";orientation=" + state.orientation);
    };
    var ask = function (eventType, key) {
      if (typeof eventType === "undefined") return Promise.resolve();
      if (typeof eventType.requestPermission !== "function") {
        state[key] = "granted";
        return Promise.resolve();
      }
      // Invoke synchronously in the button gesture, including both requests.
      // Catch per channel so a synchronous orientation failure cannot hide motion.
      try {
        return Promise.resolve(eventType.requestPermission()).then(function (result) {
          state[key] = result === "granted" ? "granted" : "denied";
        }).catch(function () {
          state[key] = "denied";
        });
      } catch (error) {
        state[key] = "denied";
        return Promise.resolve();
      }
    };
    Promise.all([
      ask(typeof DeviceMotionEvent !== "undefined" ? DeviceMotionEvent : undefined, "motion"),
      ask(typeof DeviceOrientationEvent !== "undefined" ? DeviceOrientationEvent : undefined, "orientation")
    ]).then(finish);
  },

  SevenSeasIsInIframe: function () {
    try { return window.top !== window.self ? 1 : 0; } catch (error) { return 1; }
  },

  SevenSeasOpenSelfTopLevel: function () {
    // Must stay in the button gesture; the CDN build URL changes on each upload.
    try { window.open(window.location.href, "_blank", "noopener"); } catch (error) {}
  }
});
