## 2026-09-09 最新补充：暂停风格与UI音效

- [实现/场景/编译完成，正式场景交互听感待验收] 暂停页统一羊皮纸、像素字体、棕金按钮；全场景Button自动UI音效，运行时按钮已注册，保留手动绑定且不重播。
- 用户此前确认的是单按钮手工音效测试通过。本批真实视觉及逻辑验证见progress；下一步重新Play，检查主菜单/Settings/教程/暂停/库存/排行榜的悬停、点击、返回和静音。

## 2026-09-09 最新补充：提竿提示震动

- [源码/编译/12项替身检查通过；Android待验收] 进入Striking震动一次，沿用现有Vibration开关，保留扣钩失败独立一次反馈；Editor/WebGL保持无设备调用。

## 2026-09-09 最新状态：胶囊绕行、逃离与捕食

- [源码/场景/编译完成，正式场景试玩待验收] 沿用用户修改的横向CapsuleCollider2D；生成与游动共用实际胶囊查询，移动连续扫掠、旋转分段检测。单个障碍左右路点绕行，保留路线；复杂障碍与原生成区域边界仍可能不可达，保留8秒咬钩超时。
- [用户批准并实现] Reeling期间未挂钩Small在钩3米内逃离，4米外恢复Idle，速度倍率1.5；Large/Special在挂钩Small的5米内每条每竿掷一次30%概率，嘴接触且无遮挡后替换，每竿最多一次。追捕速度倍率1.5为可调原型默认值。
- FishEcologyProfile统一调参；GameplayRoot/FishEcology接线，协调层原子替换最终鱼并保留张力、落水倍率及加速距离；捕食本身无奖励，最终鱼上岸才计分、加时、入库。
- 下一步按progress的验证记录，在正式场景依次试绕单岩咬钩、逃离、捕食上岸/失败、暂停恢复；概率测试可暂将Profile调为1，测试后恢复0.3。Android/WebGL仍待验证。

## 2026-09-09 当前阻碍：鱼群仍有停摆反馈

- [诊断工具已编译；待现场复现数据] Editor专用FishRuntimeDiagnostics在Baiting时自动写Temp/SevenSeasFishDiagnostics.txt。下一步先读文件定位真实停止原因，不继续以截图或替身通过当作修复成功。

## 2026-09-09 最新补充：大鱼圆形碰撞误挡

- [源码/编译完成；13项隔离回归通过；待用户现场复测] 修正圆碰撞体被当旋转方框导致检查范围过大的问题，改圆形端点/扫掠检查；不改鱼头咬钩与真实障碍规则。截图中的确切卡住原因仍需试玩确认，用户朝向补充待回复。

## 2026-09-09 最新补充：鱼头咬钩已批准并实现

- [源码/编译完成；10项隔离检查通过；待真实Physics试玩] 用户批准“鱼头接近钩即可咬，鱼身保留避障”。FishController从剪影Sprite局部边界与Profile朝向计算头部，追饵停在原hookRadius内；FishBiteRaceController仍统一选唯一赢家，半径仍0.25，不改成全身宽泛判定。
- 鱼头与钩之间补障碍线段及端点检查，防止隔薄岩石咬钩。保留先转向、8秒超时、生成/运动边界和完整鱼体避障，不增加绕路。验证真实岩石边缘不同尺寸鱼、被障碍完全隔开的钩、提竿及空钩鱼线。
- 以下“规则待确认”是此前调查状态，现被本条取代。

## 2026-09-09 最新补充：近岩石追饵与鱼线

- [鱼线源码/编译完成；待Play] Baiting/Striking现在保持鱼线到钩；不再落水即消失。
- [调查完成；规则待确认] 钩中心落点合法不代表整鱼能靠近到中心咬钩半径0.25。待用户选择鱼头判定或绕行方案，未擅自取消鱼体避障、扩大范围或增加寻路。

## 2026-09-09 最新补充：GUID修复与音频独立调节

- [修复完成／待Unity重导入确认] FishMovementProfile的33位非法GUID已同步修正，4373项GUID格式检查通过。
- [源码/接线/编译完成／10项隔离检查通过／待试听] BGM在界面出现时播放，各音效可单独调Level，CatchSuccess默认2；保留总Volume，循环运行时调节可即时生效。后续教学在GameFeedback进行。

## 2026-09-09 场景职责整理（最新补充）

- [场景完成／静态检查通过／Play待验证] StrikeTuning仅保留StrikeTuningPanel；新增同级PauseUI承载暂停控制/菜单及仓库数据/视图，GameFeedback承载音频与震动。组件fileID及引用、参数保持原值。
- 用户当前切换为音频配置教学；本轮只按要求整理对象组织，不继续调整音频规则。此前四个新音频已接线、双循环/抛竿源码已修改，仍未完成该版音频编译与试听验收；后续教学从GameFeedback的FishingAudioFeedback开始。

## 2026-09-09 最新状态入口：交接分支第一批并行集成

- 当前继续 `codex/parallel-development-handoff-20260909`，没有切换旧 main、合并远端或提交推送。用户本轮授权 subagent 并行；主代理统一共享契约、场景和资产。
- [源码/主场景接线完成；待 Play/Android] Android 扣钩失败单次震动；暂停仓库按本局逐条显示最终鱼图与实际分，12 条分页、重开清空；局部 Idle 与先转向再追饵；按类别加时与加速张力倍率。
- [用户本轮确认] Small/Medium/Large/Special 加时 2/4/6/6 秒，可超过初始时间；归零优先结束。加速张力倍率 0.8/1/1.3/1.3，横移和衰减不变。数值独立 Profile。
- [音频接入完成；素材待确认] 短音效与循环播放器、暂停/静音生命周期已接；当前 clip 均留空，等待试听映射及 BGM/环境循环素材路径。仅发现25个短音效，不能宣称已完成声音内容。旧 AudioPreview 关闭自动播放。
- [未实施] 逃离/捕食继续第二批：Large/Special 食肉资格、每竿抽取方式、范围/概率待讨论；保留只吃挂钩小鱼、每竿最多替换一次、最终鱼计分的已定规则。
- [验证] 首次集中 Assembly-CSharp 编译通过；奖励33项与共享结算31项真实源码/Unity替身检查通过。主场景1028个对象ID无重复/本地引用缺失。无当前 Play Mode、真实 Physics2D、Android 或 WebGL 通过证据。
- 下一步：确认转向视觉及音频素材，试玩 Idle/追饵、连续成功/失败、仓库暂停往返和 Android 震动；随后讨论第二批逃离/捕食。

以下是此前交接记录，保留历史语境：

## 2026-09-09 交接时状态：准备五线并行开发

