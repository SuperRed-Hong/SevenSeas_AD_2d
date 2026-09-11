# 实施方案：iPhone Safari + itch.io iframe 体感输入修复

> 这份文档是自包含的执行说明。实施者不需要额外上下文。
> 项目：Unity 6000.3.23f1，`D:\UnityProject\SevenSeas_AD_2d`，WebGL 发布到 itch.io。
> 背景调查见 `docs/ios-motion-itch-findings.md`。

---

## 0. 必读：为什么要这么改

itch.io 生成的游戏 iframe 的 `allow` 属性是：

```
autoplay; fullscreen *; geolocation; microphone; camera; midi; monetization;
xr-spatial-tracking; gamepad; gyroscope; accelerometer; xr; cross-origin-isolated; web-share
```

有 `gyroscope` 和 `accelerometer`，**没有 `magnetometer`**。

WebKit 的硬性规则（`Source/WebCore/page/LocalDOMWindow.cpp`）：

| API | 需要的 Permissions Policy |
| --- | --- |
| `devicemotion` / `DeviceMotionEvent.requestPermission()` | gyroscope + accelerometer |
| `deviceorientation` / `DeviceOrientationEvent.requestPermission()` | gyroscope + accelerometer + **magnetometer** |

策略不满足时，`DeviceOrientationEvent.requestPermission()` 会 **`resolve("denied")`**（不是 reject），
不弹窗，通常 < 60ms 返回。

Unity 的 WebGL 后端（`WebGLSupport/BuildTools/lib/Sensor.js`）在 Safari 上（无 generic Sensor API）：

- `AttitudeSensor` → `addEventListener('deviceorientation', ...)` ← **在 itch iframe 里永远收不到数据**
- `Accelerometer` / `Gyroscope` / `LinearAccelerationSensor` / `GravitySensor` → `addEventListener('devicemotion', ...)` ← **可以正常工作**

**所以核心改动是：iOS 上不要依赖 `AttitudeSensor`，改用 `GravitySensor` / `Accelerometer` 合成姿态。**

另外现有 `.jslib` 用 `Promise.all(...).every(r => r === "granted")`，orientation 的必然拒绝会把
motion 的成功一起拖成 `denied` —— 这就是玩家看到 `MOTION ACCESS BLOCKED` 的直接原因。

**不要删除触屏兜底。** 兜底保留，只是不该被误触发。

---

## 1. 改动清单总览

| # | 文件 | 动作 |
| --- | --- | --- |
| 1 | `Assets/Plugins/WebGL/SevenSeasWebMotion.jslib` | 分开返回两个权限结果；新增 open-top-level 函数 |
| 2 | `Assets/Scripts/Input/WebMotionPermission.cs` | 拆成 MotionState / OrientationState |
| 3 | `Assets/Scripts/Help/AttitudeReader.cs` | 新增 GravitySensor / Accelerometer 姿态源 |
| 4 | `Assets/Scripts/Controller/FishingSceneEntryGuard.cs` | 兜底判定条件改为「motion 被拒 **或** 拿不到样本」 |
| 5 | `Assets/Scripts/UI/AttitudeCalibrationPanel.cs` | 状态文案跟随新枚举 |
| 6 | 新文件 `Assets/Scripts/Input/WebTopLevelLink.cs` + UI 按钮 | 「新标签页打开」升级入口（可选，建议做） |

按 1 → 2 → 3 → 4 → 5 → 6 的顺序做，每步单独编译通过再进入下一步。

---

## 2. 改动 1：`SevenSeasWebMotion.jslib`

**位置**：`Assets/Plugins/WebGL/SevenSeasWebMotion.jslib`

**目标**：把 motion 和 orientation 的结果分开回传，不再用 `every()` 折叠。

`SevenSeasIsMobileBrowser` 保持不变。把 `SevenSeasRequestMotionPermission` 整个替换为：

