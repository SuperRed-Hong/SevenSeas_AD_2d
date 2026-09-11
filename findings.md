## 2026-09-10 — WebGL 手机身份与体感权限

- Web 手机是否使用体感不能只看权限 Promise：浏览器可能返回允许，但 Unity Input System 仍收不到姿态设备/样本。入口现在先等权限，再独立等待实际 `AttitudeCalibrationService.IsSensorReady`；任一步失败都切换触屏，避免玩家卡在永远无法完成的校准面板。
- 触屏降级继续走 `FishingInputSource` 的 Move/Cast/Strike/Accelerate 语义门控；单一 ACTION 按钮根据状态切换职责，并在状态退出或按钮隐藏时释放 held 状态，避免提竿后直接继承为收线加速。UI 运行时挂到正式 Gameplay Canvas，不改场景 YAML；按钮的设备布局仍需在 itch iframe 与真机安全区验收。
- itch.io 的 `Mobile friendly` 只控制页面嵌入/启动方式，不会改变 Unity 的输入平台判定。`Application.isMobilePlatform` 在 WebAssembly 浏览器中允许不准确，项目必须让“是否采用移动控制”成为独立、统一的运行时能力判据。
- Web 传感器与原生 Android 传感器不是同一授权路径。HTTPS 只是前提；iOS 类浏览器还要求在用户手势调用栈内请求 Motion/Orientation 权限，嵌入页仍可能受 iframe Permissions Policy 限制。当前桥接已覆盖应用内识别与权限请求，itch 真机仍是最终证据。
- 可复用候选：浏览器设备识别、用户手势权限桥接、异步授权后传感器重连和可解释失败状态。项目耦合为现有 AppRoot Reader、校准面板文案与移动手势规则；需在第二个 WebGL 传感器用例及不同浏览器验证后才能视为独立资产。

## 2026-09-09 — UI音效完整接入与暂停视觉

- 用户填好的UI clip确实有效，缺失是普通按钮未触发播放。现在UI音源和绑定单独负责：场景加载扫描按钮，懒创建按钮显式RegisterButton；原PlayUi公开方法转发路由。手工持久音效监听存在时跳过自动点击，避免双播；同帧点击/选择排队合并。
- UI源持久是为了按钮触发换场景后尾音不被OnDisable切断；不把BGM/环境也改成持久。音源ignoreListenerPause，SoundEnabled仍统一静音，主场景每帧同步Inspector音量。冷启动无音效配置的独立Leaderboard仍静音，正常启动流从GameFeedback继承。
- 暂停之前完全由脚本生成默认蓝底按钮且未指定项目字体。改复用现有排行榜纸张GUID99c7c95e5fd647942a31e8dd2904364e、PressStart2P GUIDdd8d574598480c14eb2d72e7b14b3d9f、关闭Sprite；生成式布局保持，未重建主场景或增加新美术。
- 可复用候选：语义UI音效绑定、同帧优先级、跨场景短音尾巴及动态控件注册。项目耦合为FishingAudioFeedback配置和按钮返回命名约定；真实听感/输入设备与第二用例待验证。手动运行时AddListener音效无法由Unity持久监听API识别，因此项目内显式旧调用已移除，未来新增按钮应统一注册。

## 2026-09-09 — 震动触发范围变更

- 用户明确新增Striking进入反馈，取代原“仅失败”约束。状态进入与失败各自按attempt去重，互不吞掉同竿的另一个提示；关闭震动/暂停/失焦时也记录已处理，避免恢复补震。复用Android原生Handheld.Vibrate，不新增第三方包或WebGL桥接，时长/强度仍由现有设备API决定。

## 2026-09-09 — 胶囊导航与生态规则接入

- 用户已将正式BasicFish及其Variants改为横向CapsuleCollider2D；保留其尺寸和资产。旧独立SmallFish/MediumFish/LargeFish/SpecialFish仍有圆，但正式生成器引用Variants。原生成和移动取Sprite/Collider世界AABB会消除胶囊优势，现在统一FishNavigationGeometry；Sprite仅用于嘴部位置，旧非胶囊/圆Collider仍保守Bounds回退。
- 局部绕行使用阻挡体Bounds产生左右候选路点，再以实际鱼体验证每段转动和扫掠；最多两个路点，卡转向尝试最多两段当前朝向退出。路线保持、失败重试0.5秒，不每帧左右切换。不是全局寻路，不保证复杂多障碍/原活动区域外目标可达。
- 逃离/捕食规则由用户本轮直接批准，不宣称Claude已审查。行为控制器只申请外部导航与替换，FishingLoopController独占HookedFish写入口，验证当前猎物、类别、朝向、真实嘴接触、阻挡、计时/暂停/收线/每竿一次门控；不重启收线、不发中间CatchCompleted。最终鱼自动决定原结算快照、张力倍率、鱼线和表现。
- 生态按CurrentAttemptId保留每条捕食者一次概率，暂停或组件禁用后重启不重掷。Small逃离3/4米滞回；捕食概率0.3、检测5米、追赶倍率1.5和接触容差0.1放独立Profile。Special参与是用户明确决定，非随机Sprite推断品种。
- 原生Unity测试发现6000.3的OverlapCapsule在全局queriesHitTriggers=false时可忽略filter.useTriggers=true。几何与ReelingObstacle入口均在同步查询内暂开并finally/Dispose恢复全局值，不永久修改项目设置。当前项目该开关本来为true，不能把此发现称为之前卡鱼的原因。
- 资产候选：形状一致的生成/移动查询与只读路线诊断，可迁移胶囊扫掠、旋转保守包络和路径保持；项目耦合为ReelingObstacle标记、每鱼spawnArea活动边界和嘴部规则。仅本项目/独立原生fixture验证，尚非通用导航系统；后续练习在第二个胶囊角色用例验证复杂障碍与非等比缩放边界。

## 2026-09-09 — 运行时证据缺口

- 静态发现圆/方框几何问题不代表所有不咬钩反馈由它导致。最新运行仍出现9候选无赢家；需要实际鱼状态、角差、头距及哪个检查阻挡。新增只读Editor快照工具而非进一步放宽规则。
- 当前用户场景含BigFishSpawner，生成区高6.37、横向偏移-4.35，与原生成器30.1高区域不同。现代码将每个生成区域同时用作运动边界，可能限制来自小区的鱼，但未有现场证据将其定为本次全停原因；不擅自扩大用户区域。

