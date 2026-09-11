## 2026-09-10 — 手机浏览器 WebGL 体感输入修复

- 按用户追加要求完成自动降级：Web 手机在 Motion 请求拒绝、不支持、超时，或授权后 3 秒仍无姿态样本时启用触屏控制并跳过体感校准/教程。运行时生成左、右、ACTION 三个按钮；ACTION 按玩法状态分别执行按住蓄力松开抛竿、点击提竿、按住加速。`ShoreLaneController` 统一消费语义 `MoveInput`，所以岸上选位和收线横移都能用左右按钮。
- PC 首次开始不再自动打开移动 Tutorial，Settings 中也隐藏 Tutorial 入口；能够使用体感的移动设备仍保留原教程与校准流程。Unity 6000.3.23f1 编辑器编译与 WebGL Player Build 成功。最终 itch 包为 `D:\Build__Game\SevenSeas_Web_2026-09-10-touchfallback-v2.zip`（19,964,082 bytes），ZIP 根目录直接包含 `index.html`、`Build/`、`TemplateData/`。尚待 itch.io 手机真机验证按钮位置、连续按压与 Motion 允许路径。
- 用户在 itch.io 手机浏览器实测发现游戏仍走 PC 输入、Gyro 无响应。根因是 `FishingInputRouter`、`FishingSceneEntryGuard` 与 `ShoreLaneController` 直接依赖 `Application.isMobilePlatform`；Unity 官方说明 WebAssembly 浏览器下该值可能因浏览器隐私策略返回 true 或 false，不能作为 Web 手机的唯一判据。
- 新增 `RuntimeInputPlatform`：原生 Android/iOS 保留 Unity 判据；WebGL Player 通过浏览器 `userAgentData.mobile`、移动 UA 与 iPad 桌面 UA 特征识别，结果按本次运行缓存。三个输入/入口调用点统一使用该判据，避免路由选 Mobile、入口却跳过校准的分裂状态。
- 新增 WebGL `.jslib` 与 `WebMotionPermission`：Start、菜单校准、Gyro/Attitude 测试按钮的用户点击中请求 `DeviceMotionEvent` / `DeviceOrientationEvent` 权限；无需显式授权的浏览器直接继续。传感器 Reader 原有每秒重连会在授权后取得 Input System 设备。校准面板区分等待授权、拒绝和浏览器不支持。
- 验证：包含新增源码的 Assembly-CSharp MSBuild 0 errors，只有项目既有 3 组 MSB3277 依赖警告；`.jslib` 语法检查通过，浏览器桥接 8/8 检查覆盖 Chrome mobile、Android UA、iPad desktop UA、桌面、隐式授权、显式允许/拒绝和不支持；另补 Android 平板 `userAgentData.mobile=false` 与 Windows 触屏边界 2/2。Unity 6000.3.23f1 WebGL Player Build 两次成功，最终 v2 确认 IL2CPP/WebAssembly 与 `.jslib` 实际链接通过；尚未重新上传 itch.io 或完成 Android/iOS 手机浏览器传感器验收。

## 2026-09-10 — 像素计时条与可拉出记分板

- 用户指定 `Fishing-UI_0011_Timer`、`Fishing-UI_0018_Score-Board-Default`、`Fishing-UI_0016_Score-Board-Pull-Out` 三张 UI Parts 做成一个 Timer bar 和一个 Score Board。逐像素读图确认：计时条 39x29，左边缘平切贴屏幕左缘，米色内腔 x[0,24) y[14,23)，右端金色怀表内圈 x[27,35) y[14,24)；记分板拉出图 61x22，右边缘平切贴屏幕右缘，米色文字区 x[20,59) y[3,21)；收起图 15x22 只剩鱼尾标签。三图 filterMode 已是 Point，spriteMode Multiple 各含一个 `_0` 子 Sprite。
- 新增 `TimerBarHUD`：只读 `SessionTimer`，用锚点驱动内腔填充比例（不做 Image.Filled，避免依赖内置九宫 Sprite），怀表盘内显示剩余整秒。低时间按缩放时间闪烁，暂停时一起停；检测到剩余时间被拉长时闪一次加时色。为此给 `SessionTimer` 加只读 `SessionDuration`，规则判定仍只在计时器内部。
- 用户追加要求：点击可收放，分数自动弹出保留。因此把抽屉行为抽成 `PullOutPanel`（两个真实调用点，不是提前通用化）：贴左/右缘、`RectMask2D` 裁剪、点击 `Toggle`、`OpenTemporarily(hold)` 临时弹出。自动弹出期间点击视为「钉住」而非关闭，玩家细看时不会被收走；`Close()` 同时清零停留时间，避免刚收回又弹出。`RectMask2D` 同时是 `ICanvasRaycastFilter`，收起时点击热区自然只剩露出的那一截。
- 计时条收起靠位移而非换图（只有一张原图）：左移 24px 只留 x[24,39)，怀表连读数完整可见。收起读数必须仍可读，所以读数不进 `fadeWithOpen`。默认 `startExpanded = true`。
- 新增 `ScoreBoardHUD`：订阅 `ScoreChanged`，加分时调 `OpenTemporarily`，文字按开合度淡入、单次放大回落。完全收起时 `PullOutPanel` 换回窄标签贴图，保证静止状态像素对齐。只读分数，不参与计分。
- 玩法输入不经 EventSystem（手机端抛竿是姿态手势，桌面端是 InputAction），因此把 HUD 图设为 raycastTarget 不会和抛竿抢输入。场景 `activeInputHandler: 1` 且已有 `InputSystemUIInputModule`，点击可用。
- 新增 Editor 工具 `Tools/Seven Seas/Build Fishing HUD (Timer + Score Board)`：按上述像素矩形在当前场景 Canvas 下生成 `TimerBar` / `ScoreBoard` 两个节点并回填引用，缺 EventSystem/GraphicRaycaster 时告警。同名节点替换，其余对象不动；不批量重写场景 YAML，也不改现有 `FishingSessionHUD`。
- [源码实现完成／Runtime 与 Editor 工程 MSBuild 通过 0 错误／场景未接线／Play Mode 未验证] 版面按 pixelScale=10、topMargin=40 离屏合成核对过：填充矩形正好落在木框米色内腔，文字框正好落在卷轴米色区，计时条收起构图按 cut x=20/22/24/26 比对后取 24。这些都是静态几何核对，不能替代 Play Mode。
- 下一步：在 Unity 里对 FishingLoopTest 运行该工具，Play 验证四件事——点击计时条收放、点击记分板收放、加分自动弹出后自动收回、自动弹出期间点击能钉住；再确认 1440x2304 下的大小与留白，决定是否关掉 `FishingSessionHUD.statusText` 里重复的 Score/Time 行；Level 1/2/3 三个场景需分别生成。

## 2026-09-09 — 暂停美术与全局UI音效

- 用户确认手工按钮音效测试通过，要求全部应用，并指出暂停风格不匹配。FishingPauseMenu复用排行榜羊皮纸Sprite与PressStart2P字体，棕金按钮、居中纵向卡片、安全区布局；保留Resume/Catches/音效/震动/返回菜单。关闭图标扩大透明点击区域，图标单独显示。仅新增三个场景资源引用，未改用户已配置音频素材和音量。
- 新UiAudioRouter/UiButtonAudioFeedback在场景加载时注册含inactive按钮，悬停/选中Select，点击Confirm，返回/关闭Back；动态暂停/库存/StrikeTuning按钮显式注册。移除暂停旧直接播放，兼容手动持久OnClick音效并去重，同帧点击优先于选择。
- UI短音源独立DontDestroyOnLoad，保留跨场景尾音与本次应用静音状态；BGM和环境音仍由原场景管理。FishingAudioFeedback公开PlayUi接口保留，配置仍在GameFeedback。直接冷启动无GameFeedback的Leaderboard未取得clip时安全静音，正常从游戏进入已配置。
- Runtime/Editor工程编译0错误，既有MSB3277警告；实际路由/按钮/FishingAudioFeedback配Unity事件及音源替身32断言通过。主场景local fileID无缺失/重复。
- 独立Unity6000.3.23f1 + uGUI2.0.0/项目字体素材原生Camera.Render检查480x800、480x960、800x480。关闭点击区调整后10/10检查通过，三尺寸无文字溢出/屏外元素；暂停/库存owner回路由真实Pause源码配明确业务替身检查。截图在系统Temp/SevenSeasNativePauseUI-0b2d8e8d7fdb4b84b27b66d4fc5cb13d-v2，源码hash一致。最小按钮高度竖屏48.74px、横屏33.12px，横屏偏小仍为限制。正式场景EventSystem操作、音频听感及刘海安全区/设备触摸仍待验收，不把原生视觉测试当设备测试。

## 2026-09-09 — Striking进入震动

- 用户要求新增进入Striking时震动；FishingHaptics订阅StateChanged，以CurrentAttemptId单独去重，保留原扣钩失败震动。沿用暂停菜单Vibration开关、暂停/失焦门控；禁用组件取消订阅，恢复不补播。此决定取代此前“首版仅失败震动”的范围。
- Runtime MSBuild通过，0错误、既有MSB3277警告；实际源码以UNITY_ANDROID编译配Handheld替身12断言通过，覆盖进入/重复/失败独立/开关/暂停/失焦/新竿/订阅生命周期。未改变场景接线；Editor和WebGL不调用设备震动，Android真机听感/震感待测试。

## 2026-09-09 — 胶囊绕行、逃离与捕食集成

- 沿用用户BasicFish/Variant横向胶囊配置。主代理统一共享接口、生成器与场景；fish_behavior实现实际胶囊几何/局部绕行，ambient_fade完成独立生态组件，fish_inventory在系统Temp独立Unity项目做原生验证。没有切分支、覆盖Prefab、提交或推送。
- FishNavigationGeometry以实际Collider处理生成/移动/旋转，单障碍左右两路点保持，卡转向有限前移退出，原运动边界及Baiting超时保留。FishSpawner改用相同形状检查。旧Sprite矩形不再直接阻挡胶囊鱼。
- 用户批准Small在Reeling钩3米内逃离，4米外恢复Idle，速度倍率1.5；Large/Special在挂钩Small的5米内每条每竿一次30%判定，转向完成且嘴接触/无遮挡后替换，每竿一次。捕食速度倍率1.5、接触容差0.1作为可调默认。GameOver/暂停/组件禁用、同竿重新启用均有门控。
- GameplayRoot/FishEcology已接loop与新FishEcologyProfile；loop引用同一Profile。TryReplaceHookedFish统一身份/VFX/张力并停用猎物，保留本竿倍率、张力和加速累计；得分/加时/库存仍只在最终上岸结算。没有中间捕食奖励。
- Runtime及Editor项目MSBuild通过，0错误，仅既有MSB3277依赖警告。场景local fileID无缺失/重复，新增Profile/脚本引用与GUID核对。修改脚本diff检查通过；未批量清理既有场景空字段尾随空格。
- Unity6000.3.23f1独立项目真实Physics2D几何9/9、真实PlayMode10/10：胶囊空角不误挡、真实重叠阻挡、长步薄墙/旋转扫掠、区域边界、Trigger开关恢复、直行/绕单岩/不可达停下、暂停/挂钩/禁用、停止范围内转向、目标销毁切换、Sprite嘴直行与绕岩后确实CanReachBait=true。逐帧ColliderDistance无穿障；4份源码SHA256与主仓一致。仅FishAppearance还原方法空替身，数学/物理/Controller Update是真Unity。
- 原生证据目录：C:/Users/73400/AppData/Local/Temp/SevenSeasNativePhysics-bd7a9e002df64fd4b2bf4389d23c3881，geometry-results.txt与playmode-results.txt。独立生态源码配导航/Physics替身37断言通过；实际协调器/结算源码配Unity与接触替身65断言通过，含身份替换、最终奖励、张力保持、重复/过时/暂停/归零/接触/朝向门控及新一竿重置。
- FishRuntimeDiagnostics改读实际导航状态，不再反射旧方框算法；Baiting/Reeling保存最近快照及最近60次历史至系统Temp/SevenSeasFishDiagnosticsHistory.txt。正式FishingLoopTest及Android/WebGL完整试玩仍待验收，不能以独立场景结果代替。多障碍组合、BigFishSpawner小活动区仍可能不可达。

## 2026-09-09 — 复现日志与环境音淡入

- 已读取15:16:13现场快照及Editor.log：5条BigFishSpawner鱼处于Approaching，timeScale=1、行为开关正常；4条方框检查被obstacles3_0拦截，另1条圆/区域检查失败，最终Baiting超时。确认此轮受移动/转向检查阻挡，尚不能断言碰撞体过大，也未实现绕行。
- subagent完成FishingAudioFeedback环境音淡入；主代理审查并将场景ambientFadeInSeconds接为1.5。首次启动、暂停/静音恢复渐入，重复抛竿不重启渐变。BGM及短音效逻辑保留。
- 当前运行时工程MSBuild通过，仅既有MSB3277警告；subagent实际源码配音源替身28项断言通过。脚本diff检查通过；场景全量diff检查仍报告已有空字段尾随空格，本次新增淡入字段无尾随空格，未批量清理场景。实际听感、Unity Play Mode淡入仍待用户验证；鱼群问题未标记修复。

