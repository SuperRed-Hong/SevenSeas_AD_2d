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
    var finish = function (result) {
      SendMessage(receiverName, "ReceivePermissionResult", result);
    };

    try {
      var requests = [];
      if (typeof DeviceMotionEvent !== "undefined" &&
          typeof DeviceMotionEvent.requestPermission === "function") {
        requests.push(DeviceMotionEvent.requestPermission());
      }
      if (typeof DeviceOrientationEvent !== "undefined" &&
          typeof DeviceOrientationEvent.requestPermission === "function") {
        requests.push(DeviceOrientationEvent.requestPermission());
      }

      if (requests.length === 0) {
        if (typeof DeviceMotionEvent === "undefined" &&
            typeof DeviceOrientationEvent === "undefined") {
          finish("unsupported");
        } else {
          finish("granted");
        }
        return;
      }

      Promise.all(requests).then(function (results) {
        var granted = results.every(function (value) {
          return value === "granted";
        });
        finish(granted ? "granted" : "denied");
      }).catch(function () {
        finish("denied");
      });
    } catch (error) {
      finish("unsupported");
    }
  }
});