## 2026-09-09 — 圆形碰撞体不应随鱼旋转成方框

- 旧CaptureFootprint把CircleCollider2D.bounds四角并入局部方框，45度旋转后的半宽从r增至sqrt(2)*r，再加转向余量；LargeFish_Variant世界圆半径0.8132，旧45度方框半宽约1.15，可能使圆外的岩石卡住转向。前一批只检查去重/咬钩距离的替身测试没覆盖真实尺寸的这种几何失真。
- 现Circle独立圆查询/圆扫掠，保留非中心圆旋转位移与边界检查；其他形状/剪影仍有保守误差。不宣称用户截图已唯一定位；若复测仍停住，须区分转向受阻、移动受阻、头距、竞赛成员与超时，不能继续仅凭截图扩大咬钩范围。

## 2026-09-09 — 已批准鱼头咬钩实现依据

- 用户接受推荐方案后，将竞赛中心距离改为BitePosition到钩的距离；头部取初始剪影Sprite边界中心沿SpriteForwardAngle方向与边界交点，随Transform缩放/旋转到世界空间。不新增Prefab锚点或猜测品种尺寸；无配置旧原型保留中心回退。
- 追饵目标反推鱼中心，使鱼头停在hookRadius的90%处，留数值余量且不越过钩后掉头；仍使用完整鱼体运动段避障。原hookRadius=0.25与唯一赢家排序不变。ReelingObstacle.BlocksSegment检查两端与Linecast，明确包含Trigger，只拦启用的ReelingObstacle，不让判定容差穿越薄岩石。
- 源码/Unity数学与Physics替身10项验证近岸头到钩、中心仍在旧半径外、长帧不越过目标、旋转/缩放、受阻/薄岩石分支、未转完不抢、两鱼只一赢家、结束门控。真实碰撞轮廓/保守Bounds误挡仍待Play验证。挂钩后的跟随和揭示保持既有实现，未新增寻路或改写Claude设计文档。

## 2026-09-09 — 靠岸追饵可达性与鱼线门控

- 钩落点ContainsPoint与鱼体Bounds运动检测采用不同足迹，不能从钩落点合法推断鱼中心能到达。竞赛当前中心半径0.25，贴岩石时鱼体可能在进入咬钩范围前被挡；旋转/移动的保守包围盒也可能误挡，需要真实Physics观察，不能直接扩大半径或取消避障作为已批准规则。
- 用户期望落水等待时仍有鱼线；FishingLineView新增Baiting/Striking到hookRoot的端点，Casting仍到hookVisual、Reeling到挂鱼或空钩。旧“仅Casting/Reeling显示”记录被本次显示修正取代，不改变玩法结果。

## 2026-09-09 — GUID格式漏检更正

- 新FishMovementProfile曾被写成33位GUID，尽管资源与场景字符串相同且唯一，Unity仍不能导入。已修正为生成的32位十六进制GUID并同步引用。旧“静态接线检查通过”仅覆盖当时ID/字符串对应，不证明资源可导入；以后资源引用检查必须包含GUID格式、唯一性与目标引用，而非只搜字符串。
- BGM现在在GameFeedback启用时开始，独立于玩法runStarted；每个音效Level传给PlayOneShot，不修改共享源来调单个声音，避免影响同时播放的其他音效。真实听感与浏览器自动播放仍需设备验证。

## 2026-09-09 — 场景对象职责纠正

- 此前为复用常驻对象，将暂停/仓库/反馈追加到StrikeTuning，使对象名与职责不符。用户要求先整理组织：现拆为三个独立启用的场景根StrikeTuning、PauseUI、GameFeedback。
- 保留六个迁移组件的原fileID，仅调整m_GameObject及宿主组件列表。既有按钮事件、调参共享暂停owner、仓库结算订阅和音频素材/音量引用无需重建；源码未依赖旧父对象或名称查找，运行时Canvas/AudioSource将创建到对应的新宿主下。
- 静态比较确认除宿主外六个组件序列化字段完全一致，其他既有对象块没有变化；1032个ID唯一、本地引用完整。实际Unity重新载入与Play尚待验证。

## 2026-09-09 — 当前分支并行实施依据

- 主代理在当前交接分支完成共享 CatchResult/AttemptFailureResult 契约：每次 EnterCasting 分配 AttemptId，attemptSettled 防重复结算；统一计算实际分、加时并在停用前保存最终 Category/揭示 Sprite。库存通过事件只存结果，不重算分数。收线失败原因目前统一 ReelingFailure，未进一步区分碰撞/断线设备反馈。
- FishAppearance 的现有四类 Variant 是唯一逻辑大小来源，新增只读 Category/RevealedSprite，不从 scale 或随机 Sprite 推测品种。仅本局逐条库存无需跨局 speciesId。
- 实际仓库素材为 `Assets/Art Asset Folder/Storage/Fishing-UI-_0001_inventory.png`，73×102、3×4格；独立View使用原比例面板/分页。暂停菜单保持单owner，隐藏面板而不禁用组件，关闭仓库只返回暂停。
- SessionTimer 以 DefaultExecutionOrder(-100) 先扣时；AddTime 拒绝停止/归零及非正有限输入，无上限但拒绝溢出。HUD在LateUpdate读取结算后时间。仅放大加速增长，边界后的实际横移与衰减不变。
- Idle 每条鱼保存出生中心、半径2内短程游动；追饵/失败出圈后逐步返回，受阻停住无寻路。移动段与转动范围以保守鱼体Bounds检测；这比生成终点避障覆盖更多，但狭窄处可能停住，需Physics2D试玩。四种剪影原图实际头朝左，Profile spriteForwardAngle=180。Z轴转向当前作为初版，视觉选择仍待用户答复。
- Android 首版使用 Handheld.Vibrate，时长由系统决定，无第三方依赖；暂停菜单提供本局震动开关，去重且暂停/失焦无新反馈。最后一钩失败允许一次反馈。WebGL无震动实现。
- 音频目录全量扫描仅24 WAV+1 MP3，WAV为0.08–2.98秒；未试听，不能认定BGM。新3源播放器分离UI/玩法/循环；当前音频槽位空，先完成接入，等待素材。官方参考：https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Handheld.Vibrate.html 、https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-audio.html 、https://developer.mozilla.org/en-US/docs/Web/API/Navigator/vibrate 。
- 新资产候选（尚非独立验证通用资产）：一次结果快照/去重、本局分页集合、局部转向运动、受状态门控的加时、分平台反馈与音频生命周期。依据为本次脚本/隔离检查；耦合为FishingLoopState/Category/UGUI；适用于本局单玩家，欠真实设备及第二项目接入验证。后续练习：用户重建一次结算消费者并在另一种收集对象用例验证取消/重复/重开，不在当前Prototype扩展框架。