## 2026-09-09 — 鱼群停摆：切换现场诊断

- 用户再次反馈鱼全停且不咬。最新Editor日志显示新一局先钓到LargeFish（25基础分），后一竿9候选超时；未见鱼脚本异常。不能从截图断言全局暂停或唯一避障原因，不再继续猜测改规则。
- 新增Editor专用FishRuntimeDiagnostics及meta：Baiting时每秒只读记录鱼状态、启用/配置/生成区域、头距与角差、转向/移动方框和圆检查、方框阻挡对象、鱼头到钩阻挡，写Temp/SevenSeasFishDiagnostics.txt。Tools/Seven Seas/Capture Fish Diagnostics可手动采集。不会修改鱼状态，未进入Player构建。
- Assembly-CSharp-Editor编译通过；尚无新的运行快照，已请用户重新进入Play并复现保持3秒。此轮只加诊断，不宣称集体停摆已经修好。当前场景已有用户新增BigFishSpawner（单独较小生成区），保留未修改，后续需由现场数据检查区域限制。

## 2026-09-09 — 大鱼转向避障误挡修复

- 用户反馈大鱼靠钩不咬。核对正式LargeFish_Variant（BasicFish派生）scale4.28、CircleCollider2D.radius0.19、33×13/100PPU剪影。此前把圆的世界AABB当作局部方框随鱼转动，额外扩大检查范围，存在大鱼误挡。截图不能确认唯一原因，已询问停住时朝向。
- FishController将圆形碰撞体独立记录，转向保留圆半径，仅非中心圆补中心弧线余量；移动用ReelingObstacle.BlocksCircleSweep检查圆的端点与完整扫掠。Sprite/其他形状仍按保守包围范围检测，真实障碍与水域边界保持，未改大鱼大小/半径/咬钩规则或场景。
- Assembly-CSharp编译通过；Temp/SevenSeasMouthBiteChecks原10项加3项回归共13项通过。使用实际大鱼数值构造旧方框误挡与新圆检查通过、真实圆阻挡仍有效；Unity/Physics使用替身，不等于现场Play验证。待原位置复测，保留进一步定位。

## 2026-09-09 — 用户确认鱼头咬钩后实施

- 修改FishController/FishBiteRaceController/ReelingObstable：由鱼头接近钩判定咬钩，追饵按头部位置停靠，保留鱼体避障；新增头到钩的阻挡查询，防隔岩石咬钩。没有改场景、Prefab、美术尺寸、咬钩半径或唯一赢家规则。
- Assembly-CSharp编译通过（既有MSB3277仍在）；Temp/SevenSeasMouthBiteChecks直接编译当前鱼行为/竞赛/Profile源码，10项针对性检查通过，Physics/数学用替身，不宣称Play/设备通过。真实岩石边缘不同鱼尺寸仍需试玩。未提交推送。

## 2026-09-09 — 岩石边缘追饵调查与鱼线显示修复

- 用户反馈钩落岩石附近鱼不过来、鱼线不显示。静态确认FishingLineView仅处理Casting/Reeling，落水进入Baiting及Striking即隐藏；已补两阶段连接hookRoot，继续保持Ready/GameOver隐藏。场景原引用完整，不改场景；编译通过、3条既有MSB3277，diff检查通过，尚未Play验证。
- 追饵存在规则矛盾：落点仅检查钩中心，鱼移动检查完整鱼体Bounds避障且竞赛要求鱼中心距钩<=0.25，故近岸落点可能合法但鱼不可达；当前直线受阻停住，无绕行。截图不能区分真实岩石阻挡与保守Bounds误阻，未宣称运行复现。
- 已询问用户是否采用鱼头接近钩即可咬钩（保留鱼体避障），或要求绕行。此规则尚待确认，未改鱼行为/咬钩范围；保留用户开屏设置、字体及其他未提交改动。

## 2026-09-09 — GUID导入错误修复与音频调整

- 用户截图报FishingLoopTest第3247行无法解析GUID。定位为本次新FishMovementProfile资产及场景引用写入33位GUID；之前只做存在/唯一性检查漏检长度，归因于Codex接线错误。已同步换为合法GUID eaa545d39a1f41d0a6cc7b2f88a2f508；全Assets的meta/unity/prefab/asset共4373项GUID格式检查通过，资源唯一、场景本地引用完整。Unity重新导入结果仍待确认。
- BGM改为场景界面启用时播放，无需Start/抛竿；海浪保留首次抛竿开始。每个短音效增加独立Level，CatchSuccess默认2倍；循环音量支持运行时Inspector调整。当前四个上传音频保留接线。Assembly-CSharp编译通过，3项既有MSB3277；新增10项真实音频源码/AudioSource替身检查通过，未实际试听/设备验证。

## 2026-09-09 — StrikeTuning / PauseUI / GameFeedback职责整理

- 按用户要求完成FishingLoopTest局部组织调整：原StrikeTuning仅保留调参；暂停控制/菜单、仓库数据/视图迁至PauseUI；音频和震动迁至GameFeedback。两个新根均启用，原组件fileID、音频配置及所有交叉引用保留。
- 本轮仅场景与开发记录修改，无游戏C#修改。修改前快照逐对象比对通过：六个迁移组件仅宿主改变，其余参数完整；无其他对象意外改动，1032个ID唯一/本地引用无缺失，diff --check通过。未进行本轮编译、Play或设备验证。
- 保留此前用户上传音频及全部未提交改动；用户希望自己学习配置，下一步在GameFeedback查看FishingAudioFeedback槽位与音量。不提交推送。

## 2026-09-09 — 接手交接分支并完成第一批集成

- 核实当前 `codex/parallel-development-handoff-20260909`、初始工作区干净，阅读AGENTS、三份进度记录及指定交接，未从旧main重建。三子代理并行调查后，用户确定仓库/震动/Idle/奖励规则，主代理统一结果事件与主场景。
- 实现本局逐条仓库、Android失败震动、局部Idle/转向追饵、上岸2/4/6/6秒无上限及加速张力0.8/1/1.3/1.3；Profile与现成仓库图接到主场景，保留既有美术尺寸、生成与落点规则。鱼逃离/捕食未实施。
- 音频范围用户要求包含循环音；扫描没有找到BGM/环境素材。已实现播放器/暂停/静音与暂停菜单入口，槽位留空等待素材映射。旧AudioPreview不再自动播放Confirm；没有声称试听或声音交付完成。
- 当前源码集中编译成功，使用Temp的CustomAfterMicrosoftCommonTargets纳入新脚本，未修改生成csproj。仍有既有MSB3277依赖版本警告。主场景1028个对象ID唯一、本地fileID无缺失，git diff --check通过。
- 奖励33项隔离检查：Temp/SevenSeasCatchRewardChecks；共享结算31项：Temp/SevenSeasSettlementChecks。实际编译源码配Unity替身，覆盖一次成功/失败、最后钩、空钩、快照/入库、加时归零、实际横移和震动门控。不是Play/Physics/触觉验证。
- 音频真实源码/AudioSource替身9项检查通过（Temp/SevenSeasAudioChecks-2248877cee1d4669b7a05b58d00a57e2），验证无clip、首次启动、暂停恢复、静音不补播、最后钩反馈与清理；不代表真实DSP/听感。最终主场景引用检查仍为1028个ID、无重复/本地引用缺失，8个新接入脚本/资产GUID唯一。
- 鱼行为真实源码/Unity数学及Physics替身14项通过（Temp/SevenSeasFishMovementCheck20260909）：10000次Idle目标不出出生圈、圈外渐进返回、转向/抢钩门控、饵移动重检、暂停/结束、挂钩旋转/跟随、取消及单赢家。注入blocked只验证受阻分支，不是真实Physics通过。四组共87项隔离断言通过。
- 尚未运行Unity Play Mode、Android或WebGL；鱼群运动与仓库实际布局需试玩，音频素材和转向视觉答复待续。未提交推送、未合并远端main、未升级引擎/包。

## 2026-09-09 — 文档同步与下一阶段交接（此前）

用户确认教程看完点击 X 后开始可用（用户试玩反馈，平台范围未说明）。本轮仅更新开发记录与新增并行开发交接，未实施震动、仓库、后续鱼行为、鱼获时间/大小张力、音频功能；未启动子代理。将当前工作区既有源码、场景、用户字体/教程动画/HoldRod/Close 素材一并纳入获授权的 GitHub 进度快照。

交接文档：docs/reference/2026-09-09-parallel-development-handoff.md。列出五项待决设计、共享上岸结果数据、最多主代理+3子代理时的分波次安排、独占文件、集成验收与下一聊天启动语。保留旧记录，不将静态证据升级为设备通过。推送结果以本次交付与 git log 为准。

# Fishing Loop Progress

## 2026-09-09 — 长期技术资产积累约定

- 用户希望开发伴随积累，Prototype 结束后由 Codex 引导整理个人技术栈，并要求本人理解、重建与迁移。已写入项目 AGENTS.md 长期约定、task_plan.md 后续阶段及 findings.md 候选台账，涵盖模块、算法和施工/验证流程。
- 本轮仅改文档，未改代码/场景，也未修改个人级 AGENTS.md；不立即启动通用化，不打断人物动画状态机教学。候选不是已验收通用资产，设备及运行验证欠账继续保留。

## 2026-09-09 — 鱼线复习需求已登记（未启动）

- 用户反馈鱼线效果不错，希望之后从零亲手逐步实现。已写入 task_plan.md 的后续复习计划：LineRenderer 基础、动态端点、坐标、状态切换、LateUpdate 时序、接线与验证。当前继续开发，不开始复习，不替换已接受实现。仅更新开发记录，未修改代码、场景或资产；用户体验认可不替代尚未完成的平台回归。

## 2026-09-09 — Casting / Reeling 鱼线已接入

- 新增 FishingLineView 和场景 FishingLine 对象/LineRenderer，人物连到当前飞钩或挂鱼，挂鱼对象每帧动态读取，原鱼尺寸、美术和移动流程保持。首次只做直线，无绳索物理；暂停时保留静止连线，结束时随状态隐藏。
- 命令行编译 0 errors、3 条已有 MSB3277 warnings；场景本地引用/ID 完整性和端点接线检查通过。未运行 Play Mode / Android / WebGL；需观察相机远近下粗细、竿尖起点对齐，以及 Casting 高度、Reeling 空钩/挂鱼和失败后显隐。

## 2026-09-09 — Strike 手势与冷却只读分析

- 已核对正式场景两个 Detector 的 Profile/展示时长覆盖，追踪 Mobile → Router → Strike 判定与三个冷却范围。确认第一次有效原始角速度越阈是当帧事件，无等待采样；抛竿实际等 0.35 秒采样。判定冷却手机路径同样经过，没有源码证据支持绕过。
- 已记录潜在原因：冷却吞事件仍消耗检测状态、回落重置条件、Update 时序与缺少端到端时间戳；运行参数需区分资产与当前快照。未改代码/场景/Profile，未运行手机复现。建议下一步先加检测/判定/拒绝原因时间戳，确认后再讨论独立 Strike 检测器与反馈调整。

## 2026-09-09 — 测试关卡入口与旧代码适配

- Setting 七项布局新增 Gyro Test / Attitude Test，同组按钮样式及场景导航。修复测试 Reader 与 AppRoot 服务重复管理设备的风险；抛投测试/HUD/曲线用同一 Reader，Attitude 采用共享稳定采样校准并显示状态，保留原测试运动逻辑和曲线。
- 编译 0 errors、3 条已有 MSB3277 warnings；三个场景本地引用和 ID 检查通过。独立检查引用真实 MotionTestServices / SceneLoader 源码，覆盖两条 Bootstrap 目标、一次性消费、已有 AppRoot 直达、MainMenu/Play Again 回归、共享 Reader 不启用本地 Reader、直接测试回退及本地校准单实例，全部通过。替身检查不包含真实 Unity 生命周期/传感器。
- 未运行 Unity Play Mode / Android / WebGL。待验证 Settings 进出两关、Unity Remote / 手机角速度与姿态数据、Attitude 校准与重复校准、退出测试后正式游戏体感仍正常；无传感器平台显示离线/等待，未添加模拟数据。

## 2026-09-09 — 统一设置中的 Leaderboard 外观

- 根据用户截图将白底普通字体改为同组蓝底、白色像素字体和相同字号/边距/交互色。仅修改场景视觉字段，本地引用检查通过，导航事件保留；未运行 Play Mode。

## 2026-09-09 — Strike Tuning / Leaderboard 入口归入 Setting

- 新增设置内 Strike Tuning 按钮并接线，移除运行时顶部调参入口；关闭调参回到设置。主菜单独立右上 Leaderboard 移入设置，布局调整为五项，结算排行榜入口保留。未修改张力条、美术或用户最新 HUD 调整。
- 命令行编译 0 errors、3 条已有 MSB3277 warnings；场景引用/ID 与五项按钮布局静态核对通过。未运行 Play Mode；待验证 Setting 内两个入口、关闭调参恢复、Start 后无常驻按钮以及 GameOver 查看排行榜。

## 2026-09-09 — 校准面板风格统一与自动关闭

