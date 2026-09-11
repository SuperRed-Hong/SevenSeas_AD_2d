# 触屏兜底按钮不显示 —— 诊断与修复方案

> 针对构建 19189822（itch 上的 v0.5 新版）。
> 这是一个**与 iOS 无关**的 bug，我在桌面 Chromium 上完整复现了。

---

## 1. 复现记录（证据）

测试环境：Claude 内置 Chromium 152，模拟 375x812 + Android UA，
所以 `SevenSeasIsMobileBrowser()` 返回 1，走移动端分支。
这个浏览器恰好**实现了 `DeviceMotionEvent.requestPermission` 并返回 `denied`**
（内嵌浏览器没有传感器），所以它忠实地模拟了「体感被拒 → 应当落到触屏兜底」的路径。

分别在两处跑：itch 页面内嵌 iframe、以及 `html-classic.itch.zone` 顶层直链。
两处表现完全一致。

浏览器 Console 实际输出（Unity 的 `Debug.Log`）：

```
Fishing input selected: MobileFishingInputSource
Web motion permission: motion=Denied, orientation=Denied
Web motion input is unavailable. Touch controls are now active.
Entered fishing state: ReadyToCast
```

**所以：**

| 环节 | 状态 |
| --- | --- |
| 移动端识别 | ✅ 正确，选中 `MobileFishingInputSource` |
| 权限请求与结果解析 | ✅ 正确，两个通道分别记录 |
| `WebMotionPermission.TouchFallbackActive` | ✅ 已置为 true |
| 进入游戏、状态机 | ✅ 进到 `ReadyToCast` |
| **左 / 右 / ACTION 按钮** | ❌ **完全没有渲染** |

结果：计时器在走（87 → 33），玩家什么都做不了。这就是「备用方案没生效」。

**新版本的权限逻辑是对的，坏的是兜底 UI。**

---

## 2. 已经排除的原因

我把场景 YAML 和相关脚本都核过了，以下都**不是**原因：

- `Assets/Scenes/FishingLoopTest.unity` 确实是发布用的游戏场景（在 Build Settings 里，enabled）。
- `ReelingButtonHUD` 的三个序列化引用全部已连接：
  `reelingController: 1322915249`、`accelerateButton: 1582759100`、`fishingInputSource: 1975332611`。
- `FishingInputRouter.mobileSource` 和 `FishingLoopController.inputSource`
  指向的是同一条链（router `503695418` → mobileSource `1975332611`），
  和 HUD 引用的是同一个 `MobileFishingInputSource`。
- `MobileInputSource` 的整条父链全部 active：
  `MobileInputSource → InputRouter → Input → FishingLoopRoot → GameplayRoot(根)`。
- `EnterReadyToCast()` 会调用 `SetMoveEnabled(true)` 和 `SetCastEnabled(true)`，
  所以 `IsMoveAvailable` / `IsActionAvailable` 应该都是 true。
- 构建里的 `.jslib` 和磁盘上的完全一致（我把 `WebGL.framework.js.br` 拉下来比对过），
  不是上传了旧包。
- Console 里**没有任何 C# 异常**，所以按钮创建代码没有抛错。

---

## 3. 首要嫌疑：`Awake` 时序竞争 + 静默失败

`Assets/Scripts/UI/ReelingButtonHUD.cs`：

```csharp
private void Awake()
{
    CreateTouchFallbackButtons();
}

private void CreateTouchFallbackButtons()
{
    Canvas canvas = GetComponentInParent<Canvas>();
    if (canvas == null || fishingInputSource == null)
    {
        return;          // ← 静默返回，leftButton/rightButton/actionButton 全部保持 null
    }
    ...
}

private static void SetActive(GameObject target, bool value)
{
    if (target != null && target.activeSelf != value)   // ← null 时静默什么都不做
    {
        target.SetActive(value);
    }
}
```

而 `Assets/Scripts/UI/FishingSceneUIController.cs`：