用户已确认首次教程看完点 X 自动继续开始可用。当前教程/Developer、排行榜、生成避障及用户动画美术进展准备统一提交同步；本轮不实施新功能。下一对话明确使用 subagent，由主代理统一接线与集成。

五条待推进线：手机震动（失败优先，其他时机待讨论）；暂停内鱼获仓库（素材已有，跨局/计分展示待确认）；鱼群 Idle/转向追饵/逃离/捕食；按大小奖励时间与影响张力增长；音频音效。均区分需求确认与实施完成，数值和待决规则不得擅定。详见 [并行开发交接](docs/reference/2026-09-09-parallel-development-handoff.md)，其中含分波次、文件所有权、共享结果契约和验收。以下旧进度保留为历史，冲突以本入口与交接为准。

# Fishing Loop Teaching Plan

## 2026-09-09 — 最新阶段交接：人物动画完成，鱼群行为待实施

- [用户确认] 左右移动两帧循环、抛竿单次播放与 HoldRod 姿势已接入；统一人物 PPU / Pivot，RodTip 随挥竿动画变化，Cast / Hold 坐标衔接问题已由用户确认解决。
- [源码/接线完成] 独立 FishingPole 在挥竿及持竿时隐藏，返回空手动画恢复；连续多次抛竿及显隐完整回归仍需明确运行证据。鱼钩发射与动画释放帧同步尚未实施。
- [本次仅记录] 下一阶段鱼群行为：常态停留/游动；追饵先转向再移动；Reeling 时小鱼主动远离钩饵；食肉大鱼有概率吞掉已挂钩小鱼并替代成为被钓对象。
- [本次仅记录] Spawn 避开障碍物；抛竿落到障碍物时直接失败，不进入 Baiting。
- 建议施工顺序（尚未实施）：生成避障与落点失败 → 常态移动/追饵转向 → 小鱼逃离 → 捕食与挂鱼替换。
- 待用户决定：落点失败是否扣一钩并进入现有冷却；捕食是否每次抛竿最多一次、仅大吃小、最终只计大鱼分；教学或直接实现方式。
- 后续实施前需确定：捕食机会/概率/范围、逃离范围、常态运动边界和转向时间；概率建议按一次机会抽取而非逐帧抽取。生成检测需考虑鱼尺寸、障碍 Layer/Collider、有限重试及无合法位置时的处理。
- 验收目标：生成范围不与障碍重叠；落点失败仅触发一次且不启动咬钩竞赛；追饵先完成转向；逃离鱼不抢钩；捕食替换始终只有一个挂鱼对象、正确计分且失败/结束清理一致。
- 新规则与原咬钩竞赛/挂鱼/计分流程存在设计差异：本节作为待设计修订的需求说明，不代表 Claude 已批准，不改写其设计文档。
- 擦边奖励及从零鱼线复习继续保留；透视相机探索继续延期。Prototype 技术资产集中整理尚未启动。


## Prototype 结束后的技术资产整理阶段（2026-09-09 登记，当前不启动）

- 开发中持续在 findings.md 积累可复用候选；继续当前人物 Animator 教学，不把本要求替换为立即架构重构。个人级 AGENTS.md 建议仅放长期原则，本轮未修改个人级文件。
- [ ] 用户确认 Prototype 阶段结束后，梳理候选及验证证据，按复用价值、第二用例、耦合与整理成本选择优先级。
- [ ] 按输入/运动、状态与表现、数据持久化、工程流程等领域建立个人知识与资产索引。
- [ ] 逐项明确接口、依赖、配置、适用边界、失败行为和接入步骤；仅在有真实差异时增加扩展点。
- [ ] 提供独立最小示例与关键验证，在第二个不同场景或干净项目中接入，记录迁移成本。
- [ ] 用户亲自解释取舍、重建核心最小实现并完成迁移；通过后才标为已掌握/可复用，不以复制代码作验收。
- [ ] 总结维护、版本兼容与后续练习；保留项目专有部分，不为所有工作强行建框架。

## 后续复习计划 — 从零实现鱼线（2026-09-09 记录，待用户启动）

- 用户希望亲手从头到尾按步骤实现，之前未接触 LineRenderer；现在只记录，不开始教学，不打断实施进度，不撤销或重做当前已接受的鱼线。
- 教学方式：用户动手，Codex 每次讲解一个可验证步骤，等待反馈后继续；开始时先安排独立练习对象，保留正式实现。
- [ ] 认识 LineRenderer，创建对象并连接两个固定点，理解 positionCount、世界/局部坐标、材质、颜色、线宽和排序。
- [ ] 通过两个 Transform 引用动态更新端点，理解 SetPosition 和 playerAnchor.TransformPoint(playerOffset)。
- [ ] 连接人物与空中 HookVisual，比较 Hook 根对象的水面位置和视觉高度。
- [ ] 根据 Casting / Reeling 状态选择 Hook 或当前挂鱼，处理空钩和无效引用；其他阶段隐藏。
- [ ] 理解 Update、LateUpdate 和 DefaultExecutionOrder，观察先移动后画线与帧滞后的关系。
- [ ] 完成 Inspector 接线，亲测抛竿、挂鱼/空钩收回、暂停、失败、上岸和结束后的显隐，并用自己的话解释表现与玩法分离。


- 2026-09-09 Casting/Reeling 鱼线：[implemented / compile passed / static wiring checked / Play Mode pending] 新增 GameplayRoot/FishingLine，Casting 连接人物 HookLaunchPoint 与 HookVisual，Reeling 连接挂鱼（空钩则 Hook 根）；其他状态隐藏。仅视觉表现，不改移动和张力规则。线宽/颜色/人物端偏移可在 Inspector 调整。

- 2026-09-09 Strike 延迟/冷却审查：[analysis only / device timing pending] 用户反馈手机提竿延迟、冷却似乎无效。已追踪原始 X 越阈 → GestureTriggered → Mobile → Router → Strike.HandleAttempt；没有采样/滤波等待。发现检测器和判定冷却独立、冷却内被吞动作仍消耗检测周期，以及 Update 时序可能产生帧级差异；尚未修改规则或参数。

- 2026-09-09 旧体感测试接入设置：[implemented / compile passed / isolated integration checks passed / device pending] Settings 新增 Gyro Test / Attitude Test，两场景已有 SceneCatalog / Build 登记保留。优先共用 AppRoot Reader 与校准，直接测试场景独立运行用局部服务；从无 AppRoot 的主菜单进入测试经 Bootstrap 初始化后到目标场景。Attitude 改为稳定采样、增加校准按钮/状态。

- 2026-09-09 设置排行榜按钮样式：[scene updated / static checked / Play Mode pending] Leaderboard 与 Tutorial 统一背景、交互色、像素字体、字号及文本边距，保留原导航事件。