- 更新共享 MotionCalibrationPanel：羊皮纸底板、深棕像素文字、金色 SET / 进度条、CANCEL；说明和传感器不可用提示精简。保持菜单及游戏内两套实例引用与原有校准规则。成功由服务 Completed 事件通知面板自动关闭。
- 项目编译 0 errors、3 条已有 MSB3277 warnings；Prefab 本地引用/ID 检查通过。独立检查引用实际面板代码、使用服务/UI 替身，覆盖已有校准打开、无关完成、成功自动关闭、取消保留旧校准、启动失败、重复尝试、无服务，全部通过。
- 未执行 Unity Play Mode 视觉验收或 Android 传感器实测；需验证竖屏文字/布局、实际握稳完成后自动返回设置，以及强制开局校准完成后转场。

## 2026-09-09 — UI 启动策略提升到管理层

- 新增 FishingSceneUIController 并挂到 SceneEntry，统一初始化/渐隐转场/正式游戏 Canvas 显隐；原分散初始化迁移，三处场景接线同步。编辑器中临时开启教程、设置或 HUD，或关闭主菜单，不再决定已登记 UI 的运行时初始状态。
- 项目编译 0 errors、3 条已有 MSB3277 warnings；三场景 ID / 本地引用检查通过。临时独立 C# 检查引用实际管理源文件，覆盖 128 种初始开关组合、重复初始化不重置已打开面板、转场及正式游戏显隐、Canvas/CanvasGroup 恢复，全部通过；使用 Unity 类型替身，不等于 Unity 生命周期验证。
- 未运行 Play Mode / Android / WebGL。下一步在编辑器故意打开 HUD 与弹窗、关闭 MenuCanvas 后进入 Play，验证仅菜单显示，再测 Setting、教程返回、Start 并行转场及 HUD 出现。

## 2026-09-09 — 统一菜单入口 UI 初始状态

- 用户反馈菜单与游戏 HUD 混杂。只读核对发现 GamePlayCanvas 默认启用，入口已有运行时隐藏代码；修正场景默认状态，并把 UI 显隐初始化提前到玩法初始化之前，集中配置初始隐藏面板。
- 编译 0 errors、3 条已有 MSB3277 warnings；新增场景引用与 ID 完整性检查通过。未运行 Play Mode，不宣称已复现或修复截图中的运行时问题；需确认截图拍摄模式，并验证进入 Play 仅主菜单、Start 转场后仅 HUD、Settings/Tutorial/Calibration 默认关闭。

## 2026-09-09 — 设置面板与教程接线

- 新增 MenuSettingsPanel，创建 SettingsPanel 和 Set Calibration / Tutorial / Close，改接原 Setting 按钮；复用已存在的校准与教程面板，不更改启动、校准规则、教程素材与翻页按钮布局。教程每次从第一页打开，首尾页禁用对应导航。
- 编译 0 errors，3 条已有 MSB3277 warnings；场景 ID 唯一性、本地引用、三个设置按钮事件与教程组件引用静态检查通过。未运行 Unity Play Mode / Android / WebGL。下一步验证 Setting → 教程翻页 → Close → 校准 → Close → 设置 Close → Start，校准设备行为需单独测试。

## 2026-09-08 — 暂停按钮改为真正暂停

- 已实现 FishingPauseController / FishingPauseMenu，并改好现有 PausePanelButton 的场景事件；保留原按钮位置、美术与其他菜单配置。暂停面板提供继续和独立返回菜单；调参共用暂停控制，避免相互误恢复。
- 命令行编译通过：0 errors、3 条已有 MSB3277 warnings。场景所有本地引用及 ID 唯一性检查通过；已核对暂停组件列表和按钮事件，不再直接调用导航。
- 未执行 Unity Play Mode / Android / WebGL。下一步运行中分别在抛竿、提竿判定、收线暂停，确认计时/移动冻结、继续保留进度、暂停 UI 不触发玩法，最后验证返回菜单及再次 Start。

## 2026-09-08 — 游戏内提竿参数面板已实现

- 用户要求 StrikeWindowProfile 可在游戏内调整。接入 STRIKE TUNING 入口与 8 个滑条，Apply、Defaults、Close；参数暂存运行时副本，不写回资产，下次提竿判定生效。当前 Profile 保存值保持不变。
- 调参面板打开时暂停局内时间和玩法/输入，关闭恢复；开局转场期间入口禁用。震屏幅度与时长从本轮判定快照实际传入相机，完成此前未消费字段的接入。
- 编译 0 errors、3 条已有 MSB3277 warnings；12 个暂停组件、入口守卫与场景本地引用静态核对通过。未运行 Unity Play Mode / Android / WebGL，需验证滑条触控、Apply 后下轮生效、判定中途暂停恢复和震屏变化。运行时生成面板，未改已有菜单或 HUD 布局。

## 2026-09-08 — 修订开局激活时序

- 按用户要求把场景表现和正式玩法分开：进入场景时先禁用玩法协调器、岸边移动和手势/语义输入，再激活 GameplayRoot，鱼/角色/涟漪无需等待转场。渐隐与镜头转场结束后重新启用 Loop，开始计时与输入。
- 仅新增 Loop.PrepareForEntry、调整入口守卫并序列化已有 Loop 引用；不改计分、鱼逻辑、镜头/渐隐时长和菜单布局。旧“整根延迟激活”已被本记录取代。
- 编译 0 errors、3 条已有 MSB3277 warnings；源码核对初始语义输入关闭、玩法 Begin 由协调器触发、SessionTimer.Awake 不计时。未运行 Play Mode / Android / WebGL；下一步观察转场前鱼和角色已显示、无法操作且计时未走，转场后正常操作。

## 2026-09-08 — Start 菜单渐隐已接入

- 用户要求 UI 消失改为渐隐。新增 MenuCanvasFader，在现有 MenuCanvas 挂 CanvasGroup 并完成守卫引用；默认 0.35 秒，与既有镜头转场并行，渐隐开始停止菜单点击。
- 编译 0 errors、3 条已有 MSB3277 warnings；场景组件、引用和 ID 完整性检查通过。未运行 Play Mode 或设备验证，需观察退场观感；可在 MenuCanvas 的 Fade Out Duration 调整时间。未改变菜单布局和游戏 HUD 出现方式。

## 2026-09-08 — 菜单鼠标点击接线修复

- 用户反馈菜单按钮鼠标点击无反应。定位为上轮关闭 GamePlayCanvas 时同时关闭了其子对象 EventSystem；这是上轮入口显隐调整遗漏的依赖。
- 将现有 EventSystem 移至场景根，静态核对唯一性、对象及输入模块启用、父子引用和全场景 fileID 完整性通过。本轮未改 C#，未重复编译；未运行 Play Mode，需重新进入 Play 验证菜单点击和 Start 转场。

## 2026-09-08 — 新主菜单与空镜开局转场已接线

- 用户保存 MenuCanvas 后要求初始场景空镜、Start 后切到 Overview。已复用新 UI，接好 Start / Setting / Exit 和排行榜；旧 MainMenu 从正式路由与 Build 启用列表退出，文件保留。
- 新建 MenuCamera，位置 (0, 8, -10)、Size 9；配置到 Overview 的 1 秒 EaseInOut。菜单时玩法及游戏 HUD 停用，转场完成再启用；静态 MainLanscape 移至场景根以保持空镜背景。其他布局、鱼配置及原相机跟随行为保留。
- SceneLoader 区分同场景的 MainMenu / GameScene 进入意图，排行榜返回菜单与再玩一局分别生效；直接 Editor Play 支持菜单入口。移动端保留校准，Setting 暂复用已有校准面板；尚无额外设置系统。
- 编译 0 errors、3 条已有 MSB3277 warnings。保存场景 fileID 无重复/悬空；初始显隐、静态景物独立、Start Prefab 回调来源与实例覆盖、Setting/Exit 回调通过静态检查。对本轮开始时场景快照的比较确认未删除既有对象，仅局部接线和新增相机/入口。
- 未运行 Unity Play Mode / Android / WebGL；空镜构图、转场观感、计时延后、HUD、校准和导航回归待用户试玩。未提交、还原或清理已有改动。

## 2026-09-08 — 本地排行榜首版实现

- 用户要求创建本地排行榜系统/关卡。按独立排行榜页面、分数+日期默认方案实现 Top 10，GameOver 自动记录，主菜单和结束页入口，榜单返回菜单/再玩一局。询问过是否需可玩关卡或姓名，本轮尚未收到回复；本版不包含这两项。
- 新增 LeaderboardData、LocalLeaderboard、LeaderboardView、LeaderboardNavigationButton、LeaderboardSaveStatus 及 Leaderboard 场景；追加 SceneCatalog/Build Settings 登记。原玩法规则、鱼配置、计分公式及已有 UI 布局未改。
- 编译使用临时 MSBuild targets 纳入新增脚本，0 errors、3 条已有 MSB3277 warnings。独立 Tests/Leaderboard 检查通过 Top 10、排序、重复局、同分、零负分及异常数据；不读写真实存档。三场景本地 fileID 无悬空/重复，榜单 10 行 TMP 引用核对通过；本次场景与登记文件 diff check 通过。
- 未运行 Unity Play Mode、存档关闭重启读回、Android 或 WebGL；新场景外观及入口点击仍待运行验收。下一步 Unity 刷新后从主菜单及 GameOver 各进入一次，完成多局并重启确认榜单。没有提交、推送、还原或清理用户改动。

## 2026-09-08 — 鱼外观分类随机 Profile 已实现

- 用户接受首个固定外观效果并理解实现，要求扩展为四列表 Profile。已创建 FishAppearanceProfile 及资产，填充全部 29 张对应 Sprite，配置四种 Variant 类别。
- 首次成功提竿随机揭示所属类别的一张图，实例缓存结果；失败恢复剪影，再次揭示同图。列表空槽跳过，缺失 Profile / 空列表保留隐藏外观。未修改揭示时机、计分、鱼尺寸或碰撞。
- 命令行通过临时 MSBuild targets 纳入新增脚本，编译 0 errors、3 条已有 MSB3277 warnings；分类数量与 Sprite 引用、Variant 类别静态核对。随机分类版本尚未运行 Play Mode / Android / WebGL，不能沿用固定外观用户反馈作为新版运行通过。
- 下一步：Unity 刷新后观察四类随机外观、失败再次揭示及各素材显示比例。原设计文档未修改，未提交或清理已有改动。

## 2026-09-08 — 首个鱼样貌揭示通路已实现

- 用户要求开始实施。新增 FishAppearance，BasicFish 共享组件，SmallFish_Variant 配置固定 Small-1；提竿成功揭示，失败/计时结束通过 ResetToIdle 恢复隐藏外观，上岸停用清理显示。其他鱼类别暂不配置。
- 保留用户根尺寸、Collider、涟漪间隔、分值与场景/UI/美术配置。没有新增上岸等待、品种随机规则或复杂动画；未改写 Claude 的设计文档，本记录保存用户直接确定的新增规则与实现差异。
- 编译：使用临时 MSBuild targets 纳入尚未被 Unity 生成 csproj 收录的新脚本，0 errors、3 条已有 MSB3277 warnings。Prefab 组件、Sprite GUID/fileID 及正式场景生成器引用已静态核对；未运行 Unity Play Mode、Android 或 WebGL。
- 下一步：在正式场景试玩小鱼的提竿前/后外观、失败恢复、再次咬钩、计时结束及上岸结算，确认 Small-1 显示比例后扩展品种。仅预览两张 Small 候选，29 张素材适配未完成。未提交、还原或清理已有修改。

## 2026-09-08 — 鱼样貌揭示三条规则已确认

- 用户确定提竿成功时揭示真实样貌、此前沿用现有水下外观、揭示后失败恢复隐藏外观。已同步 task_plan.md 的规则、建议接入点与验收条件，以及 findings.md 的依据。
- 下一步在用户要求实施后，选择一条鱼及固定素材做最小验证，再扩展品种；29 张素材视觉适配尚未逐张确认，身份选择与额外品种数值规则仍未确定。
- 本轮仅修改三份现有记录，未改代码、场景、Prefab、美术或 Claude 主导设计文档；未运行编译、Play Mode 或设备测试，未提交、还原或清理现有修改。

## 2026-09-08 — 鱼真实样貌揭示计划记录

- 用户表示倍率与尺寸先保持现状，提出下一机制“吊起鱼后揭示真实样貌”，明确仅记计划。详细计划已直接合并进 task_plan.md，findings.md 记录调查依据；误建的独立计划文件已删除，交接链接已修正。记录素材清点、触发歧义、组件边界、小步路线和验收条件。
- 本轮仅更新文档；未修改代码、场景、Prefab 或美术。只读检查文件分类及相关状态节点，未运行编译/Play Mode；未修改 Claude 主导设计文档。

## 2026-09-08 — 飞行实时倍率表现与收线重置

- 按用户最终澄清，HUD 在 LateUpdate 将 Casting / Baiting / Striking / Reeling 的实际倍率直接用于数字与尺寸；飞行中连续增长，落水后锁定。1–1.5 蓝色，1.5–2 渐变金色，2–2.5 金色，2.5–3 渐变红色。
- ReadyToCast / GameOver 入口将 CurrentDistanceMultiplier 重置为 1，HUD 同时恢复原始尺寸和颜色，数字显示 x1.00。成功得分在状态切换之前完成，避免重置影响本次结算。取代之前只在落地放大的版本。
- 编译 0 errors、3 个已有 MSB3277 warnings；HUD diff check 通过。未运行 Play Mode，实时增长与结束重置待观察。

