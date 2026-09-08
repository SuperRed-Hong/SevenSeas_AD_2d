# Fishing Loop Findings

## 2026-09-08 — StrikeWindowProfile 游戏内调参

- 8 项为 windowDuration、attemptCooldown、shakeAmplitude、shakeDuration、targetBandWidth、minBandCenterRadius、maxBandCenterRadius、hitForgivenessRadius。新增运行时应用接口复用合法值约束，拒绝 NaN/Infinity；调整宽度后自动校正随机中心上下限。
- StrikeController 懒创建运行时副本，BeginCheck 使用独立快照；时间、宽度、容错、冷却及震屏均从同一轮快照读取。Defaults 恢复原资产值，场景退出销毁副本，无磁盘保存。此前震屏 Profile 字段未消费，现通过拒绝事件协调层传给相机，保留原震动方向。
- StrikeTuningPanel 在独立根节点建立运行时 uGUI，不依赖菜单/游戏 Canvas 的显隐。调参时保存并暂停 12 个玩法/输入组件和 Time.timeScale，关闭恢复，不暂停 EventSystem。开局转场中不允许打开，避免与延迟启用玩法竞争。滑条值 Apply 后回读校正结果，当前正在进行的判定不改变。

## 2026-09-08 — 场景表现先运行，玩法控制延后

- 用户不希望角色/鱼等场景内容等转场结束才出现。新增 FishingLoopController.PrepareForEntry，在根激活前停用 Loop、ShoreLaneController、Cast/Strike 检测并关闭语义输入。Loop 被停用时 Start 尚未执行，计时器不会开始。
- FishingSceneEntryGuard 随后立即激活 GameplayRoot，允许 FishSpawner.Start 和鱼身 VFX 运行；FishBiteRace / Strike / Reeling 原本均需显式 Begin 才推进，因此无需冻结整个场景或 Time.timeScale。转场结束仅重新启用 Loop，沿用原有 Start 初始化与输入门控。
- 输入组件初始语义门控默认关闭；即使 Router.Awake 晚于 PrepareForEntry、其 activeSource 尚未建立，岸边移动和手势仍停用且底层语义输入默认关闭。独立 EventSystem 继续接收菜单点击。

## 2026-09-08 — 菜单渐隐表现

- MenuCanvasFader 独立控制 CanvasGroup，使用 unscaledDeltaTime 与 SmoothStep 淡出全部菜单子 UI，不修改原图片和文字颜色。默认 0.35 秒，0 秒直接完成。
- FishingSceneEntryGuard 同时启动镜头转场与菜单渐隐；渐隐完成再停用 MenuCanvas，等待镜头也完成后启用 GameplayRoot / GamePlayCanvas。重新启用菜单恢复 alpha、interactable、blocksRaycasts；EventSystem 继续独立在场景根。

## 2026-09-08 — 菜单点击失效根因

- 上轮将 GamePlayCanvas 设为菜单阶段停用，却遗漏其子对象 EventSystem（1185182857），连带禁用了 InputSystemUIInputModule。按钮回调和 GraphicRaycaster 已接线，但没有活动的 EventSystem 分发点击。
- 已仅将 EventSystem 的 RectTransform（1185182860）从 GamePlayCanvas 移至场景根，更新原父子列表和 SceneRoots。保留输入 Action 引用、组件启用状态及唯一 EventSystem，不改按钮、布局或玩法。

## 2026-09-08 — 新 MenuCanvas 与空镜入口

- 用户保存后的正式场景包含 MenuCanvas、StartButton Prefab 实例、SettingButton、ExitButton。Start 原 Prefab 回调会导航 GameScene；Setting/Exit 仍是测试场景路由。已局部覆盖为 FishingSceneEntryGuard.StartGame / OpenSettings / ExitGame，未修改共享 NavigateButton Prefab。
- GameplayRoot 原包含 MainLanscape；单纯关闭玩法根会同时隐藏场景。已仅把这条静态景物根移到场景根，原父层位姿为单位变换，因此保留世界位置。GameplayRoot 与游戏 Canvas 保存为初始停用，防止 Start 在菜单后面提前计时。
- 入口守卫负责菜单、必要校准、等待镜头转场和启用玩法；FishingCameraController 负责新空镜选择及等待 CinemachineBrain blend，现有六态玩法流程不增加 Menu 状态。转场独立写在现有 Camera Blends 资产中。
- MainMenu 和 GameScene 逻辑 ID 共用 FishingLoopTest 资产；SceneLoader 的一次性请求区分显示菜单与直接开局，SubsystemRegistration 重置静态请求以兼容禁用 Domain Reload。排行榜和普通导航的无 AppRoot Editor 路径共用该规则。
- 本次为用户直接确定的主菜单/镜头流程变更；原设计文档未改写，记录与既有独立 MainMenu 入口的差异，未宣称 Claude 审查。