- 2026-09-09 调参/排行榜入口收纳：[implemented / compile passed / static wiring checked / Play Mode pending] 移除 StrikeTuningPanel 创建的常驻左上按钮，改为 Setting → Strike Tuning；调参 Canvas 默认及关闭后整体隐藏。原主菜单右上 Leaderboard 移入 SettingsPanel，结算页入口保留。设置五项依次为 Calibration / Tutorial / Strike Tuning / Leaderboard / Close。

- 2026-09-09 校准面板修缮：[implemented / compile passed / isolated behavior checks passed / Play Mode pending] 共享 MotionCalibrationPanel 改为项目现有羊皮纸底图、像素字体、棕金按钮与进度条，简化持握说明与状态文字。本次校准完成事件自动关闭面板；已有校准后重新打开仍允许重新设置，取消按钮保留。

- 2026-09-09 高层 UI 管理：[implemented / compile passed / isolated state checks passed / Play Mode pending] 新增 SceneEntry 上的 FishingSceneUIController，统一代码策略：初始化显示菜单并关闭 HUD/设置/教程/结束/校准，转场渐隐菜单且 HUD 关闭，正式游戏仅开启 HUD。入口委托阶段转换，移除设置/教程零散 Awake 初始显隐。已迁移所有三处 Entry 场景引用。

- 2026-09-09 UI 初始显隐修正：[implemented / compile passed / Play Mode pending] GamePlayCanvas 场景默认关闭；SceneEntry.InitializeEntryUI 在玩法初始化前统一关闭 HUD、教程、设置、GameOver 和两套校准面板，显示主菜单。开局仍在转场后启用 HUD。截图是否为 Play Mode 尚待用户确认。

- 2026-09-09 设置/教程入口：[implemented / compile passed / static wiring checked / Play Mode pending] Setting 打开场景内 SettingsPanel，提供 Set Calibration、Tutorial、Close。教程/校准显示在设置上方，关闭子面板回到设置。现有教程导航接线保留，补充首尾页按钮禁用和全屏点击遮挡。

- 2026-09-08 真正暂停：[implemented / compile passed / static wiring checked / Play Mode pending] PausePanelButton 从直接加载 MainMenu 改为打开暂停面板；Resume 保留本局并恢复，Main Menu 独立结束本局返回。FishingPauseController 统一保存/恢复时间和 12 个玩法输入组件，与 StrikeTuningPanel 互斥，避免重复暂停覆盖状态。待试玩各玩法阶段暂停、继续及返回菜单。

- 2026-09-08 游戏内提竿调参：[implemented / compile passed / static wiring checked / Play Mode pending] StrikeTuning 根组件在运行时创建独立 uGUI 面板，左上 STRIKE TUNING 入口，8 个滑条、Apply / Defaults / Close。打开暂停时间和玩法组件，关闭恢复原启用状态；开局转场期间禁止打开。运行时 Profile 副本保留至场景退出，每次 BeginCheck 快照保证本轮判定稳定；下一次判定使用已应用参数。震屏幅度/时长已实际接入拒绝反馈；Profile 资产原值不覆盖。

- 2026-09-08 开局显示/逻辑分离：[implemented / compile passed / Play Mode pending] 用户要求场景先生效、操作延后。入口 Awake 先调用 Loop.PrepareForEntry 禁用协调器、岸边移动及两种手势，再激活 GameplayRoot；鱼生成、角色显示与涟漪立即运行。菜单渐隐和镜头转场完成后仅启用 Loop，由其 Start 开始计时和 ReadyToCast 输入。取代下方“转场结束才激活整个玩法根”的旧记录；游戏 HUD 仍在开局时显示。

- 2026-09-08 菜单渐隐：[implemented / compile passed / static wiring checked / Play Mode pending] MenuCanvas 新增 CanvasGroup / MenuCanvasFader，Start 后与镜头转场同时执行 0.35 秒平滑渐隐；两者都结束后才启用玩法。Fade Out Duration 可在 MenuCanvas 调整。渐隐开始关闭菜单交互，再显示时恢复透明度和交互。

- 2026-09-08 菜单点击修复：[scene wiring fixed / Play Mode pending] EventSystem 原在被菜单入口停用的 GamePlayCanvas 下，导致所有菜单点击失效；已移至场景根，保持唯一且启用。待用户重新 Play 验证 Start / Setting / Leaderboard。

## 当前任务 — 2026-09-08 同场景主菜单与空镜转场

- 用户明确替换旧 MainMenu，使用 FishingLoopTest 中已布置的 MenuCanvas；初始空镜，Start 后转到 Overview。
- [implemented / compile passed / static wiring checked / Play Mode pending] 启动仍经 BootStrap 初始化服务，MainMenu 逻辑路由已指向 FishingLoopTest；旧 MainMenu 场景取消 Build 勾选但未删除。直接打开 FishingLoopTest 也先显示新菜单。
- MenuCamera 初值位置 (0, 8, -10)、Orthographic Size 9；MenuCamera → OverviewCamera 单独配置 1 秒 EaseInOut。转场完成才启用 GameplayRoot / GamePlayCanvas，从而开始计时及输入；保留旧相机参数与失败返回修复。
- 已接 Start / Setting（现有校准面板）/ Exit，并追加排行榜入口。排行榜 Main Menu 返回菜单；Play Again 直接进入开局流程。退出在 Editor 停止 Play、独立包退出，WebGL 禁用退出按钮。
- MainLanscape 静态景物移出 GameplayRoot，保持世界位置，让菜单有背景且玩法不提前执行。其他鱼、美术、UI 布局与倍率未调整。移动端 Start 先完成校准，再转场；取消校准可重新 Start。
- 下一步：Unity 刷新后检查空镜构图、Start 单次转场、转场前计时不走、HUD 显隐、排行榜两个返回分支与局内返回菜单；Android 校准以及 WebGL 页面需分开记录。尚未实际 Play Mode 验收。

## 当前任务 — 2026-09-08 本地分数排行榜

- 用户要求排行榜系统/关卡，先本地保存。已按独立排行榜场景（非新玩法关卡）、分数和日期的默认方案实施；姓名输入与新增玩法关卡不在本版范围。
- [implemented / command-line compile passed / static wiring checked / Play Mode pending] 新增 Leaderboard 场景，显示最高 10 条成绩、排名及本地日期；主菜单与 GameOver 提供入口，榜单有 Play Again / Main Menu。SceneCatalog 和 Build Settings 已登记，既有枚举序号未改变。
- 数据用独立 PlayerPrefs key 保存 JSON。仅 EnterGameOver 记录一次已结算总分（包括 0 分），未完成返回菜单不记录；分数降序、同分较早优先。局 ID 去重，损坏/未知版本存档不覆盖，显示加载/保存失败提示。
- 数据模型独立检查通过：排序、Top 10、同局去重、同分顺序、0/负分、异常日期、空列表；检查不触碰实际 PlayerPrefs。新增场景及入口引用静态检查通过，当前尚未运行 Unity、Android、WebGL。
- 下一步：Unity 刷新后检查主菜单入口、GameOver 保存提示与入口、榜单两种返回按钮；完成多局并重启确认保存，再验竖屏布局及 WebGL 本地存储。保留鱼外观逐张视觉适配和之前未执行的回归。