## 2026-09-09 — 五线并行接入调查（交接时）

本轮只读核实：HandleRetrievalCompleted 在 AddScore 后立即停用鱼并清空 HookedFish，仓库/奖励/反馈需要同一成功结果快照；SessionTimer 尚无增加时间接口；FishingPauseController 用单一 owner 管理暂停，仓库关闭不能擅自恢复游戏；FishAppearanceProfile 是四个 Sprite 列表，没有据此确认稳定品种存档方案。Audio 目录已有素材但未试听，仓库素材确切路径待下轮定位。上述是设计接入依据，尚未实施五项新功能。

用户明确授权下一对话采用 subagent 并行；当前未派代理。主代理独占共享协调层、场景与进度文档写入，各模块代理先独立调查，再按批准规则分波次实现。详情见 docs/reference/2026-09-09-parallel-development-handoff.md。

# Fishing Loop Findings

## 可复用技术资产候选台账（2026-09-09 起持续维护）

以下均为候选，不代表已通用化或已完成设备验收。后续每项补充原问题、实现证据、通用部分、项目耦合、边界和验证欠账。

| 候选 / 领域 | 当前依据与可迁移价值 | 主要耦合 / 下一项验证或练习 |
|---|---|---|
| 语义输入与平台路由 | FishingInputSource / Router，设备与玩法隔离 | 依赖钓鱼动作命名与 AppRoot；用另一种角色控制验证接口 |
| 传感器校准与手势检测 | AttitudeCalibrationService、CastGestureDetector，稳定采样/四元数平均/阈值重置 | 平台采样、冷却与生命周期；真实设备延迟、重连和退出回归未完成 |
| 状态驱动 UI 与转场 | FishingSceneUIController / Entry / Fader，初始化不依赖 Inspector 开关 | 具体场景与校准入口；第二流程验证显隐和输入时序 |
| 暂停所有权与恢复 | FishingPauseController，共享面板互斥与组件状态快照 | 暂停名单、组件 OnDisable 副作用；验证不同玩法状态恢复 |
| 动态连线表现 | FishingLineView，世界端点/LateUpdate/视觉和逻辑分离 | FishingLoopState 与 HookedFish；用户从零重建，再连接其他动态对象 |
| 运行时调参和判定快照 | StrikeWindowProfile / StrikeController，资产默认值与运行副本分离 | 固定参数数组/钓鱼判定；用另一种 Profile 检验数据接口与约束 |
| 本地排行榜与存档边界 | LocalLeaderboard 与排序/去重检查，持久化与纯数据规则分离 | PlayerPrefs/WebGL 行为；重启保存、坏档、第二种成绩数据迁移 |
| 运动和风险算法 | HookFlight、实际位移驱动张力/加速计分、倾斜死区与限位 | 坐标与钓鱼规则；提炼数学核心，验证限位/零输入/帧率差异 |
| 资源配置与外观揭示 | FishAppearanceProfile 分类列表、随机身份缓存、失败恢复 | Fish 类别、生命周期、美术尺寸；第二类实体的配置复用 |
| 安全施工与验证流程 | GUID 保留、局部 YAML 修改、增量编译、独立源码检查和进度记录 | 检查脚本尚散落临时目录；整理最小工具并明确不能替代 Play/设备验证 |
| 人物动画状态机（待实现） | 当前教学准备中的状态/动画/移动分工 | 先完成项目用例，再决定值得提炼的动画参数与映射 |

验收目标：在明确领域内可扩展和迁移，而非预先保证任何项目都适用。实现复用与个人理解分别验收。

## 2026-09-09 — 鱼线教学偏好

- 用户首次接触此类 LineRenderer 表现，希望后续完整亲手实现，每次一个可验证步骤。当前只记录复习安排，不影响开发进度；具体清单维护在 task_plan.md，不另建计划文档。用户已反馈当前鱼线效果不错。

## 2026-09-09 — 人物至 Hook / 鱼的视觉连线

- Hook 飞行使用独立 HookVisual 表现虚拟高度，因此 Casting 端点必须引用 HookVisual，不能使用水面位置 Hook 根。Reeling 挂鱼在 FishController.LateUpdate 跟随 Hook，FishingLineView 使用 DefaultExecutionOrder(1000) 的 LateUpdate 后绘制，避免端点落后。
- 以 Loop.CurrentState 决定 Casting/Reeling 可见性，不用 FishState.Hooked（咬钩即设定，早于提竿成功）。Reeling 无鱼时连接 Hook 根，结算/失败/其他阶段隐藏。复用现有 StrikeRingMaterial、不修改材质；Player 层 order 8，位于人物及 Hook 后，宽度默认 0.035，浅米黄色。起点沿用 HookLaunchPoint，局部 Player Offset 可微调而不改变实际抛竿起点。

## 2026-09-09 — 手机 Strike 延迟与 Cooldown 分层分析

- 实际场景 StrikeDetector 是 CastGestureDetector 第二实例，StrikeTuningProfile 为 X 正向、Trigger 1.25 rad/s、Rearm 0.45、SampleWindow 0.05 s、CooldownDuration 0；场景检测结果展示时长覆盖为 0。Ready 且 armed 时检查原始 DirectedVelocity>=阈值，当帧同步 GestureTriggered，Mobile/Router 转发到 Strike.HandleAttempt，不等待滤波和采样峰值；这是电平条件，并非严格前后帧越阈判断。
- StrikeWindowProfile 保存 AttemptCooldown=0.35 s，仅判定失败后生效，HandleAttempt 对所有输入统一拦截；冷却期传感器可继续触发事件，但被判定层丢弃且不缓存。检测器已经进入 Sampling、isArmed=false，因此被丢弃挥动仍会消耗采样/状态切换/回落重置周期。再次准备需回落到 Rearm 阈值内；无可用输入反馈容易被理解为延迟。
- 抛竿另用 CastDetected，等待 0.35 s 峰值采样后才发射；Cast Profile 冷却 0.75 s，结果展示 0.2 s。检测器 OnEnable/ResetDetector 会清空自身冷却；正式一轮结束后的再次抛竿由 Loop.PostAttemptCooldown=0.8 s 控制。Reeling 加速由 UI 按住控制，不消费提竿冷却。
- GyroscopeReader、检测器、StrikeController、StrikeWindowHUD 均在 Update 读写，没有这条链路的显式执行顺序和传感器时间戳配对。可能读上一帧传感器缓存或环半径，产生帧级时差；当前未做手机性能/传感器端到端采样，不能断言具体延迟毫秒数。运行时调参按 BeginCheck 快照，下次判定生效，静态保存参数不等于当前运行副本值。