```js
  SevenSeasRequestMotionPermission: function (receiverNamePointer) {
    var receiverName = UTF8ToString(receiverNamePointer);

    // 回传格式固定为 "motion=<state>;orientation=<state>"
    // state ∈ granted | denied | unsupported
    var state = { motion: "unsupported", orientation: "unsupported" };
    var finish = function () {
      SendMessage(receiverName, "ReceivePermissionResult",
        "motion=" + state.motion + ";orientation=" + state.orientation);
    };

    var ask = function (Ev, key) {
      if (typeof Ev === "undefined") {
        state[key] = "unsupported";
        return Promise.resolve();
      }
      if (typeof Ev.requestPermission !== "function") {
        // 非 iOS 分支：事件存在且无需显式授权
        state[key] = "granted";
        return Promise.resolve();
      }
      // 必须在用户手势的同步调用链里发起，调用方要保证这一点
      return Ev.requestPermission().then(function (result) {
        state[key] = (result === "granted") ? "granted" : "denied";
      }).catch(function () {
        // NotAllowedError 等：当作拒绝，由 C# 侧决定是否重试
        state[key] = "denied";
      });
    };

    try {
      Promise.all([
        ask(typeof DeviceMotionEvent !== "undefined" ? DeviceMotionEvent : undefined, "motion"),
        ask(typeof DeviceOrientationEvent !== "undefined" ? DeviceOrientationEvent : undefined, "orientation")
      ]).then(finish).catch(finish);
    } catch (error) {
      finish();
    }
  },

  SevenSeasIsInIframe: function () {
    try { return (window.top !== window.self) ? 1 : 0; } catch (e) { return 1; }
  },

  SevenSeasOpenSelfTopLevel: function () {
    // iframe 内的文档 URL 就是 itch CDN 直链，顶层打开即可绕过 Permissions Policy。
    // 必须在用户手势里调用，否则会被弹窗拦截。
    try { window.open(window.location.href, "_blank"); } catch (e) {}
  }
```

注意逗号：`SevenSeasIsMobileBrowser` 后面要有逗号，最后一个函数后面不要有。

---

## 3. 改动 2：`WebMotionPermission.cs`

**位置**：`Assets/Scripts/Input/WebMotionPermission.cs`

**目标**：两个权限独立追踪；`State` 保留但语义改为「有没有任何可用的体感通道」。

要点：

1. 新增两个静态属性：
   ```csharp
   public static WebMotionPermissionState MotionState { get; private set; } = WebMotionPermissionState.NotRequested;
   public static WebMotionPermissionState OrientationState { get; private set; } = WebMotionPermissionState.NotRequested;
   ```
2. `State` 改成计算属性（保持现有调用方可编译）：
   ```csharp
   public static WebMotionPermissionState State
   {
       get
       {
           if (MotionState == WebMotionPermissionState.Requesting ||
               OrientationState == WebMotionPermissionState.Requesting)
               return WebMotionPermissionState.Requesting;
           // 只要 devicemotion 通道拿到了授权，就算可用。
           if (MotionState == WebMotionPermissionState.Granted ||
               OrientationState == WebMotionPermissionState.Granted)
               return WebMotionPermissionState.Granted;
           if (MotionState == WebMotionPermissionState.Denied ||
               OrientationState == WebMotionPermissionState.Denied)
               return WebMotionPermissionState.Denied;
           if (MotionState == WebMotionPermissionState.NotRequired)
               return WebMotionPermissionState.NotRequired;
           return MotionState;
       }
   }
   ```
   > `State` 现在是只读计算属性，原来所有 `State = ...` 的赋值必须改成给
   > `MotionState` / `OrientationState` 赋值。编译器会把这些点全部报出来。
3. `ReceivePermissionResult(string result)` 改为解析 `"motion=granted;orientation=denied"`：
   ```csharp
   public void ReceivePermissionResult(string result)
   {
       foreach (string part in result.Split(';'))
       {
           int eq = part.IndexOf('=');
           if (eq <= 0) continue;
           string key = part.Substring(0, eq);
           WebMotionPermissionState value = part.Substring(eq + 1) switch
           {
               "granted" => WebMotionPermissionState.Granted,
               "denied" => WebMotionPermissionState.Denied,
               _ => WebMotionPermissionState.Unsupported
           };
           if (key == "motion") MotionState = value;
           else if (key == "orientation") OrientationState = value;
       }
       Debug.Log($"Web motion permission: motion={MotionState}, orientation={OrientationState}");
   }
   ```
   **兼容性**：如果收到的字符串里没有 `=`（旧格式 `"granted"` / `"denied"`），
   就把两个都设成同一个值，避免热更新时序问题。