## 当前阶段 — 2026-09-08 鱼样貌揭示（分类 Profile 随机外观已接入）

- 用户反馈首个固定外观“还可以”，随后要求 Profile 包含 Small / Medium / Large / Special 多个列表并填充各类素材，随机揭示。
- [implemented / command-line compile passed / Play Mode pending] FishAppearanceProfile 已填入 9 / 9 / 7 / 4 张 Sprite；BasicFish 引用共享 Profile，四种 Variant 指定类别。Middle 映射 Medium、EasterEgg 映射 Special。
- 每条鱼首次 Reveal 在所属列表有效项中等概率抽取并缓存；失败仅恢复隐藏外观，再次揭示保留身份（本步实现选择）。空槽跳过，空列表或缺失 Profile 保留隐藏外观。
- 当前下一步：试玩四类鱼的随机揭示、失败后再次揭示和素材尺寸；29 张素材引用已配置，逐张视觉适配未完成。根尺寸、碰撞、VFX、计分和生成权重未修改。

以下首个固定外观记录为已完成的前一步，固定 Sprite 配置已被上述 Profile 取代。

用户已在确认三条规则后明确要求开始实施，取代此前仅记录计划的限制。不另建计划文件。

- [implemented / static wiring checked / command-line compile passed / Play Mode pending] FishAppearance 已在 BasicFish 接入，仅 SmallFish_Variant 配置固定 Small-1 素材。提竿成功揭示；ResetToIdle 和停用恢复原 Sprite / tint。其他类别不换图，未增加随机品种。
- 下一步：Unity 刷新后试玩小鱼，检查 Striking 仍为剪影、成功进入 Reeling 变为真实外观、碰撞/断线/计时结束恢复、上岸仍正常计分且停用。观察新 Sprite 的比例和显示大小，再扩展品种。

当前基线：倍率在飞行中实时变大/变色，落水锁定到本次结束，结束后重置 x1.00；用户表示先保持现状。下方旧的落水才放大及短暂脉冲记录已被此行为取代。

## 用户需求

- 鱼被“吊起来”时揭示真实样貌，使用现有 `Assets/Art Asset Folder/Fish_v2` 素材。
- 复用现有鱼分类及 BasicFish / Variant 结构，保留已调整的鱼尺寸与涟漪节奏。
- 当前倍率表现先保持现状，不重新调整；本步仅实现揭示通路及相关 Prefab 接线，保留场景、美术导入和已有尺寸配置。

## 已确认的项目事实

- `Fish_v2/Fishes` 下共有 29 张 PNG，按文件名分类：Small 9、Middle 9、Large 7、EasterEgg 4。这是文件清点，不代表已逐张确认鱼种名称、朝向、透明边距或切片效果。
- 当前 `FishController` 仅有 Idle / Approaching / Hooked；Hooked 在咬钩胜出时设置，随后进入 Striking，不能直接等同于“提竿成功”或“吊上岸”。
- `HandleStrikeSucceeded` 进入 Reeling；`HandleRetrievalCompleted` 成功计分后立即停用鱼对象。如果选择上岸展示，需要安排可见的展示时段，不能在同一帧换图后立即停用。
- `SmallFish_Variant` / `MediumFish_Variant` / `LargeFish_Variant` / `SpecialFish_Variant` 继承 BasicFish。素材 Middle 对应现有 Medium 是建议映射；EasterEgg 是否全部归 Special 仍待确认。
- 当前已实现首个固定真实 Sprite 与现有隐藏外观的切换，尚无随机鱼种身份系统。素材文件名不能直接作为生物品种名称。

## 已确认的玩法规则 — 2026-09-08

1. 提竿成功、进入 Reeling 时揭示真实样貌；不在咬钩胜出时揭示，也不等待成功上岸。
2. 揭示前沿用现有水下外观。
3. 揭示后失败，恢复隐藏外观。

用户随后明确授权开始实施；首步限定为固定素材通路，运行验收后再扩展。

## 后续实施细节

- 身份选择：生成时在所属类别内随机一次，还是每个 Variant 固定一个品种？建议先固定一张做通路，再扩展为生成时选定并保存。同一条鱼失败后保留品种身份仍是建议，本次未单独确认。
- 不同品种是否影响分数、速度、稀有度？建议首版只改变外观，沿用现有玩法数值；额外规则另行讨论。

## 建议实现边界（尚未批准实施细节）

- 身份与显示分离：鱼保存选定的外观数据，表现组件只负责隐藏/揭示和复位，不决定咬钩胜者、分数或收线结果。
- 可用小型 ScriptableObject 外观配置记录 ID、真实 Sprite、局部显示尺寸和偏移；需要批量品种时再引入，不建立图鉴、背包或复杂数据库。
- 不为 29 张素材复制整套鱼逻辑。优先在 BasicFish 共享表现能力，在现有 Variant 配置类别或可选素材。
- 鱼根对象继续承担行为、Collider 与移动；显示尺寸校正尽量在视觉层完成，避免换图连带改变碰撞尺寸和涟漪缩放。实施前检查根 SpriteRenderer 与 VFX 的实际层级，必要时局部拆出视觉子对象并保留引用。
- 由 FishingLoopController 在 HandleStrikeSucceeded 的有效成功分支通知目标鱼揭示；只影响这一条鱼。不能在通用 Reeling 入口无条件揭示，因为空钩超时也会进入 Reeling。FishBiteRaceController 仍是唯一咬钩裁决方。
- 揭示前后检查 SpriteRenderer 的 tint：若隐藏外观靠深色染色，真实素材展示时须恢复正确颜色。检查 PPU、Pivot、Filter Mode、排序和透明边距，不统一覆盖用户已调数值。
- 当前选择提竿成功时揭示，不增加独立上岸展示时段或延迟下一竿；保留现有上岸结算与停用流程。

## 小步实施顺序