## 2026-09-08 — 倍率尺寸持续显示（取代短暂脉冲）

- 按用户修订，落水后文字 localScale = 原始缩放 × 锁定倍率，蓝金红颜色同时保留，在 Baiting / Striking / Reeling 持续显示；ReadyToCast / Casting / GameOver 或 HUD 禁用恢复原始大小和颜色。HUD 中途重新启用也恢复对应显示。移除脉冲倍数/时长字段及正式场景对应键。
- 编译 0 errors、3 个已有 MSB3277 warnings，HUD diff check 通过；未运行 Play Mode，3 倍尺寸布局及结束恢复待观察。

## 2026-09-08 — 三倍倍率、稀有度颜色与加速奖励

- 用户批准基础分×落水倍率＋实际向岸加速距离×单位奖励、合计后取整。ReelingController 测量移动前后到岸距离差，在断线检查后、完成事件前发布；Loop 仅在 Reeling 且挂鱼时累计，成功结算后退出，其他退出清空。初值 1 分/单位，可在 ScoreTuningProfile 调整，空钩无奖励。
- 最大倍率设为 3，保留现有满倍率距离 20；HUD 落水脉冲 1.65 倍/0.65 秒，低倍率蓝色、1.9 起金色、2.99 起红色。锁定颜色保留至下一次 Casting 恢复原色。参数已写入正式场景，未修改布局。
- 编译 0 errors、3 个已有 MSB3277 warnings；针对性检查奖励先累计后结算、退出重置与 Profile/HUD 引用。Play Mode、设备未运行，成功/失败/空钩/计时结束奖励及颜色观感待验证。

## 2026-09-08 — 落水倍率文字反馈

- FishingLoopController 在有效 Casting 落水、最终倍率写入后发布 DistanceMultiplierLocked；FishingSessionHUD 使用现有 loopController / multiplierText 接线，播放一次 0.4 秒、峰值 1.25 倍的缩放。保留原始缩放，结束/禁用/GameOver/新抛竿恢复。使用游戏时间，暂停时不推进。
- 编译 0 errors、3 个已有 MSB3277 warnings。场景引用已核对，无需新接线；diff check 仅报告 LoopController 原有两处 Header 行尾空白，本次未改这些行。Play Mode 视觉验收待用户观察。

## 2026-09-08 — SwimRipple 随机播放节奏

- 用户确认未上钩鱼已能播放；新要求为错开出现时间、小鱼更频繁、大鱼更稀疏。FishWaterVFX 每条鱼独立随机首播延迟，Animator 播完一轮后隐藏并随机等待；不受其他鱼 Hooked / Reeling 影响。继续复用子对象，无 Instantiate / Destroy。
- Small / Medium / Large Variant 的 swimRippleInterval 分别为 0.5–1.1 / 1.2–2.2 / 2.3–3.8 秒，表示每轮结束后的等待；Basic 与 Special 默认 1.2–2.2 秒。频率为试玩初值，未修改尺寸或动画速度。
- 编译 0 errors、3 个已有 MSB3277 warnings；代码 diff check 通过，Variant 字段目标核对通过。Play Mode 中节奏与暂停恢复待验证。

## 2026-09-08 — 未上钩鱼的常驻涟漪

- 用户确认相机返回问题已解决。
- SwimRipple 移除实际位移门槛，仅依据所属鱼 State != Hooked 显示，因此 Idle / Approaching 均播放，不受其他鱼咬钩后候选者 ResetToIdle 影响。HookedSplash 仍要求自身 Hooked、收线激活及实际位移。未增加鱼的游动行为。
- 编译 0 errors、3 个已有依赖 warnings；diff check 通过。涟漪显示待 Play Mode 确认。

## 2026-09-08 — 相机返回过渡修复（待运行验证）

- 录屏约 17.8 秒显示先跳到船边再缩放。FishingCameraController.ShowOverview 在 Dock 前暂存并解除 Follow，FollowHook 恢复目标。Cinemachine 会先刷新旧 live 相机，再建立冻结快照，停用对象不足以阻止该次跟随更新。
- dotnet build 成功：0 errors，3 个已有 MSB3277 warnings；代码 diff check 通过。未运行 Unity Play Mode。待验证失败返回、再次抛竿、最后一钩返回及过渡中再次抛竿。场景和 Blend 参数未修改。

## 2026-09-08 — 鱼身跟随 VFX 职责修正

- 按用户本步直接修改授权，FishWaterVFX 改为未上钩且实际移动时显示 SwimRipple；已上钩、ReelingController.IsActive 且实际移动时显示 HookedSplash。Striking/静止不显示，启用与禁用时清理两种效果显隐。
- BasicFish 已绑定现有 HookedSplash 嵌套子对象；四种鱼 Variant 均无相关引用/启用覆盖，可继承基础配置。保留用户尺寸、动画、颜色；常驻子对象复用，不恢复定时 Instantiate/Destroy。
- 检查：嵌套对象引用与 ID 唯一性通过，git diff --check 通过；dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet 成功，0 错误、3 条程序集版本冲突警告。未执行 Unity Play Mode 或设备验证，需观察游动涟漪、Striking 隐藏和收线水花。
- 旧场景的鱼 Prefab 引用迁移仍按用户中途取消保留未处理，旧 Prefab 未删除。

## 2026-09-08 — HookShadow 显隐接入

- 用户完成 Dock 图像偏移复位并确认；创建 HookShadow 后反馈腾空感“还可以”。此前“Dock 复位尚缺”的记录已过时。
- 按用户本步明确授权，Codex 在 FishingHookController 的 LateUpdate 提前返回之前加入投影显隐逻辑，仅 Flying 显示，并在 FishingLoopTest 绑定既有 HookShadow（GameObject fileID 15465561）。保留用户布局、颜色和其他逻辑。
- 静态确认引用指向正确子对象；未运行 Unity 编译或 Play Mode，待观察待抛隐藏、飞行显示、落水/取消后隐藏。git diff --check 报告该脚本已有的空白行尾空格，未为此改动用户其他内容。
- 此次直接实施授权限于投影显隐步骤；后续继续按用户指定协作方式推进。

## 2026-09-08 — 教学进度与透视探索延期

- 用户决定先在 Orthographic 正交场景完成各项功能；透视探索保留为延期项，恢复条件为用户重新提出并可复用现有美术，不预设新增素材或模型。
- 已同步 task_plan.md、findings.md、2026-09-07-progress-and-plan.md、2026-09-07-development-handoff.md 的最新入口，明确旧快照仅为历史。未改写 Claude 主导的设计文档，也未发送跨代理消息。
- 教学已推进 POWER 常显/保留数值、Slider 独立显隐、Accelerate 移动端输入门控、最终落水倍率锁定、成功上岸倍率结算和 ScoreTuningProfile 缺失检查。用户报告对应步骤完成；POWER 正常由用户明确确认，其他“好了”不扩大为全分支或平台通过。
- HookFlightProfile 已创建并被正式飞行消费；HookVisual 拆分和场景绑定已静态确认。Launch 已加入默认 1 的装备初速度倍率参数，恒重力解析计算飞行时间/射程和虚拟高度。旧距离范围不再生效，5～25 只是旧测试地图参考；装备系统和 PC 蓄力时长 Profile 迁移未完成。
- 用户明确反馈当前腾空不明显；已解释同轴投影原因并将“可见弧线完成”的暗示更正。正交内的投影/缩放表现尚待选定实施，不能标为完成。
- 本次读取发现 Dock() 尚未加入 hookVisual 局部位置复位，作为下一教学收尾项；未代写修复。Launch 的引用错误提示尚未提及 hookVisual，可在该局部收尾时一并校正。
- 此前按用户明确请求处理过场景冲突（保留双方对象改动，静态检查无悬空引用）及 FishingLoopController Region 分类（不改变逻辑）；不据此扩大为默认代写授权，也不声称已完成用户后续 Git 合并提交。
- 当前下一步：一个适量步骤补 Dock 显示复位，再继续正交腾空表现、倍率/落水反馈、奖励、鱼生成/行为/动画和交付。已完成的 Profile、UI 和层级不重建。
- 验证边界：此次文档整理没有修改游戏代码、场景、Prefab 或 Profile，没有运行编译、Play Mode、Android、WebGL 或系统回归，没有提交或推送。

## 2026-08-29

- Read `docs/design/2026-08-29-fishing-loop-design.md` in full, starting with its Process note.
- Read `docs/design/2026-08-29-fishing-loop-teaching-plan.md` in full.
- Read `docs/reference/motion-input-guide.md` in full, including the sections reserved for later M2 and M5 teaching.
- Confirmed the non-negotiable coaching boundary: Codex will not edit C# files or Unity scenes; the student types and wires everything.
- Started M0 and asked the orientation-versus-angular-velocity thinking prompt.
- Paused M0 before explanation when the student noticed Step 0 had not yet been performed.
- Repurposed `task_plan.md`, `findings.md`, and `progress.md` from the old gyroscope-HUD task into living fishing-loop records.
- Marked Step 0 complete and M0 in progress, awaiting the student's own answer to the thinking prompt.
- Completed M0 after the student correctly distinguished persistent attitude from angular velocity that returns to zero when rotation stops.
- Clarified the data direction (`sensor -> reader -> controller -> view`) and that the existing attitude prototype maps tilt to a target position rather than unbounded movement.
- Started M1 at its think-first stage; no implementation instructions or code have been given yet.
- M1 think-first attempt completed: the student proposed dispatching `Update()` through a `switch` on the current state.
- Read the existing `CastGestureDetector` and `CastTestController` as architecture precedent without modifying either file.
- Began M1 architecture discussion; implementation has not started.
- Completed the M1 architecture discussion: selected enum + switch with explicit state lifecycle methods and small delegated components, while preserving a mechanical extraction path to State Pattern if evidence justifies it.
- Created and indexed the Obsidian note `游戏开发-Unity-State Pattern与可演进状态机架构.md`; verified its metadata, related-note links, and both Unity/root index entries.
- Inspected existing enum placement conventions. Component-specific enums live beside their controller, so the fishing-loop enum will begin in `FishingLoopController.cs`.
- M1 implementation is ready to begin with the student typing the six-state enum; Codex has not edited any C# or scene file.
- Student created `FishingLoopController.cs` and correctly typed all six `FishingLoopState` values.
- Read-only inspection confirmed chat formatting artifacts were not present in the actual C# file.
- Treated the student's request for the next step as confirmation that Unity compiled the enum without red Console errors.
- Student completed the enum + switch controller skeleton with state-specific Enter/Update/Exit hooks and centralized transitions.
- `Assembly-CSharp.csproj` compiled with 0 errors; unrelated Unity package reference conflicts produced 3 warnings.
- Confirmed the script initially had no scene or prefab reference; the student then attached it in Unity for Play Mode verification.
- Console verification passed with ordered transitions from `ReadyToCast` through `GameOver`, one second apart, and no transition after `GameOver`.
- M1 has one cleanup step remaining: remove the temporary timed auto-advance harness before beginning M2.
- Student removed the temporary timed auto-advance fields, checks, and method after learning how `Time.unscaledTime` works.
- Verified the debug harness is absent, `Start()` only enters `ReadyToCast`, and `Assembly-CSharp.csproj` builds with 0 errors.
- Marked M1 complete and started M2 at the required attitude-code deep-dive stage.
- Completed the M2 conceptual checkpoint on dead zone, axis-lock hysteresis, and frame-rate-independent exponential smoothing.
- At the student's explicit request, deferred the `AttitudeControlTest.unity` phone/HUD observation until M2's control-tuning/debug pass; it remains an open M2 verification item.
- Advanced to M2's one-axis shore-lane controller thinking prompt before architecture or implementation guidance.
- The student reconsidered the deferral and resumed the planned `AttitudeControlTest.unity` Play Mode observation before lane-controller design.
- Play Mode observations confirmed dead-zone filtering, axis-switch hysteresis, and the visible response difference between low and high smoothing values.
- The student updated `GyroscopeDebugHUD` labels to show the device-axis/rotation-name mapping (`X-PITCH`, `Y-YAW`, `Z-ROLL`) in both online and offline display paths.
- Read-only inspection found the temporary test values persisted in the scene (`axisSwitchHysteresisDegrees = 10`, `smoothing = 30`), so restoration to 2 and 12 remains the immediate cleanup step.
- The student explicitly chose to retain hysteresis 10 and smoothing 30 as preferred tuning values; the M2 attitude deep dive and Play Mode observation checkpoint are complete.
- Started the one-axis shore-lane controller think-first stage.
- Completed the M2 lane think-first and architecture discussion. Selected a new `ShoreLaneController` plus a combined lane-X/power snapshot at the transition to `Casting`, without changing the generic cast detector event.
- Began learner-written implementation of the lane controller.

## Verification log