## 2026-09-09 — 旧 Gyro / Attitude 测试与当前服务衔接

- 两测试场景各带本地 Reader，且 Reader.OnDisable 会 DisableDevice；与常驻 AppRoot 同时启用会有退出测试影响全局传感器的风险。场景本地 Reader 默认禁用，MotionTestServices 供 CastTestController / CastDebugHUD / GyroscopeDebugHUD / GyroscopeHistoryGraphic / AttitudeCircleController 统一选择全局服务，仅无 AppRoot 时启用已序列化本地 Reader。
- AttitudeCircleController 原以单帧姿态自动设零，现读取 AttitudeCalibrationService.NeutralAttitude / IsCalibrated，无校准时自动启动同样的稳定采样；按 Calibrate 可重新校准，重复点击不会重启正在进行的采样。退出时取消本测试发起且仍在进行的校准，保留服务既有取消规则和原移动轴/限位/平滑参数。场景新增底部校准状态与 Calibrate，Main Menu 调整到同排。
- 无 AppRoot 的主菜单入口通过 SceneLoader 保存一次性测试目标、加载 BootStrap，AppRoot.Start 消费目标；普通 Bootstrap 仍进主菜单。现有 AppRoot 直接切测试，返回仍使用 MainMenu → FishingLoopTest，不自动开始新局。Gyro 测试与正式场景引用同一个 CastTuningProfile，未更改其参数。

## 2026-09-09 — 排行榜按钮样式遗漏

- 上一步仅迁移 Leaderboard 入口位置，保留了旧白底和默认字体，与设置按钮不一致。本次复制同面板 Tutorial 的视觉字段，保留 Leaderboard 对象 ID、布局槽位和导航引用。

## 2026-09-09 — 非玩法入口收进设置

- Strike Tuning 原由独立运行时 Canvas 创建常驻按钮，故会覆盖游戏画面；现删除该按钮及 Update 交互刷新，设置组件 OpenStrikeTuning 调用已有调参 Open。调参打开/关闭时同步切换整个 Canvas，原参数副本、Apply、Defaults 和暂停恢复保持。
- 原右上 LEADERBOARD 实际是 MenuCanvas 子对象，并非 GamePlayCanvas 内常驻按钮；现移动到 SettingsPanel 内，沿用原按钮/文本和导航事件。结算页 GameOverPanel 下另一个入口保留，仅结束显示。

## 2026-09-09 — 校准界面与成功自动关闭

- 复用 UI Parts 下 Point 过滤的 Map-PullOut Sprite（GUID 99c7c95e5fd647942a31e8dd2904364e、fileID 5410595409154769235）及 PressStart2P SDF 字体，不生成/修改美术素材。共享 MotionCalibrationPanel 原 GUID、组件和按钮事件保留，改为归一化锚点、Scale 1，增加羊皮纸、标题、说明、CancelLabel；旧 X 装饰停用，进度条改为不可操作的显示条。
- AttitudeCalibrationService 在写入 NeutralAttitude、Progress01=1、State=Calibrated 后发送 Completed。面板启用时订阅、关闭时解绑，只对本次 BeginCalibration 返回 true 的尝试自动关闭，避免打开已校准面板立即关闭。取消时仍调用既有 CancelCalibration，保留先前有效校准。开局守卫仍根据服务 IsCalibrated 继续转场。

## 2026-09-09 — Canvas 显隐单一管理入口

- 用户明确要求运行时初始状态独立于 Inspector 预览开关。新增 FishingSceneUIController，放在独立 SceneEntry 上，以 DefaultExecutionOrder(-500) 提前初始化，Entry 再显式调用幂等 Initialize。初始策略写在代码中，Inspector 只负责对象引用。显示 Canvas 时同时恢复 Canvas.enabled 与 CanvasGroup.alpha/interactable/blocksRaycasts。
- Entry 不再持有 menuCanvas/gameplayCanvas/menuFader 或初始隐藏列表，调用 FadeMenuForGameplay / ShowGameplay，仍负责相机等待和玩法启用。MenuSettingsPanel / TutorialPanelController 移除 Awake 初始关闭，各自保留交互职责。GameOver、校准、暂停等子功能继续按事件管理自身内容，不新增通用 UI 框架。
- FishingLoopTest 1、FishingLoopTest2 也引用同一 Entry 类型，已迁移原 UI 引用到管理组件，保留旧场景缺少菜单时直接开局及校准回退路径。未来新增菜单弹窗需登记到 Menu Overlays；本实现不按名字自动扫描未知 UI。

## 2026-09-09 — UI 初始状态与预览

- 当前场景保存的 GamePlayCanvas 为启用，菜单也为启用；未运行的 Game 预览因此会叠加 HUD 和菜单。截图占位文字与此一致，但不能单凭截图断定运行时初始化未执行。入口原本已有隐藏 HUD 的 Awake，只是放在玩法根初始化之后。
- 本次把 UI 初始显隐提前为独立 InitializeEntryUI，序列化 Initially Hidden Panels（TutorialRoot / SettingsPanel / GameOverPanel），两套校准面板也统一关闭；GamePlayCanvas 保存为关闭。保留菜单背景鱼运行、转场后开启 HUD 的设计。当前日志未提供能证明本次截图由运行时异常导致的证据，历史教程异常不作当前根因。

## 2026-09-09 — 设置选择面板