1. [规则已确认] 提竿成功时揭示，之前沿用水下外观，失败恢复隐藏外观。
2. [首个素材已选 / 视觉试玩待做] 预览 Small-1、Small-2 及现有小鱼剪影；选 Small-1。13×15 与剪影 11×6、均为 PPU 100，中心对齐；未改导入设置或根缩放。其他类别素材尚未逐张核验。
3. [已实现 / 待 Play Mode] 先在 SmallFish_Variant 打通固定 Small-1 揭示与复位；该 Variant 生成的小鱼均用此测试外观，未增加复杂动画。
4. 验证后配置 Small / Medium / Large / Special，按需求加入每类随机身份和外观数据。
5. 最后再讨论揭示过渡、离水动作、音效或名称提示；这不是首版默认范围。

## 验收条件

- 揭示只在提竿成功时发生一次；咬钩、提竿超时和空钩进入 Reeling 均不揭示。只有目标鱼改变外观，其他鱼和 SwimRipple 独立运行。
- 同一鱼的身份在一次存在期间稳定，重复咬钩不重新抽取（若采纳该建议）。
- 成功、失败、超时、GameOver 和复用路径按确认规则恢复，不残留外观或触发重复计分。
- 各类真实素材可辨认且尺寸合理；Collider、泳速、分值与 VFX 尺寸不因换图意外改变。
- Reeling 期间玩家能看见真实外观；揭示后失败恢复现有水下外观。
- 源码、接线、编译、Play Mode、设备验证分别记录，不用文档计划代表已完成。

## 与现有计划的关系

下一次先讨论本机制；擦边奖励、鱼生成避遮挡、最小鱼行为/动画、操作说明与交付保留，不删除。透视相机探索继续延期。加速奖励已有源码与编译验证，运行结算尚待确认，不回到未实现阶段重做。


## 2026-09-08 — 当前倍率显示验收

- [implemented / compile passed / Play Mode pending] 落水锁定倍率直接驱动文字尺寸，保持至本次尝试结束；验证 2 倍/3 倍大小、收线持续显示及结束恢复。替代此前短暂脉冲。

## 2026-09-08 — 新奖励实现与待验收

- [implemented / compile passed / Play Mode pending] 3 倍上限、落水蓝金红反馈、强化脉冲、向岸加速距离奖励（成功上岸合计取整，失败丢弃）。待测满倍率封顶、末帧位移、空钩无奖及失败后无残留。擦边奖励待下一步规则确认。

## 2026-09-08 — 倍率最小反馈

- [implemented / compile passed / Play Mode pending] 落水锁定倍率后，HUD 倍率文字单次放大回落。默认峰值 1.25、时长 0.4 秒；待观察落水一次、Reeling 不重复。鱼涟漪节奏用户已反馈差不多。

## 2026-09-08 — 当前相机修复

- [implemented / compile passed / Play Mode pending] 返回 Overview 前解除 Follow，下次抛竿恢复；验证失败返回无跳变、下一竿正常跟随、最后一钩和过渡中重抛。

## 当前入口 — 2026-09-08 更新

当前状态与实施顺序以 [进度与新计划](docs/reference/2026-09-07-progress-and-plan.md) 为准。下方 M0–M8 是历史教学清单；其中 M7 未开始、revision 9/冷却待做等旧描述不再代表当前实现。

- [complete — user reported] 时机圈 revision 9、post-attempt cooldown 试玩通过。
- [implemented / user confirmed display] PC 空格蓄力；POWER 始终显示并保留上次显示值，Slider 独立控制显隐。不要删除 POWER。素材复用完成情况未确认。
- [implemented / partial verification] 最终落水倍率锁定、成功上岸倍率结算、ScoreTuningProfile 引用检查；移动端输入启用且收线时显示 Accelerate。用户完成步骤，完整运行/设备验证待做。
- [implemented / partial verification] HookFlightProfile、HookVisual 层级和引用、恒重力解析飞行、Launch 的装备初速度倍率接口。5～25 只是测试地图参考；装备系统尚未实现。
- [implemented / partial verification] 用户已补 Dock 复位、创建 HookShadow 并反馈腾空感“还可以”；Codex 按本步授权接入仅 Flying 显示及场景引用，显隐运行观察待做。缩放表现尚未实现，不自动扩展；PC 蓄力时长 Profile 迁移待做。
- [deferred — user decision] 透视相机探索：先在正交场景完成当前功能；用户重新提出且现有素材足够时恢复，不依赖新增美术。
- [pending] 倍率 VFX、加速/擦边奖励、鱼生成避遮挡、最小鱼行为/动画、试玩说明和重开。
- [deferred] 系统回归及尚未执行的平台验证；不当成通过。

本轮继续用户亲自编写、一次一个适量步骤、英文注释，不重复考查与反复检查。最新 AGENTS.md 和用户要求优先于下方历史协议。

## Goal

Coach the project owner through hand-implementing the approved fishing loop:
`ReadyToCast -> Casting -> Baiting -> Striking -> Reeling -> GameOver`.

The project owner types every line of C# and performs every Unity Editor step.
Codex may inspect files and give feedback, but must never write or edit game code
or Unity scenes.

## Sources of truth

1. `docs/design/2026-08-29-fishing-loop-design.md`
2. `docs/design/2026-08-29-fishing-loop-teaching-plan.md`
3. `docs/reference/motion-input-guide.md`
4. This checklist, followed by `findings.md` and `progress.md`

If the conversation drifts, re-read these files before relying on chat history.

## Teaching protocol for every milestone

For each milestone, follow this order independently:

1. Think first: ask the student to reason from the milestone's thinking prompts.
2. After a real attempt, discuss architecture, alternatives, and codebase precedent.
3. Give one implementation step at a time; wait for the student to type/click and report back.

## Step 0 — Living project memory

- [complete] Read the design spec, teaching plan, and motion-input reference in full and in order.
- [complete] Repurpose `task_plan.md` as the M0–M8 checklist.
- [complete] Repurpose `findings.md` as the running discovery log.
- [complete] Repurpose `progress.md` as the dated session log.
- [in-progress] Keep all three files current as milestones advance.

## M0 — Quick orientation

Status: **complete**

- [complete] Ask why orientation and angular velocity use separate reader components.
- [complete] Wait for the student's own explanation before teaching.
- [complete] Briefly discuss the reader/controller/view split.
- [complete] Discuss attitude as rotational state (`Quaternion`) versus angular velocity as rotational rate (`Vector3`, rad/s).
- [complete] Checkpoint: student can explain why the two measurements are not interchangeable.

Scope guard: do not teach thresholds, filtering, or the gesture detector state machine here.

## M1 — State machine skeleton

Status: **complete**