```csharp
private void Awake() => Initialize();

public void Initialize()
{
    if (initialized) return;
    initialized = true;
    SetVisible(gameplayCanvas, false);   // ← gameplayCanvas 就是 GamePlayCanvas
    ...
}
```

场景里 `gameplayCanvas` = GameObject `2109608892` = **`GamePlayCanvas`**，
而 `ReelingButtonHUD` 正是 `GamePlayCanvas` 的直接子物体。

也就是说：**一个脚本在自己的 `Awake` 里把另一个脚本的父 Canvas 关掉了，
而 Unity 不保证两者的 `Awake` 顺序。**

`GetComponentInParent<Canvas>()` 的无参重载**不搜索 inactive 的对象**，
一旦在错误的时序下取到 null，三个按钮就永远不会被创建，
而且 `CreateTouchFallbackButtons` 和 `SetActive` **两处都是静默失败，一条日志都没有**。
这正好解释了「功能没生效但 Console 干干净净」。

同一类时序问题还有第二个受害面：`CreateTouchFallbackButtons()` 只在 `Awake` 跑一次，
如果那一次失败，后面再也没有第二次机会。

> 注意：我无法在不跑 Editor 的情况下 100% 确证是这一条。
> 但它是唯一能同时解释「所有引线都对、没有异常、按钮完全不出现」的机制。
> 下面第 4 节给的修复方案不依赖于先确诊——它把整类问题都消掉。

---

## 4. 修复（按这个顺序做）

### 4.1 先在 Editor 里确诊（5 分钟，别跳过）

在 `WebMotionPermission` 里临时加一个强制开关，或者直接在
`ActivateTouchFallback()` 上方加一行 `TouchFallbackActive = true;`，
然后在 Editor 里进 `FishingLoopTest` 场景播放。

在 `ReelingButtonHUD.CreateTouchFallbackButtons()` 的早退分支加日志：

```csharp
if (canvas == null || fishingInputSource == null)
{
    Debug.LogError(
        $"Touch fallback buttons not created: canvas={canvas}, " +
        $"inputSource={fishingInputSource}", this);
    return;
}
```

Console 会立刻告诉你是不是 `canvas == null`。同时在 Hierarchy 里看
`GamePlayCanvas` 下有没有 `TouchLeftButton` / `TouchRightButton` / `TouchActionButton`
三个对象——**有没有创建出来**是分叉点：

- **没创建** → 就是第 3 节的时序问题，按 4.2 修。
- **创建了但不可见** → 是布局问题，按 4.3 修。

### 4.2 修法：改成惰性创建 + 不依赖 Awake 时序

把 `CreateTouchFallbackButtons()` 从 `Awake()` 里拿掉，改成在需要时才建、
建不出来就下一帧重试：

```csharp
private bool buttonsCreated;

private void Awake()
{
    // 不要在这里建按钮：父 Canvas 可能已被 FishingSceneUIController.Awake 关掉。
}

private void RefreshTouchFallbackVisibility()
{
    bool fallbackActive = fishingInputSource != null &&
                          fishingInputSource.isActiveAndEnabled &&
                          fishingInputSource.UsesTouchFallback;

    if (fallbackActive && !buttonsCreated)
    {
        CreateTouchFallbackButtons();   // 成功时把 buttonsCreated 置 true
    }

    SetActive(leftButton, fallbackActive && fishingInputSource.IsMoveAvailable);
    ...
}
```

`CreateTouchFallbackButtons()` 里：

1. 用 `GetComponentInParent<Canvas>(true)`（**带 `includeInactive` 参数的重载**），
   这样即使父链暂时 inactive 也能找到 Canvas。
2. 成功创建后设 `buttonsCreated = true`；失败**不要**设，让下一帧重试。
3. 失败时 `Debug.LogWarning` 一次（用一个 bool 防止刷屏），不要静默。

这样无论 Awake 顺序如何、Canvas 何时被开关，按钮都会在真正需要显示的那一帧被建出来。

### 4.3 如果按钮建出来了却看不见

Canvas 是 `Screen Space - Overlay`，`CanvasScaler` 为
`Scale With Screen Size`，参考分辨率 **1440 x 2304**，match 0.5。