- 用户明确要求直接实施 Setting → Set Calibration / Tutorial。原 Setting 直接调用入口守卫 OpenSettings（实际打开校准）；现改接 MenuSettingsPanel.Open，复用现有校准组件和 TutorialPanelController。新面板与三个按钮序列化在 MenuCanvas 下，可直接编辑布局。
- 子面板打开时移到同级末尾，设置面板保留在其下方，因此关闭教程/校准后返回选择。TutorialRoot 新增透明全屏 Image 阻挡下层点击，保留页面与动画布局。菜单控制器继续挂在 MenuCanvas，Awake 关闭教程和设置，避免首次激活时自关闭。

## 2026-09-08 — 暂停按钮接线修正

- 原 PausePanelButton 的 OnClick 实际绑定 SceneNavigationButton.Navigate、targetScene MainMenu，因此点击会重载场景丢失本局。现移除该按钮的旧导航组件，接入 FishingPauseMenu.Open；运行时独立 Canvas 显示 PAUSED、RESUME、MAIN MENU，遮罩阻挡背后操作。
- 把调参面板已有的 12 个组件暂停快照集中到 FishingPauseController，保留原 enabled 和 Time.timeScale；仅持有暂停的面板可恢复，两个面板互斥，转场期间不接受暂停。EventSystem 与 UI 保持运行。返回菜单仍沿用现有 SceneLoader，不记录未完成局成绩。
- 按用户明确纠正实施，未改 Claude 设计文档。暂停不重置玩法状态、钩子或分数；既有输入 OnDisable 会取消尚未发出的蓄力，恢复后需重新蓄力。

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


## 2026-09-09 — 人物抛竿动画与鱼竿显隐

- 当前 PlayerAnimator 的 IsHoldingROd 与脚本 IsHoldingRod 大小写不一致；Cast → Hold 无条件且未开启 Exit Time，会立即跳过动画。已统一名称并设置 Exit Time=1。第二次抛竿症状仍需运行复验，不能仅由静态检查断言唯一原因。独立 FishingPole 显隐由人物表现脚本控制 SpriteRenderer，结合持竿参数与当前/过渡动画，避免动画结束前提前显示重复鱼竿。


## 2026-09-09 — 人物动画交接与鱼群设计边界

- HoldRod 只含一个 RodTip 位置关键帧；此前 Cast 末尾 y=3.05、Hold y=3.22，切换产生向上偏移。用户对齐后明确反馈正常，属于用户验证；未独立运行复测。
- 下一阶段需求及待决规则集中登记于 task_plan.md 最新交接，当前未实现鱼群新行为。捕食替换会影响唯一挂鱼引用、表现、基础分及收线结果，不能仅替换 Sprite；落点障碍检查必须先于 Baiting/竞赛启动。具体实现需以实际代码与 Collider 配置调查为准。
- 可复用候选：Sprite PPU/Pivot 一致性、序列帧附着点关键帧、玩法事件驱动表现和独立部件显隐。项目耦合为 Animator 名称及鱼竿素材；验证欠账为跨素材/第二用例、重入与中断；后续练习是在另一套人物素材上独立完成接入。
- Notion 个人技术资产库已创建：https://app.notion.com/p/3d6b8a4f3e3581cf8c8cca1bfd05bf36 ，已有 PPU 笔记、资产数据库和写作模板。当前项目记录仍负责实施证据；原型结束后再集中提炼，不把候选视为已掌握能力。


## 2026-09-09 — 合并后 Tutorial 入口布局

- 上次磁盘检查与合并前相同，刷新原因只是推测。本次用户保存后的场景明确显示 TutorialButton 锚点被设为中心 (0.5,0.5)，SizeDelta=100×100，导致入口缩小居中。已仅恢复该 RectTransform 的锚点 (0.2,0.62)～(0.8,0.695)、SizeDelta=0，保留新加第三页教程引用及其他用户修改。静态差异检查完成，未运行 Play Mode。


## 2026-09-09 — 生成避障及落点失败依据

- 本轮按推荐规则直接实现生成避障与落点失败：先选鱼品类，再按实例的 SpriteRenderer 和 Collider2D 世界包围盒加 0.05 单位边距检测生成区域和障碍，保留 0.75 中心间距、每鱼 50 次尝试，失败跳过该鱼并汇总告警。落水点命中 ReelingObstacle 时走统一扣钩失败路径，进入 PostAttemptCooldown，不进入 Baiting、不锁倍率；不检查飞行路径。
- 检测使用带标记的启用 Collider2D，显式包含 Trigger，不依赖新增 Layer。鱼包围盒不包含水花/涟漪 Renderer；按生成 BoxCollider2D 的局部坐标检查四角，支持偏移和旋转。保守包围盒不是像素轮廓，也不是游动路径避障。
- 场景 98 个障碍标记对应 97 个对象，其中 4 个原本缺 Collider（647159900、1450760607、1690327378、1903091375）。没有同 Sprite 的现成碰撞形状可复用，因此按各 Sprite 实际裁切矩形/PPU、Center Pivot 补齐 BoxCollider2D，沿用现有障碍 Trigger 配置；未改变美术位置或大小。现有重复标记不属于本次修改范围。


## 2026-09-09 — 排行榜流程与表现

- 用户明确要求将排行榜改为街机式结算流程：Game Over 展示本局分数，5 秒后自动进入 Leaderboard，点 Play Again 立即重开并取消自动跳转；Settings 和结算页的手动 Leaderboard 入口移除。
- 检查发现旧 GameOverPanel 只有 Leaderboard 按钮，没有 Play Again，因此复用该按钮并改接 GameOverPresentation.PlayAgain；禁用旧 LeaderboardSaveStatus，倒计时统一显示保存失败提示。移除 Settings 的按钮及其专属组件并重排剩余六项，避免空位。
- LocalLeaderboard 临时保存最近 sessionId/score/保存结果，跨场景展示使用，不改变 PlayerPrefs 持久化格式。相同分数按 sessionId 识别当前局，未入前十只显示分数及 OUTSIDE TOP 10，不高亮其他局。
- 最近会话临时状态在 SubsystemRegistration 清空，支持关闭域重载的 Editor Play。新结算组件只接正式 FishingLoopTest，旧实验场景没有批量迁移。


## 2026-09-09 — 排行榜入口范围更正

- 用户更正：Settings 中保留 Leaderboard 手动入口；只将 Game Over 的手动进榜改为自动等待进榜。已恢复 Settings 按钮、导航接线与七项布局，保留 5 秒自动跳转和 Play Again 取消逻辑。场景本地引用及唯一 ID 检查通过，未运行 Play Mode。此前“移除 Settings 入口”的记录已被本条取代。