4. `RequestIfNeeded()` 里把 `State = Requesting` 改成同时设两个；
   守卫条件改成「motion 已 granted 或正在请求中就直接返回」。
5. `ResetState()` 里重置两个新属性。
6. 新增：
   ```csharp
   public static bool IsInIframe { get; private set; }
   ```
   在 `RequestIfNeeded` 里通过 `SevenSeasIsInIframe()` 填充（仅 `UNITY_WEBGL && !UNITY_EDITOR`）。
   声明对应的 `[DllImport("__Internal")] private static extern int SevenSeasIsInIframe();`
   和 `private static extern void SevenSeasOpenSelfTopLevel();`，
   并加一个 `public static void OpenTopLevel()` 包一层（非 WebGL 下空实现）。

---

## 4. 改动 3：`AttitudeReader.cs` —— 核心改动

**位置**：`Assets/Scripts/Help/AttitudeReader.cs`

**现状**：只读 `AttitudeSensor.current.attitude`，对外暴露 `Attitude`（Quaternion）、
`HasSample`、`IsAvailable`、`IsEnabled`、`SamplingFrequency`。

**要求**：**对外接口一个都不能改**。下游 `AttitudeCalibrationService`、
`AttitudeCircleController`、`MobileFishingInputSource` 全部依赖这几个属性，不要动它们。

### 4.1 新增姿态源枚举

```csharp
public enum AttitudeSource
{
    None,
    Attitude,      // AttitudeSensor，Android / 原生 / 顶层页面
    Gravity,       // GravitySensor，iOS in-iframe 首选
    Accelerometer  // Accelerometer + 低通滤波，最后的传感器兜底
}

public AttitudeSource ActiveSource { get; private set; } = AttitudeSource.None;
```

### 4.2 选源逻辑

替换 `TryConnect()`。按优先级尝试，第一个能成功 enable 的就用：

1. **`AttitudeSensor`** —— 但在 WebGL 上，如果
   `WebMotionPermission.OrientationState == Denied`，**直接跳过**，不要试。
   否则会白等 3 秒（`deviceorientation` 永远不来）。
   编辑器里的 remote device 查找逻辑（现有 `FindAttitudeSensor` 的 `#if UNITY_EDITOR` 分支）保留。
2. **`GravitySensor.current`**
3. **`Accelerometer.current`**

每一个都要 `InputSystem.EnableDevice(device)` 并设 `samplingFrequency = requestedSamplingFrequency`。
`InputSystem.EnableDevice` 对已启用的设备是幂等的。

沿用现有的 `nextConnectionAttemptTime` 每秒重试机制——权限是异步到的，
第一次 `TryConnect` 可能全部失败，必须能重试。

**重要**：如果当前源是 Gravity/Accelerometer，而之后 `AttitudeSensor` 变得可用
（比如顶层页面），允许升级；反过来不要降级抖动。简单做法：只在
`ActiveSource == AttitudeSource.None` 或连续 1 秒没有样本时才重新选源。

### 4.3 从重力向量合成姿态四元数

`Update()` 里按 `ActiveSource` 分支：

```csharp
case AttitudeSource.Attitude:
    // 现有逻辑，原样保留
    break;

case AttitudeSource.Gravity:
    Vector3 g = gravitySensor.gravity.ReadValue();
    if (!IsUsableVector(g)) return;   // NaN / 零向量守卫，见下
    ApplyGravity(g);
    break;

case AttitudeSource.Accelerometer:
    Vector3 a = accelerometer.acceleration.ReadValue();
    if (!IsUsableVector(a)) return;
    // 低通滤出重力分量。alpha 越小越平滑但越迟钝。
    smoothedGravity = smoothedGravity == Vector3.zero
        ? a
        : Vector3.Lerp(smoothedGravity, a, gravityFilterAlpha); // gravityFilterAlpha 默认 0.12f
    ApplyGravity(smoothedGravity);
    break;
```

守卫和合成：

```csharp
private static bool IsUsableVector(Vector3 v)
{
    if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)) return false;
    return v.sqrMagnitude > 0.01f;
}

private void ApplyGravity(Vector3 gravity)
{
    // gravity 是「世界向下」在设备坐标系里的方向。
    // 构造把它映射回 Vector3.down 的最小弧旋转，即设备→世界的旋转（yaw 不定）。
    Attitude = Quaternion.FromToRotation(gravity.normalized, Vector3.down);
    HasSample = true;
}
```