## 2026-09-08 — 本地排行榜接入

- 原先只有 GameOver 分数文字，无持久化成绩或榜单。EnterGameOver 是最后一钩和计时结束的共同结束节点，保存从 ScoreTracker 读取最终值；未改变捕鱼计分、状态顺序或结束条件。
- 采用 LeaderboardData（纯数据排序/去重）、LocalLeaderboard（JSON + PlayerPrefs）、LeaderboardView（读取和显示）分工。排行榜不成为第二个局内分数来源，不要求 AppRoot 持有新服务。
- 独立 Leaderboard 场景使用现有 TMP / uGUI 序列化结构和字体，新布局保留在场景中可编辑。已有 MainMenu / FishingLoopTest 仅追加按钮、保存状态文字及父子引用，原 GameOverText 保持原布局和内容。
- LeaderboardNavigationButton 支持 AppRoot / SceneLoader 正式入口及直接 Editor Play 的三个固定路由。新增 E_SceneID.Leaderboard 追加在末尾，避免已有枚举序列化引用变化。
- 本版默认最高 10 条、分数和日期、不输入姓名、不新增可玩地图；这是范围澄清尚未收到回复时采用的实现方案，未声称用户逐条确认或 Claude 审查。仅与用户要求有关的排行榜扩展已实施，原设计文档保持不变。
- Unity 6.3 PlayerPrefs 文档确认跨局本地持久化；Web 平台使用浏览器存储，设备/浏览器间不共享。实际 PlayerPrefs 重启读回及平台验证尚未进行。

## 2026-09-08 — 分类外观 Profile 与随机揭示

- 用户要求集中管理 Small / Medium / Large / Special 列表。新增 FishAppearanceProfile，素材按 Small / Middle / Large / EasterEgg 映射填充 9 / 9 / 7 / 4 张。使用原始切片 GUID 与 fileID，不重导入、不复制图片。
- FishAppearance 的序列化 revealedSprite 已迁移为 appearanceProfile 和 category；旧 BasicFish / Small Variant 对应字段已迁移。随机结果留在各鱼实例，不写回共享 Profile；首次有效揭示等概率选择非空项，后续恢复隐藏不会重新抽取。空列表无异常且不换图。
- 四类 Variant 继承 BasicFish 的共享 Profile，只覆盖类别。现有根缩放、Collider、VFX、分数及生成权重保持原值。已配置全部素材引用，不等于所有 Sprite 视觉尺寸均已验收。

## 2026-09-08 — 首个固定鱼外观实现

- 用户授权开始实施。FishAppearance 单独负责 Sprite / tint 保存、揭示和恢复；BasicFish 共享该组件，未配置 revealedSprite 时保持原样。仅 SmallFish_Variant 指向 Small-1，正式场景现有生成器已引用该 Variant，无需改场景。
- 揭示位于 HandleStrikeSucceeded 的有效分支；咬钩及空钩路径不会调用。FishController.ResetToIdle 恢复外观，覆盖碰撞、断线及计时结束；FishAppearance.OnDisable 覆盖上岸停用及复用清理。固定 Sprite 不随机改变身份。
- Small-1 切片为 13×15，现有小鱼剪影为 11×6，PPU 均为 100，中心对齐。直接换图保留根缩放 5.4、Collider 半径 0.06 及 VFX 参数；真实外观比剪影更高，比例和可辨认性待试玩。未修改素材导入设置。
- 命令行首次构建因生成 csproj 尚未包含新脚本失败；通过 TEMP 下 CustomAfterMicrosoftCommonTargets 临时纳入 FishAppearance 后编译 0 errors、3 个既有 MSB3277 warnings。未手改生成工程，Unity 刷新和 Play Mode 尚待验证。

## 2026-09-08 — 鱼样貌揭示规则确认

- 用户确认：提竿成功时揭示；揭示前沿用现有水下外观；揭示后失败恢复隐藏外观。品种身份保持仍是建议，未随外观复位规则自动视为批准。
- 对应节点为 HandleStrikeSucceeded 的有效成功分支。Hooked 仍早于该节点；空钩超时也会进入 Reeling，因此不能仅依据进入 Reeling 无条件揭示。当前无需新增上岸展示时段。仅记录计划，未实施。

## 2026-09-08 — 鱼真实样貌揭示前置发现