- [complete] Think first: compare a fishing-attempt enum with `CastDetectionState`.
- [complete] Discuss enum + `switch` versus a formal state-pattern hierarchy.
- [complete] Student defines the six-state enum.
- [complete] Student scaffolds the fishing-loop controller.
- [complete] Student stubs enter/update/exit behavior for each state.
- [complete] Student adds transition logging.
- [complete] Student verifies all six empty-state transitions in the Console.
- [complete] Student removes the temporary timed auto-advance code before M2.

## M2 — ReadyToCast: lane tilt + cast flick

Status: **in-progress**

- [complete] Coached deep dive: motion-input guide §2–3 and the existing attitude scripts.
- [complete] Checkpoint: dead zone, axis hysteresis, and frame-rate-independent smoothing.
- [complete] Run `AttitudeControlTest.unity` and connect phone/HUD observations to the code.
- [complete] Think first: identify what a one-axis shore controller keeps and drops.
- [complete] Discuss a new small component versus generalizing `AttitudeCircleController`.
- [complete] Student implements and wires the position-target lane controller (variant A).
- [complete] Student designs and implements a shared 3-second motion-calibration service that survives scene changes through `AppRoot`.
- [in-progress] Student adds explicit calibration entry points in the main menu and pause menu, with hold-still progress and retry feedback. Main-menu entry and mandatory fishing-scene fallback are complete; pause-menu reuse remains pending until pause UI exists.
- [complete] Student replaces position-target lane mapping with one velocity-controlled scheme shared by keyboard direction and normalized phone tilt; neutral input means immediate stop.
- [complete] Student explicitly selects velocity control after the absolute-target keyboard experiment proved difficult to stop precisely.
- [in-progress] Verify the selected velocity scheme on Android: greater tilt increases speed, dead-zone/neutral stops immediately, and lane bounds still clamp correctly.
- [complete] Student subscribes to cast detection and captures lane + power.
- [complete] Student verifies ordinary lane tilting does not cast accidentally.
- [complete] Student checks lane-motion input against the cast detector and confirms direction, rather than threshold magnitude, caused the observed backswing false trigger.
- [complete] Student corrects the detection direction with `InvertAxis` and verifies the existing `TriggerThreshold` accepts deliberate forward flicks without ordinary-tilt false positives.

## M3 — Casting: flight and landing transition

Status: **core state flow verified — final parabola deferred**

- [complete] Think first: reinterpret `CastBallController.Landed` for the new loop.
- [complete] Discuss event decoupling and why the flight object need not know about `Baiting`.
- [complete] Separate the formal vertical gameplay flight from the horizontal gyroscope test projectile.
- [complete] Student builds a minimal vertical flat-flight controller with Docked/Flying/Landed lifecycle and a landing event.
- [deferred] Replace the minimal flat trajectory with the final vertical, 2D-simulated-3D parabolic presentation.
- [complete] Student launches from the selected lane using cast power.
- [complete] Student uses the new vertical flight's `Landed(Vector2)` event to preserve the landing point.
- [complete] Student transitions from `Casting` to `Baiting` on landing.
- [complete] Verify the M0–M3 core state flow end to end using the temporary flat trajectory.
- [complete] Mandatory time checkpoint: student chose to continue without simplifying M4 or M6.

If time is short, only the student may choose to simplify M4 and/or M6. Any chosen
simplification must go back to Claude for a design-doc update before implementation diverges.

## M4 — Baiting: fish race + bait wiggle

Status: **in-progress — fish race implementation and integration**

- [complete] Think first: prevent two fish from claiming the same bite.
- [complete] Discuss per-fish state and a single authority for first-writer-wins resolution.
- [complete] Student implements `Fish` state and score data.
- [complete] Student implements detection range and approach movement.
- [deferred] Student postpones bait wiggle. A proposed one-dimensional left/right version is waiting for Claude to update the design before implementation.
- [complete] Student implements and verifies deterministic winner resolution and loser reset through the full Editor loop.
- [in-progress] Student implements the 8-second timeout; code exists, formal state-transition verification remains.
- [in-progress] Student verifies the bite path transitions to `Striking`; the empty `Reeling` timeout path still needs an isolated/full-loop verification.

## Cross-platform input test infrastructure

Status: **in-progress**

- [complete] Student creates a common semantic `FishingInputSource` contract and a keyboard/mouse implementation using Unity's Input System.
- [complete] `ReadyToCast` gates Move and Cast input; Space emits one Cast event and A/D emits signed velocity input.
- [complete] `FishingSceneEntryGuard` treats calibration as a mobile-only prerequisite, so Editor/desktop full-loop play starts without a motion-calibration panel.
- [complete] Editor A/D test verifies release produces zero velocity and stops accurately at the current lane position.
- [complete] Verify Space drives `ReadyToCast -> Casting`, hook landing, Baiting, fish response, and the bite transition in the full Editor loop.
- [in-progress] Add keyboard Strike and Accelerate behavior when M5/M6 reaches those states. Keyboard Strike is verified through the M5 timing check; Accelerate remains for M6.

## M5 — Striking: shrinking-ring timing check

Status: **prototype-complete — revision 8 shake-profile wiring and regression check deferred**

- [complete] Coached deep dive: motion-input guide §5 and §9.2 plus the detector source.
- [complete] Checkpoint: trigger/rearm hysteresis and filtered-versus-raw peak.
- [complete] Think first: reuse `InvertAxis` for the opposite flick direction and use immediate threshold crossing rather than Cast power/peak completion.
- [complete] Discuss two detector/profile instances versus new detection logic.
- [complete] Verify `OnEnable()` resets the detector in the actual source.
- [complete] Student creates the inverted tuning profile and second detector.
- [complete] Student adds `GestureTriggered` at the Ready-to-Sampling boundary while keeping `CastDetected(power)` unchanged for casting.
- [complete] Student enables each detector only in its relevant fishing-loop state.
- [complete] Claude design revision 6 approves the shrinking-ring timing check and rewrites the M5 teaching sequence.
- [complete] Think through ring timing, rejected-attempt cooldown, ownership, and scaled-versus-unscaled clock choice before continuing implementation.
- [complete] Retune the Strike detector's own sample/display/cooldown lockout so `StrikeController` is the attempt-pacing authority.
- [complete] Student implements `StrikeController`: timer, normalized ring radius, band bounds, cooldown, and succeeded/timed-out/rejected events.
- [complete] Student verifies the existing keyboard Strike path against the semantic input contract.
- [complete] Student implements `StrikeWindowHUD` as display-only logic reading the controller's normalized values.
- [complete] Student implements rejected-attempt red feedback and an event-driven Cinemachine Impulse shake.
- [complete] Student wires success and timeout outcomes through `FishingLoopController`.
- [complete] Student completes the required Play Mode tuning pass.
- [complete] Revision 8 think first: student confirmed the Play-Mode persistence and single-responsibility reasons were already understood and requested no repeated questioning.
- [complete] Revision 8 architecture discussion: keep `StrikeController` as runtime judgement authority, move only feel/balance data into a dedicated `StrikeWindowProfile`, and keep scene wiring on components.
- [complete] Student creates and wires `StrikeWindowProfile`, migrates window duration, inner/outer band radii, and attempt cooldown into it, and adds revision 8 validation rules.
- [deferred] Route rejected-attempt shake amplitude/duration from `StrikeWindowProfile` into `FishingCameraController`; the existing accepted Cinemachine settings remain active meanwhile.
- [deferred] Verify Play-Mode profile persistence and rerun the complete Strike success/reject/timeout regression after the remaining shake wiring.