## 2026-09-09 — 新手引导本地进度依据

- LocalPlayerProgress 使用 SevenSeas.TutorialCompleted / SevenSeas.GamesStarted 两个 PlayerPrefs key，每次更新 Save；不缓存第二份布尔值，不改排行榜数据。默认未完成、0次；已存在的玩家若此前没有此标记，也会首次进入教程。
- TutorialPanelController 区分手动回看与首次开局回调。完成前清理回调再进入开局，重复按钮不会重复启动；关闭回调释放 FishingSceneEntryGuard.isStarting，继续复用原校准流程。教程完成不等于实际开局，计数在过渡结束才记录。
- DeveloperPanel 使用作者可编辑的场景 UI 与像素字体、蓝色按钮；Developer Names 七项初始为空，显示编号占位。Settings 调整为八项布局；开发者面板由统一 UI 管理器初始化隐藏。
- 技术资产候选：本地引导进度与可重置调试入口。可迁移部分为完成标记/启动门控/取消语义；项目耦合为三页教程、校准与场景过渡。适用于单设备本地存档，清除应用/浏览器数据会重置，不识别跨设备真实玩家。验证欠账为真实 PlayerPrefs 持久化、移动端校准及第二用例；后续练习独立重建带取消与调试重置的引导。

## 2026-09-09 — 教程关闭即开始（取代 Start Fishing 按钮）

- 用户修正规则：首次教程已翻到最后一页后，点击现有 X 保存 TutorialCompleted 并直接继续校准/开场流程；移除独立 Start Fishing 按钮及其场景组件/引用。已看完后返回前页回看，再点击 X 也算完成。
- 未看完时 X 仍取消本次开始且不标完成；Settings 手动回看 X 只关闭。回调先清空再执行，重复点击不重复启动。
- 编译 0 errors、3 条既有警告；6 项隔离检查通过（提前关闭、重开第一页、到末页等待 X、X 完成一次、手动回看只关闭、看完返回前页后完成）。场景 fileID 完整无重复。未运行 Unity Play Mode/设备验证，未提交。
## 2026-09-09 — 现场确认追饵受阻

- Temp/SevenSeasFishDiagnostics.txt在15:16:13记录5条追饵鱼：3条Special及1条Large的转向或移动方框被obstacles3_0拦截；另1条Large的moveCircle=false，该项同时包含圆碰撞和生成区域限制，不能凭现有输出指定具体原因。鱼头距钩1.7774–2.9370，尚未达到咬钩距离。
- Editor日志对应同一落钩点(-1.03,12.93)、5候选、等待超时，随后收线触发障碍失败。当前Approaching受阻后反复尝试原直线路径，没有绕行；保守Sprite世界AABB仍可能误挡，需将实际形状与包围框区分验证。未更改追饵设计。
- Ambient Loop新增ambientFadeInSeconds默认1.5秒，以非缩放时间推进音量包络；暂停/静音清零，恢复重新渐入。实际听感待验收。

## 2026-09-09 — main 集成兼容发现

- 自动合并不保证运行兼容：main根Animator持续写m_Sprite会覆盖FishAppearance揭示图；最小修复只暂停对应Animator并恢复原启用状态，原生8项验证通过。三个鱼Variant同时保留动画引用及胶囊尺寸。字体main已有历史冲突标记，使用完整自洽字体版本修复。详细证据见docs/reference/2026-09-09-main-merge-report.md。


## 2026-09-09 — Map fix 与 main 场景文本冲突
- 本地 99ef369 与合入 84a9a75 的 FishingLoopTest 出现 15 处文本冲突。按共同祖先及 fileID 比较，无双方修改同一对象、无删除/修改冲突、无新增 ID 撞号。
- 本地修改22、新增26个序列化对象；合入修改58、新增125个。局部修复6段覆盖15处冲突，保留其他原文，最终606个对象逐块匹配预期三方结果，内部 fileID 无重复/缺失。
- 可复用排错候选：先按 Unity 对象身份比较三方，再局部解决文本冲突并检查引用；并非适用于双方修改同一对象的通用合并器，静态检查不代表运行验收。

## 2026-09-09 — FishingLoopTest接线核对

- 更正目标为main分支的FishingLoopTest，Level 1/2/3修改已撤回。正式场景两Spawner、移动Profile、Layer 6/mask64、生态和震动引用静态完整；需要Unity实际解析结果才能排除导入/内存状态问题。新增只读接线采集，不把静态存在GUID当作运行通过。


## 2026-09-09 — UI Select 悬停音效排查

- UI Select 听感排查：FishingLoopTest 的 Select/Confirm/Back 均引用 UI select & firm & back.mp3。解码显示时长0.444秒，包络有多段起伏；不能仅凭波形证明事件重复。UiAudioRouter 使用非循环 PlayOneShot 且合并同帧请求，未发现悬停循环播放代码。先将 Select 单独换为现有 Select 1.wav（一次快速衰减），保留其余音效和音量。运行事件计数及实际听感待验证。

## 2026-09-09 — UI hover 复现补充与修复（更正）

- 用户补充：同一按钮内移动时反复响，前条素材听感假设不足以解释；已恢复原 uiSelect 引用。当前 InputSystem 在命中背景与子文字切换时可能向按钮再次派发 Enter，原组件每次都 Queue；同帧合并无法消除跨帧重复。UiButtonAudioFeedback 以 pointerId 记录悬停，Exit 时检查当前命中是否仍属按钮层级，真实离开才清除；OnDisable 清空，指针 Select 不另播，键盘 Select 保留。

## 2026-09-09 — 随机地图展示时机与开场演出

- 当前随机选择在FishingMapSelector.Awake，碰撞准备后FishSpawner.Start生成；Entry菜单镜头仍看得到本局地图。用户指出这是展示流程冲突，决定引入独立空镜及Start后的地图巡览，不等同于要求改为多个Unity场景或延迟随机算法。
- 现有TransitionToOverview只切镜头并等待Cinemachine混合；BeginAfterTransition结束才启用Loop，Loop.Start才开始计时。新演出应保留此门控并接回旧流程，避免正常收线/失败ShowOverview被开场逻辑污染。
- 两图同显曾有现场日志证据：mapContainer空引用导致初始化直接退出；已补场景引用及共同父推导。测试必须覆盖引用缺失，并检查实际导入/运行，不只测有效配置。
- 技术资产候选：可取消的开场演出与玩法启动交接。当前仅需求/方案候选，尚未实现或验证；可迁移部分为阶段编排、输入计时门控、完成/取消一次性语义，项目耦合为Cinemachine三镜头、教程和手机校准。后续待实现后用独立小场景重建，不能宣称已掌握。
- 完整交接见docs/reference/2026-09-09-map-intro-camera-handoff.md。三份文档索引冲突状态仍保留，本轮只更新正文。