- Fish_v2/Fishes 含 29 张 PNG（Small 9 / Middle 9 / Large 7 / EasterEgg 4），分类来自文件名。尚未核验视觉适配。
- Hooked 在提竿前已设置；成功上岸会立即 SetActive(false)。新机制需要先确定触发节点，避免提早揭示或展示同帧消失。详见 [计划](task_plan.md)。

## 2026-09-08 — 倍率反馈修订

- 用户要求几倍倍率就放大几倍，并持续到本次收线结束，取代 1.65 倍/0.65 秒脉冲。缩放基于 Awake 保存的原始值，避免逐帧乘法累积；保持到本次尝试成功、失败或游戏结束。

## 2026-09-08 — 奖励规则与倍率显示

- 用户确认加速奖励单独相加，不乘落水倍率；只计算实际向岸加速距离，横移不计入。采用完整加速期间的向岸位移，不是相对普通收线的额外位移。失败及计时耗尽丢弃未结算奖励，擦边尚未实现。
- 用户要求倍率上限 3、蓝/金/红落水反馈。分档阈值 1.9 / 2.99 和 1.65 倍放大为本次原型实现选择，色值可在 HUD 手调；未声称 Claude 审查。

## 2026-09-08 — 倍率反馈事件边界

- HUD 监听协调器的 DistanceMultiplierLocked，避免直接监听物理 Landed 时依赖订阅顺序读到旧倍率。事件只在 Casting 落水有效分支发布；HUD 不计算倍率或分数。缩放参数在现有 HUD Inspector 可调，不新增动画资产或场景引用。

## 2026-09-08 — Follow 返回时的目标复位时序

- CinemachineBrain.DoNonFixedUpdate 先 UpdateVirtualCameras（包含已停用但仍 live 的相机），再 UpdateRootFrame；FreezeWhenBlendingOut 在新 blend 建立时才取快照。ShowOverview 后同帧 Dock 会让快照之前的更新读到复位目标。现已在相机控制器解除并恢复 Follow，阻止复位传给退出中的相机。运行效果待用户验证。

## 2026-09-08 — 正交腾空、输入与飞行迁移更正

- 当前决定：透视探索延期，在正交场景内完成功能。现有平面精灵可尝试固定视角轻度透视，但这只是素材可行性判断，未完成 Unity 透视效果验证；大幅转动镜头所需的多视角信息不能由现有单张精灵自动产生。恢复探索前仍须满足用户“不增加美术需求”的前提。
- 前进方向与虚拟高度都使用世界 +Y，在当前正对 XY 的正交视角下仍是同一条屏幕竖线。Vector3 不会自动带来可见弧线，单纯切换透视也不会解决同平面表现问题。用户明确目标为明显腾空感，后续考虑逻辑位置投影与图像偏移/缩放配合。
- HookVisual 是根对象的直接子对象；根缩放为 0.2，因此已用世界坐标高度偏移避免局部缩放缩小显示高度。碰撞、距离和倍率继续使用根对象逻辑位置。落水复位已有，Dock 复位尚缺；取消飞行后不得继续发布落水事件，运行分支待验证。
- HookFlightProfile 是正式飞行基础配置，CastTuningProfile 仍服务手势与旧原型。当前 Launch(power, launchSpeedMultiplier = 1f) 根据初速度分量和重力计算总时长，UpdateFlight 使用解析高度；不是固定时长调参。装备倍率接口已预留，升级系统未实现。
- PC 蓄力和移动端手势均输出归一化 power；移动端不使用蓄力说法。初速度倍率不等于射程倍率，当前同高起落固定角度/重力模型下射程随速度平方变化。5～25 只来自测试地图，不是最终设计上限。
- 2026-09-07 的“倍率未结算、无 Profile 缺失检查、删除 POWER、飞行算法未定”等结论已过时：源码已补倍率锁定/结算/引用检查；POWER 常显并保留最后显示值，Slider 独立显隐。POWER 当前保存的是最后一次显示采样值，不应宣称已精确记录松手事件的最终力度。
- 保存场景已引用新 flightProfile/hookVisual，同时保留旧距离/时长 YAML 键；当前 C# 已不再序列化消费旧键。不为清理残余数据批量改写场景。
- 本次仅针对性读取源码/场景并更新开发记录，没有运行编译、Play Mode、Android 或 WebGL。

## Confirmed before implementation