**为什么这样够用**：下游 `AttitudeCircleController` 只消费
`Quaternion.Inverse(neutral) * current`，再取 `relative * Vector3.forward` 的方向角。
整条链路是相对于标定中性姿态的，yaw 不参与默认的 X/Y 轴映射。钓鱼循环不用 yaw。
额外好处：重力源没有罗盘漂移。

**已知限制（必须写进代码注释）**：
`AttitudeCircleController.horizontalAxis / verticalAxis` 如果被配成 `GyroscopeAxis.Z`
（roll，来自 `relativeAttitude.eulerAngles.z`），在重力源下不可靠——
`FromToRotation` 的 yaw 是任意的，会泄漏进 Z。
默认配置是 `horizontal = Y, vertical = X`，不受影响。**不要改默认配置。**

### 4.4 符号 / 轴向

Unity 的 `Sensor.js` 在 iOS 上用 `+1/g`、其他平台用 `-1/g` 的乘数，所以
`Vector3.down` 还是 `Vector3.up` 可能要翻。

**不要在代码里猜。** 先按 `Vector3.down` 实现，然后在真机上验证，
用 `AttitudeCircleController` 上已有的 `invertHorizontal` / `invertVertical`
Inspector 勾选项去校正方向。这两个开关就是为这种事准备的。
如果发现整体反了，改 Inspector，不要改代码。

### 4.5 `OnDisable`

现有代码 `InputSystem.DisableDevice(attitudeSensor)`。要对当前实际用的那个设备做，
并把 `smoothedGravity` 和 `ActiveSource` 一起重置。

---

## 5. 改动 4：`FishingSceneEntryGuard.cs`

**位置**：`Assets/Scripts/Controller/FishingSceneEntryGuard.cs`，
协程 `ResolveWebMotionInput()`（约 190–228 行）。

现在的判定：

```csharp
if (WebMotionPermission.State != WebMotionPermissionState.Granted ||
    !calibrationService.IsSensorReady)
{
    WebMotionPermission.ActivateTouchFallback();
}
```

改为：**只看 motion 通道 + 实际有没有样本**。orientation 被拒不是失败。

```csharp
bool motionUsable =
    WebMotionPermission.MotionState == WebMotionPermissionState.Granted ||
    WebMotionPermission.MotionState == WebMotionPermissionState.NotRequired;

if (!motionUsable || !calibrationService.IsSensorReady)
{
    WebMotionPermission.ActivateTouchFallback();
}
```

上面那段等待循环同理：

```csharp
while (WebMotionPermission.State == WebMotionPermissionState.Requesting && ...)
```
`State` 现在是计算属性，这句仍然成立，不用改。

```csharp
if (WebMotionPermission.State == WebMotionPermissionState.Granted)
```
改成 `if (motionUsable)`，把 `motionUsable` 的计算提到这一句之前。

**等待样本的超时**：现在是 3 秒。`GravitySensor` 在 iOS 上依赖 `devicemotion`
第一帧到达，通常 < 200ms，但首次授权后可能略慢。保持 3 秒，不要缩短。

**`StartGame()` 里 `WebMotionPermission.RequestIfNeeded()` 的调用位置不要动。**
它必须留在按钮 onClick 的同步调用链的最前面——iOS 要求 `requestPermission()`
在用户手势里发起，中间插入任何 `await` / 协程 yield 都会让手势失效。

---

## 6. 改动 5：`AttitudeCalibrationPanel.cs`

**位置**：`Assets/Scripts/UI/AttitudeCalibrationPanel.cs` 约 85–95 行。

现在按 `WebMotionPermission.State` 出文案。因为 `State` 语义已经变成「聚合可用性」，
`Denied` 现在只在 **两个通道都被拒** 时出现，文案本身仍然正确。

只需要补一条：当 `MotionState == Granted` 但 `IsSensorReady` 还是 false 时，
文案应该是「正在连接传感器…」而不是 `MOTION ACCESS BLOCKED`。
现在这种情况会掉进 `_ =>` 的兜底分支，检查一下兜底文案是否合适，不合适就加一个分支。