- Verified `task_plan.md` contains Step 0 plus M0–M8 with M0 in progress and M1–M8 not started.
- Verified no stale August 27, gyroscope-driven Circle, or Scene1 task markers remain in the three planning files.
- Verified `git diff --check` reports no whitespace errors in the three planning files.

## 2026-08-30

- Continued learner-written implementation of `ShoreLaneController` variant A and worked through the quaternion-relative-attitude, rotated-forward-vector, supported-angle guard, normalization, range mapping, and exponential-smoothing math.
- Changed calibration UX by student request: calibration will be an explicit player action via UI rather than an implicit delayed `OnEnable()` capture; the calibrated neutral pose persists across fishing-state re-enables and can be replaced by a later recalibration action.
- Added an observable supported-tilt flag so the future UI can explain when extreme orientation pauses lane input instead of failing silently.
- Added a planned variant B and A/B test: compare absolute position targeting against tilt-controlled velocity before choosing the final M2 lane behavior.
- Read-only reviewed the learner-written `ShoreLaneController` variant A and built `Assembly-CSharp.csproj`: 0 errors and 4 warnings (three existing Unity/package reference conflicts plus one deprecated `FindObjectOfType<T>()` call in the new controller).
- Confirmed the movement algorithm is present, but manual calibration is currently unreachable because `Calibrate()` is private and has no caller; the script is also not yet referenced by any scene or prefab.
- Student expanded calibration UX to two entry points: main menu and pause menu. Calibration now requires a three-second stable hold and an averaged neutral attitude rather than a one-frame snapshot.
- Confirmed lifecycle choices: calibration persists across scenes and new attempts for the current application run, resets on application restart, and restarts its countdown with `Hold still` feedback if movement exceeds tolerance.
- Identified the existing persistent `AppRoot` as the composition root for `AttitudeReader` and a focused motion-calibration service. `ShoreLaneController` will consume shared calibration rather than own it.
- Student implemented `AttitudeCalibrationService`: three-second unscaled-time hold, fixed-rate sample collection, movement-triggered restart, hemisphere-aligned quaternion component averaging, normalization, cancellation, and session-lifetime neutral attitude.
- Added persistent `AttitudeReader` and `AttitudeCalibrationService` components to `AppRoot`; verified they survive the Bootstrap-to-MainMenu scene transition without duplication.
- Student created a reusable `MotionCalibrationPanel` prefab with progress/status UI, manual calibrate/recalibrate, open/close controls, and a MainMenu entry button.
- Student created `FishingLoopTest.unity`, mapped `GameScene` to it, built the placeholder shore lane, attached the fishing loop and lane controller, and replaced the lane controller's local calibration state with the shared service.
- Student implemented and wired `FishingSceneEntryGuard`, keeping calibration entry outside the six-state fishing loop and preventing `GameplayRoot` from starting until requirements are satisfied.
- Runtime verification passed for the uncalibrated entry path: Play loaded `FishingLoopTest`, the mandatory panel opened with gameplay disabled, three-second calibration completed, the panel closed, gameplay activated, `ReadyToCast` began, and tilt movement worked smoothly.
- Student tuned the calibration movement tolerance from 3 degrees to 8 degrees after device testing; the three-second hold now feels reliable and was accepted as the current value.
- Runtime verification also passed for the pre-calibrated entry path: calibration performed in MainMenu survived the scene transition, the mandatory panel stayed closed, and lane control used the shared neutral attitude.
- The student pushed the completed calibration and lane-controller work to the remote repository.
- At the student's request, lane variant B and its A/B feel comparison were deferred until a later tuning pass without removing them from M2.
- Advanced to the next M2 slice: connect the existing gyroscope flick detector to `ReadyToCast`, capture lane X and cast power, then enter `Casting`.
- Added a persistent `GyroscopeReader` to `AppRoot` and runtime-injected it into the fishing-scene `CastGestureDetector` while keeping its tuning profile as scene-assigned data.
- Student subscribed `FishingLoopController` to `CastDetected`, captured `CurrentLaneX` plus cast power, transitioned to `Casting`, and disabled the detector outside `ReadyToCast`.
- Installed and configured Unity Android Logcat, filtered to package `com.DefaultCompany.urp_2d`, and observed the device log `Cast selected: lane X = -0.37, power = 1.00`.
- Combined-input regression passed: ordinary lane tilting did not cast, a deliberate forward flick cast once, and subsequent flicks in `Casting` were ignored.
- Corrected cast direction with `InvertAxis`; retained the existing 1.25 rad/s trigger threshold because runtime testing showed no ordinary-tilt false positives after the direction fix.
- M2's playable lane-plus-cast path is complete. Variant B and the formal A/B feel comparison remain explicitly deferred; began M3 at its required think-first stage.
- M3 think-first and event-ownership discussion completed: the flight component reports landing, while `FishingLoopController` owns the transition to `Baiting`.
- Paused the planned reuse after the student identified a real orientation mismatch: the prototype flies horizontally, but the production fishing scene must cast vertically into the water. Recorded the need for a separate gameplay flight component and a quick Claude design-doc correction before treating the revised M3 as final.
- Student specified the future production flight as a vertical 2D simulation of a 3D parabola and explicitly deferred its implementation to prioritize the remaining state-machine infrastructure. M3 remains open; started M4's think-first stage out of sequence by explicit student choice.
- Student clarified that M3 itself must still be completed now with a minimal representative flight. Returned from the premature M4 prompt to M3: build a separate vertical flat-flight controller, complete `Casting -> Baiting`, and defer only the final 2.5D parabolic trajectory presentation.
- Student implemented and wired the formal `FishingHookFlightController` with Docked/Flying/Landed lifecycle, vertical power-to-distance mapping, temporary linear interpolation, and `Landed(Vector2)`.
- Android device integration verified the full core sequence: ReadyToCast -> cast at lane X -0.05/power 1.00 -> Casting -> landing at (-0.05, 2.42) -> Baiting. The final 2.5D parabolic visual remains deferred without changing the established event contract.
- Completed the mandatory post-M3 checkpoint: the student chose to proceed to the next milestone without simplifying M4 or M6. Started M4 at its required think-first stage.
- Completed M4 think-first and architecture discussion: individual fish own only Idle/Approaching/Hooked movement state, while one `FishBiteRaceController` owns the candidate race, deterministic winner, loser reset, and timeout.
- Student implemented `FishController`, runtime attraction-range discovery, approach movement, deterministic first-winner resolution, loser reset, and the eight-second timeout infrastructure.
- Student added `FishSpawner` for a configurable fish count at random non-overlapping positions inside a BoxCollider2D area, plus a `BasicFish` prefab on a dedicated `Fish` physics layer.
- Added and verified an Editor-only M4 Test Harness path that runs the current scene without AppRoot or phone sensors. The initial stationary-fish failure was diagnosed from serialized data: both `BasicFish` and the race LayerMask excluded the `Fish` layer. After correcting both, range candidates were discovered and fish approached the bait.

## 2026-09-01

- Began a second keyboard/mouse control path using Unity's Input System without duplicating the fishing state machine. `FishingInputSource` carries semantic Move, Cast, Strike, and Accelerate intentions; the keyboard implementation references the `Fishing` action map.
- Bound A/D through the Move action and Space to state-gated Cast, Strike, and Accelerate actions. Cast uses a one-frame press event, while later acceleration will use held state with a release-before-accelerate guard after Strike.
- Repurposed the M4-only direct-play Harness into a full-loop Editor entry: it now bypasses only Bootstrap/calibration prerequisites and no longer disables gameplay controllers, repositions the hook, or calls `BeginRace()` directly.
- Made `FishingLoopController.Start()` tolerate missing `AppRoot` during direct Editor play while preserving gyroscope reader injection when Bootstrap is present.
- Diagnosed failed Editor A/D movement: `ShoreLaneController.Update()` returned when both lane limits were valid because the null check was inverted; the scene also lacked the saved input-source reference.
- Rejected the attempted absolute/virtual target keyboard scheme after playtesting showed poor stopping control. The student explicitly chose velocity control for both keyboard and mobile attitude: input magnitude controls speed, neutral input means immediate stop.
- Replaced the final lane mapping with frame-rate-independent velocity integration plus world-space boundary clamping. Editor A/D verification passed: releasing the key stops accurately at the current position.
- Moved the non-mobile calibration bypass into `FishingSceneEntryGuard`, where entry prerequisites belong. Editor/desktop now start gameplay without posture calibration; the mobile path still requires the persistent calibration service.
- The former M4 Harness is no longer required for full-loop Editor testing because the normal scene entry and state machine now support the keyboard path directly.
- Full Editor integration passed: A/D moved and stopped precisely, Space cast once, the hook landed, nearby fish responded in Baiting, and the bite path advanced to Striking.
- The student deferred M4 bait wiggle and requested M6 Reeling next. The one-dimensional bait-wiggle proposal remains blocked on a Claude design-doc update; no implementation divergence was made.
- The student immediately corrected the requested order before M6 implementation began: proceed with M5 Striking first, then M6 Reeling.
- Started the mandatory M5 detector deep dive using motion-input guide §5 and §9.2 plus the current `CastGestureDetector` and `CastTuningProfile` sources.
- Completed the M5 detector checkpoint: trigger gates deliberate intent, rearm hysteresis supports repeated use, and filtered peak protects Cast power from raw one-frame sensor spikes.
- Refined the Strike requirement during discussion: it should react immediately to the first valid opposite-direction threshold crossing, publish once, and then disable on the transition to Reeling rather than waiting for a Cast-style power peak.
- Added a generic immediate `GestureTriggered` event at the detector's Ready-to-Sampling boundary while preserving the existing delayed `CastDetected(power)` event for Cast power.
- Created the learner-written `StrikeController` skeleton direction and began separating Strike input/window rules from `FishingLoopController` orchestration.
- Paused further Strike-window implementation after the student replaced the simple reaction window with a shrinking concentric-ring skill check, including retry cooldown, red invalid-input flash, and camera shake. The revision is waiting for Claude to update the approved design and teaching plan.
- Re-read design revision 6, the rewritten M5 teaching plan, and motion-input guide §5/§9.2 after Claude's update. The radial Strike design is now approved and the design-update blocker is cleared.
- Read-only source review confirmed the immediate `GestureTriggered` event, inverted Strike profile, second state-gated detector, keyboard Strike event, and initial `StrikeController` skeleton already exist. The skeleton still reflects the superseded 0.7-second design, and the Strike profile still carries the cast detector's long sample/cooldown values; revised M5 resumes before either is expanded.
- Retuned and verified the saved Strike-only detector path: `SampleWindow = 0.05`, profile `CooldownDuration = 0`, and Strike detector display duration `0`; the original Cast detector retains its `0.2` display duration.
- Chose the M5 clock semantics: Striking freezes with gameplay pause, so its window/ring/cooldown use scaled delta time and paused sensor events must not be judged.
- Implemented the learner-written Strike timing core and transferred mobile/keyboard attempt ownership from `FishingLoopController` into `StrikeController`. Success enters Reeling; timeout releases the fish and returns to ReadyToCast, with hook deduction explicitly awaiting M7's tracker.
- Built a world-space `StrikeWindowHUD` with a thick LineRenderer target annulus and a thin shrinking ring anchored to the hook. After assigning a URP 2D sprite-unlit material so vertex colors render correctly, the base ring display passed Play Mode testing.
- Completed and tuned the rejected-attempt feedback: the HUD remains red for the controller-owned cooldown and a Cinemachine Impulse Source produces a short Rumble through a listener on `HookFollowCamera`.
- Preserved presentation separation by routing `StrikeController.AttemptRejected` through `FishingLoopController` to `FishingCameraController.PlayStrikeRejectedShake()`.
- Accepted the current camera presentation settings: HookFollow-to-Overview uses the student's preferred Hard Out 0.4-second blend, while rejected Strike attempts use a 0.15-second impulse with default velocity `(0.20, 0.12, 0)`.
- M5 Striking is complete. The next teaching checkpoint is M6's tension accumulator thinking prompt.
- Began M6 think-first discussion. The student defined the boundary behavior and requested speed-sensitive lateral-dodge tension in addition to Accelerate tension.
- Paused M6 code implementation pending a Claude design update because the current approved document explicitly says only Accelerate creates tension risk.
- Fully re-read fishing-loop design revision 8 and the rewritten M6 teaching-plan section in the required order.
- Cleared the former M6 movement-driven-tension design blocker and rewrote its living checklist to mirror all five think-first prompts, six architecture topics, and ten learner-written implementation steps.
- Reopened M5 only for the revision 8 data-location back-migration: its scene-component tuning values must move into a dedicated `StrikeWindowProfile` before M6 proceeds.
- The student confirmed the revision 8 profile rationale was already understood and explicitly requested a faster path without repeating the think-first questions. Marked that checkpoint complete, confirmed the data/runtime/presentation boundary, and advanced to the first learner-written migration step.
- Created and scene-wired `StrikeWindowProfile`; `StrikeController` now reads window duration, band radii, and attempt cooldown from the asset. At the student's explicit rapid-prototype time decision, wiring the profile's shake amplitude/duration and the final M5 regression were deferred without removing them from the approved scope.
- Resumed the main vertical slice at M6. No M6 mechanic has been silently removed; the teaching cadence will be compressed where prior reasoning already covers a prompt, while implementation remains learner-written and stepwise.
- M6 Step 1 complete: the learner added a scene-wired `ReelingController`; entering Reeling now moves the landed hook at a temporary constant speed toward `HookLaunchPoint`, stops at its Y coordinate, and leaves the formal shore-success transition for step 9.
- M6 Step 2 complete: Editor `MoveInput` drives frame-rate-independent lateral hook velocity within the shared lane limits while auto-retrieval continues. `ShoreLaneController` is disabled outside `ReadyToCast`, preventing the shore player from consuming the same enabled input during Reeling.
- Fixed the attached-fish presentation gap discovered during the Step 2 test: `FishController` now retains the winning hook transform and snaps to it in `LateUpdate()`, while reset clears the attachment without changing scene hierarchy.
- Diagnosed the premature Casting reset after adding hook physics: the newly active Rigidbody caused the hook's obsolete `PlayerHazard` component to receive the `Sea` object's `Hazard` trigger and reload the scene. The new `ReelingController.AttemptFailed` path was not responsible; `ReelingObstacle.cs` and its marker were not yet present in the saved project.
- Removed the obsolete `PlayerHazard` component from `FishingHook` and verified Casting no longer reloads the scene when the hook enters the `Sea` trigger. The new Reeling-only obstacle path remains to be completed and tested.
- M6 Step 3 complete: a marker-based `ReelingObstacle` path now reports one `AttemptFailed` event only while retrieval is active. Regression checks passed: Casting contact is ignored, Reeling contact releases the fish and returns to `ReadyToCast`, and dodging around the rock continues retrieval.
- M6 Step 4 complete: the learner measured actual post-clamp lateral movement rather than input intent. Logs show about `4 units/s` during full keyboard movement and `0` while continuing to push against a lane boundary.
- At the student's explicit request, Codex made a one-time implementation exception for the mechanical M6 profile migration: created `ReelTuningProfile` plus its asset, preserved retrieval `2` and dodge `4`, initialized tension thresholds from measured speed, replaced both prototype constants, and serialized the scene reference. The first command-line build could not see the new type because Unity had not yet refreshed its generated `.csproj`; Unity Refresh and a fresh build/Play test remain before Step 5 is complete.
- After Unity Refresh, the generated project includes `ReelTuningProfile.cs`; a fresh `Assembly-CSharp.csproj` build completed with 0 errors and the same 3 pre-existing assembly-version warnings. Scene GUID verification confirmed `ReelingController` references the new asset, so M6 Step 5 is complete.
- The student explicitly reprioritized the rapid prototype toward an end-to-end repeatable loop. M6's tension UI, snap outcome, and full tuning verification remain deferred rather than removed; shore arrival and the return to `ReadyToCast` are now the immediate target, with score storage still reserved for M7's `ScoreTracker`.