- `CastTuningProfile.TriggerThreshold` is currently 1.25 rad/s, but that value was tuned with flick input in isolation. In M2 it must be re-measured while normal lane-selection tilting is active and raised as needed so ordinary tilt does not trigger a cast.
- The approved loop has six states: `ReadyToCast`, `Casting`, `Baiting`, `Striking`, `Reeling`, and `GameOver`.
- `AttitudeReader` supplies absolute rotational orientation as a `Quaternion`; `GyroscopeReader` supplies angular velocity as a signed `Vector3` in rad/s. They serve different gameplay intentions and are not interchangeable.
- The existing sensor prototypes follow a reader -> controller -> view/HUD split. New gameplay controls should preserve that separation where practical.
- `CastBallController.Landed` should remain a generic event. The fishing-loop state machine will reinterpret landing as the transition from `Casting` to `Baiting`.
- A reversed strike flick can likely reuse a second `CastGestureDetector` with a second profile whose `InvertAxis` value is flipped.
- Cast and strike detectors must only be enabled in their relevant fishing-loop states. Otherwise the strike detector could enter cooldown before `Striking`; `CastGestureDetector.OnEnable()` already resets its state.
- The M4 fish race needs one authority to accept the winning bite so two fish cannot independently claim it in the same frame.
- Rock collision and maximum tension intentionally share the same attempt-failure consequence: lose one hook and end the attempt.

## Scope and process constraints

- Codex coaches only. The student writes all C# and performs all Unity Editor and Inspector work.
- M0 stays limited to sensor orientation: reader/controller/view and orientation-versus-angular-velocity. Thresholds, filtering, and gesture-state details wait for M2/M5.
- M0–M3 are the stability floor. A mandatory time check happens after M3.
- M4 and M6 may be simplified only after the student explicitly chooses to do so.
- Any simplification must be returned to Claude for a design-document update before it becomes the implementation target.
- Placeholder content for the full MVP is two fish and one or two rocks.

## Implementation discoveries