## 2026-09-09 — 菜单布景与可跳过镜头交接

- 用户最终选定独立菜单场地，用剩余随机地图及活动鱼群，不要人物/平台；取代先前“独立空海面”建议。实际由FishingIntroController在选图后、Spawner.Start前复制地图和水面视觉，给菜单鱼绑定独立移动矩形与无Loop订阅的Spawner。
- FishEcologyController使用全局FindObjectsByType；仅把鱼移远不足以构成长期隔离，因此交接完成时停用整个菜单场地，再释放正式玩法。物理布景按地图边界与镜头尺寸留出间距。
- Cinemachine退出镜头在混合期间不能立即重置；开场先对齐Overview位置/Lens，切换后保留退出姿态，原生检查未回跳。
- 两条异步准备链都需完成：镜头演出结束不代表菜单淡出完成。零时长测试必须等整个Entry交接，不能提前断言计时启动。
- 可复用候选与边界、调参和验证见docs/reference/2026-09-09-map-intro-camera-implementation.md；当前仅本项目已验证实现，尚未提炼独立资产或验证个人掌握程度。

## 2026-09-09 — 开场视觉未执行的实际原因

- 保存场景EntryGuard.intro再次为空，导致旧fallback直接切Overview；此前调整Survey Seconds/Bar Height并未参与这个调用链。通过同对象组件恢复和有组件但未准备好时阻止启动修复；验证必须故意清空引用再执行入口，不能仅测试有效配置。
- 独立IntroCamera已实现。Survey改为世界单位/秒匀速，避免SmoothStep下中段速度更快造成调参含义不直观。

## 2026-09-10 — Start透明留白误触与触屏双音

- Start动画100×160图片仅约X21..74/Y84..99可见，但Image以放大后的整张矩形射线检测。实际关闭过滤器可复现透明区命中；新增归一化ICanvasRaycastFilter限定可见按钮附近，保留布局/动画，不需要贴图可读。
- 触屏Enter与Button.onClick跨帧分别排队Select/Confirm，是按下/抬起双声来源；按ExtendedPointerEventData设备类型排除Touch悬停，保留鼠标和键盘反馈。
- 可迁移候选、当前美术耦合、边界、测试与后续练习见docs/reference/2026-09-10-mobile-menu-input-fix.md；未扩展为任意Sprite轮廓识别工具。

## 2026-09-10 — GameOver 无声原因

- 结束状态原先只停止音乐/环境/旧音，没有结束音入口；计时结束无AttemptFailed，所以完全没有提示。最后一钩则先StateChanged(GameOver)再AttemptFailed。
- 现在结束状态统一播一次独立Game Over Clip，结束后的普通失败音跳过，避免两种结算提示重叠。当前复用现有Attempt Failure.mp3，后续可独立换素材；不新增声音资产。
- 排错经验候选：表现层应核对状态事件与结果事件的先后顺序，避免同一次结算双声。可迁移的是一次性反馈门控；本项目耦合为FishingLoopState与失败事件顺序。原生音源专项通过，设备听感和第二用例未验证，后续练习可重建计时/耗尽两条事件链解释去重。

## 2026-09-10 — 结算Inventory与设置外观

- 用户确认3秒GameOver→本局Inventory→1秒后显示Leaderboard/Play Again，取代5秒自动排行榜。鱼获沿用原CatchInventory，不从展示层生成计分。
- Developer截图内容未进入磁盘developerNames，原Refresh会以空数组覆盖成占位符；本次保存七人名单并按职位/姓名分行。
- 极深UI底色在Linear渲染下，即使alpha=0.98仍能明显透出浅色后层按钮；阅读面板改不透明，避免叠字。
- 配置、设计差异和验证边界见docs/reference/2026-09-10-results-settings-ui.md。继续复用现有UI组件，不新增通用皮肤框架。
- [2026-09-10 更正] Developer的双行富文本此前仅由Open刷新，Scene保存单行导致预览不一致。本次把七行富文本写入场景，并增加Refresh Credits Preview组件菜单供修改名单后同步；人员顺序仍待用户决定，未调整。
- [2026-09-10 排序落定] 替代前述“排序待决定”：用户批准程序、设计、美术、技术美术、制作人顺序；姓名采用Shuo Hong (Flynn)。本次仅修改场景名单及对应富文本，静态一致性核对通过。
- [2026-09-10] 用户明确Developer的Tutorial Completed是开发者隐藏开关；显示状态标签与说明不再适用。场景热区右下角6%×4%，Image透明但保留raycast，状态TMP禁用但引用保留，Refresh不会重新显示。
- [2026-09-10 最新名单决定] 取代此前程序首位：用户希望Production先、本人其次、美术后技术美术；未指定的设计署名保留末尾，编辑预览与运行数据已同步。
- [2026-09-10 名单单一来源] Developer Names为唯一维护入口；ExecuteAlways与延迟OnValidate生成编辑预览，Open复用同一格式函数。TMP场景文本仅为自动生成显示，不再手动同步。编辑预览不读取或修改游戏进度，修改/重排和Undo专项通过。
- [2026-09-10 教程入口更正] 用户强调游戏里应能找到并修改，不要全隐形。改为Developer面板右下角、Close右侧小号Tutorial文本，保持透明背景与无说明；点击仍调用原ToggleTutorialCompleted，组件右键入口只是补充。
- [2026-09-10 Developer视觉方向更正] 用户否定暗绿/土黄并同意标题隐藏教程入口。Developer单页改深海蓝、清白姓名、青蓝标题/按钮、蓝灰职位；收紧名单、隐藏Games Started与独立Tutorial标签，保留点击DEVELOPER标题切换。其他设置页面本轮未扩展换肤。
- [2026-09-10] 用户继续否定Settings旧绿色，主设置页配色同步Developer新版；其他子面板尚维持各自当前配色，不能宣称全部面板已改蓝。
- [2026-09-10 结束流程最新修订] 用户取消GameOver过渡页。GameOverPresentation.OnEnable同步OpenResults，删除Inventory Delay和倒计时逻辑，避免依靠下一帧或零秒延时切换；Actions Delay保留1秒。Inventory仍接收保存状态，结束音由原音频组件负责。
- [2026-09-10 Settings旧色复现] 当前磁盘Scene的Settings颜色重新变成绿底/米黄字和白色按钮Tint，确实与用户截图一致；不能断言具体是谁回写，可能是已打开的旧场景后来保存。新增MenuSettingsPanel Settings Colors作为颜色维护入口，编辑态延迟OnValidate/OnEnable与每次Open应用，旧Graphic颜色不再决定外观。
- [2026-09-10 Leaderboard返回入口] 用户澄清：只改Leaderboard的Main Menu按钮。新增一次性Developer菜单请求，SceneLoader仍加载MainMenu/FishingLoopTest并在入口Start打开Developer，Close自然露出菜单；不是修改Inventory导航。请求只在有效MainMenu加载时设置、消费后清空，普通导航重置；含无AppRoot直跑路径。