---

## 7. 改动 6：「新标签页打开」升级入口（建议做）

**场景**：即使重力源能用，顶层页面的体验仍然更完整（`AttitudeSensor` 可用、有 yaw）。
给 iframe 内的 iOS 玩家一个可选的升级路径。

1. 新建 `Assets/Scripts/Input/WebTopLevelLink.cs`：一个小 MonoBehaviour，
   挂在按钮上，`OnClick` 调 `WebMotionPermission.OpenTopLevel()`。
2. 按钮只在 `RuntimeInputPlatform.IsWebMobilePlayer && WebMotionPermission.IsInIframe`
   时 `SetActive(true)`，其他情况隐藏。
3. 放在设置面板（`MenuSettingsPanel`）里，文案例如
   "OPEN IN NEW TAB FOR FULL MOTION"。
4. `window.open` 必须在手势里调用，Unity 的 Button onClick 满足这个条件。

**不要**硬编码 itch CDN 的 URL。iframe 里 `window.location.href` 本身就是
`https://html-classic.itch.zone/html/<buildId>/index.html`，而 `<buildId>`
每次上传都会变。用 `location.href` 自动跟随。

---

## 8. 不要做的事

- **不要**试图在 WebGL Template 里加 `allow` / `Permissions-Policy` 头。
  Permissions Policy 只能父页面向子页面委派，子文档无法自授。改 Template 没用。
- **不要**删除或弱化触屏兜底。
- **不要**改 `AttitudeReader` 的公开属性签名。
- **不要**把 `WebMotionPermission.RequestIfNeeded()` 移到协程 / `async` 之后。
- **不要**为了「修好」而移除 `AttitudeSensor` 分支——Android 和顶层页面靠它。

---

## 9. 验收

### 9.1 编译

```bash
```
在 Unity Editor 里打开项目，确认 Console 无编译错误。
`State` 改成只读计算属性后，编译器会把所有旧的赋值点报出来，逐个改掉。

### 9.2 Editor 内

- PC 播放模式：`RuntimeInputPlatform.UsesMobileControls == false`，
  不走体感、不显示移动端教程 —— 行为与改动前完全一致。

### 9.3 真机（决定性验证）

先用诊断页 `docs/motion-diagnostics/index.html` 确认环境
（打包成 zip，`index.html` 放在 zip 根目录，作为 restricted 的 HTML 项目传到 itch）。
跑之前确认 iPhone **设置 → Safari → 运动与方向访问** 是开的。

然后构建游戏 WebGL 上传，在 iPhone Safari 上验证：

| 场景 | 期望 |
| --- | --- |
| itch 页面内嵌 iframe，点 Start | 弹出一次 iOS 权限询问；允许后进入校准，进度条能填满 |
| 同上，权限选「不允许」 | 落到触屏按钮模式，与改动前一致 |
| 诊断页「新标签页打开」后的顶层页面 | `AttitudeSensor` 路径生效，`ActiveSource == Attitude` |
| Android Chrome（itch 内嵌） | 与改动前完全一致，`ActiveSource == Attitude`，**不能有回归** |
| PC 浏览器 | 与改动前完全一致，无移动端 UI |

用 Safari Web Inspector（Mac + 数据线，开发 → iPhone）看 Console，
应该能看到我们打的 `Web motion permission: motion=Granted, orientation=Denied`，
以及 WebKit 自己的警告
`Call to requestPermission() failed, reason: Third-party iframes are not allowed access to device orientation ...`
—— 这两条同时出现就证明分析和改动都对上了。

### 9.4 倾斜方向

进游戏后检查左右 / 前后倾斜的方向是否符合直觉。
如果反了，改 `AttitudeCircleController` 的 `invertHorizontal` / `invertVertical`
Inspector 勾选项，**不要改 `ApplyGravity` 的数学**。

---

## 10. 附带事项

- 新增的 `.cs` 文件需要 `.meta`，Unity 下次打开项目会自动生成，不要手写。
- 本仓库的提交信息**不带任何 AI 署名 / co-author trailer**。
- 相关调查记录在 `docs/ios-motion-itch-findings.md`，改完可以在
  `findings.md` / `progress.md` 里补一条指向它的记录。