## 2026-09-03

- Read-only inspected the saved tension implementation and scene. `ReelingController` exposes `Tension01`, resets it at retrieval start/cancel, and already routes maximum tension through guarded `ReportAttemptFailed()`. No runtime verification was performed in this review.
- The student requested complete step-by-step teaching for the tension UI. Resumed M6 Step 7; asked the student to map tension 0.25 onto endpoints -100 and 100. Awaiting their answer before the first Unity setup step.
- The student explicitly requested continued maintenance of `task_plan.md`; confirmed ongoing synchronization with `progress.md` and `findings.md`. Updated M6's stale heading and snap status without claiming runtime completion.
- Inspected the student's saved Slider setup. Bottom-to-top direction, `0..1` range, disabled interaction, Fill/Handle Rect assignments, and foreground Frame ordering are present. The frame uses the intended attention-bar sprite. Before binding the HUD script, the renamed EventSystem parent must be separated back into an EventSystem and a plain `TensionBarHUD`; the Handle still uses Unity's default sprite.

## 2026-09-06 — Project collaboration agreement

- Created root AGENTS.md with project roles, design handoff, architecture boundaries, Unity editing discipline, and verification requirements.
- User explicitly selected collaborative implementation: Codex may directly edit code and scene wiring within requested tasks; important gameplay and design changes are discussed with the user first. Claude remains the primary design-document author and design reviewer.
- This replaces the historical learner-written-only default; teaching is now available on request. No gameplay code or scenes changed in this task.

## 2026-09-06 — Tension HUD and session cleanup

- Implemented and wired TensionBarHUD, retrieval-only display, continuous green/yellow/red fill, noninteractive value updates, and the existing pointer sprite.
- Added silent flight/race cancellation at GameOver and explicit Strike cancellation. No gameplay balance parameters changed.
- Validation: dotnet build Assembly-CSharp.csproj passed with 0 errors and 2 CS0649 warnings in SceneCatalog. Scene file IDs checked for uniqueness and target references; diff reviewed. Unity Play Mode, Android and WebGL were not run.
- Next: playtest HUD/reset, snap hook loss and timer expiration during flight/baiting, then verify complete M7 outcomes.

## 2026-09-06 — Teaching and prototype priorities

- User returned to step-by-step teaching and confirmed tension pointer/fill alignment works in play. User adjusted aspect/scaling/placement; saved lateral tension rate is 0.9.
- User explicitly deferred the proposed systematic regression pass, preferring current playability and remaining feature development. Tests remain deferred, not passed.
- Read-only backlog check: mobile accelerate methods lack serialized UI bindings in searched scenes/prefabs; PausePanelButton navigates to MainMenu; gameplay-specific restart flow not found; linear cast and shake-profile integration remain unfinished. FishingLoopTest2 exists but catalog/build still target FishingLoopTest.


## 2026-09-06 — Two-day implementation planning

- Created the Android playtest implementation proposal and a separate Claude design-change brief covering all current requests, Profile ownership, cooldown paths, implementation order, deferred scope and open decisions.
- Kept Claude's canonical revision 9 design and teaching plan unchanged. No gameplay/scene edits or runtime tests in this planning task. Brief prepared for user handoff, not sent to Claude.

## 2026-09-06 — Strike revision 9 user checkpoint

- User explicitly reported the timing-ring migration complete and its functional test passed. This is user-reported verification, not an additional Codex test run.
- Continue teaching post-attempt cast cooldown next. Add a dedicated FishingLoopProfile for cooldown first; starting-hooks/session-duration migration will be a separate coordinated change to avoid duplicate live configuration sources.

## 2026-09-07 — 本轮教学进度与重新规划

- 用户继续亲自编写代码、英文注释、一次一个适量步骤；不反复检查。时机圈按用户报告通过，随后 post-attempt cooldown 完成并获用户试玩确认。
- 已教学并在保存源码中看到 ScoreTuningProfile、实时距离/倍率、PC 按住空格蓄力和竖条 HUD。显示统一归 FishingSessionHUD；用户确认 Slider 手动及运行时 Value 都会增长。
- 用户确认倍率在飞行中变化、落水锁定，距离显示在 Reeling 中继续变化。检查发现落水最终倍率重算、上岸倍率结算和 scoreTuningProfile 缺失检查尚未接入；不能标记计分闭环完成。
- 当前蓄力条显隐仍依赖旧文字对象的显隐条件，记录为下一小步收尾项。PC 蓄力字段仍在输入组件，飞行距离/固定时长仍在 FishingHookController；Profile 迁移尚未执行。
- 用户希望飞行时间来自模拟；初速度/角度/重力与虚拟高度为讨论建议，尚未确认可见弧线范围，没有实现。
- 更新 task_plan.md、findings.md、本日志和旧入口提示，新增 docs/reference/2026-09-07-progress-and-plan.md，重新安排收尾、飞行方案、反馈、技巧奖励、鱼行为及设备交付顺序。
- 本次只更新文档并进行局部源码/保存场景读取，没有修改代码或场景，没有运行编译、Play Mode、Android、WebGL 或完整回归，也未提交/推送。没有改写 Claude 主导设计或发送外部消息。

## 2026-09-08 — 编辑器 UI 布局刷新问题已缓解

- 用户报告：修改代码返回 Unity 后 Scene 中 HUD 布局异常，点击 Game 再返回恢复。
- 局部检查：Unity 6000.3.23f1；Canvas 为 Screen Space - Overlay，Scale With Screen Size，参考分辨率 1440×2304，Match 0.5。未发现项目编辑模式脚本自动重排该 HUD。
- 用户按建议将 Game 与 Scene 并排保持可见，并固定 Game 为 1440×2304 竖屏预设后，明确反馈问题解决。支持隐藏 Game View 后尺寸/Canvas 刷新不及时的判断；未独立复现，不认定为某个已确认 Unity bug。
- 不需要重摆 UI、改游戏代码或修改 Canvas 缩放配置。此为用户确认有效的编辑器布局规避方式。


## 2026-09-09 — 人物抛竿动画与鱼竿显隐

- 修正 Animator 持竿参数拼写、Cast → Hold 完整播放条件；PlayerAnimationController 启用时同步玩法状态，抛竿/持竿隐藏独立鱼竿，并在回到空手动画时恢复。FishingLoopTest 已接 FishingPole SpriteRenderer，保留用户 LaunchPoint 布局。编译 0 errors、3 条既有 MSB3277 警告；参数、过渡条件和场景引用静态检查通过。未运行 Unity Play Mode 或设备测试。


## 2026-09-09 — 阶段保存与下一阶段计划

- 用户确认人物移动循环、Cast/Hold 播放与 RodTip 位置衔接正常；独立 FishingPole 显隐源码和场景接线已完成。鱼钩仍按原流程发射，尚未与动画释放帧同步。
- 按用户要求仅更新鱼群下一阶段计划，列出明确需求、未决参数、施工建议及验收条件，没有提前实现捕食、逃离或障碍落点失败。
- 用户授权本地提交此前进展：包含人物动画/素材导入与场景调整、鱼线、旧体感测试接入 Settings/共享服务，以及技术资产协作约定和开发记录。不推送远端。
- 历史个人级约定已完成更新；Notion 个人技术资产库及 PPU 条目已实际创建，旧计划中“本轮未修改个人级文件”仅描述当时阶段。
- 本次提交前重新编译：0 errors、3 条既有 MSB3277 警告。未运行新的 Play Mode、Android、WebGL 或全流程回归。


## 2026-09-09 — 合并后 Tutorial 入口布局

- 上次磁盘检查与合并前相同，刷新原因只是推测。本次用户保存后的场景明确显示 TutorialButton 锚点被设为中心 (0.5,0.5)，SizeDelta=100×100，导致入口缩小居中。已仅恢复该 RectTransform 的锚点 (0.2,0.62)～(0.8,0.695)、SizeDelta=0，保留新加第三页教程引用及其他用户修改。静态差异检查完成，未运行 Play Mode。


## 2026-09-09 — 鱼群第一步验证

- 已实现生成避障、落点扣钩冷却和 4 块岩石 Collider 接线，保留用户其他未提交改动。
- Assembly-CSharp 编译 0 错误、3 条既有 MSB3277 警告。临时隔离检查使用当前 HandleHookLanded/TransitionTo/LoseHookAndFinishAttempt 方法及引擎/服务替身，验证障碍落点扣钩且不 Baiting/锁倍率、冷却门控、重复落点不重复扣钩、最后一钩结束、水面正常入 Baiting、结束后落点忽略，六项检查通过。
- 静态确认 97 个障碍对象有启用的 2D Collider、新 ID 无重复。尚未运行 Unity Play Mode、真实 Physics2D 查询或 Android/WebGL；生成数量、岩石边缘范围和失败反馈待试玩。未提交或推送。

## 2026-09-09 — 生成避障复盘文档

- 按用户要求新增 docs/reference/2026-09-09-fish-spawn-obstacle-review.md，整理实际算法、流程图、接线、局限、验证证据和独立重建练习。本轮仅写文档，未修改玩法代码或提交。


## 2026-09-09 — 自动排行榜交付

- 已实现结算倒计时/重开取消、移除主场景手动入口、排行榜羊皮纸与像素棕金样式、本局高亮。保留此前鱼群、教程、美术未提交修改；未提交或推送。
- Assembly-CSharp 编译 0 错误、3 条既有 MSB3277 警告；新组件编译通过临时 MSBuild Include 验证，未修改生成 csproj。两场景本地 fileID 完整、无重复ID。
- 十项隔离检查直接编译 GameOverPresentation/LeaderboardView/LocalLeaderboard/LeaderboardData 与引擎替身：不提前跳转、5秒非缩放时间只跳一次、重开取消及防连点、面板禁用取消、非GameOver不跳转、AppRoot路径、同分按session识别、未入前十、保存失败和损坏数据处理均通过。
- 未运行 Unity Play Mode 或实际视觉截图验收、Android/WebGL；独立测试不代表真实 UI/场景加载已通过。


## 2026-09-09 — 排行榜入口范围更正

- 用户更正：Settings 中保留 Leaderboard 手动入口；只将 Game Over 的手动进榜改为自动等待进榜。已恢复 Settings 按钮、导航接线与七项布局，保留 5 秒自动跳转和 Play Again 取消逻辑。场景本地引用及唯一 ID 检查通过，未运行 Play Mode。此前“移除 Settings 入口”的记录已被本条取代。

## 2026-09-09 — 首次教程与 Developer 实现