## 2026-09-10 — iOS WebGL 体感权限与姿态源修复

- 依据 `docs/ios-motion-implementation-plan.md` 实施必需项 1–5；调查背景见 `docs/ios-motion-itch-findings.md`。motion/orientation 独立解析，兼容旧结果；orientation 拒绝不再覆盖 motion 成功。
- `AttitudeReader` 保留公开接口和 Editor remote 查找，加入 Gravity/Accelerometer、0.12 低通、非有限/零向量过滤、1 秒无有效样本重选。Gravity 断流先尝试 Accelerometer，避免两个静默设备交替阻塞最后的兜底；健康源不切换。姿态仍用 FromToRotation(gravity, down)，未猜测或改动 Inspector 方向。
- 入口依据 MotionState 与实际样本决定触屏兜底；保留 Start 同步授权调用及 3 秒样本等待。校准面板在 motion granted 时显示正在连接。
- 可选项 6 仅预留 iframe 检测和 OpenTopLevel 桥接接口；本轮未新增 WebTopLevelLink 或设置按钮，未改场景。
- 可复用资产候选（未独立验收）：浏览器权限分通道追踪 + 有效样本驱动的选源。原问题为 iframe 限制一个 API 导致可用通道被误判；可迁移部分为权限解析、数值守卫和重试。项目耦合为 Unity Input System、静态 WebMotionPermission 与校准流程；边界为只合成倾斜，不保证 yaw/GyroscopeAxis.Z。验证欠账为 iOS/Android 真机、浏览器挂起恢复及第二项目接入；后续练习为解释权限与样本就绪的区别并在最小项目重建拒绝/断流恢复。

## 2026-09-11 — 触屏兜底惰性创建与传感器退避

- 依据 `docs/touch-fallback-bug.md`，先在未修复 UI 上加早退错误日志并强制 Editor TouchFallbackActive。实际 FishingLoopTest 菜单阶段三个按钮未创建，ShowGameplay 后三者均已创建/active，未触发早退错误。因此本机未确证文档的 Awake 时序根因，不能宣称复现了“永远不创建”；证据 `D:/UnityProject/Builds/touch-fallback-before.txt` / `.log`。
- 同时用上一版实际 WebGL 测试包、本地 Chromium、375×812 Android UA、强制两个权限 denied 对照。Start 后三按钮可见（`D:/UnityProject/Builds/TouchFallbackBrowserChecks/before-game.png`）。此结果不否定文档记录的 itch 环境问题，也不证明 iPhone 行为。
- 按方案消除脆弱点：ReelingButtonHUD 在兜底需要时才创建，搜索 inactive Canvas；创建失败只警告一次，保留下一帧重试；兜底刷新独立于普通加速按钮引用的早退。未修改位置、尺寸或场景，因为未获得布局异常证据。
- AttitudeReader 连续三次无样本连接尝试后改为5秒重试；有效样本重置计数和正常等待，权限结果变化立即重新评估；只在成功读到新源样本时记录源日志，连接异常只警告一次。不会因为 orientation 被拒而绕过退避每帧重选。
- Unity渲染后的15项UI专项通过：三按钮可见/命中、不重复创建、左右按住/松开/移出、状态门控、缺Canvas重试及inactive父链。最初同帧命中测试失败，等待渲染帧后通过，未据此误改布局。
- 隔离Unity 25项模拟传感器专项通过（包含三次失败退避、等待期间不重选、静默连接不记录成功源）；模拟断流显式清理旧的当前帧标记，避免把测试中残留样本当新样本。此类模拟不代表设备事件投递通过。
- 最终浏览器补充：惰性创建不是充分修复。默认D3D11与SwiftShader均见间歇缺帧，三按钮保持active=true、layer=5、culled=false、alpha=1，触摸功能正常；禁用测试页面UI深度测试、保留绘图缓冲均未解决。仍需渲染批次定位/真机对照；不升级为已验证资产或全平台修复。OnEnable即时刷新和UI层继承已补上，生产权限请求逻辑未新增强制开关。

## 2026-09-11 — 手动触屏入口
- 用户要求明确的开启方式，新增 Settings 的 TOUCH CONTROLS: ON/AUTO 和 Router 的 Force Touch Controls。强制选择与自动兜底分开存储；AUTO 不撤销已判定的设备兜底，避免传感器不可用时失去输入。Editor 也能选择移动触屏输入，便于直接验证。
- 提竿回调可同步打开收线输入，必须先记录 accelerateHeld 再发提竿事件，否则释放门控会漏掉同一次按下。此为输入时序排错候选：可迁移的是同步事件前提交按住状态，耦合于项目 Strike/Reeling 门控；已有 Play Mode 针对性证据，尚未独立抽取/第二用例验证；后续练习为重建最小按钮事件与释放门控用例。

- [2026-09-11 设置布局] 手工调整锚点遗漏了名称为 LEADERBOARD 的按钮，且桌面隐藏 Tutorial 后留下空位。现改为一个 Vertical Layout Group 管理全部设置按钮，避免按名称后缀识别或手排位置；标题不参与布局。场景结构静态检查通过，运行显示待验收。