## M6 — Reeling: retrieve + dodge + tension

Status: **in-progress — tension UI teaching resumed; gameplay think-first and architecture checkpoints complete**

- [complete] Revision 8 design update received; movement-driven tension is approved and the former design blocker is cleared.

### Think first — answer before architecture or implementation

- [complete] Prompt 1: express each frame's tension change as one summed net-rate line so passive decay, Accelerate, and fast lateral movement can stack.
- [complete] Prompt 2: choose intended input or actual hook movement as the lateral-risk measurement, including the full-input-at-lane-boundary case.
- [complete] Prompt 3: map actual lateral speed from a safe threshold to a maximum evaluated threshold into a normalized `0..1` contribution, relating it to the existing normalization pattern.
- [complete] Prompt 4: identify the two profile invariants that guarantee Accelerate alone and maximum lateral movement alone can each overpower passive decay, and reason about the lateral crossover speed.
- [complete] Prompt 5: prevent repeated line-snap events while tension remains clamped at its maximum.

### Architecture discussion — only after all five thinking attempts

- [complete] Discuss one accumulator, summed rates, and one final clamp rather than mutually exclusive branches.
- [complete] Discuss actual movement versus intended input as a general gameplay-measurement principle.
- [complete] Discuss frame ordering: move/clamp the hook first, then measure actual lateral speed for that frame.
- [complete] Discuss `maxEvaluatedLateralSpeed` as both the top of the tuning range and protection against frame-hitch/position-spike values.
- [complete] Decide that movement and tension state live together in `ReelingController`, while its eventual `ReelTuningProfile` remains data-only.
- [complete] Discuss one shared attempt-failed path for rock collision and line snap.

### Learner-written implementation — one checked step at a time

- [complete] Step 1: student implements constant-speed retrieval.
- [complete] Step 2: student implements `MoveInput`-driven lateral dodge clamped to the lane; the attached fish follows the hook in `LateUpdate()`.
- [complete] Step 3: student adds rocks/collision and routes collision through the shared attempt-failed path; Casting ignores rocks and Reeling failure fires once.
- [complete] Step 4: student measures and logs actual lateral speed after movement: keyboard maximum is approximately `4 units/s`, and sustained input at a boundary measures `0`.
- [complete] Step 5: by the student's explicit one-time exception to learner-written mode, Codex created and wired `ReelTuningProfile`, migrated retrieval/dodge speeds, and added tension parameters plus validation; Unity refreshed and `Assembly-CSharp` builds with 0 errors.
- [in-progress] Step 6: student has implemented the tension accumulator; concise logging/play verification remains.
- [in-progress] Step 7: student resumed the display-only live tension bar on 2026-09-03. First Slider pass saved with correct direction/range/disabled interaction and Frame ordering. Next: restore the accidentally renamed EventSystem, create a plain `TensionBarHUD` under Canvas, and reparent the Slider; then assign the pointer sprite, bind the display script, add color/visibility behavior, and verify in Play Mode.
- [in-progress] Step 8: saved code already checks maximum tension and calls the guarded shared attempt-failure path. Runtime verification of one snap/one hook loss remains; the previous deferred status was stale.
- [in-progress] Step 9: prioritize the shore-arrival event and return-to-`ReadyToCast` loop now; final score accumulation remains owned by M7's `ScoreTracker`.
- [not-started] Step 10: student measures real maximum lateral speed first, tunes thresholds before rates, and verifies design §7 checklist 6a–f separately.

## M7 — Hooks, timer, score, GameOver

Status: **not-started**

- [not-started] Think first: decide where hooks remaining should live.
- [not-started] Discuss small trackers versus folding all data into the state machine.
- [not-started] Student implements the hook/lives tracker with 5 starting hooks.
- [not-started] Student implements the 90-second session timer.
- [not-started] Student implements the score tracker.
- [not-started] Student wires both game-over conditions.
- [not-started] Student builds and wires the minimal results UI.

## M8 — End-to-end verification

Status: **not-started**

- [not-started] Think first: isolate a failing state without replaying the whole loop.
- [not-started] Discuss temporary debug state jumps and HUD-based observability.
- [not-started] Run design §7 checklist item 1: lane + cast.
- [not-started] Run checklist item 2: bait wiggle + fish race + strike entry.
- [not-started] Run checklist item 3: bait timeout -> empty reel.
- [not-started] Run checklist item 4: correct/wrong/missed strike behavior.
- [not-started] Run checklist item 5: reeling dodge + rock failure.
- [not-started] Run checklist item 6: accelerate + tension rise/drain/snap.
- [not-started] Run checklist item 7: shore arrival scoring rules.
- [not-started] Run checklist item 8: hooks/timer GameOver and final score.
- [not-started] Re-run the entire checklist after fixes.
- [not-started] Report the implementation outcome to Claude for review.

## Current next action

**2026-09-08 当前动作：** 继续正交场景功能开发；先补飞行 Dock 显示复位，并以一个适量步骤确定/实现明显腾空表现，再按最新计划推进反馈、奖励、鱼行为和交付。透视探索延期。下方 2026-09-06 动作为历史记录，勿重新创建已完成的按钮。详见 `docs/reference/2026-09-07-progress-and-plan.md` 顶部更新。

2026-09-06: User approved the two-day implementation plan and requested starting. Continue step-by-step teaching; do not wait for an additional Claude approval as a prerequisite to this explicitly approved work. Current step: create an AccelerateButton under Canvas in FishingLoopTest, then wire press/release to existing MobileFishingInputSource methods. Read-only inspection found no saved accelerate button bindings. Library/LastSceneManagerSetup and build/catalog all identify FishingLoopTest (saved editor-state evidence). No gameplay or scene changes made by Codex in this step. Full plan: docs/design/2026-09-06-android-playtest-implementation-plan.md; Claude handoff remains prepared, not sent.

## Errors

| Error | Attempts | Resolution |
|---|---:|---|
| A single patch tried to delete and re-add each planning file | 1 | Split replacement into one delete patch followed by one add patch. |
| A broad status patch matched the wrong milestone heading | 1 | Re-read the exact M5/M6 lines and applied a heading-scoped correction. |