- 实现 LocalPlayerProgress、DeveloperPanel，扩展 TutorialPanelController / FishingSceneEntryGuard / MenuSettingsPanel；FishingLoopTest 新增 Developer 入口、完整面板与末页 Start Fishing，接入统一显隐初始化。保留用户教程素材及其他未提交修改，未提交或推送。
- Assembly-CSharp 编译通过：0 errors、3 条既有 MSB3277。使用临时 MSBuild Include 加入新脚本，未改生成 csproj。场景 fileID 无缺失/重复。
- 临时独立工程直接编译当前 TutorialPanelController / LocalPlayerProgress，配合 Unity 与内存 PlayerPrefs 替身通过10项：初始状态、提前完成拦截、关闭保持未完成、重开第一页、末页按钮、一次完成且不提前计数、手动回看、回看关闭无旧回调、开发者重置、实际开始计数。测试未触碰玩家真实存档。
- 未运行 Unity Play Mode、实际布局截图、Android/WebGL；真实持久化、移动端教程后校准、返回/重开流程待用户试玩。七位开发者姓名留空，用户在 MenuCanvas 的 DeveloperPanel.Developer Names 填写。

## 2026-09-09 — 教程关闭即开始（取代 Start Fishing 按钮）

- 用户修正规则：首次教程已翻到最后一页后，点击现有 X 保存 TutorialCompleted 并直接继续校准/开场流程；移除独立 Start Fishing 按钮及其场景组件/引用。已看完后返回前页回看，再点击 X 也算完成。
- 未看完时 X 仍取消本次开始且不标完成；Settings 手动回看 X 只关闭。回调先清空再执行，重复点击不重复启动。
- 编译 0 errors、3 条既有警告；6 项隔离检查通过（提前关闭、重开第一页、到末页等待 X、X 完成一次、手动回看只关闭、看完返回前页后完成）。场景 fileID 完整无重复。未运行 Unity Play Mode/设备验证，未提交。

## Git 交接分支（2026-09-09）

fetch 后发现本地 main 与 origin/main 分叉（提交前本地独有2、远端独有6；远端头9de9266，包含 WaterTest/tiling/pixelization 工作）。当前进度保存并推送至 codex/parallel-development-handoff-20260909，未强推、未合并远端 main。下一对话从此交接分支及当前实际代码开始；集成远端是单独的待办，需审阅场景/美术差异并验证，不能假设 main 已包含此次功能。当前用户确认的是本地交接版本。避免开新任务时默认从旧 main 建工作树而遗漏本轮进度。

## 2026-09-09 — main 集成验证

- 按用户要求，将交接分支8711115与远端main 0d76fc6在独立工作树普通合并，保留两边历史和资产；原工作区不切分支、不覆盖。详细处理见docs/reference/2026-09-09-main-merge-report.md。
- 4个文本冲突已解决：Large/Medium/Small Variant保留main Animator和交接版Capsule尺寸；字体采用交接版完整资产，清除main原先残留的旧冲突标记。未解决冲突列表为空。
- 修复额外运行兼容问题：FishAppearance揭示时暂停根剪影Animator，恢复隐藏/禁用时恢复原启用状态，不影响子VFX。
- 合并源码Runtime/Editor MSBuild 0错误，仅既有MSB3277告警。FishingLoopTest、Leaderboard、Level 1/2/3本地fileID无重复或缺失。
- Unity 6000.3.23f1隔离Play Mode使用真实Animator通过8/8：动画推进、揭示保持5个周期、子VFX持续、恢复动画、禁用恢复、重启无残留、原禁用状态两种路径保持。受测源码SHA256与集成树一致。证据在临时SevenSeasNativeAppearance-2ad6a9a9dd1a4d7cb8b2e5094cef38e9工程；日志有一次Editor.Search初始化异常，不影响8项完成。
- 正式合并场景完整Play Mode及Android/WebGL仍待验证；隔离生成动画素材不代表main真实素材视觉验收。后续测试应使用合并后的main。


## 2026-09-09 — 本地 Map fix 合并冲突修复
- 用户授权直接处理：局部解决 FishingLoopTest 的15处冲突，保留本地 Tilemap/地图/障碍调整与合入版本玩法、UI、动画、反馈接线。未改 C#、Prefab 或设计规则。
- 场景已 git add，未解决冲突列表为空；未提交、未推送。原冲突文件备份在 C:/Users/u1591680/AppData/Local/Temp/SevenSeas-conflict-backup-vi7w8vg1/FishingLoopTest.unity。
- 606个对象内容与三方合并预期完全一致，内部 fileID 无缺失或重复；忽略继承的 Unity YAML 空字段行尾空格后 diff --check 通过。未运行本次 Unity 编译、正式场景 Play Mode 或 Android/WebGL。下一步在 Unity 确认地图和玩法，再由用户 Continue merge。
- 补充静态检查：含 Prefab stripped Transform 的父子层级检查通过（139条边）；完整外部资源 GUID 扫描随后正常完成，Assets/Packages/Library PackageCache 中无缺失引用（排除 Unity 内建 GUID）。
## 2026-09-09 — FishingLoopTest接线检查（范围更正）

- 用户明确目标是main分支的FishingLoopTest，撤回本轮对Level 1/2/3的全部33行/场景修改；不将那些关卡配置缺口解释为用户故障。
- 静态逐项检查：两个Spawner均绑定真实Movement Profile和同一Loop；四个鱼Prefab资产存在，BasicFish启用并处于Layer 6，BiteRace mask=64；Loop各本地引用指向正确组件；Ecology Profile与Loop一致；GameFeedback和Haptics启用，loop引用正确、vibrationEnabled=true。未发现此范围内的序列化断线。
- 加入只读Tools/Seven Seas/Capture Fishing Scene Wiring菜单与脚本重载后自动采集，记录Unity实际解析后的对象引用、启用状态、生成区域、平台和震动开关至系统临时SevenSeasFishingWiring.txt。Runtime/Editor编译通过，仅既有MSB3277警告。
- 当前Editor尚未生成新报告；旧鱼日志16:01不能证明本次故障。需要用户切回Unity完成脚本重载/采集，再检查运行引用或复现；未声称Play Mode已通过，未更改FishingLoopTest场景或推送。

## 2026-09-09 — UI Select 悬停音效排查

- 针对用户反馈 hover 连响，检查触发链路和音频包络，仅替换 FishingLoopTest 的 uiSelect GUID 为现有 Select 1.wav。字节级检查确认本轮场景仅一处引用变更，资源存在且音频可解码。未改 C#，未运行 Unity Play Mode / Android / WebGL；不能宣称重复触发已排除。

## 2026-09-09 — UI hover 复现补充与修复（更正）

- 根据用户复现说明撤回 Select 1.wav 替换，修复 UiButtonAudioFeedback 重复 hover 入队。Assembly-CSharp MSBuild 0错误、3条既有MSB3277警告。临时工程直接编译该源码，Unity替身12项检查通过：首次进入、背景/文字切换、内部exit/enter、空白/其他UI移出重入、指针选择去重、点击确认、键盘选择、禁用重开、多指针及不可交互。此检查不代表原生输入事件或正式场景 Play Mode 已通过。

## 2026-09-09 — 地图专项文档更新与开场镜头交接

- 按用户要求更新三份进度与地图记录，新增docs/reference/2026-09-09-map-intro-camera-handoff.md。核对当前main/a9811ba工作区、Unity6000.3.23f1/Cinemachine3.1.7、CameraController/EntryGuard/SceneUI/选图与场景接线，以及用户Editor教学代码。
- 汇入当前已实现内容：三图随机、预览状态覆盖及空父引用恢复；独立鱼矩形与生成避Tilemap/鱼；统一鱼钩障碍判定。原生地图64组合512断言/Play生命周期、物理14项为此前实际完成的专项证据，不冒充正式场景全流程或设备通过。
- 记录用户新要求：独立空镜主菜单，Start后上方入场、电影画幅由上往下巡览，到ShorePlayer平台再交回原镜头。新镜头完全未实施；比例/节奏/跳过/教程校准编排交给下一chat讨论。
- 原chat继续用户手写Editor工具：GetSelectedSprites已有，但OnGUI仍使用GetFiltered；下一步只接新方法与Count并验证。镜头任务不代写教学文件。
- 本轮仅文档修改，未修改C#或场景，未运行新玩法测试。三份进度文档虽然无文本冲突标记，Git索引仍UU；未暂存、解决索引、提交或推送。

## 2026-09-09 — 地图开场镜头交付

- 按用户讨论确认后“开始”的授权，实现不同地图的独立菜单场地（活动鱼、无人/无平台）、Start后电影黑边向下巡览、平台停留、正常镜头交接及每局可跳过。Profile保存时长/取景/黑边参数。保留原三图编辑布局和既有教程/校准顺序。
- Runtime MSBuild通过；Unity6000.3.23f1临时工程加载当前正式场景的七轮专项103断言通过，补充零时长/暂停时非缩放演出两轮通过；校验受测六份源码、场景及Profile SHA256与工作区相同。
- 原生1440×2304截图确认菜单、巡览与交回效果；720×1152新开场UI正常，现有菜单裁切已记录。本轮没有修改原菜单布局。临时测试环境有Unity Search及2D Tooling资源导入告警/异常，详细界限与证据路径见实施记录。
- 尚未执行用户当前Editor完整试玩、Android/WebGL、实际手机校准、结算/排行榜/Play Again全链路。未改用户并行编辑的ObstablePrefabGeneratorWindow.cs，未暂存、提交或推送。

## 2026-09-09 — 下巡时长更正与装饰编辑说明

- 用户明确要求向下巡览本身为8秒；此前实现为下巡5秒、整段约8.75秒。已将实际FishingIntroProfile资产和新建Profile默认值改为8秒，下巡之外时长保持，总时长约11.75秒。静态核对Profile绑定及MoveTo读取surveySeconds；本次未重跑Play Mode。
- 说明菜单为运行时复制地图，临时副本不能保存装饰；当前应在编辑态对应TileMapGrid根内添加装饰，会同时用于正式地图及菜单副本。仅菜单装饰与编辑态预览能力尚未实施，不擅自改动作者工作流。

## 2026-09-09 — 巡览速度、独立电影镜头与入口断线修复

- 用户要求Survey Seconds改Survey Speed：下巡改为MoveTowards匀速，默认2世界单位/秒，距离决定时长，非缩放时间；实际Profile资产和默认值同步迁移，不把旧秒数用FormerlySerializedAs误解释成速度。Bar Height说明为每条黑边占屏幕高度比例，0.1=上下各10%。
- 用户反馈巡览/黑边仍未生效，现场保存的FishingSceneEntryGuard.intro为fileID 0，旧fallback只运行TransitionToOverview。补回场景引用，并在Awake从同对象恢复遗漏引用；Intro存在但未准备好时明确报错，不再走旧镜头流程。
- 应用户要求新增场景Cameras/IntroCamera（Cinemachine），独立承担入场/下巡/交回。MenuCamera保留菜单用途，切入同姿态避免额外混合等待，切出仍对齐Overview。原菜单布景装饰方式未改变。
- 原生Unity速度专项16项通过：逐帧速度、路程/速度决定时长、终点不超越、跳过、timeScale=0及零速度约束。临时当前场景故意清空Intro引用，两轮正常/跳过通过；Brain确实选择IntroCamera、10%黑边和SKIP原生截图确认。随后对最终入口就绪保护补做回归。
- 本次未操作用户当前Unity Play模式，未执行Android/WebGL。证据仍在SevenSeasIntro-643247fabe084c548704eb55e2099f1b临时目录：survey-speed-results.txt、dedicated-results.txt、survey-dedicated-screen.png和entry-guard-final.log。

## 2026-09-10 — 移动菜单误触和重复音效修复

- 用户确认地图/开场镜头部分完成。本轮定位Start大透明图片的整屏命中区域，以及触屏PointerEnter/Click双声。
- 新增UiButtonHitArea，仅接到正式场景Start实例，以归一化矩形包住六帧可见内容与小边距；UiButtonAudioFeedback忽略触摸悬停，保留有效点击音/鼠标悬停/键盘选择。保留用户已有字体和场景修改，未改按钮动画与布局。
- Unity6000.3.23f1临时副本编译和原生Input System检查通过，640×480、1440×2304各13项，覆盖故障复现、空白/有效点击、拖出/拖入、禁用和音效路由次数。场景本地fileID完整无重复。
- 未运行Android/WebGL设备测试，真机听感待复测；未暂存、提交或推送。证据与接入说明见docs/reference/2026-09-10-mobile-menu-input-fix.md。

## 2026-09-10 — GameOver 音频提示

- FishingAudioFeedback在首次进入GameOver时停止局内循环/旧提示，播放独立gameOverClip；正式场景先复用Attempt Failure.mp3，gameOverLevel=1，可在Inspector独立换音和调音量。
- 最后一钩的AttemptFailed在GameOver之后发布，因此跳过这次普通失败音，避免叠音；保留普通丢钩音。重复结束事件不重播，静音/组件重新启用不补播。
- Unity6000.3.23f1临时副本编译通过；原生Play Mode音源专项9项通过（场景引用、播放、重复事件、最后一钩、静音/恢复、重新启用、普通失败与旧音替换）。测试直接调用音频事件处理器并使用可控长度测试音源；未跑整局计时/耗钩流程，未进行Android/WebGL听感验证。
- 证据：临时SevenSeasIntro-643247fabe084c548704eb55e2099f1b/gameover-audio-results.txt和gameover-audio.log。未提交或推送。

