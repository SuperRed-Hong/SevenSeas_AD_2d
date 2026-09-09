# Fishing Loop Teaching Plan

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