- M1 architecture decision: use one `FishingLoopState` enum, centralized `switch` dispatch, and explicit Enter/Update/Exit method boundaries. Keep the controller as an orchestrator that delegates detailed gameplay to small components. Extract state classes later only if real state-owned complexity, reuse, replacement, or isolated-test requirements appear.
- State Pattern does not automatically make Unity code testable; testability depends on isolating rules from `MonoBehaviour`, `Time`, Transform, collision, and sensor dependencies.
- Project convention keeps component-specific enums beside their controller (`CastDetectionState`, `GyroscopeAxis`, `CastSensorAxis`), while broadly shared scene identity uses the separate `Enums` directory.
- M1 runtime verification succeeded: the controller entered `ReadyToCast`, then produced ordered Exit/Enter pairs through `Casting`, `Baiting`, `Striking`, `Reeling`, and finally `GameOver`, where automatic advancement stopped.
- Rider's “expensive method invocation” marker propagated from `Update()` through the debug transition call chain because `EnterState()`/`ExitState()` call `Debug.Log`; this is informational and acceptable for infrequent state transitions.
- `Time.unscaledTime` was used only by the temporary M1 harness to compare an absolute, time-scale-independent deadline; the harness was removed after verification.
- M2 concept checkpoint passed: the student connected dead zone to sensor noise, axis hysteresis to preventing horizontal/vertical flapping, and time-based exponential smoothing to frame-rate-independent response.
- The student explicitly deferred the `AttitudeControlTest.unity` phone/HUD observation until the M2 control-tuning/debug pass to prioritize infrastructure. This is a scheduling change, not a design simplification; the verification remains required before M2 is complete.
- The resumed Play Mode test made all three effects observable: a 10-degree dead zone delayed movement, 10-degree axis hysteresis held the current axis, and smoothing values 2 versus 30 produced slow versus near-immediate following.
- `GyroscopeDebugHUD` is an independent angular-velocity display, not a display of the attitude angles driving the Circle. The student relabeled its rows as `X-PITCH`, `Y-YAW`, and `Z-ROLL` while preserving the underlying x/y/z data mapping.
- After comparing the temporary values with the defaults, the student explicitly chose to keep `axisSwitchHysteresisDegrees: 10` and `smoothing: 30` because that response felt better. These are now intentional tuning values, not unrestored test overrides.
- M2 lane architecture decision: create a new single-purpose `ShoreLaneController` rather than generalizing the 2D screen-oriented `AttitudeCircleController`. It keeps calibration, Yaw extraction, dead zone, normalization, smoothing, and shore-range mapping while dropping Pitch/Roll, dominant-axis switching, and camera-screen mapping.
- Preserve the generic `CastGestureDetector.CastDetected(power)` contract. When it fires, the fishing-loop orchestrator combines the current lane X with power and passes/snapshots both at the `Casting` boundary; the detector itself does not gain a dependency on the shore character.
- The student requested a formal M2 A/B comparison instead of assuming the existing position-mapped control is best. Variant A maps normalized tilt to an absolute target lane position; variant B will map normalized tilt to signed movement velocity, so neutral input stops at the current position. Both variants must share the same calibration, dead-zone, supported-angle, inversion, and normalization behavior; only their final movement mapping should differ, and only one may be enabled at a time.
- `Mathf.Lerp(leftX, rightX, t)` in variant A is a stateless range conversion, not temporal smoothing. The later `Vector3.Lerp(current, target, blend)` is the part that adds response lag. Setting smoothing to zero selects immediate absolute-position mapping, not constant-speed movement.
- Read-only review of variant A found its Yaw-to-position, dead-zone, supported-angle, range-mapping, and frame-rate-independent smoothing pipeline complete and compiling. The functional blocker is that `Calibrate()` is private and never called internally, so `IsCalibrated` can never become true; it must be public for the planned UI button and should reset `IsTiltWithinSupportedRange` on success.
- `ShoreLaneController` currently has no scene or prefab reference, so variant A still requires attachment to the shore player, left/right limit objects and Inspector references, an explicit calibration button/status UI, and Play Mode verification. Its only code-specific build warning is the deprecated `FindObjectOfType<T>()`; `FindFirstObjectByType<T>()` is the current project-compatible replacement.
- Calibration scope expanded by explicit student request: both the main menu and an in-game pause menu must be able to start calibration. The player holds a neutral pose for three stable seconds; movement beyond tolerance resets the countdown and UI explains that the player must hold still.
- Calibration survives scene changes and new fishing attempts during the current application run, but intentionally resets after the application closes. This is session-lifetime state, not disk-saved player data.
- Existing `AppRoot` is the project precedent and composition root for cross-scene services because it already uses `DontDestroyOnLoad`. A focused motion-calibration service should own the neutral attitude; scene UI and gameplay controllers consume it instead of keeping competing copies.
- Future cross-scene data should be separated by lifecycle and responsibility rather than accumulated in one generic `GameManager` data bag: session services, per-run state, and disk-persisted settings/progress have different reset and storage rules.
- For the tightly clustered samples collected during a stable three-second hold, the planned practical quaternion average is: align sample signs against a reference quaternion using `Quaternion.Dot`, sum components, then normalize. Movement/spread is rejected before averaging. The full Markley eigenvector solution is unnecessary for this coursework-sized input cluster.
- Pause-menu calibration must use unscaled time because gameplay may set `Time.timeScale` to zero.
- The reusable `MotionCalibrationPanel` prefab is scene-local UI backed by the persistent service. MainMenu exposes optional manual calibration/recalibration; `FishingLoopTest` contains the same prefab as a mandatory fallback with its close button disabled only on that scene instance.
- `FishingSceneEntryGuard` owns the pre-game invariant rather than `SceneNavigationButton` or `FishingLoopController`: it disables `GameplayRoot`, opens calibration when required, and enables gameplay only after calibration. This protects every route into the gameplay scene without adding a non-gameplay state to the six-state fishing loop.
- `ShoreLaneController` no longer owns `neutralAttitude`, `IsCalibrated`, or `Calibrate()`. It reads the persistent `AttitudeReader` and `AttitudeCalibrationService` through `AppRoot`, freezes during calibration, and consumes the shared averaged neutral attitude afterward.
- `FishingLoopTest.unity` is now the single end-to-end placeholder gameplay scene and `SceneCatalog` maps `GameScene` to `FishingLoopTest` instead of the missing legacy `Scene1`.
- Runtime tuning selected `movementToleranceDegrees = 8` for the three-second neutral-pose hold. The original 3-degree default reset too easily during natural hand movement; 8 degrees felt reliable without making deliberate movement appear stable.
- Both calibration entry paths have now passed runtime testing: the mandatory uncalibrated fishing-scene path and the already-calibrated MainMenu-to-fishing path. Shared calibration continues to drive the lane controller after the scene transition.
- The student explicitly deferred implementation of lane variant B and the A/B feel comparison until a later tuning pass. This changes task order only; neither requirement nor the approved design has been removed.
- M2 cast-input ownership remains layered: `AppRoot` owns the persistent raw `GyroscopeReader`, the gameplay scene owns `CastGestureDetector` as a gesture interpreter, and `FishingLoopController` snapshots lane X plus detected power at the `ReadyToCast -> Casting` boundary.
- Android device `Debug.Log` output is verified through Unity Android Logcat by selecting package `com.DefaultCompany.urp_2d` and filtering message text.
- The combined-input test captured `lane X = -0.37` and `power = 1.00`. Ten seconds of ordinary lane tilting did not cast, one deliberate forward flick produced exactly one event, and further flicks in `Casting` produced no event because the detector was disabled on exit from `ReadyToCast`.
- The initial backswing false trigger was directional: device X angular velocity was positive during backswing and negative during the desired forward flick. Enabling `InvertAxis` made the desired direction positive. The existing 1.25 rad/s trigger threshold then passed the combined-input regression test without needing a magnitude increase.
- `CastTuningProfile` is a shared `ScriptableObject` asset, not scene-local data. Both the detector and `CastBallController` must hold valid references; deleting and recreating the asset changes its GUID and can leave serialized scene references null.
- M3 design correction from the student: `GyroscopeCastTest` presents a horizontal side-view projectile, while the actual fishing scene casts vertically into the water on screen. Reusing the old `Rigidbody2D` X-distance/Y-gravity trajectory would produce the wrong direction and spatial model. The old test flight must remain isolated; formal gameplay needs its own vertical hook-flight implementation. They may share power input and an event-based landing contract, but not the motion algorithm.
- The intended production cast is a 2D simulation of 3D motion: the hook travels vertically into the fishing scene while following a visually parabolic arc. The student explicitly deferred this trajectory implementation until after the broader state-machine infrastructure; this is a scheduling change, not removal or simplification of M3.
- Clarification: only the final parabolic presentation is deferred, not M3's state flow. A new formal `FishingHookFlightController` will first implement a minimal vertical flat trajectory with a stable Dock/Launch/Landed contract. Later 2.5D arc work stays internal to that component, leaving `FishingLoopController` unchanged.
- The minimal M3 device test passed in order: `ReadyToCast`, cast at lane X `-0.05` with power `1.00`, `Casting`, landing at `(-0.05, 2.42)`, then `Baiting`. Matching launch and landing X confirms the vertical flight preserved the selected shore lane.
- M4 candidate membership is runtime spatial data, not an Inspector-authored fish list. `FishBiteRaceController` performs one `Physics2D.OverlapCircleAll` query when Baiting begins, filtered by the dedicated `Fish` layer; fish outside `attractionRadius` remain idle.
- Fish spawning is scene-start test/content infrastructure owned by `FishSpawner`, while bite arbitration remains owned by `FishBiteRaceController`. The current configurable count defaults to 20 for stress testing, uses rejection sampling to enforce minimum spacing, and may spawn fewer fish rather than violate the non-overlap constraint when the area is full.
- The old Editor-only `M4BaitingTestHarness` became incompatible with full-loop testing because it disabled `FishingLoopRoot`, `ShoreLaneController`, and `FishingHookController`, then directly called `BeginRace()`. It has been repurposed to bypass only Bootstrap/calibration prerequisites and activate normal gameplay; the state machine must now advance the loop itself.
- Cross-platform movement now has one semantic contract: normalized input is a signed velocity multiplier, not an absolute target position. Keyboard A/D supplies `-1/0/1`; calibrated phone Yaw supplies a continuous `-1..1` value through the existing dead-zone and normalization pipeline. Zero input immediately stops, while lane bounds remain shared.
- The first Editor movement failure had two serialized/code causes: the lane-limit null guard was reversed and returned when both limits were valid, and `ShoreLaneController.inputSource` had not yet been saved in the scene. Correcting the guard and assigning the source restored movement.
- Editor playtest selected velocity control over position-target control: A/D now stops accurately at the current position on release. Android tilt behavior still needs a regression test before the final control selection is fully verified across both device paths.
- Calibration is an input-mode prerequisite, not a test-Harness concern. Under the current platform policy, `FishingSceneEntryGuard` requires calibration only on mobile platforms; Editor and desktop activate gameplay directly. This makes the old M4 Harness redundant for full-loop testing while preserving the Android calibration path.
- The full Editor keyboard path is verified through A/D lane selection, one-shot Space cast, hook landing, Baiting candidate response, and the bite transition. Strike and Reeling behavior remain future milestones, so stopping after their entry is expected.
- The student postponed M4 bait wiggle but corrected the next milestone choice before M6 began: M5 Striking now proceeds normally, preserving the dependency into fish-attached Reeling. The proposed one-dimensional bait wiggle is not an implementation target until Claude updates the approved design.
- M5 detector checkpoint passed. The student identified that Strike is a binary, time-critical intent and does not need Cast power/peak sampling; a successful Strike immediately exits the state and disables its detector. The generic detector still retains rearm/cooldown for long-lived consumers such as the gyroscope test scene.
- `FilteredPeak` means the maximum of the already low-pass-filtered angular-velocity samples. It prevents a single raw sensor spike from turning a light cast into maximum power; Strike can ignore power entirely.
- The `FishingLoopTest` Cast detector currently references `Assets/Scripts/Controller/CastTuningProfile.asset`, whose `castAxis` is X and `invertAxis` is enabled. The Strike profile should therefore begin as a duplicate with `invertAxis` disabled while preserving the already-tested thresholds/filter values.
- Design revision 6 formally approves the M5 shrinking-ring timing check. A moving ring shrinks linearly from normalized radius 1 to 0 over 1.5 seconds; the fixed full annulus starts at inner radius 0.24 and outer radius 0.40. An out-of-band attempt is rejected, starts a 0.35-second input cooldown, and emits feedback without ending Striking; only timeout fails and costs a hook.
- Strike ownership is now explicit: `StrikeController` is the sole timer/judgement/cooldown authority and publishes normalized ring/band data plus succeeded, timed-out, and rejected events. `StrikeWindowHUD` only renders those values; camera shake independently consumes the rejected event; `FishingLoopController` only starts/stops the feature and interprets outcomes as global transitions.
- The Strike input must use `CastGestureDetector.GestureTriggered`, raised at the Ready-to-Sampling threshold crossing, rather than delayed `CastDetected(power)`. The current Strike profile still has the cast defaults (`0.35` sample + `0.75` detector cooldown, with the detector's display delay on top), so it must be shortened before rejected attempts can be retried within the 1.5-second window.
- The saved Strike detector configuration now uses the minimum permitted `SampleWindow = 0.05`, `CooldownDuration = 0`, and detector display duration `0`. This leaves only a very short internal state-machine tail after the immediate event; `StrikeController.attemptCooldown` remains the gameplay-facing attempt limiter.
- The student explicitly chose scaled gameplay time for Striking: opening a pause menu must freeze the shrinking ring, the overall window, and the rejected-attempt cooldown. The controller will advance with `Time.deltaTime`; because the sensor detector itself uses unscaled time, the Strike attempt entry point must ignore input while gameplay is paused.
- Strike feedback preserves dependency direction: `StrikeController` publishes `AttemptRejected`, `FishingLoopController` coordinates the response, and `FishingCameraController` owns the Cinemachine-specific shake implementation.
- The accepted rejected-attempt shake uses a Cinemachine Impulse Source configured as Uniform/Rumble for 0.15 seconds with default velocity `(0.20, 0.12, 0)`. Only `HookFollowCamera` listens because a rejected attempt remains in `Striking`; a full-window timeout is a separate outcome that returns to Overview.
- M6 think-first result: tension remains clamped at its minimum, reaching maximum reports one line-snap outcome, and Accelerate must raise tension faster than passive release drains it. The student added a design change: gentle lateral dodging should add almost no risk and may still permit net decay, while sufficiently fast lateral movement should raise tension. This supersedes the approved rule that only Accelerate creates tension risk and requires a Claude design revision before code implementation.
- Design revision 7 approves the movement-driven tension model. Each frame sums passive decay, held-Accelerate rise, and a lateral contribution derived from the hook's actual post-movement speed; the result is integrated once and clamped to `0..1`. Measuring actual motion makes full input against a lane boundary cost no tension.
- The lateral contribution uses `InverseLerp(safeLateralSpeed, maxEvaluatedLateralSpeed, abs(hookLateralSpeed)) * maxLateralTensionRate`. `accelerateRiseRate > decayRate` and `maxLateralTensionRate > decayRate` guarantee that Accelerate alone and maximum-speed dodging alone can each create net tension growth.
- Design revision 8 establishes the tuning-data boundary: feel/balance values adjusted during Play Mode belong in `ScriptableObject` profiles so the edits persist, while scene references and placement remain component fields. M5 therefore requires a dedicated `StrikeWindowProfile`, and M6 requires `ReelTuningProfile`; both must self-validate their coupled invariants.
- M6 ownership decision: add a focused `ReelingController` for retrieval movement, dodge, measured lateral speed, and tension because their within-frame ordering is tightly coupled. `FishingHookController` remains the Dock/Fly/Land authority, while `FishingLoopController` interprets Reeling success/failure events as global transitions.
- M6 collision diagnosis: `FishingHook` still had the obsolete `PlayerHazard` component from `OnCollisionEnter2D.cs`, while the entire `Sea` object is tagged `Hazard`. Adding a `Rigidbody2D` for Reeling activated that old trigger path during Casting; it reloads the current scene directly, bypassing `ReelingController.IsActive` and making the loop appear to return to `ReadyToCast`.
- M6 lateral-speed measurement passed: the current keyboard dodge reaches approximately `4 world units/s`, while holding full input against a clamped lane boundary measures `0`. Initial profile thresholds should therefore use `maxEvaluatedLateralSpeed ≈ 4` and `safeLateralSpeed ≈ 1.33`, then be tuned from Play Mode evidence.

## 2026-09-03 — Tension UI source review

- `FishingLoopTest.unity` contains the attention-bar background as a root-level `SpriteRenderer`, with no display script on that object. The pointer sprite exists as an asset but its GUID is not referenced in saved scenes or prefabs; no script currently consumes `Tension01` for UI.
- Tension UI still needs pointer/endpoints, display-only value mapping, retrieval-state visibility, and layout verification during camera movement. Screen-fixed Canvas versus world-space presentation has not yet been confirmed with the student.
- Maximum-tension failure is already implemented in saved code despite the old deferred checklist entry. Source inspection establishes implementation presence only; one-shot snap and hook-loss behavior still need runtime verification.
- The first saved Slider pass has the correct vertical direction, value range, disabled interaction, Fill/Handle assignments, and Frame-last sibling order. Its current `TensionBarHUD` object is actually the former EventSystem (it retains `EventSystem` and `InputSystemUIInputModule`), so it must be renamed back and the Slider moved under a new plain HUD container. The Handle has not yet received the pointer sprite, and `TensionBarHUD.cs` is not yet attached or serialized in the scene.

## 2026-09-06 — HUD and GameOver integration

- The separate TensionBarHUD container had no display component. It now references ReelingController and its child Slider; only the child is hidden so the HUD can reactivate on the next retrieval. Slider notifications are suppressed and disabled-state tinting is removed.
- GameOver previously left hook flight and an active bite race running. Explicit cancellation now freezes flight without a Landed event and resets approaching candidates without a bite/timeout event. Runtime behavior still requires Play Mode verification.

## 2026-09-06 — Android playtest design handoff

- Revision 9 is now synchronized locally but its random target band, hidden forgiveness and two-pass ring remain unimplemented. User requests extending the return-to-ready cooldown to Strike timeout as well; last-message state naming ambiguity is documented explicitly in the handoff.
- User accepted fish-base-score times longitudinal-distance multiplier, plus near-miss and accelerated-retrieval-distance bonuses. Lateral travel does not count; pending bonuses settle only on successful catch. Numeric curves/rates remain undecided.
- Fish generation exclusion means visible rock occlusion, not the whole shoreward lane. User also requests a basic behavior tree and Idle/Swim/Hooked animation states.

## 2026-09-07 — 计分、蓄力与飞行方案更正

- 距离规则已改变：倍率随 Casting 距离更新、落水锁定；HUD 的当前到岸距离在 Reeling 中减少。旧咬钩时锁定规则仅为历史设计，后续按用户本轮确认实现。
- FishingLoopController 保存 castDistance；CurrentCastDistance 实际委托 ReelingController.DistanceToShore 计算当前距离，不能误认为它是固定落点距离。CurrentDistanceMultiplier 是运行时倍率；Profile 只存配置。
- 当前保存代码的 HandleHookLanded 没有最终倍率重算，HandleRetrievalCompleted 仍只结算基础分；Start 没有检查 scoreTuningProfile。教学代码示例已给出不等于用户实际接完。
- FishingSessionHUD 保留 castChargeText 和 castChargeSlider，Slider 显隐放在文字 activeSelf 变化分支中；去除文字 UI 时必须同步修改消费者和引用，不能只删除文字对象。
- PC 蓄力以 fullChargeDuration 归一化；旧 castPower 固定值已被替代。用户要竖条，仅键鼠蓄力时显示；运行时 Value 增长由用户确认。
- 现有正式飞行仍用 minimumCastDistance / maximumCastDistance / flightDuration。用户明确不满意固定飞行时间，但没有确定抛物线方案或可见弧线范围。不要将建议当成获批设计，也不要将独立 CastBallController/GyroscopeCastTest 直接替换正式控制器。
- 将来虚拟高度仅用于表现时，距离 HUD、倍率和水面逻辑必须读取逻辑坐标，避免把腾空高度算成抛远距离。此项是拟议方案的技术边界，尚未实现。
- 新计划见 docs/reference/2026-09-07-progress-and-plan.md；旧两日安排不自动延长。技巧奖励数值、时间耗尽的待结算奖励处理和实际截止时间仍有待确定。
