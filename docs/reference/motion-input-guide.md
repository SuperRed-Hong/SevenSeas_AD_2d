# 体感输入系统完全拆解指南

> 覆盖两个测试关卡：`AttitudeControlTest`（倾斜移动）和 `GyroscopeCastTest`（甩杆蓄力）。
> 目标：读完之后，你能说清楚每一行代码在干什么、HUD 上每个数字是什么、以及这两个场景是怎么被"造"出来的。
>
> 写于 2026-08-29。

---

## 目录

- [第 0 章：全景图](#第-0-章全景图)
- [第 1 章：两个传感器的本质区别](#第-1-章两个传感器的本质区别)
- [第 2 章：AttitudeReader.cs 逐段拆解](#第-2-章attitudereadercs-逐段拆解)
- [第 3 章：AttitudeCircleController.cs —— 倾斜怎么变成移动](#第-3-章attitudecirclecontrollercs--倾斜怎么变成移动)
- [第 4 章：GyroscopeReader.cs 逐段拆解](#第-4-章gyroscopereadercs-逐段拆解)
- [第 5 章：CastGestureDetector.cs —— 甩动检测状态机](#第-5-章castgesturedetectorcs--甩动检测状态机)
- [第 6 章：CastTuningProfile —— 参数总表与调参手册](#第-6-章casttuningprofile--参数总表与调参手册)
- [第 7 章：HUD 是怎么"制作"出来的](#第-7-章hud-是怎么制作出来的)
- [第 8 章：HUD 上每个数字的含义](#第-8-章hud-上每个数字的含义)
- [第 9 章：两条完整数据流](#第-9-章两条完整数据流)
- [第 10 章：排查表与已知坑](#第-10-章排查表与已知坑)
- [第 11 章：术语表](#第-11-章术语表)

---

## 第 0 章：全景图

项目里有 **两套独立的体感原型**。它们共用一种"读数器 + 控制器 + HUD"的三层结构，但用的是**不同的传感器**。

| | 关卡 A | 关卡 B |
|---|---|---|
| 场景文件 | `Assets/Scenes/AttitudeControlTest.unity` | `Assets/Scenes/GyroscopeCastTest.unity` |
| 玩法原型 | 岸边移动 / 收线躲避 | 甩杆抛投 |
| 传感器 | **AttitudeSensor**（姿态） | **Gyroscope**（角速度） |
| 读数器 | `Help/AttitudeReader.cs` | `Help/GyroscopeReader.cs` |
| 逻辑层 | `Controller/AttitudeCircleController.cs` | `Controller/CastGestureDetector.cs` |
| 表现层 | Circle 精灵跟着倾斜移动 | `CastBallController` 抛物线飞行 |
| HUD 脚本 | `UI/GyroscopeDebugHUD.cs` | `UI/CastDebugHUD.cs` |
| 场景生成器 | `Editor/GyroscopeDebugSceneSetup.cs` | `Editor/GyroscopeCastTestSceneBuilder.cs` |
| 参数存放 | 直接写在组件 Inspector 上 | `Assets/Settings/CastTuningProfile.asset` |

**三层结构**是理解全部代码的骨架：

```
[硬件传感器]
     │
     ▼
第 1 层  读数器 Reader      ← 只负责"拿到干净的数据"，不懂玩法
     │                       AttitudeReader / GyroscopeReader
     ▼
第 2 层  控制器 Controller  ← 只负责"把数据翻译成玩法意图"，不懂 UI
     │                       AttitudeCircleController / CastGestureDetector
     ▼
第 3 层  表现层 View / HUD  ← 只负责"显示"，不做任何计算
                             Circle 移动 / CastBall 飞行 / 两个 Debug HUD
```

**为什么要分三层？** 因为每一层都可以被单独替换。

- 换传感器 → 只改第 1 层。
- 调手感 → 只改第 2 层的参数。
- 换 UI → 只改第 3 层。

HUD **从来不计算任何东西**。它只是把第 1、2 层的 public 属性读出来，格式化成字符串。这一点非常关键，后面第 8 章会反复用到。

---

## 第 1 章：两个传感器的本质区别

这是整份文档最重要的概念。搞混这两个，后面全部看不懂。

### 1.1 AttitudeSensor = 姿态 = "手机现在朝哪边"

- 返回类型：**`Quaternion`（四元数）**
- 含义：手机相对于"世界"的**绝对朝向**
- 类比：**位置**。它是一个状态，不是一个变化量。
- 手机静止不动 → 读数**保持不变**（比如一直是"向左倾 20°"）

### 1.2 Gyroscope = 陀螺仪 = "手机现在转多快"

- 返回类型：**`Vector3`**，单位 **rad/s**（弧度每秒）
- 含义：绕三个轴的**旋转角速度**
- 类比：**速度**。它是一个变化率。
- 手机静止不动 → 读数**回到 0**，不管手机当前是什么朝向

### 1.3 一句话记忆

> **姿态是位置，角速度是速度。**

所以：

- 「**倾斜**手机让角色左右走」→ 需要知道手机**停在**哪个角度 → 用 **AttitudeSensor**
- 「**甩一下**手机把鱼线抛出去」→ 需要知道手挥得**多快** → 用 **Gyroscope**

如果用陀螺仪做倾斜控制，手机倾斜后停住，读数会归零，角色就停下来了 —— 那不是我们要的。
如果用姿态做甩动检测，你没法区分"慢慢转 90°"和"猛甩 90°"—— 那也不是我们要的。

### 1.4 单位换算

角速度的单位是 **rad/s**。1 弧度 ≈ **57.3°**。

| 读数 (rad/s) | 大约相当于 | 体感 |
|---|---|---|
| 0.1 | 6°/秒 | 手抖 / 噪声 |
| 0.45 | 26°/秒 | 缓慢移动手机 |
| 1.25 | 72°/秒 | 明显的一下挥动 |
| 3.0 | 172°/秒 | 用力甩 |
| 7.0 | 400°/秒 | 大力甩，接近上限 |

后面看到 `triggerThreshold = 1.25` 时，你就知道它的意思是"**每秒转 72 度以上才算一次甩动**"。

### 1.5 手机的三个轴

Unity 里手机传感器的坐标轴（手机竖着拿，屏幕朝你）：

```
        +Y  屏幕向上
         │
         │
         └──── +X  屏幕向右
        ╱
      +Z  屏幕朝向你（从屏幕里指出来）
```

绕轴旋转的含义：

| 轴 | 绕它转 = | 现实动作 |
|---|---|---|
| **X** | 屏幕上缘前后翻 | **点头 / 甩鱼竿**（pitch） |
| **Y** | 左右转身 | 摇头（yaw） |
| **Z** | 屏幕平面内打转 | 歪头（roll） |

`CastTuningProfile` 里 `castAxis = X`，就是因为**甩鱼竿是"点头"方向的动作**。

---

## 第 2 章：`AttitudeReader.cs` 逐段拆解

**文件**：`Assets/Scripts/Help/AttitudeReader.cs`（93 行）

**职责**：一句话 —— **把系统传感器包装成一个随时可读、永远不崩的 `Quaternion` 属性**。

它不知道有 Circle，不知道有 HUD，不知道什么是游戏。

### 2.1 对外暴露的接口

```csharp
public bool IsAvailable => attitudeSensor != null;
public bool IsEnabled   => attitudeSensor != null && attitudeSensor.enabled;
public bool HasSample { get; private set; }
public float SamplingFrequency => ...;
public Quaternion Attitude { get; private set; } = Quaternion.identity;
```

这五个属性就是**整个类的全部产出**。别人只能读，不能写（`private set`）。

| 属性 | 含义 | 什么时候是 false / 0 |
|---|---|---|
| `IsAvailable` | 找到设备了吗 | PC 上没有姿态传感器 → false |
| `IsEnabled` | 设备找到了**并且**已经打开 | 忘了 `EnableDevice` → false |
| `HasSample` | **已经收到过至少一帧有效数据** | 刚启动的头几帧 → false |
| `SamplingFrequency` | 实际采样率（Hz） | 没设备 → 0 |
| `Attitude` | 当前姿态四元数 | 没数据 → `identity`（无旋转） |

**为什么要 `HasSample`？** 因为"设备已连接"和"设备已经吐出数据"是两回事。校准（第 3 章）必须等到真的有数据才能做，否则会拿 `identity` 当基准，全盘错位。

### 2.2 `OnEnable()` —— 开机

```csharp
private void OnEnable() { TryConnect(); }
```

一行。Unity 在组件启用时调用。

> ⚠️ **拼写坑**：`OnEnable` 不能写成 `OnEnabled`。Unity 的生命周期方法是**按名字**反射调用的，拼错不会报错，只会**静默地永远不执行**。这个项目早期就踩过这个坑（见 `progress.md`）。

### 2.3 `TryConnect()` —— 连接设备

```csharp
private void TryConnect()
{
    nextConnectionAttemptTime = Time.unscaledTime + 1f;   // ① 一秒内不再重试
    attitudeSensor = FindAttitudeSensor();                 // ② 找设备

    if (attitudeSensor == null) { return; }                // ③ 没找到就算了

    InputSystem.EnableDevice(attitudeSensor);              // ④ 必须手动打开！
    attitudeSensor.samplingFrequency = requestedSamplingFrequency; // ⑤ 设采样率
}
```

四个要点：

1. **第 ① 行是节流**。找不到设备时不要每帧都找，太浪费。设成"1 秒后再试"。
2. **第 ④ 行最容易漏**。Unity 的 Input System 里，传感器**默认是关闭的**（省电）。不调 `EnableDevice`，`ReadValue()` 永远返回零。
3. **第 ⑤ 行 `requestedSamplingFrequency = 60`**，意思是"请给我 60Hz"。这是**请求**不是命令，硬件可能给别的值 —— 所以才有 `SamplingFrequency` 这个只读属性让你看真实值。
4. `Time.unscaledTime` 而不是 `Time.time`：**不受 `Time.timeScale` 影响**。暂停游戏时（timeScale = 0）传感器逻辑还得正常跑。

### 2.4 `Update()` —— 每帧读数

```csharp
private void Update()
{
    if (attitudeSensor == null)                    // ── 断线分支
    {
        HasSample = false;
        if (Time.unscaledTime >= nextConnectionAttemptTime) { TryConnect(); }
        return;
    }

    if (!attitudeSensor.wasUpdatedThisFrame) { return; }        // 防线 1

    Quaternion sample = attitudeSensor.attitude.ReadValue();

    if (Quaternion.Dot(sample, sample) < 0.0001f) { return; }   // 防线 2

    Attitude = Quaternion.Normalize(sample);                     // 防线 3
    HasSample = true;
}
```

**三道防线**，每一道都在挡一种真实故障：

**防线 1：`wasUpdatedThisFrame`**
传感器是 60Hz，游戏可能跑 120fps。没有新数据的那些帧，这个标志是 false。直接 `return`，保留上一帧的 `Attitude` 不动。
→ 挡的是：**用陈旧数据做无意义的重复计算**。

**防线 2：`Quaternion.Dot(sample, sample) < 0.0001f`**
`Dot(q, q)` 就是四元数长度的平方。合法的旋转四元数长度必须是 1，所以 `Dot` 应该 ≈ 1。
如果传感器还没初始化好，会吐出 `(0,0,0,0)`，`Dot` = 0 → 被挡住。
→ 挡的是：**零四元数**，用它做旋转会得到 NaN，整个物体会消失。

**防线 3：`Quaternion.Normalize`**
即使长度接近 1，浮点误差累积也会让它漂移。强制归一化。
→ 挡的是：**长期运行后的数值漂移**。

### 2.5 `OnDisable()` —— 收尾

```csharp
private void OnDisable()
{
    if (attitudeSensor != null) { InputSystem.DisableDevice(attitudeSensor); }
    attitudeSensor = null;
    Attitude = Quaternion.identity;
    HasSample = false;
}
```

**关掉设备省电**，并把状态**完全归零**。为什么要归零？因为组件可能被重新启用，残留的旧姿态会让第一帧的校准结果错误。

### 2.6 `FindAttitudeSensor()` —— Unity Remote 特殊处理

```csharp
private static AttitudeSensor FindAttitudeSensor()
{
#if UNITY_EDITOR
    foreach (InputDevice device in InputSystem.devices)
    {
        if (device.remote && device is AttitudeSensor remoteAttitudeSensor)
            return remoteAttitudeSensor;
    }
#endif
    return AttitudeSensor.current;
}
```

这段是**开发效率的关键**。

- `#if UNITY_EDITOR` 包起来的代码**只在编辑器里编译**，打包成 WebGL / APK 后这段代码不存在。
- **Unity Remote** 是一个手机 App：手机连数据线，手机的传感器数据流回编辑器。
- 通过 Remote 来的设备，`device.remote == true`。
- **`AttitudeSensor.current` 在 Remote 场景下不可靠**，可能返回编辑器本机的（不存在的）设备，所以要手动遍历 `InputSystem.devices` 优先找 remote 的那个。

**没有这段代码，你每改一个参数都要打包到手机上测。** 有了它，插上线按 Play 就能测。

---

## 第 3 章：`AttitudeCircleController.cs` —— 倾斜怎么变成移动

**文件**：`Assets/Scripts/Controller/AttitudeCircleController.cs`（225 行）

这是两个关卡里**数学最密集**的一个文件。但它的结构其实是一条直线管线。

### 3.1 六步管线

```
① 校准     记住"当前朝向"作为零点          → neutralAttitude
      ↓
② 相对化   当前姿态 ÷ 零点姿态               → relativeAttitude
      ↓
③ 投影     四元数 → 前方向量 → 两个角度      → tiltAngles (度)
      ↓
④ 归一化   死区 + 映射到 [-1, 1]             → normalizedTilt
      ↓
⑤ 锁轴     只保留左右 或 只保留上下（D-pad） → 单轴 normalizedTilt
      ↓
⑥ 映射     [-1,1] → 屏幕坐标 + 平滑          → transform.position
```

下面逐步拆。

### 3.2 步骤 ①：校准（`Calibrate`）

```csharp
[SerializeField, Min(0f)] private float autoCalibrationDelay = 0.5f;

public void Calibrate()
{
    if (attitudeReader == null || !attitudeReader.HasSample) { return; }
    neutralAttitude = attitudeReader.Attitude;   // 记住"现在"就是零点
    TiltDegrees = Vector2.zero;
    dominantAxis = DominantAxis.None;
    IsCalibrated = true;
}
```

**为什么必须校准？** 因为**没有"正确的握持姿势"**。有人躺着玩，有人趴着玩。绝对姿态毫无意义，**相对于开局姿势的偏移**才有意义。

**`autoCalibrationDelay = 0.5f`**：启动后等 0.5 秒才自动校准。给玩家时间把手机端稳，也给传感器时间稳定下来。这 0.5 秒内圆点停在中心不动。

### 3.3 步骤 ②：相对化

```csharp
Quaternion relativeAttitude = Quaternion.Inverse(neutralAttitude) * attitudeReader.Attitude;
```

**四元数的"减法"就是乘以逆**。

- 数字世界：`当前 - 零点`
- 旋转世界：`Inverse(零点) * 当前`

结果 `relativeAttitude` 的含义：**"从校准那一刻起，手机转了多少"**。手机没动 → 结果是 `identity`。

### 3.4 步骤 ③：投影（`GetTiltAngles`）

四元数是 4 个数，人类看不懂。要变成"左右倾了几度、前后倾了几度"。

```csharp
Vector3 relativeForward = relativeAttitude * Vector3.forward;
```

**这一行的意思**：拿一支笔，垂直插在手机背面（这就是 `Vector3.forward`）。手机一转，笔尖就指向别处。`relativeForward` 就是**笔尖现在指的方向**。

- 手机没动 → `relativeForward = (0, 0, 1)`
- 手机向右倾 → 笔尖偏右 → `relativeForward.x > 0`
- 手机向前倾 → 笔尖偏下 → `relativeForward.y < 0`

然后用 `Atan2` 把向量变回角度：

```csharp
private static Vector3 GetTiltAngles(Quaternion relativeAttitude, Vector3 relativeForward)
{
    float x = Mathf.Atan2(-relativeForward.y, relativeForward.z) * Mathf.Rad2Deg;  // 前后倾 pitch
    float y = Mathf.Atan2( relativeForward.x, relativeForward.z) * Mathf.Rad2Deg;  // 左右倾 yaw
    float z = Mathf.DeltaAngle(0f, relativeAttitude.eulerAngles.z);                // 屏幕内旋转 roll
    return new Vector3(x, y, z);
}
```

`Atan2(对边, 邻边)` 就是**从两条直角边反推夹角**。`Mathf.Rad2Deg` 把弧度换成度。

> **为什么不用 `eulerAngles` 直接读三个角？**
> 因为欧拉角有**万向锁（gimbal lock）**和**多解**问题：`eulerAngles` 可能返回 `(350, 0, 0)`，也可能返回 `(-10, 0, 0)`，同一个姿态两种写法。用 `Atan2` 从方向向量算，结果永远唯一、连续。
> 注意 z 轴用的仍然是 `eulerAngles.z`，但外面套了 `Mathf.DeltaAngle(0f, ...)` 把 `350°` 修正成 `-10°`。

**安全阀**（步骤 ③ 之前）：

```csharp
[SerializeField, Range(45f, 89f)] private float maxSupportedTiltDegrees = 75f;

if (Vector3.Angle(Vector3.forward, relativeForward) > maxSupportedTiltDegrees)
{
    TiltDegrees = Vector2.zero;
    dominantAxis = DominantAxis.None;
    MoveTo(Vector2.zero);
    return;
}
```

当手机相对零点转过 **75°** 时，`relativeForward.z` 接近 0，`Atan2` 会剧烈跳变甚至翻转符号 —— 圆点会疯狂抽搐。所以**超过 75° 直接放弃，回到中心**。这是数学上的必要保护，不是设计选择。

### 3.5 步骤 ④：归一化（`NormalizeTilt`）+ 死区

```csharp
[SerializeField, Min(0f)]  private float deadZoneDegrees = 1.5f;
[SerializeField, Range(1f, 90f)] private float maxTiltDegrees = 25f;

private float NormalizeTilt(float degrees)
{
    float magnitude = Mathf.Abs(degrees);
    if (magnitude <= deadZoneDegrees) { return 0f; }              // 死区

    float usableRange = Mathf.Max(0.001f, maxTiltDegrees - deadZoneDegrees);
    float normalized = (magnitude - deadZoneDegrees) / usableRange;
    return Mathf.Sign(degrees) * Mathf.Clamp01(normalized);
}
```

图解（当前参数 死区 1.5°，满量程 25°）：

```
输出
 1.0 ┤                    ┌────────────  ← 25° 以上都是满速
     │                  ／
 0.5 ┤              ／
     │          ／
 0.0 ┼──────┬─────────────────────────  输入角度
     0    1.5°                25°
       └死区┘└──── 有效区间 23.5° ────┘
```

- **死区 `deadZoneDegrees = 1.5`**：手不可能绝对稳。1.5° 以内一律当作 0，否则圆点会一直微微抖。
- **满量程 `maxTiltDegrees = 25`**：倾斜 25° 就到最大速度。**再倾也没用**。
- 注意 `usableRange` 是 `25 - 1.5 = 23.5`，不是 25。**死区被"减掉"而不是"吃掉"**，这样刚出死区时输出是 0 而不是突然跳到 0.06 —— 手感是连续的。

### 3.6 步骤 ⑤：锁轴（`SelectDominantAxis`）

这是**为了满足项目规则「使用方向键（D-pad）控制」**而存在的。

真实的方向键**不能同时按左和上**（或者说，斜向不是设计目标）。所以这里**强制每次只有一个轴生效**。

```csharp
[SerializeField, Range(0f, 10f)] private float axisSwitchHysteresisDegrees = 2f;

private Vector2 SelectDominantAxis(Vector2 normalizedTilt, Vector2 tiltDegrees)
{
    float horizontal = Mathf.Abs(tiltDegrees.x);
    float vertical   = Mathf.Abs(tiltDegrees.y);

    if (normalizedTilt == Vector2.zero) { dominantAxis = DominantAxis.None; return Vector2.zero; }

    // 已经在横轴 → 除非竖轴明显更大，否则继续待在横轴
    if (dominantAxis == DominantAxis.Horizontal && vertical <= horizontal + axisSwitchHysteresisDegrees)
        return new Vector2(normalizedTilt.x, 0f);

    // 已经在竖轴 → 同理
    if (dominantAxis == DominantAxis.Vertical && horizontal <= vertical + axisSwitchHysteresisDegrees)
        return new Vector2(0f, normalizedTilt.y);

    // 重新裁决：谁大听谁的
    dominantAxis = horizontal >= vertical ? DominantAxis.Horizontal : DominantAxis.Vertical;
    return dominantAxis == DominantAxis.Horizontal
        ? new Vector2(normalizedTilt.x, 0f)
        : new Vector2(0f, normalizedTilt.y);
}
```

**关键概念：迟滞（hysteresis）**

如果只写"谁大听谁的"，当横轴 = 10.0°、竖轴 = 10.1° 时，微小的手抖会让主轴**每帧疯狂来回切换**，圆点会卡在原地抽搐。

`axisSwitchHysteresisDegrees = 2f` 的意思是：

> 当前在横轴时，**竖轴要比横轴大 2° 以上**，才允许切到竖轴。

这就制造了一个"粘性"。切换需要明确的意图，不会被噪声触发。

**这个 hysteresis 模式在游戏开发里到处都是** —— 门槛不对称、进入和退出用不同阈值。第 5 章的 `triggerThreshold` / `rearmThreshold` 是同一个思想的另一种形态。

### 3.7 步骤 ⑥：映射到屏幕 + 平滑

```csharp
[SerializeField, Range(0.1f, 1f)] private float movementRange = 0.85f;

private void MoveTo(Vector2 normalizedTilt)
{
    float halfHeight = targetCamera.orthographicSize;         // 相机可视高度的一半
    float halfWidth  = halfHeight * targetCamera.aspect;      // 宽 = 高 × 宽高比
    Vector3 extents  = spriteRenderer.bounds.extents;         // 圆点自己的半径

    center = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
    availableRange = new Vector2(
        Mathf.Max(0f, halfWidth  - extents.x),                // 减掉自身半径 → 不会半个身子出屏
        Mathf.Max(0f, halfHeight - extents.y)) * movementRange;

    Vector3 targetPosition = center + new Vector3(
        normalizedTilt.x * availableRange.x,
        normalizedTilt.y * availableRange.y,
        0f);
    ApplySmoothedPosition(targetPosition);
}
```

三个细节：

1. **`halfHeight - extents.x`**：减掉圆点自身尺寸，保证圆点**贴边而不出界**。
2. **`movementRange = 0.85`**：再留 15% 边距。屏幕最边缘在手机上可能被圆角、刘海、手指挡住。
3. 注意 `center` 用的是**相机位置**，不是世界原点 —— 相机移动时圆点跟着。

**平滑：**

```csharp
[SerializeField, Min(0f)] private float smoothing = 12f;

private void ApplySmoothedPosition(Vector3 targetPosition)
{
    float blend = smoothing > 0f ? 1f - Mathf.Exp(-smoothing * Time.deltaTime) : 1f;
    transform.position = Vector3.Lerp(transform.position, targetPosition, blend);
}
```

**`1f - Mathf.Exp(-k * dt)` 这个公式非常重要，第 5 章还会再出现一次。**

初学者常写 `Lerp(current, target, 0.1f)`。**这是个 bug**：60fps 时每帧追 10%，120fps 时每帧也追 10% —— 帧率越高，移动越快。**手感会随设备变化**。

`1 - e^(-k·dt)` 把 `dt` 考虑进去了，**结果与帧率无关**。

`k`（这里是 `smoothing = 12`）的物理含义是"**时间常数的倒数**"：

- 时间常数 τ = 1/k = 1/12 ≈ **0.083 秒**
- 含义：**约 0.083 秒走完到目标距离的 63%**
- k 越大 → 越跟手、越硬；k 越小 → 越飘、越软

| `smoothing` | τ | 手感 |
|---|---|---|
| 30 | 0.033s | 几乎瞬移，会显得抖 |
| 12 | 0.083s | **当前值**，跟手但不抖 |
| 5 | 0.2s | 明显的"惯性"/漂浮感 |
| 0 | — | 关闭平滑，直接瞬移 |

### 3.8 `AttitudeCircleController` 参数总表

| Inspector 字段 | 当前值 | 含义 | 调大 | 调小 |
|---|---|---|---|---|
| `attitudeReader` | 场景引用 | 数据来源 | — | — |
| `horizontalAxis` | `Y` | 哪个倾斜角控制**左右** | — | — |
| `verticalAxis` | `X` | 哪个倾斜角控制**上下** | — | — |
| `invertHorizontal` | `false` | 左右反向 | — | — |
| `invertVertical` | `true` | 上下反向（**默认开**：手机前倾 = 角色向下） | — | — |
| `deadZoneDegrees` | `1.5` | 死区角度 | 更稳，但反应迟钝 | 更灵敏，可能抖 |
| `maxTiltDegrees` | `25` | 满速所需倾角 | 需要倾更多才满速（更"重"） | 一点点倾斜就满速（更"轻"） |
| `maxSupportedTiltDegrees` | `75`（脚本默认） | 超过此角放弃计算 | 数学不稳定风险 ↑ | 更早"失灵" |
| `axisSwitchHysteresisDegrees` | `2`（脚本默认） | 换轴粘性 | 更难换轴 | 更容易误换轴 |
| `movementRange` | `0.85` | 可移动区域占屏幕比例 | 更贴边 | 活动范围更小 |
| `smoothing` | `12` | 平滑强度（1/τ） | 更跟手 | 更飘 |
| `autoCalibrationDelay` | `0.5` | 启动后多久自动校准 | 给玩家更多准备时间 | 更快能动，但可能校准到抖动姿态 |

> ⚠️ **注意**：`maxSupportedTiltDegrees` 和 `axisSwitchHysteresisDegrees` 这两个字段**目前没有存进 `AttitudeControlTest.unity` 场景文件**（它们是在场景最后一次保存之后才加进脚本的）。Unity 会退回用脚本里的默认值 75 和 2。下次在 Unity 里打开并保存场景，它们就会被写入。行为上没问题，但你在 Inspector 里看到的值是"默认值"而不是"存过的值"。

---

## 第 4 章：`GyroscopeReader.cs` 逐段拆解

**文件**：`Assets/Scripts/Help/GyroscopeReader.cs`（71 行）

**好消息**：它和 `AttitudeReader` **结构完全对称**。第 2 章看懂了，这一章 3 分钟就能过。

### 4.1 命名冲突处理

```csharp
using InputSystemGyroscope = UnityEngine.InputSystem.Gyroscope;
```

Unity 里有**两个** `Gyroscope` 类：

- `UnityEngine.Gyroscope` —— 旧的 Legacy Input（已废弃）
- `UnityEngine.InputSystem.Gyroscope` —— 新的 Input System（本项目用的）

两个 `using` 都在的话编译器会报"歧义引用"。这行 **类型别名** 给新的起个不重名的名字，之后代码里写 `InputSystemGyroscope` 就不会歧义。

### 4.2 接口对照表

| `AttitudeReader` | `GyroscopeReader` | 差别 |
|---|---|---|
| `IsAvailable` | `IsAvailable` | 相同 |
| `IsEnabled` | `IsEnabled` | 相同 |
| `SamplingFrequency` | `SamplingFrequency` | 相同 |
| `Attitude` (Quaternion) | `AngularVelocity` (Vector3) | **核心数据类型不同** |
| — | `AngularSpeed` (= `AngularVelocity.magnitude`) | 陀螺仪独有：三轴合成的"总转速" |
| — | `DeviceName` | 陀螺仪独有：设备显示名 |
| `HasSample` | `WasUpdatedThisFrame` | 语义不同，见下 |

### 4.3 `Update()` 的关键差别

```csharp
private void Update()
{
    if (gyroscope == null)
    {
        AngularVelocity = Vector3.zero;      // ← 断线时归零
        WasUpdatedThisFrame = false;
        if (Time.unscaledTime >= nextConnectionAttemptTime) { TryConnect(); }
        return;
    }

    WasUpdatedThisFrame = gyroscope.wasUpdatedThisFrame;
    AngularVelocity = gyroscope.angularVelocity.ReadValue();   // ← 每帧无条件读
}
```

对比 `AttitudeReader`，有两处**故意的不同**：

**① 没有"没更新就 return"的防线。** 陀螺仪即使没新数据，读到的也是"上一次的角速度"，这是合理值。而且陀螺仪本来就在快速变化，多读一次不会出错。（`WasUpdatedThisFrame` 被暴露出来，让调用方自己决定要不要在意。）

**② 断线时 `AngularVelocity = Vector3.zero`。**
姿态的"安全值"是 `identity`（无旋转），角速度的"安全值"是 `zero`（不转）。**语义上都是"什么都没发生"**。

**③ 没有零值检查。** 因为 `(0,0,0)` 对角速度来说是**完全合法的**（手机静止）。而对姿态四元数来说 `(0,0,0,0)` 是非法的。

### 4.4 `AngularSpeed` 的用途

```csharp
public float AngularSpeed => AngularVelocity.magnitude;
```

`magnitude` = √(x² + y² + z²)，**三轴合起来的总转速**，与方向无关。

HUD 上的 `MAG` 和 `PEAK` 就是它。它的作用是**快速判断"手机到底动没动"** —— 不用看三个轴，看一个数就够。

---

## 第 5 章：`CastGestureDetector.cs` —— 甩动检测状态机

**文件**：`Assets/Scripts/Controller/CastGestureDetector.cs`（168 行）

这是整个项目**最精妙**的一个文件。它解决的问题是：

> 从一串连续的角速度数字里，识别出"玩家甩了一下"，并给出 0~1 的力度。

### 5.1 为什么需要状态机

天真的写法：

```csharp
if (angularVelocity.x > 阈值) { 抛出鱼线(); }   // ❌ 完全不行
```

三个问题：

1. **一次挥动会触发 N 次**。一次甩动持续 0.3 秒，60fps 下有 18 帧超过阈值 → 抛 18 次。
2. **力度是错的**。超过阈值的**第一帧**通常不是最快的那一帧，真正的峰值在中间。
3. **回手也会触发**。你把手甩出去还要收回来，收回的动作也会超阈值。

状态机把这三个问题一次解决。

### 5.2 状态机全图

```
                    ┌──────────────┐
                    │ Unavailable  │  没传感器 / 没配置文件
                    └──────┬───────┘
                           │ 传感器就绪
                           ▼
     ┌───────────────►┌──────────┐
     │                │  Ready   │  等待甩动。同时检查 isArmed
     │                └────┬─────┘
     │   冷却结束           │ DirectedVelocity ≥ triggerThreshold (1.25)
     │   (0.75s)           │ 且 isArmed == true
     │                     ▼
┌────┴─────┐         ┌──────────┐
│ Cooldown │         │ Sampling │  记录峰值，持续 sampleWindow (0.35s)
└────▲─────┘         └────┬─────┘
     │                    │ 0.35 秒到
     │  0.2s 显示时间      ▼
     │              ┌──────────────┐
     └──────────────┤ CastDetected │  算出 CastPower，触发事件
                    └──────────────┘
```

**每个状态的存在理由：**

| 状态 | 干什么 | 为什么需要它 |
|---|---|---|
| `Unavailable` | 什么都不做 | 传感器还没连上时不能误判 |
| `Ready` | 监听触发 | 正常待机 |
| `Sampling` | **记录 0.35 秒内的最大值** | 解决"力度取值不对"问题 |
| `CastDetected` | 停 0.2 秒 | 纯粹给 HUD **看得见** —— 否则这个状态一帧就过去了 |
| `Cooldown` | 停 0.75 秒 | 解决"回手误触发"问题 |

### 5.3 每帧固定做的两件事

不管在哪个状态，`Update()` 开头都会跑这两步：

```csharp
// 第 1 步：取出"甩杆方向"上的速度
DirectedVelocity = ReadDirectedVelocity(reader.AngularVelocity);

// 第 2 步：低通滤波
float blend = tuningProfile.FilterSharpness > 0f
    ? 1f - Mathf.Exp(-tuningProfile.FilterSharpness * Time.unscaledDeltaTime)
    : 1f;
FilteredVelocity = Mathf.Lerp(FilteredVelocity, DirectedVelocity, blend);
```

**第 1 步 —— `ReadDirectedVelocity`：**

```csharp
private float ReadDirectedVelocity(Vector3 angularVelocity)
{
    float value = tuningProfile.CastAxis switch
    {
        CastSensorAxis.X => angularVelocity.x,
        CastSensorAxis.Y => angularVelocity.y,
        CastSensorAxis.Z => angularVelocity.z,
        _ => 0f
    };
    return tuningProfile.InvertAxis ? -value : value;
}
```

三维向量 → 一个数。只关心**甩杆那个方向**（当前是 X 轴 = 点头方向）。

**注意它是有符号的**：正值 = 往一个方向转，负值 = 往回转。这非常关键 —— 后面 `Ready` 状态只在 `DirectedVelocity >= triggerThreshold`（**正值**）时触发，所以**往回甩不会触发**。`InvertAxis` 就是给你翻转"哪个方向算甩出去"的开关。

**第 2 步 —— 低通滤波（Low-pass filter）：**

这里又出现了 `1 - Mathf.Exp(-k * dt)`，和第 3.7 节完全一样的公式。只是这次用来**平滑数据**而不是平滑位置。

传感器数据是**很脏的**（噪声、抖动、单帧尖峰）。滤波把毛刺磨平：

```
RAW（原始）：      ╱╲  ╱╲╱╲    ╱╲
                 ╱  ╲╱      ╲╱  ╲
FILTERED（滤波）： ╱‾‾‾‾‾‾‾‾‾‾‾╲
                ╱             ╲
```

`filterSharpness = 20` → τ = 1/20 = **0.05 秒**。滤波器"记忆"约 50 毫秒。

- 调大（如 40）→ 更贴近原始数据，反应快但噪声多
- 调小（如 8）→ 更平滑，但会**削峰**（真正的最大值被磨掉），力度普遍偏低

### 5.4 `Ready` 状态与 **armed 机制**

```csharp
private void UpdateReadyState()
{
    if (!isArmed)
    {
        isArmed = Mathf.Abs(DirectedVelocity) <= tuningProfile.RearmThreshold;  // 0.45
        return;
    }

    if (DirectedVelocity < tuningProfile.TriggerThreshold) { return; }          // 1.25

    State = CastDetectionState.Sampling;
    stateStartTime = Time.unscaledTime;
    RawPeak      = Mathf.Max(0f, DirectedVelocity);
    FilteredPeak = Mathf.Max(0f, FilteredVelocity);
    SamplingProgress = 0f;
    isArmed = false;      // ← 用掉了，必须重新"上膛"
}
```

**`isArmed` 是"扳机上膛了吗"。** 想象一把枪：开一枪之后，必须**松开扳机**才能开下一枪。按住不放不会连发。

**两个不同的阈值：**

| 阈值 | 值 | 作用 |
|---|---|---|
| `triggerThreshold` | **1.25** rad/s | 超过它 → **触发**甩动 |
| `rearmThreshold` | **0.45** rad/s | **绝对值**低于它 → 重新上膛 |

这叫 **施密特触发器（Schmitt trigger）**，是硬件电路里的经典模式。

**为什么两个阈值不能相等？** 如果都是 1.25，当速度在 1.24 ↔ 1.26 之间抖动时，会**疯狂触发**。设成 1.25 / 0.45，速度必须**真正回落到接近静止**才能再次触发。

注意 `rearmThreshold` 用的是 `Mathf.Abs()` —— **正反两个方向都要静下来**才算。这就是"回手甩不会误触发"的保证：回手时 `DirectedVelocity` 是很大的负数，绝对值远超 0.45，不会重新上膛。

### 5.5 `Sampling` 状态 —— 峰值采样

```csharp
private void UpdateSamplingState()
{
    RawPeak      = Mathf.Max(RawPeak, DirectedVelocity);      // 一直取最大
    FilteredPeak = Mathf.Max(FilteredPeak, FilteredVelocity);

    float elapsed = Time.unscaledTime - stateStartTime;
    SamplingProgress = Mathf.Clamp01(elapsed / tuningProfile.SampleWindow);  // 0.35s

    if (elapsed < tuningProfile.SampleWindow) { return; }     // 还没到时间，继续等

    CastPower = tuningProfile.EvaluatePower(FilteredPeak);    // ← 用滤波峰值算力度
    State = CastDetectionState.CastDetected;
    stateStartTime = Time.unscaledTime;
    SamplingProgress = 1f;
    CastDetected?.Invoke(CastPower);                          // ← 广播事件
}
```

**核心思想**：不在触发的那一瞬间就下结论。**再等 0.35 秒**，看这段时间里最快到底有多快。

```
速度
  │        ╭──╮  ← 真正的峰值（第 0.18 秒）
  │       ╱    ╲
1.25├─────●──────╲────────  ← triggerThreshold
  │    ╱          ╲         ● = 触发点（速度才刚过线）
  │  ╱              ╲
  └─┴────────────────╲──── 时间
    ↑                 ↑
  触发            0.35 秒后结算
    └── Sampling 窗口 ──┘
```

如果在 ● 点就结算，力度只有刚过阈值的一点点。等 0.35 秒，才能拿到真正的最大值。

**`sampleWindow = 0.35` 怎么定的？** 人类一次挥动手臂大约 0.3~0.4 秒。太短抓不到峰值，太长会把回手动作也算进去。

### 5.6 为什么用 `FilteredPeak` 而不是 `RawPeak` 算力度？

```csharp
CastPower = tuningProfile.EvaluatePower(FilteredPeak);   // 注意是 Filtered
```

因为 `RawPeak` 会被**单帧噪声尖峰**污染。传感器偶尔会吐一个离谱的大值，`Mathf.Max` 会忠实地把它记下来 → 玩家轻轻一甩却得到 100% 力度 → **不公平、不可控**。

`FilteredPeak` 经过了 0.05 秒的平滑，单帧尖峰被压掉了，反映的是**持续的挥动强度**。

**两个都暴露在 HUD 上**，就是为了让你调试时能看出差距。如果 `RAW PEAK` 和 `FILTERED PEAK` 差很多，说明噪声大或者 `filterSharpness` 太小。

### 5.7 `CastDetected` 和 `Cooldown`

```csharp
case CastDetectionState.CastDetected:
    if (Time.unscaledTime - stateStartTime >= castDetectedDisplayDuration)  // 0.2s
    {
        State = CastDetectionState.Cooldown;
        stateStartTime = Time.unscaledTime;
    }
    break;

case CastDetectionState.Cooldown:
    if (Time.unscaledTime - stateStartTime >= tuningProfile.CooldownDuration)  // 0.75s
    {
        EnterReadyState();
    }
    break;
```

- **`castDetectedDisplayDuration = 0.2`**：这个参数**纯粹为 HUD 服务**。没有它，`CastDetected` 状态一帧就过去了，你在 HUD 上永远看不到 `STATE  CastDetected`。它是**调试可见性**参数，不影响玩法。
- **`cooldownDuration = 0.75`**：抛出之后 0.75 秒内不接受新的甩动。让玩家的手有时间回位，也防止连抛。

回到 `Ready` 时还要再检查一次上膛：

```csharp
private void EnterReadyState()
{
    State = CastDetectionState.Ready;
    stateStartTime = Time.unscaledTime;
    SamplingProgress = 0f;
    isArmed = Mathf.Abs(DirectedVelocity) <= tuningProfile.RearmThreshold;  // ← 双保险
}
```

### 5.8 `CastDetected` 事件 —— 解耦的关键

```csharp
public event Action<float> CastDetected;
...
CastDetected?.Invoke(CastPower);
```

`CastGestureDetector` **不知道有球，不知道有场景，不知道有 HUD**。它只是喊一声"甩动了，力度 0.73"。

订阅方在 `CastTestController.cs`：

```csharp
private void OnEnable()  { detector.CastDetected += HandleCastDetected; }
private void OnDisable() { detector.CastDetected -= HandleCastDetected; }   // ← 必须取消订阅！

private void HandleCastDetected(float power)
{
    if (TrialStarted) { return; }     // 一次测试只抛一次
    TrialStarted = true;
    ball?.Launch(power);
}
```

> ⚠️ **事件订阅必须成对**。`OnEnable` 里 `+=`，`OnDisable` 里 `-=`。忘了取消订阅会导致对象被销毁后事件还在调用它 → 内存泄漏 + `MissingReferenceException`。这个项目做对了。

---

## 第 6 章：`CastTuningProfile` —— 参数总表与调参手册

**文件**：`Assets/Scripts/Controller/CastTuningProfile.cs` + `Assets/Settings/CastTuningProfile.asset`

### 6.1 为什么它是 ScriptableObject 而不是普通字段？

```csharp
[CreateAssetMenu(fileName = "CastTuningProfile", menuName = "Seven Seas/Cast Tuning Profile")]
public sealed class CastTuningProfile : ScriptableObject
```

**ScriptableObject = 存在硬盘上的数据资产**，不属于任何场景、任何 GameObject。

三个好处：

1. **一处修改，处处生效**。`CastGestureDetector` 和 `CastBallController` 都引用同一个 asset。不会出现"检测器的阈值改了，但球的发射速度还是旧的"。
2. **Play Mode 里改了会保留**。改场景里组件的值，退出 Play Mode 就还原了。改 ScriptableObject 的值，**会留下**。这对调手感是巨大的效率提升。
3. **可以做多套预设**。以后可以有 `EasyProfile` / `HardProfile`，运行时切换。

### 6.2 完整参数表（含当前 asset 里的实际值）

#### `[Header("Sensor")]` —— 检测参数

| 字段 | 当前值 | 单位 | 含义 |
|---|---|---|---|
| `castAxis` | `X` | — | 用哪个轴的角速度。X = 点头方向 = 甩鱼竿 |
| `invertAxis` | `false` | — | 翻转正负方向。如果"往前甩"检测不到但"往后甩"能，打开它 |
| `triggerThreshold` | `1.25` | rad/s | 超过它才算一次甩动（≈ 72°/秒） |
| `rearmThreshold` | `0.45` | rad/s | 绝对值低于它才重新上膛（≈ 26°/秒） |
| `sampleWindow` | `0.35` | 秒 | 触发后采集峰值的时长 |
| `cooldownDuration` | `0.75` | 秒 | 抛出后的冷却时间 |
| `filterSharpness` | `20` | 1/秒 | 低通滤波强度，τ = 1/20 = 0.05 秒 |

#### `[Header("Power")]` —— 力度映射

| 字段 | 当前值 | 含义 |
|---|---|---|
| `minimumPeak` | `1.5` | 峰值 ≤ 1.5 rad/s → 力度 **0%** |
| `maximumPeak` | `7` | 峰值 ≥ 7 rad/s → 力度 **100%** |
| `powerCurve` | EaseInOut | 归一化之后再过一条曲线 |

```csharp
public float EvaluatePower(float peak)
{
    float rangeMaximum = Mathf.Max(minimumPeak + 0.001f, maximumPeak);  // 防止除零
    float normalized = Mathf.InverseLerp(minimumPeak, rangeMaximum, peak);  // 1.5~7 → 0~1
    return Mathf.Clamp01(powerCurve.Evaluate(normalized));                  // 再过曲线
}
```

**`Mathf.InverseLerp` 是 `Lerp` 的反函数**：给它一个值，告诉你它在区间里的百分比位置。

- `InverseLerp(1.5, 7, 1.5)` = 0
- `InverseLerp(1.5, 7, 4.25)` = 0.5
- `InverseLerp(1.5, 7, 7)` = 1
- `InverseLerp(1.5, 7, 20)` = 1（自动 clamp）

**`powerCurve` 当前是 `AnimationCurve.EaseInOut(0,0,1,1)`** —— 一条 S 形曲线，两端平缓、中间陡。

```
力度
 1.0 ┤              ╭───
     │            ╱
 0.5 ┤          ╱          ← S 形：中间敏感，两端"稳"
     │        ╱
 0.0 ┼───╯
     0        0.5        1.0   归一化峰值
```

**S 形的手感含义**：轻甩和大力甩都"稳定"（不会因为一点点差别就跳很多），中等力度区间最敏感（玩家能精细控制）。这是**刻意的设计**，不是随手填的。

> 💡 `powerCurve` 在 Inspector 里是**可以直接拖曲线**的。想要"轻甩也能飞很远"就把曲线前段拉高。这是最直观的手感调节工具。

#### `[Header("Launch")]` —— 发射速度

| 字段 | 当前值 | 含义 |
|---|---|---|
| `minimumLaunchVelocity` | `(3.5, 6.5)` | 力度 0% 时的初速度（x = 水平，y = 垂直） |
| `maximumLaunchVelocity` | `(9.5, 12.5)` | 力度 100% 时的初速度 |

```csharp
public Vector2 EvaluateLaunchVelocity(float power)
{
    return Vector2.Lerp(minimumLaunchVelocity, maximumLaunchVelocity, Mathf.Clamp01(power));
}
```

**注意 x 和 y 是分开插值的。** 这意味着抛射角度会随力度变化：

- 力度 0%：角度 = atan2(6.5, 3.5) ≈ **62°**（高抛，飞不远）
- 力度 100%：角度 = atan2(12.5, 9.5) ≈ **53°**（更平，飞得远）

大力甩不仅更快，而且**角度更平**，距离增长是**超线性**的。这让"用力甩"感觉更爽。

### 6.3 调参手册：玩家反馈 → 改哪个参数

| 玩家说 | 大概率是 | 改这个 |
|---|---|---|
| "我甩了但没反应" | 触发阈值太高 | `triggerThreshold` ↓（1.25 → 0.9） |
| "我没甩它就自己抛了" | 触发阈值太低 | `triggerThreshold` ↑ |
| "一甩抛两次" | 上膛阈值太高 | `rearmThreshold` ↓（0.45 → 0.25） |
| "抛完要等好久才能再抛" | 冷却太长 | `cooldownDuration` ↓ |
| "力度总是满的" | 满量程太低 | `maximumPeak` ↑（7 → 10） |
| "怎么甩力度都很低" | 满量程太高 / 削峰 | `maximumPeak` ↓ 或 `filterSharpness` ↑ |
| "力度很随机、控制不了" | 噪声进了峰值 | `filterSharpness` ↓（20 → 12） |
| "抛出去总是慢半拍" | 采样窗口太长 | `sampleWindow` ↓（0.35 → 0.25） |
| "轻甩重甩距离差太多" | 曲线太陡 | 把 `powerCurve` 拉平 |
| "球飞得不够远" | 发射速度太小 | `maximumLaunchVelocity` ↑ |

**调参顺序建议**：先调 `triggerThreshold`（能不能触发），再调 `minimumPeak` / `maximumPeak`（力度范围合不合理），最后调 `powerCurve`（手感曲线）。

---

## 第 7 章：HUD 是怎么"制作"出来的

这是你问题里最核心的一块。答案可能和你想的不一样：

> **HUD 不是在 Unity 编辑器里手动拖出来的。它是被 C# 代码生成的。**

### 7.1 核心做法：Editor 脚本一键生成场景

两个文件：

| 文件 | 菜单项 | 生成什么 |
|---|---|---|
| `Editor/GyroscopeDebugSceneSetup.cs` | `Seven Seas > Setup Gyroscope Debug Scene` | 在**已有场景**里加 HUD |
| `Editor/GyroscopeCastTestSceneBuilder.cs` | `Seven Seas > Create Gyroscope Cast Test Scene` | 从零**创建整个场景** |

它们放在 `Assets/Editor/` 文件夹里。**这个文件夹名是 Unity 的魔法约定**：里面的脚本只在编辑器运行，**不会被打包**进游戏。

```csharp
[MenuItem("Seven Seas/Create Gyroscope Cast Test Scene")]
public static void CreateScene() { ... }
```

`[MenuItem]` 特性会在 Unity 顶部菜单栏加一项。点一下，整个场景就建好了。

**为什么这么做？**

| 优点 | 缺点 |
|---|---|
| **可重复**：删了重建，结果一模一样 | 写起来比手拖慢 |
| **可 review**：布局是代码，能看 diff | 看不到"所见即所得" |
| **可交接**：别人 clone 下来点一下就有 | 微调布局要改代码再重跑 |
| **不会漏连线**：引用是代码写死的 | — |

对于**调试用 HUD**，这个取舍非常划算。正式游戏 UI 通常还是手拖 + Prefab。

### 7.2 理解布局代码的钥匙：`anchorMin` / `anchorMax`

**如果你只从这一章记住一件事，就是这个。** 整个 HUD 布局代码全靠它。

uGUI 里每个 UI 元素都有 `RectTransform`。`anchorMin` / `anchorMax` 是**相对于父物体的归一化坐标**：

```
父物体（比如 Canvas）
┌─────────────────────────────┐  (1, 1)  ← anchorMax 的最大值
│                             │
│                             │
│                             │
│                             │
└─────────────────────────────┘
(0, 0)  ← anchorMin 的最小值
```

- `(0, 0)` = 父物体的**左下角**
- `(1, 1)` = 父物体的**右上角**
- `(0.5, 0.5)` = 正中心

配合这两行：

```csharp
rect.offsetMin = Vector2.zero;
rect.offsetMax = Vector2.zero;
```

**含义是"完全贴合锚点，没有额外像素偏移"**。这样元素的大小**100% 由百分比决定**，屏幕多大都自动适配。

**实例解读**（来自 `GyroscopeDebugSceneSetup`）：

```csharp
RectTransform panel = CreateImage("Panel", canvasObject.transform, PanelColor,
    new Vector2(0.02f, 0.04f),      // anchorMin
    new Vector2(0.45f, 0.96f));     // anchorMax
```

翻译成人话：

> 这个面板**从屏幕宽度的 2% 到 45%，从高度的 4% 到 96%**。
> 也就是：**贴在左边，占左边不到一半，上下几乎撑满**。

再看一个：

```csharp
TMP_Text statusText = CreateText("Status", panel, "GYRO OFFLINE", ...,
    new Vector2(0.04f, 0.9f),
    new Vector2(0.96f, 0.98f));
```

> 在**面板内部**，从宽度 4% 到 96%（左右各留 4% 边距），从高度 90% 到 98%（**顶部一条**）。

**父物体是谁，百分比就是相对谁的。** `statusText` 的父物体是 `panel`，所以 0.9 指的是"panel 高度的 90%"，不是屏幕高度的 90%。

### 7.3 `Canvas` 三件套

```csharp
GameObject canvasObject = new GameObject("CastDebugHUD",
    typeof(RectTransform),
    typeof(Canvas),           // ① 画布本体
    typeof(CanvasScaler),     // ② 分辨率适配
    typeof(GraphicRaycaster)); // ③ 点击检测

Canvas canvas = canvasObject.GetComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;   // 永远画在最上层
canvas.sortingOrder = 1000;                          // 层级很高，压住一切

CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1080f, 1920f);   // 设计分辨率
scaler.matchWidthOrHeight = 0.5f;
```

| 设置 | 值 | 含义 |
|---|---|---|
| `renderMode` | `ScreenSpaceOverlay` | UI 画在**所有 3D/2D 内容之上**，不受相机影响 |
| `sortingOrder` | `1000` | 排序层级，越大越靠前。1000 = "谁也别想盖住我" |
| `uiScaleMode` | `ScaleWithScreenSize` | 屏幕变大，UI 跟着**等比放大**（而不是保持像素大小） |
| `referenceResolution` | `(1080, 1920)` | **设计基准分辨率**。字号 34 是"在 1080×1920 下的 34" |
| `matchWidthOrHeight` | `0.5` | 0 = 只按宽度缩放，1 = 只按高度，**0.5 = 两者折中** |

> 📌 注意两个 HUD 的 `referenceResolution` **不一样**：
> - 姿态 HUD：`1920 × 1080`（**横屏**）
> - 甩杆 HUD：`1080 × 1920`（**竖屏**）
>
> 这反映了两个原型的预期握持方式不同。

### 7.4 四个"工厂函数"

两个 Editor 脚本里都有这四个私有静态方法。它们是**积木**，整个 HUD 就是拿它们搭的。

**① `CreateRect` —— 最基础的空容器**

```csharp
private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
{
    GameObject gameObject = new GameObject(name, typeof(RectTransform));
    RectTransform rect = gameObject.GetComponent<RectTransform>();
    rect.SetParent(parent, false);        // ← false 很重要！
    rect.anchorMin = anchorMin;
    rect.anchorMax = anchorMax;
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
    return rect;
}
```

> ⚠️ **`SetParent(parent, false)` 里的 `false` 是 `worldPositionStays`。**
> 传 `true`（默认）会让 Unity 尝试"保持世界坐标不变"，在 UI 里会把 scale 和 position 搞得一团糟。**UI 里永远传 `false`。**

**② `CreateImage` —— 一块纯色矩形**

```csharp
private static RectTransform CreateImage(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
{
    RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax);
    Image image = rect.gameObject.AddComponent<Image>();
    image.color = color;
    image.raycastTarget = false;      // ← 性能优化
    return rect;
}
```

`raycastTarget = false` 的意思是"**这块 UI 不接受点击**"。HUD 上的面板、条、文字都不需要点，关掉能省掉每帧的射线检测开销。只有按钮才设 `true`。

**③ `CreateText` —— TextMeshPro 文字**

```csharp
TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
text.text = value;
text.fontSize = fontSize;
text.fontStyle = fontStyle;              // Bold / Normal
text.alignment = alignment;              // Left / Right / Center
text.color = color;
text.raycastTarget = false;
text.textWrappingMode = TextWrappingModes.NoWrap;   // ← 不换行
```

`NoWrap` 很关键：数值文字（`RAW  -2.34 rad/s`）**绝对不能换行**，否则数字跳到第二行，布局就乱了。

**④ `CreateButton` —— 可点击按钮**（只在 Cast 场景里）

```csharp
RectTransform rect = CreateImage(name, parent, color, anchorMin, anchorMax);
Image image = rect.GetComponent<Image>();
image.raycastTarget = true;             // ← 按钮必须能被点到
Button button = rect.gameObject.AddComponent<Button>();
button.targetGraphic = image;           // 告诉 Button 哪块图形要做按下效果
CreateText("Label", rect, label, ...);  // 文字作为子物体
```

### 7.5 反射连线：`SerializedObject` + `FindProperty`

这是整个 Editor 脚本里最"魔法"的部分。

**问题**：HUD 脚本里的引用字段都是 `private`：

```csharp
public sealed class CastDebugHUD : MonoBehaviour
{
    [SerializeField] private GyroscopeReader reader;
    [SerializeField] private TMP_Text sensorText;
    // ... 全是 private
}
```

Editor 脚本**没法直接写** `hud.reader = reader;` —— 编译不过。

**解法**：

```csharp
private static void SetObjectReference(Object target, string propertyName, Object value)
{
    SerializedObject serializedObject = new SerializedObject(target);
    serializedObject.FindProperty(propertyName).objectReferenceValue = value;
    serializedObject.ApplyModifiedPropertiesWithoutUndo();
}
```

用法：

```csharp
SetObjectReference(hud, "reader", reader);
SetObjectReference(hud, "sensorText", sensorText);
```

**`SerializedObject` 是 Unity 序列化系统的"后门"**。Inspector 面板本身就是这么工作的 —— 你在 Inspector 里拖一个引用进去，底层就是这套 API。它**能无视 C# 的 private 访问控制**，因为它操作的是序列化数据而不是 C# 字段。

三个变体：

```csharp
SetObjectReference(hud, "reader", reader);       // 对象引用
SetFloat(hud, "barRange", 8f);                   // .floatValue
SetEnum(navigation, "targetScene", (int)E_SceneID.MainMenu);  // .enumValueIndex
```

> ⚠️ **代价**：`"reader"` 是**字符串**。改了字段名，编译器**不会报错**，但运行时 `FindProperty` 返回 `null` → `NullReferenceException`。这是用反射换来的便利要付的价。

**按钮的事件也是这么连的：**

```csharp
UnityEventTools.AddPersistentListener(restartButton.onClick, hud.RestartTest);
```

`AddPersistentListener` 加的是**持久化**监听器 —— 会存进场景文件里，就像你在 Inspector 的 `OnClick()` 列表里手动拖一样。（对比 `onClick.AddListener()` 是运行时临时的，不会被保存。）

### 7.6 居中条 `SetCenteredBar` 的数学

HUD 上那三条会从中间往两边长的彩色条，逻辑在**运行时 HUD 脚本**里（不是 Editor 脚本）：

```csharp
private void SetCenteredBar(RectTransform bar, float value)
{
    if (bar == null) { return; }

    float normalized = Mathf.Clamp(value / barRange, -1f, 1f);   // ① 归一化到 [-1, 1]
    float edge = 0.5f + normalized * 0.5f;                        // ② 映射到 [0, 1]

    bar.anchorMin = new Vector2(Mathf.Min(0.5f, edge), 0f);       // ③ 左边界
    bar.anchorMax = new Vector2(Mathf.Max(0.5f, edge), 1f);       // ④ 右边界
    bar.offsetMin = Vector2.zero;
    bar.offsetMax = Vector2.zero;
}
```

**逐步推演**（假设 `barRange = 8`）：

| `value` | ① normalized | ② edge | ③ anchorMin.x | ④ anchorMax.x | 视觉 |
|---|---|---|---|---|---|
| `0` | 0 | 0.5 | 0.5 | 0.5 | 中间一条线（宽度 0） |
| `+4` | +0.5 | 0.75 | 0.5 | 0.75 | **从中心向右**长到 3/4 处 |
| `+8` | +1.0 | 1.0 | 0.5 | 1.0 | 从中心一直到最右 |
| `-4` | −0.5 | 0.25 | 0.25 | 0.5 | **从中心向左**长到 1/4 处 |
| `-20` | −1.0（clamp） | 0.0 | 0.0 | 0.5 | 撞满左边（**已饱和**） |

**`Mathf.Min` / `Mathf.Max` 这一对是精髓**：不管 `edge` 在 0.5 左边还是右边，`anchorMin` 永远取小的、`anchorMax` 永远取大的 —— **矩形自动从中心朝正确方向生长**，不需要写 if-else。

条的父物体是 `XTrack`（那条暗色底槽），所以 0~1 是相对底槽的。底槽中间还有一根 `XZero` 白线做刻度：

```csharp
CreateImage(axis + "Zero", track, new Color(1f, 1f, 1f, 0.5f),
    new Vector2(0.498f, 0f), new Vector2(0.502f, 1f));   // 宽度只有 0.4%
```

### 7.7 曲线图 `GyroscopeHistoryGraphic` —— 自己画三角形

**文件**：`Assets/Scripts/UI/GyroscopeHistoryGraphic.cs`

HUD 底部那个滚动的三色曲线图。uGUI **没有内置的折线图组件**，所以这里是**手动生成网格**。

**① 继承 `MaskableGraphic`**

```csharp
public sealed class GyroscopeHistoryGraphic : MaskableGraphic
```

`MaskableGraphic` 是 uGUI 里所有可绘制元素的基类（`Image`、`Text` 都继承它）。继承它就能**自定义要画什么形状**。

**② 收集数据**

```csharp
private struct Sample { public float Time; public Vector3 Value; }
private readonly List<Sample> samples = new List<Sample>(256);

private void Update()
{
    float now = Time.unscaledTime;

    if (reader != null && reader.IsEnabled)
        samples.Add(new Sample { Time = now, Value = reader.AngularVelocity });

    float oldestAllowedTime = now - historyDuration;      // 3 秒前
    int removeCount = 0;
    while (removeCount < samples.Count && samples[removeCount].Time < oldestAllowedTime)
        removeCount++;
    if (removeCount > 0) samples.RemoveRange(0, removeCount);   // 丢掉过期的

    SetVerticesDirty();     // ← 告诉 uGUI：我变了，下一帧重画
}
```

这是一个**滑动窗口**：每帧加一个新样本，砍掉 3 秒前的旧样本。列表长度自然稳定在 ~180 个（60fps × 3s）。

`new List<Sample>(256)` 预分配 256 个容量，**避免运行时反复扩容产生 GC**。

**③ 画（`OnPopulateMesh`）**

```csharp
protected override void OnPopulateMesh(VertexHelper vertexHelper)
{
    vertexHelper.Clear();
    Rect rect = rectTransform.rect;

    // 中线（0 刻度）
    AddLine(vertexHelper, new Vector2(rect.xMin, rect.center.y),
                          new Vector2(rect.xMax, rect.center.y), gridColor, 1f);

    // 两条竖直网格线（把 3 秒分成 3 段）
    for (int i = 1; i < 3; i++) { ... }

    DrawTrace(vertexHelper, rect, 0, xColor);   // X 轴 红
    DrawTrace(vertexHelper, rect, 1, yColor);   // Y 轴 绿
    DrawTrace(vertexHelper, rect, 2, zColor);   // Z 轴 蓝
}
```

**④ 坐标换算（`GetPoint`）**

```csharp
private Vector2 GetPoint(Sample sample, Rect rect, float now, int axis)
{
    float normalizedTime = 1f - Mathf.Clamp01((now - sample.Time) / historyDuration);
    float value = axis switch { 0 => sample.Value.x, 1 => sample.Value.y, 2 => sample.Value.z, _ => 0f };
    float normalizedValue = Mathf.Clamp(value / displayRange, -1f, 1f);

    return new Vector2(
        Mathf.Lerp(rect.xMin, rect.xMax, normalizedTime),                    // 横 = 时间
        Mathf.Lerp(rect.yMin, rect.yMax, normalizedValue * 0.5f + 0.5f));    // 竖 = 数值
}
```

- **横轴 = 时间**。`normalizedTime`：**刚刚**的样本 → 1（最右），**3 秒前** → 0（最左）。所以图是**从右往左滚**。
- **竖轴 = 数值**。`normalizedValue * 0.5f + 0.5f` 把 `[-1, 1]` 映射到 `[0, 1]`，**0 落在正中间**。

**⑤ 用四边形画"线"（`AddLine`）**

GPU 只会画三角形，**画不了"线"**。所以每一段折线其实是一个**细长的矩形（= 2 个三角形）**：

```csharp
private static void AddLine(VertexHelper vh, Vector2 start, Vector2 end, Color color, float thickness)
{
    Vector2 direction = end - start;
    if (direction.sqrMagnitude <= Mathf.Epsilon) { return; }

    // 法线：把方向向量旋转 90°（(x,y) → (-y,x)），再缩放到半个线宽
    Vector2 normal = new Vector2(-direction.y, direction.x).normalized * thickness * 0.5f;

    int index = vh.currentVertCount;
    vh.AddVert(start - normal, color, Vector2.zero);   // 0 起点下侧
    vh.AddVert(start + normal, color, Vector2.zero);   // 1 起点上侧
    vh.AddVert(end   + normal, color, Vector2.zero);   // 2 终点上侧
    vh.AddVert(end   - normal, color, Vector2.zero);   // 3 终点下侧

    vh.AddTriangle(index, index + 1, index + 2);       // 三角形 A
    vh.AddTriangle(index, index + 2, index + 3);       // 三角形 B
}
```

```
        1 ●───────────────● 2
          │╲              │
          │  ╲     A      │       ← 两个三角形拼成一个矩形
          │    ╲          │
          │  B   ╲        │
        0 ●───────────────● 3
       start              end
```

**`(x, y) → (-y, x)` 是把向量逆时针转 90°**，得到垂直于线段的法线方向。把线段两端各向两侧推半个线宽，就得到矩形的 4 个角。

### 7.8 刷新节流

两个 HUD 都有这段：

```csharp
[SerializeField, Min(1f)] private float refreshRate = 30f;    // Cast HUD 是 30，Gyro HUD 是 20
private float nextRefreshTime;

private void Update()
{
    if (Time.unscaledTime < nextRefreshTime) { return; }
    nextRefreshTime = Time.unscaledTime + 1f / refreshRate;
    Refresh();
}
```

**为什么不每帧刷新？**

1. **性能**：`$"..."` 字符串插值每次都**分配新的 string**，60fps × 14 个文字 = 每秒 840 次 GC 分配。手机上会卡。
2. **可读性**：数字每秒变 60 次，**人眼根本看不清**。20~30 次已经足够流畅，还更容易读。

> 📌 注意：**图表不受这个节流影响**。`GyroscopeHistoryGraphic` 有自己的 `Update`，每帧采样。所以数字文字是 20Hz，曲线是满帧 —— 各取所需。

### 7.9 Editor 脚本的安全检查（`GyroscopeDebugSceneSetup`）

姿态 HUD 的生成器是"往已有场景里加东西"，比从零建场景**危险得多**。所以它开头有三道守卫：

```csharp
if (scene.name != "Scene1")                  { Debug.LogError(...); return; }  // ① 场景对不对
GameObject circle = GameObject.Find("Circle");
if (circle == null)                          { Debug.LogError(...); return; }  // ② 目标在不在
if (GameObject.Find("GyroscopeDebugHUD") != null) { Debug.LogError(...); return; }  // ③ 是不是跑过了
```

第 ③ 条是**幂等性保护**：防止跑两次生成两套 HUD 叠在一起。

还有 `Undo` 支持：

```csharp
Undo.RegisterCreatedObjectUndo(debugRoot, "Create Gyroscope Debug Root");
Undo.RecordObject(legacyTest, "Disable Legacy Motion Sensor Test");
Undo.SetTransformParent(circle.transform, null, "Detach Circle From Camera");
```

这样跑完之后**按 Ctrl+Z 能整个撤销**。写 Editor 工具时这是基本礼貌。

> ⚠️ **已知问题**：场景已经从 `Scene1` 改名成 `AttitudeControlTest`，但第 ① 条守卫里的字符串**还是 `"Scene1"`**（`GyroscopeDebugSceneSetup.cs:22`）。现在再点这个菜单项会直接报错退出。HUD 已经生成好并存进场景了，所以不影响使用；但如果哪天要重建，得先改这个字符串。

### 7.10 Cast 场景生成器还多做的事

`GyroscopeCastTestSceneBuilder` 是从零建整个场景，所以除了 HUD 还建了：

| 方法 | 建什么 | 要点 |
|---|---|---|
| `CreateCamera()` | 正交相机 | `orthographicSize = 7`，背景深色 |
| `CreateBall()` | 小球 | `Rigidbody2D` + `CircleCollider2D`，`FreezeRotation`，用内置 `Knob.psd` 当图 |
| `CreateGround()` | 地面 + 距离刻度 | 每 5 米一根竖线，共 11 根 |
| `CreateEventSystem()` | 输入事件系统 | `InputSystemUIInputModule` + `AssignDefaultActions()`，**没有它按钮点不动** |
| `GetOrCreateProfile()` | 参数资产 | 已存在就复用，不存在才新建（**不会覆盖你调好的参数**） |
| `AddSceneToBuildSettings()` | 注册到 Build | 先查重再加 |
| `AddSceneToCatalog()` | 注册到 `SceneCatalog` | 让 `SceneLoader` 能按 `E_SceneID.GyroscopeTest` 找到它 |

---

## 第 8 章：HUD 上每个数字的含义

现在把 HUD 的每一行拆开。**记住第 0 章说的：HUD 不计算任何东西**，每一行都能追溯到某个属性。

### 8.1 姿态关卡 HUD（`GyroscopeDebugHUD`）

场景：`AttitudeControlTest`。位置：屏幕左侧的深色面板。

```
┌────────────────────────────────────┐
│ GYRO ONLINE | 60 Hz | SCALE +/-6 rad/s │  ← Status
│                                    │
│ X  +1.23  ▓▓▓▓▓█░░░░░░░░░░░        │  ← XRow (XValue + XTrack/XFill)
│ Y  -0.45  ░░░░░█▓▓░░░░░░░░░        │  ← YRow
│ Z   0.00  ░░░░░█░░░░░░░░░░░        │  ← ZRow
│                                    │
│ MAG  1.31              PEAK  4.82  │  ← Magnitude / Peak
│                                    │
│ ┌────────────────────────────────┐ │
│ │  ╱╲    ╱╲                      │ │  ← Graph / Lines
│ │ ╱  ╲__╱  ╲___________          │ │    (GyroscopeHistoryGraphic)
│ └────────────────────────────────┘ │
│ RAW ANGULAR VELOCITY | LAST 3 SEC  │  ← GraphLabel
└────────────────────────────────────┘
```

| HUD 显示 | 代码来源 | 含义 | 正常范围 |
|---|---|---|---|
| `GYRO ONLINE` / `GYRO OFFLINE` | `reader.IsAvailable` | 陀螺仪找到没有 | 手机上应为 ONLINE |
| `60 Hz` | `reader.SamplingFrequency` | 传感器**实际**采样率 | 一般 50~100 |
| `SCALE +/-6 rad/s` | `displayRange` 字段 | **条和图的量程**，不是数据 | 固定 6 |
| `X +1.23` | `reader.AngularVelocity.x` | 绕 X 轴角速度（点头方向） | 静止 ≈ 0 |
| `Y -0.45` | `.y` | 绕 Y 轴（摇头方向） | 静止 ≈ 0 |
| `Z 0.00` | `.z` | 绕 Z 轴（歪头方向） | 静止 ≈ 0 |
| `MAG 1.31` | `reader.AngularSpeed` | 三轴合成总转速（**永远 ≥ 0**） | 静止 < 0.05 |
| `PEAK 4.82` | 本地字段 `peak` | 开场至今 MAG 的**历史最大值**，**只增不减** | — |
| 三条彩色横条 | `SetCenteredBar` | X/Y/Z 的可视化，中心 = 0 | 撞满边 = 超过 ±6 |
| 底部曲线图 | `GyroscopeHistoryGraphic` | 最近 **3 秒**的 X(红)/Y(绿)/Z(蓝) 波形 | — |

> 💡 **一个容易困惑的点**：这个 HUD 显示的是**陀螺仪（角速度）**，但控制圆点移动的是**姿态传感器**。
> 也就是说，**HUD 上的数字和圆点的位置不是直接对应的**。
> 这是故意的：曲线图能帮你直观看到"手机在动"，而姿态是个四元数，没法用三条横条表示清楚。
> 场景里 `GyroscopeDebug` 这个 GameObject 上**同时挂了两个 Reader**（`GyroscopeReader` 给 HUD，`AttitudeReader` 给圆点），见 `GyroscopeDebugSceneSetup.cs:58-59`。

`OFFLINE` 时的行为：所有数字归零，条回到中心（`SetOfflineState()`）。

### 8.2 甩杆关卡 HUD（`CastDebugHUD`）

场景：`GyroscopeCastTest`。竖屏，分五块。

```
┌─────────────────────────────────────┐
│ CAST MOTION TEST      GYRO ONLINE|60Hz │  Header
│ STATE  Ready            CAST AXIS  X   │
├─────────────────────────────────────┤
│ RAW  +0.12 rad/s   FILTERED  +0.09 rad/s │  Values
│ RAW PEAK  0.00     FILTERED PEAK  0.00 │
│ CAST POWER  0%     BALL VELOCITY  0.00, 0.00 │
├─────────────────────────────────────┤
│ X ▓▓▓░░░░░░█░░░░░░░░░░               │  AxisPanel
│ Y ░░░░░░░░░█▓░░░░░░░░░               │
│ Z ░░░░░░░░░█░░░░░░░░░░               │
├─────────────────────────────────────┤
│  ╱╲     ╱╲                          │  GraphPanel
│ ╱  ╲___╱  ╲_______                  │
│ ANGULAR VELOCITY | X/Y/Z | 3 s      │
├─────────────────────────────────────┤
│         DISTANCE  0.00 m            │  Distance
│  [ MAIN MENU ]      [ RESTART ]     │  按钮
└─────────────────────────────────────┘
```

| HUD 显示 | 代码来源 | 含义 | 怎么用它调试 |
|---|---|---|---|
| `GYRO ONLINE \| 60 Hz` | `reader.IsEnabled` + `SamplingFrequency` | 传感器状态 | OFFLINE → 检查 Unity Remote / 权限 |
| `STATE Ready` | `detector.State` | **状态机当前状态** | 看它有没有按预期流转 |
| `CAST AXIS X` | `detector.SelectedAxis` | 当前用哪个轴（来自 profile） | 甩不动就试试换轴 |
| `RAW +2.34 rad/s` | `detector.DirectedVelocity` | **选中轴上**的原始角速度（有正负） | 甩的时候它应该冲到 2~7 |
| `FILTERED +2.10 rad/s` | `detector.FilteredVelocity` | 滤波后的同一个值 | 和 RAW 差太多 → 滤波太狠 |
| `RAW PEAK 5.12` | `detector.RawPeak` | 本次采样窗口内 RAW 的最大值 | — |
| `FILTERED PEAK 4.60` | `detector.FilteredPeak` | 本次窗口内 FILTERED 的最大值 | **这个值决定力度** |
| `CAST POWER 68%` | `detector.CastPower` | 最终力度，`EvaluatePower(FilteredPeak)` | 总是 0% 或 100% → 调 min/maxPeak |
| `BALL VELOCITY 7.20, 9.85` | `ball.Velocity` | 球当前的 2D 速度（x, y） | 起飞瞬间应等于 `EvaluateLaunchVelocity` |
| `DISTANCE 12.40 m` | `ball.CurrentDistance` | 球**当前**水平位移 | 飞行中实时变化 |
| `FINAL DISTANCE 18.75 m` | `testController.FinalDistance` | 球**落地后**的最终成绩 | 落地后标题会从 DISTANCE 变成 FINAL DISTANCE |
| 三条横条 | `SetCenteredBar` | 陀螺仪三轴原始值，量程 ±8 | 看哪个轴动得最大 → 决定 `castAxis` |
| 曲线图 | `GyroscopeHistoryGraphic` | 3 秒波形，量程 ±8 | 看波形能不能过阈值 |

**这些数字的调试用法（重点）：**

**关键关系链**：`RAW` → `FILTERED` → `FILTERED PEAK` → `CAST POWER` → `BALL VELOCITY` → `DISTANCE`

甩一下，从左往右看这条链，**哪一环断了就调哪一环的参数**：

| 现象 | 断在哪 | 改什么 |
|---|---|---|
| `RAW` 一直接近 0 | 轴选错了 | 看三条横条哪条动最大 → 改 `castAxis` |
| `RAW` 有大负值但没触发 | 方向反了 | 打开 `invertAxis` |
| `RAW` 冲到 3.0 但 `STATE` 一直 `Ready` | 触发阈值太高 | `triggerThreshold` ↓ |
| `STATE` 卡在 `Cooldown` | 冷却时间长 | `cooldownDuration` ↓ |
| `FILTERED PEAK` 远小于 `RAW PEAK` | 滤波削峰 | `filterSharpness` ↑ |
| `FILTERED PEAK` = 6.5 但 `POWER` = 100% | 满量程低 | `maximumPeak` ↑ |
| `POWER` 正常但球飞得近 | 发射速度小 | `maximumLaunchVelocity` ↑ |

### 8.3 状态文字 `STATE` 完整对照

| 显示 | 意味着 | 你该看到它多久 |
|---|---|---|
| `Unavailable` | 没传感器 / `reader` 或 `tuningProfile` 没连上 | 手机上不该出现 |
| `Ready` | 待机，等你甩 | 大部分时间 |
| `Sampling` | **正在测你甩多快** | 0.35 秒（一闪） |
| `CastDetected` | 已判定，力度算出来了 | 0.2 秒（一闪） |
| `Cooldown` | 冷却中，甩了也没用 | 0.75 秒 |

> 💡 `Sampling` 和 `CastDetected` 都是"一闪而过"。如果你想看清楚，可以临时把 `sampleWindow` 和 `castDetectedDisplayDuration` 调大到 1~2 秒。

---

## 第 9 章：两条完整数据流

### 9.1 姿态关卡：手机倾斜 → 圆点移动

```
手机硬件（姿态传感器）
   │  Quaternion
   ▼
AttitudeSensor.attitude.ReadValue()
   │  防线1: wasUpdatedThisFrame
   │  防线2: Dot > 0.0001
   │  防线3: Normalize
   ▼
AttitudeReader.Attitude ─────────────────────┐
   │                                         │
   ▼                                         │  （HUD 不读这条）
AttitudeCircleController.Update()            │
   │ ① Inverse(neutral) * current            │
   │ ② * Vector3.forward  → relativeForward  │
   │ ③ 超 75° ? 归零                          │
   │ ④ Atan2 → tiltAngles (度)                │
   │ ⑤ 死区1.5° / 满量程25° → [-1,1]          │
   │ ⑥ 主轴锁定（迟滞 2°）                     │
   │ ⑦ × 相机可视范围 × 0.85                   │
   │ ⑧ Lerp 平滑 (k=12)                       │
   ▼                                         │
Circle.transform.position                    │
                                             │
手机硬件（陀螺仪，独立的一路）                   │
   │  Vector3 rad/s                          │
   ▼                                         ▼
GyroscopeReader.AngularVelocity ───► GyroscopeDebugHUD（20Hz 刷新）
   │                                    ├─ 三个数字 X/Y/Z
   │                                    ├─ MAG / PEAK
   └────────────────────────────────►  └─ GyroscopeHistoryGraphic（每帧）
```

### 9.2 甩杆关卡：甩手机 → 球落地

```
手机硬件（陀螺仪）
   │  Vector3 rad/s
   ▼
GyroscopeReader.AngularVelocity
   │
   ├──────────────────────────────────► CastDebugHUD 三条横条 + 曲线图
   │
   ▼
CastGestureDetector.Update()
   │ ① ReadDirectedVelocity → 取 X 轴 → DirectedVelocity
   │ ② 低通滤波 (k=20) → FilteredVelocity
   │ ③ 状态机
   │      Ready: isArmed && Directed ≥ 1.25 → Sampling
   │      Sampling: 0.35s 内取 Max → FilteredPeak
   │      → EvaluatePower(FilteredPeak)
   ▼
CastTuningProfile.EvaluatePower()
   │  InverseLerp(1.5, 7, peak) → powerCurve.Evaluate()
   ▼
CastPower  (0.0 ~ 1.0)
   │
   │  event CastDetected?.Invoke(CastPower)
   ▼
CastTestController.HandleCastDetected(power)
   │  if (TrialStarted) return;  ← 一局只抛一次
   ▼
CastBallController.Launch(power)
   │  body.bodyType = Dynamic
   │  body.linearVelocity = EvaluateLaunchVelocity(power)
   │        = Lerp((3.5,6.5), (9.5,12.5), power)
   ▼
Unity 2D 物理引擎（重力 gravityScale = 1）
   │
   ├──► CastCameraFollow.LateUpdate()  相机 SmoothDamp 跟随
   │
   ▼
OnCollisionEnter2D / OnCollisionStay2D
   │  条件1: IsFlying
   │  条件2: 飞行 ≥ 0.2 秒（防止发射瞬间误判）
   │  条件3: 有接触法线 normal.y > 0.5（确实是踩到地面，不是撞侧面）
   │  条件4: |velocity.y| ≤ 0.5（真的停下来了，不是弹起）
   ▼
Landed?.Invoke(CurrentDistance)
   ▼
CastTestController.HasResult = true; FinalDistance = distance
   ▼
CastDebugHUD 显示 "FINAL DISTANCE  18.75 m"
```

**注意落地判定的 4 个条件**都是在防误判：

- 条件 2 防"球刚生成就贴着地面被判落地"
- 条件 3 防"撞到墙侧面被当成落地"
- 条件 4 防"第一次弹跳的最高点被当成落地"

---

## 第 10 章：排查表与已知坑

### 10.1 常见问题

| 症状 | 可能原因 | 怎么查 |
|---|---|---|
| HUD 显示 `GYRO OFFLINE` | 编辑器里没有真传感器 | 用 **Unity Remote** 连手机，或直接打包到手机 |
| | 忘了 `EnableDevice` | 检查 `TryConnect()` 有没有被调到 |
| | `OnEnable` 拼成了 `OnEnabled` | **拼写！** Unity 不会报错 |
| 圆点完全不动 | `AttitudeReader.HasSample` 一直 false | 检查设备是否有姿态传感器（部分低端机没有） |
| | 还在 `autoCalibrationDelay` 内 | 等 0.5 秒 |
| | 手机倾斜超过 75° | 回到接近校准姿势 |
| 圆点抖个不停 | 死区太小 | `deadZoneDegrees` ↑ |
| 圆点只能横着或竖着走 | **这是设计** | 见 3.6 节主轴锁定（D-pad 规则要求） |
| 圆点方向反了 | 轴向 / 反转配置 | 试 `invertVertical` / `invertHorizontal` |
| 甩了没反应 | 见 8.2 节的诊断表 | 从 `RAW` 开始一环环看 |
| 一甩抛两次 | `rearmThreshold` 太高 | 调到 0.25 |
| 力度总是 100% | `maximumPeak` 太低 | 看 `FILTERED PEAK` 实际能到多少，据此设 |
| 按钮点不动 | 场景缺 `EventSystem` | 见 `CreateEventSystem()` |
| | `Image.raycastTarget = false` | 按钮的 Image 必须是 `true` |

### 10.2 代码里的已知问题（记录，不修）

这些不影响当前使用，但值得知道：

**① `GyroscopeDebugSceneSetup` 的场景名过期**
`GyroscopeDebugSceneSetup.cs:22` 检查 `scene.name != "Scene1"`，但场景已改名为 `AttitudeControlTest`。重新运行该菜单项会直接报错退出。要重建得先改这个字符串。

**② 两个 HUD 的空引用防护不对称**
`CastDebugHUD.Refresh()` 对 `reader` 做了 null 检查，但 `detector` / `ball` / `testController` / 所有 `TMP_Text` 都是直接解引用（`CastDebugHUD.cs:51-74`）。少连一根线就会每帧抛 `NullReferenceException`。
`GyroscopeDebugHUD` 同理（`GyroscopeDebugHUD.cs:58-68`）。
目前靠 Editor 生成器保证所有引用都连好了，所以没暴露。

**③ `MotionSensorHUD.cs` 里的类名不匹配**
文件名是 `MotionSensorHUD.cs`，但里面的类叫 `NewMonoBehaviour`，而且是**完全空的**（`Start` / `Update` 都是空方法）。这是个残留的模板文件，**不是真正的 HUD 脚本**。真正的 HUD 在 `Assets/Scripts/UI/` 下。
（C# 不要求类名与文件名一致，所以能编译，但 Unity 会因为文件名与 MonoBehaviour 类名不符而无法把它挂到 GameObject 上。）

**④ `MotionSensorTest.cs` 是被弃用的早期版本**
它每帧 `Debug.Log` 传感器数据，是最原始的验证脚本。已被 `GyroscopeDebugSceneSetup` 自动禁用（`legacyTest.enabled = false`），保留在场景里作为历史记录。

**⑤ 场景里缺两个字段**
见 3.8 节的注意框：`maxSupportedTiltDegrees` 和 `axisSwitchHysteresisDegrees` 未写入场景文件，用脚本默认值。

---

## 第 11 章：术语表

| 术语 | 英文 | 一句话解释 |
|---|---|---|
| 姿态 | attitude / orientation | 物体在空间中的**朝向**，用四元数表示 |
| 四元数 | quaternion | 4 个数表示 3D 旋转，没有万向锁问题 |
| 角速度 | angular velocity | 旋转的**快慢**，单位 rad/s |
| 弧度 | radian | 角度单位，1 rad ≈ 57.3°，一整圈 = 2π ≈ 6.28 rad |
| 死区 | dead zone | 输入太小时**一律当作 0**，用来抵消手抖 |
| 迟滞 | hysteresis | 进入和退出用**不同阈值**，防止在边界疯狂抖动 |
| 施密特触发器 | Schmitt trigger | 迟滞的经典实现：一个高阈值触发、一个低阈值复位 |
| 低通滤波 | low-pass filter | 保留缓慢变化、滤掉快速抖动 |
| 指数平滑 | exponential smoothing | `1 - e^(-k·dt)`，帧率无关的平滑公式 |
| 时间常数 | time constant (τ) | 平滑速度的度量，τ = 1/k，约 τ 秒走完 63% |
| 归一化 | normalize | 把任意范围映射到标准范围（通常 0~1 或 -1~1） |
| 幂等 | idempotent | 执行一次和执行多次结果相同 |
| 锚点 | anchor | uGUI 里元素相对父物体的**百分比位置** |
| 正交相机 | orthographic camera | 无透视的相机，2D 游戏标配 |
| ScriptableObject | — | 存在硬盘上的数据资产，不属于任何场景 |
| 序列化 | serialization | 把内存里的对象存成文件（Unity 场景/资产就是这么存的） |

---

## 附录：文件速查

| 文件 | 行数 | 一句话 |
|---|---|---|
| `Scripts/Help/AttitudeReader.cs` | 93 | 姿态传感器包装，产出 `Quaternion Attitude` |
| `Scripts/Help/GyroscopeReader.cs` | 71 | 陀螺仪包装，产出 `Vector3 AngularVelocity` |
| `Scripts/Controller/AttitudeCircleController.cs` | 225 | 姿态 → 归一化倾斜 → 屏幕位置 |
| `Scripts/Controller/CastGestureDetector.cs` | 168 | 角速度 → 状态机 → `CastPower` + 事件 |
| `Scripts/Controller/CastTuningProfile.cs` | 58 | 所有甩杆参数的 ScriptableObject |
| `Scripts/Controller/CastBallController.cs` | 102 | 球的发射 / 落地判定 / 距离 |
| `Scripts/Controller/CastTestController.cs` | 70 | 把检测器、球、相机串起来的流程控制 |
| `Scripts/Controller/CastCameraFollow.cs` | 40 | `SmoothDamp` 水平跟随，只前进不后退 |
| `Scripts/UI/GyroscopeDebugHUD.cs` | 99 | 姿态关卡 HUD，20Hz 刷新 |
| `Scripts/UI/CastDebugHUD.cs` | 91 | 甩杆关卡 HUD，30Hz 刷新 |
| `Scripts/UI/GyroscopeHistoryGraphic.cs` | 164 | 自绘 3 秒滚动曲线图 |
| `Editor/GyroscopeDebugSceneSetup.cs` | 289 | 一键在场景里加姿态 HUD |
| `Editor/GyroscopeCastTestSceneBuilder.cs` | 551 | 一键从零建整个甩杆测试场景 |
| `Settings/CastTuningProfile.asset` | — | 参数实际存放处 |
| `Scripts/Controller/MotionSensorTest.cs` | 49 | ⚠️ 已弃用的早期验证脚本 |
| `Scripts/Controller/MotionSensorHUD.cs` | 21 | ⚠️ 空模板残留，类名是 `NewMonoBehaviour` |