## 2026-09-09 — 人物抛竿动画与鱼竿显隐

- [已实现／待运行验证] 人物重复抛竿与独立鱼竿显隐修复。下一步连续完成两次抛竿（含一次成功和一次失败），确认每次完整挥竿、持竿期间无重复鱼竿、返回待机恢复独立鱼竿。鱼钩与动画释放帧同步仍未实施。


## 2026-09-09 — 鱼群阶段第一步

- [实现完成／待场景试玩] 本轮按推荐规则直接实现生成避障与落点失败：先选鱼品类，再按实例的 SpriteRenderer 和 Collider2D 世界包围盒加 0.05 单位边距检测生成区域和障碍，保留 0.75 中心间距、每鱼 50 次尝试，失败跳过该鱼并汇总告警。落水点命中 ReelingObstacle 时走统一扣钩失败路径，进入 PostAttemptCooldown，不进入 Baiting、不锁倍率；不检查飞行路径。
- [采用推荐规则／后续实施] 食肉大鱼只捕食已挂钩小鱼，每次抛竿最多替换一次，上岸按最终鱼计分；具体概率与范围待原型调参。
- [未实施] 常态移动、追饵转向、逃离与捕食替换。先验收本步生成与落点行为再继续。

- 后续复盘材料：docs/reference/2026-09-09-fish-spawn-obstacle-review.md；待用户启动独立练习，不改变当前鱼群实施顺序。


## 2026-09-09 — 街机结算与排行榜

- [源码/主场景接线完成／待运行验收] 用户明确要求将排行榜改为街机式结算流程：Game Over 展示本局分数，5 秒后自动进入 Leaderboard，点 Play Again 立即重开并取消自动跳转；Settings 和结算页的手动 Leaderboard 入口移除。
- GameOverPanel 的 GameOverPresentation.leaderboardDelay 默认 5 秒，使用 unscaledDeltaTime，在 LateUpdate 检查超时，让本帧 UI 点击先取消等待；OnDisable 取消倒计时，防止重复导航。
- 排行榜沿用项目羊皮纸 Sprite、PressStart2P 字体和棕金按钮；显示本局成绩、排名或未入前十，按 sessionId 高亮对应记录。保存结构与前十规则不变。
- 验收：结算等待5秒只跳转一次、临界时刻 Play Again 不再跳榜、暂停时间不阻止跳转、前十/未入榜/保存失败显示正确；Android/WebGL 和真实 UI 仍待验证。
- 鱼群阶段计划保留，本次仅调整结算流程。


## 2026-09-09 — 排行榜入口范围更正

- 用户更正：Settings 中保留 Leaderboard 手动入口；只将 Game Over 的手动进榜改为自动等待进榜。已恢复 Settings 按钮、导航接线与七项布局，保留 5 秒自动跳转和 Play Again 取消逻辑。场景本地引用及唯一 ID 检查通过，未运行 Play Mode。此前“移除 Settings 入口”的记录已被本条取代。

## 2026-09-09 — 首次教程与开发者入口

- [源码/场景接线完成，待运行验收] Start 读取本地 TutorialCompleted；false 时先打开现有三页教程，最后一页 Start Fishing 保存 true 并继续校准/过渡流程。提前关闭保持 false，重新 Start 从第一页开始。Settings 中 Tutorial 仍为手动回看。
- Settings 新增 Developer，保留 Leaderboard 等原有入口。点击 Tutorial Completed 状态按钮立即切换并保存，影响下一次 Start，不中断当前局。显示 GamesStarted，实际结束开场过渡并启用玩法时才增加。
- MenuCanvas 上 DeveloperPanel 组件预留 7 项 Developer Names；开发者面板纳入 FishingSceneUIController.menuOverlays，初始化强制隐藏。
- 下一步：退出 Play Mode 后加载磁盘场景，测试 false→Start→提前关闭→再次Start→末页Start Fishing，以及再次开局跳过教程；手机检查教程后校准；填写七人署名。鱼群后续阶段继续保留。

## 2026-09-09 — 教程关闭即开始（取代 Start Fishing 按钮）

- 用户修正规则：首次教程已翻到最后一页后，点击现有 X 保存 TutorialCompleted 并直接继续校准/开场流程；移除独立 Start Fishing 按钮及其场景组件/引用。已看完后返回前页回看，再点击 X 也算完成。
- 未看完时 X 仍取消本次开始且不标完成；Settings 手动回看 X 只关闭。回调先清空再执行，重复点击不重复启动。
- 编译 0 errors、3 条既有警告；6 项隔离检查通过（提前关闭、重开第一页、到末页等待 X、X 完成一次、手动回看只关闭、看完返回前页后完成）。场景 fileID 完整无重复。未运行 Unity Play Mode/设备验证，未提交。

## Git 交接分支（2026-09-09）

fetch 后发现本地 main 与 origin/main 分叉（提交前本地独有2、远端独有6；远端头9de9266，包含 WaterTest/tiling/pixelization 工作）。当前进度保存并推送至 codex/parallel-development-handoff-20260909，未强推、未合并远端 main。下一对话从此交接分支及当前实际代码开始；集成远端是单独的待办，需审阅场景/美术差异并验证，不能假设 main 已包含此次功能。当前用户确认的是本地交接版本。避免开新任务时默认从旧 main 建工作树而遗漏本轮进度。
## 2026-09-09 — 当前复现跟进

- [实现/场景/编译完成，听感待验收] Ambient Loop 1.5秒淡入，参数在GameFeedback的FishingAudioFeedback；暂停和静音恢复也渐入。
- [定位推进，未修复] 最新5候选超时由转向/移动检查拦截；obstacles3_0明确阻挡4条鱼。下一步核对实际形状与保守包围框误挡；局部绕行属于追饵行为扩展，先讨论范围再实施。

## 2026-09-09 — main 集成

- [合并/针对性验证完成] 交接分支8711115合入远端main 0d76fc6；4个文本冲突及剪影动画覆盖揭示图的兼容问题已处理，详见合并报告。
- [待正式场景验收] 在合并后的main验证真实美术、场景完整流程及Android/WebGL；不以原分支试玩或隔离Animator测试替代。


## 2026-09-09 — 本地 Map fix 场景冲突处理
- [x] 按 fileID 三方合并 FishingLoopTest 的 15 处冲突并暂存场景；保留本地地图替换和合入功能。
- [x] 验证对象内容与三方决策一致、内部引用完整、冲突列表为空。
- [ ] Unity 正式场景视觉与 Play Mode 回归；由用户继续 merge，尚未创建合并提交。