代码里的硬编码位置是：

```csharp
leftButton   anchor(0,0)  anchoredPosition(130, 170)   size 180x180
rightButton  anchor(0,0)  anchoredPosition(340, 170)   size 180x180
actionButton anchor(1,0)  anchoredPosition(-150, 170)  size 180x180
```

对照场景里既有的 accelerate 按钮：`anchor(1,0)`，`anchoredPosition(-156, 712)`，
`size 160x160`。也就是**既有按钮在 y=712，而新按钮在 y=170**，低了很多。
在 1440x2304 的参考系里 180x180 也偏小（既有的是 160x160，量级相近，但 y 差了 4 倍）。

如果确认是这一类问题：把三个按钮的尺寸和 y 坐标向既有 accelerate 按钮对齐
（比如 size 200x200、y 在 260~360 之间），并且**在真机竖屏下逐一核对**，
别只在 Editor 的 Game 视图里看。

---

## 5. 顺带修：`AttitudeReader` 的源切换抖动

同一次测试里，Console 每秒刷这样一组日志，**永不停止**：

```
Attitude source: Attitude
Attitude source: Gravity
Attitude source: Accelerometer
Attitude source: Attitude
Attitude source: Gravity
...
```

原因在 `Assets/Scripts/Help/AttitudeReader.cs` 的 `Update()`：

```csharp
if (orientationBlocked || !IsEnabled || Time.unscaledTime - lastSampleTime >= 1f)
{
    HasSample = false;
    if (orientationBlocked || Time.unscaledTime >= nextConnectionAttemptTime)
    {
        TryConnect(IsEnabled ? activeSensor : null);   // 把当前源当作 silentSensor 排除掉
    }
}
```

当所有传感器都拿不到样本时，每秒换一个源、无限循环，并且每次都打一条 `Debug.Log`。

要改两点：

1. **加退避**：连续 N 次（比如 3 次）轮换都拿不到样本后，把重试间隔从 1 秒
   拉长到 5 秒甚至停止，别无限刷。
2. **日志降噪**：`Debug.Log($"Attitude source: {ActiveSource}")` 只在
   **真正发生变化且之前成功出过样本**时打，或者干脆降级成只在首次确定源时打一次。
   现在这个量级的日志在 WebGL 上是实打实的性能开销。

这个抖动在我的测试环境里不是致命问题（因为那里本来就没传感器），
但在真机上如果第一帧样本来得慢，它可能会把已经选对的源换掉。

---

## 6. 重要：iOS 体感的结论现在还不成立

**在按钮兜底修好之前，不要再用 iPhone 去判断体感有没有修好。**

因为一旦体感不可用就会落到触屏兜底，而触屏兜底本身是坏的，
两条路都走不通，从表现上根本分不清是「体感没修好」还是「兜底坏了」。

修好 4.2 之后再按 `docs/ios-motion-implementation-plan.md` 第 9.3 节
在 iPhone 上重跑验证，那时候的结论才有意义。

另外那份 `docs/motion-diagnostics/index.html` 诊断页到现在还没跑过。
它是独立于游戏的，**现在就可以跑**，能直接回答「iPhone 上 devicemotion
到底能不能用」这个问题，不受这个 UI bug 影响。建议先跑它。

---

## 7. 给实施者的检查清单

- [ ] 在 `CreateTouchFallbackButtons()` 早退分支加 `Debug.LogError`
- [ ] Editor 里强制 `TouchFallbackActive = true`，确认按钮到底有没有被创建
- [ ] 按 4.2 改成惰性创建 + `GetComponentInParent<Canvas>(true)` + 失败重试
- [ ] 如属 4.3，对齐按钮尺寸与 y 坐标
- [ ] 按 5 给 `AttitudeReader` 加退避和日志降噪
- [ ] 重新构建，在桌面浏览器用手机模拟视口验证按钮出现且可点
- [ ] 再上传 itch，用 iPhone 验证体感
- [ ] 本仓库提交信息不带任何 AI 署名