## 2026-09-10 — 结束展示与设置系列换肤

- 用户确认：GameOver停留3秒→自动Inventory→1秒后显示Leaderboard/Play Again，由玩家决定离开时间。
- 正式场景接线完成；新增结果模式与按钮，保留原局中关闭/分页、实际分数和保存失败提示。
- Settings、Developer、校准、暂停、Strike Tuning统一深海墨绿/米白沙金，使用Empty_button__0；Developer保存截图七人名单并分行展示职位和姓名。
- Unity6000.3.23f1隔离副本编译及18项原生专项通过；1440×2304截图已检查。最终5个C#源文件与测试副本hash一致，场景fileID无重复。测试调用真实计时结束入口，鱼获为注入测试数据，导航按钮验证Raycast与对应回调。
- 未测试Android/WebGL实机、完整导航加载和刘海安全区。未提交或推送。详见docs/reference/2026-09-10-results-settings-ui.md；最终日志results-ui-delivery2.log。
- [2026-09-10 Developer预览修复] 将双行富文本直接保存到场景，抽出RefreshNames并增加Refresh Credits Preview菜单。Unity隔离副本编译/编辑态检查通过：七个场景文本与运行格式函数输出完全一致；人员顺序未改。证据credits-preview-results.txt、credits-preview-fixed.log。
- [2026-09-10 名单排序确认] 用户同意程序→设计→美术→技术美术→制作人。场景developerNames与七条预览同步更新，首位Programming / Shuo Hong (Flynn)，同组为Li/Zhao、Shu/Whitehouse。针对性静态核对七条格式/顺序通过，对象数不变；无C#修改，未重跑Play Mode。
- [2026-09-10 隐藏教程开关] 按用户要求，Developer教程完成按钮移到右下角6%宽×4%高透明热区（x=.91-.97，y=.02-.06），隐藏状态文字和说明，关闭视觉过渡与键盘导航；保留ToggleTutorialCompleted。仅修改场景，针对性静态接线检查通过，未重跑Play Mode；玩家Settings/Tutorial入口不变。
- [2026-09-10 名单顺序再次调整] 用户要求Production→Programming→Game Art→Technical Art。Jordan Reynolds首位、Shuo Hong (Flynn)第二位；未指定的Game Design / Yilin Qian保留在最后。场景数组与七条富文本同步，静态一致性检查通过；仅改数据，未重跑Play Mode。
- [2026-09-10 名单自动生成] DeveloperPanel的Developer Names作为唯一维护入口，编辑模式修改/重排/Undo自动生成预览，运行Open复用同一函数；不再维护单独预览文案。隔离Unity编辑态18项检查通过，包含Undo、进度不被预览修改以及教程菜单切换。
- [2026-09-10 游戏内Tutorial入口验证] Developer右下角小号Tutorial文字按钮：原生Unity Play Mode 6项通过，覆盖可见/无溢出、透明背景、Raycast、实际PointerClick切换及再次恢复，切换后不出现大状态文字；1440×2304截图已检查。证据developer-button-results.txt、developer-button-fixed.log、developer-small-tutorial.png；未测手机实机。
- [2026-09-10 Developer清蓝版] 深海蓝底、白色姓名、蓝灰职位与青蓝点缀；名单收紧，Close缩窄上移。隐藏Games Started、角落Tutorial及说明，点击DEVELOPER标题仍切换教程完成状态。Unity临时副本编译/7项原生UI检查通过，1440×2304运行截图已检查；未测手机设备。证据developer-blue.log、developer-button-results.txt、developer-blue-title.png。本轮仅Developer页换色。
- [2026-09-10 Settings配色跟进] 按用户要求将Settings主面板也换成Developer同款深海蓝底、白标题、青色像素按钮与深蓝按钮字；仅改正式场景18个颜色字段，按钮布局、事件、素材引用不变。
- Settings新版已在Unity临时副本打开并检查1440×2304运行截图：settings-blue.png；主面板与八个按钮配色更新，文字可见，无需新增行为测试。未测手机实机。
- [2026-09-10 GameOver直接鱼获] 用户取消过渡画面；OnEnable同步打开Inventory并移除Inventory Delay/倒计时，保留Actions Delay=1。Unity隔离副本编译和18项专项通过，含真实结束入口→HUD同帧打开Inventory、timeScale=0、延后按钮、分页/分数和保存失败提示。证据results-immediate.log与results-ui-checks.txt；未测Android/WebGL。
- [2026-09-10 Settings配色防回退] 核实当前场景确已恢复旧色，修复为MenuSettingsPanel集中四个可编辑颜色并自动应用；Scene快照同步蓝色。Unity临时副本编译通过，运行测试故意注入旧绿底/米黄字/原色按钮后Open，背景、八按钮及全部标签均恢复指定配色。日志settings-palette-source.log、developer-button-results.txt；未测用户当前Editor会话与真机。
- [2026-09-10 Leaderboard→Developer] 仅Leaderboard/Main Menu勾选showDeveloperOnMainMenu；SceneLoader一次性携带展示请求，FishingSceneEntryGuard在菜单入口打开已接线Developer。Close回主菜单，Inventory不变。Unity隔离副本编译与34项真实场景导航检查通过，覆盖无AppRoot、BootStrap/AppRoot、实际按钮onClick双击只加载一次、Close、普通菜单无残留请求及Play Again正常进入。证据leaderboard-developer.log/results.txt（结果实际文件名leaderboard-developer-results.txt）；未测Android/WebGL。保留用户新建Assets/Resources未改动，未提交。

## 2026-09-10 — iPhone Safari / itch iframe 体感修复

- 按 `docs/ios-motion-implementation-plan.md` 完成必需项 1–5：JS 独立回传两个权限（含同步异常隔离），C# 分通道状态与旧格式兼容，AttitudeReader 增加重力/加速度姿态源，入口按 motion 权限与样本判定，校准显示连接状态。
- 保留触屏兜底、Start 按钮同步授权位置、3 秒样本等待、原有公开属性、Editor remote 查找及 Inspector 方向设置。无场景修改；未提交、推送或发布。
- Unity 6000.3.23f1 当前项目批处理编译通过，最终日志 `C:/Users/73400/AppData/Local/Temp/sevenseas-ios-final-compile.log`；`git diff --check` 通过。
- Node VM 权限桥接模拟通过：8 种权限组合/异常/缺失情况，以及同步发起授权、iframe 检测和当前 URL 打开检查。
- 隔离 Unity Play Mode 22 项模拟状态检查通过：混合/旧格式/缺字段权限，PC 旁路，姿态优先、重力合成、低通、零/NaN/Infinity 拒绝、健康源稳定、断流换源、迟到设备、禁用清理。三份受测源码 SHA256 与工作区一致。证据 `C:/Users/73400/AppData/Local/Temp/SevenSeasIosMotionChecks/motion-results.txt`、`snapshot-check.log`、`Assets/Editor/MotionChecks.cs`。
- 测试范围说明：初版排队事件在批处理 Editor 环境未更新设备，最终通过 InputState.Change 与当前帧标记注入模拟状态并调用 Reader.Update；只验证读取/数学/选源，不证明原生或浏览器事件投递。未跑完整游戏流程或真实校准 UI。
- 可选项 6：iframe/OpenTopLevel 接口已预留，未新增 WebTopLevelLink 或设置页按钮。后续如做体验升级再接入。
- 下一步：实际 WebGL 构建后在 iPhone Safari 的 itch iframe 验证 motion=Granted/orientation=Denied、校准与倾斜方向、拒绝后触屏兜底，再检查顶层页面、Android Chrome 和 PC 浏览器。尚无真机或 WebGL 构建通过结论。
- [2026-09-10 WebGL 测试包] 用户要求打包试玩：Unity 6000.3.23f1 WebGL 正式构建成功，0 errors；沿用启用场景（BootStrap 首场景）和现有 Brotli 配置。产物 `D:/UnityProject/Builds/SevenSeas-iOS-motion-20260910-232253/SevenSeas-iOS-motion-test.zip`，19,507,026 字节。ZIP 根目录 index.html、产物中的分通道权限桥接、Brotli 解压与 WebAssembly.validate 均通过；临时 Editor 构建脚本已移出项目保存在构建目录。未上传 itch.io、未进行浏览器/真机试玩；build.log 同目录可查。

## 2026-09-11 — 触屏兜底修复

- 依照 `docs/touch-fallback-bug.md` 先做原版强制兜底诊断，再修改 ReelingButtonHUD 与 AttitudeReader。未改权限桥接、输入规则、场景和按钮布局。
- 原版 Editor 菜单期间按钮不存在，ShowGameplay 后正常创建；旧 WebGL 包在本地 Chromium 375×812 Android UA + 双权限 denied 下亦显示三按钮。本机未确证文档的 Awake 时序根因，保留与 itch 报告的差异，不将推测写成事实。
- UI 改为按需惰性创建，包含 inactive Canvas；失败只警告一次并重试。源读取在3次无样本尝试后退避至5秒，样本/权限变化恢复快速响应；仅成功收到新源样本才记录来源，异常日志同样限流。
- 当前正式场景 Unity 渲染后15项UI检查通过：可见/命中、左右按住与释放/移出、状态门控、无重复实例、缺Canvas重试、inactive父链；验证日志 `D:/UnityProject/Builds/touch-fallback-after-frames.log`，结果 `touch-fallback-after.txt`。临时强制兜底仅在 Editor 测试脚本通过反射设置，未写入生产代码。
- 隔离 Unity 25项模拟传感器检查通过，包括退避与静默日志；日志 `D:/UnityProject/Builds/touch-motion-backoff3.log`。测试显式注入当前帧状态，不等于真机事件投递通过。
- [2026-09-11 最终补充] ReelingButtonHUD 改在 OnEnable 即时刷新，动态按钮及文字继承父 Canvas 的 UI Layer（5）；增加手动 Log Touch Fallback Status / LogTouchFallbackStatus 诊断入口。最终源码的15项Unity UI检查再次通过（touch-fallback-final.txt / .log）。
- [WebGL 构建通过、显示未验收] 最终目录 `D:/UnityProject/Builds/SevenSeas-touch-fallback-final-20260911`，build.log 报 Succeeded、0 errors；ZIP 为 `SevenSeas-touch-fallback-test.zip`，19,499,112字节。最终产物有源码hash记录，临时Editor脚本已清理。
- [浏览器功能通过] Chrome 375×812 Android UA，两个权限强制denied，使用默认 RTX4080/D3D11 后端；实际触摸右移、释放、ACTION 蓄力抛投成功（power=0.56，进入Casting，后因障碍落点正常扣钩回ReadyToCast）；没有无样本 Attitude source 日志。检测脚本及日志位于 `D:/UnityProject/Builds/TouchFallbackBrowserChecks`，final-results.txt、browser-final.log、final-right.png、final-release.png 可查。
- [未解决/不能标记显示修复完成] 在强制SwiftShader和默认硬件后端均捕获到按钮图形间歇缺失；原生状态同时是 HUD/input enabled、created/fallback/move/action=true，三按钮 active=true、layer=5、culled=false、alpha=1，且实际触摸仍可操作。直接Canvas读回也出现间歇缺失；测试页面禁用UI深度测试和保留绘图缓冲并未消除，不能把它归因于单纯截图时机或已确诊的Awake时序。未把这些JS诊断覆盖写进交付index.html。
- [剩余] 继续定位实际渲染/Canvas批次问题，并在itch iframe与真实iPhone复测显示；当前仅是诊断测试包，尚未通过显示验收。未上传itch、未提交或推送。

## 2026-09-11 — 显式开启触屏控制
- 设置菜单新增已序列化的 TouchControlsButton，ON 强制触屏、AUTO 恢复平台判断和既有自动兜底；FishingInputRouter Inspector 暴露 Force Touch Controls，可在 Editor/运行时开启。选择保留在当前应用会话，不写 PlayerPrefs。
- 手动开启直接选中 MobileFishingInputSource，并跳过启动时权限等待、体感教学与校准；按游戏阶段显示可用触屏按钮。路由切换保留输入门控，取消旧的按住/蓄力状态，避免误抛竿或重复订阅。
- 修正 PressAccelerate 的状态顺序，提竿回调同步切换到收线时已知当前按住，要求松手后再次按下才加速。
- Unity 6000.3.23f1 编译及 Play Mode 14 项针对性检查通过：场景菜单事件与引用、Editor 手动路由、权限跳过、状态门控、移动/松手、取消蓄力、事件不重复、提竿释放门控、三个按钮 active、AUTO 保留自动兜底。日志 D:/UnityProject/Builds/manual-touch-checks.log，脚本 D:/UnityProject/Builds/ManualTouchChecks.cs；临时 Editor 脚本已清理。git diff --check 通过。
- 本轮未重新构建 WebGL，之前的 ZIP 不含手动开关；浏览器间歇图形缺失及 iPhone 显示仍待验证，不能以 active 状态检查代替渲染验收。
