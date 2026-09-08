# Seven Seas 新对话开发交接 — 2026-09-07

## 最新交接 — 2026-09-08 鱼样貌揭示仅规划

- 当前用户要求仅更新文档，不开始实现。下一次读 [鱼真实样貌揭示计划](../../task_plan.md)，先确认提竿成功还是吊上岸揭示。Hooked 目前只是咬钩胜出，不能直接作为揭示事件。
- Fish_v2/Fishes 文件清点：Small 9、Middle 9、Large 7、EasterEgg 4，共 29 张；未逐张视觉确认。保留 BasicFish Variant 的尺寸、涟漪配置。
- 相机返回用户已确认解决；鱼涟漪节奏用户接受。倍率最高 3 倍，飞行实时放大/变色，落水锁定，收线结束重置 x1.00，用户接受先保持现状。加速距离奖励已实现/编译通过，运行结算未全面验收。下方旧状态不要求重做。

## 最新接手状态 — 2026-09-08（优先于下方历史快照）

本节后续更正：用户已完成 Dock 图像偏移复位并确认，已创建 HookShadow 水面投影并反馈腾空感“还可以”。Codex 按用户明确的“直接帮我实施”授权，加入 LateUpdate 中仅 Flying 显示投影的逻辑，绑定场景既有 HookShadow；静态引用检查完成，编译/运行显隐尚未验证。下方“Dock 尚缺、投影待创建”已过时，勿重做；下一项先观察显隐，再继续既定反馈与功能计划，缩放不自动追加。

- 用户决定延期透视相机探索，当前继续在 Orthographic 正交场景中完成各项功能。不是放弃探索：待当前功能完成、用户重新提出时恢复；必须复用现有美术，若需要补画多视角素材或建模才成立，则不采用。之前只提出另存 FishingPerspectiveTest 的教学步骤，未确认创建或测试完成。
- 继续中文教学、用户亲自写代码和操作 Unity，一次一个适量步骤；注释、Header、Tooltip、日志和 Region 名称用英文。用户确认“好了”后直接推进，不重复检查；必要时针对性读取依赖。此次仅更新开发文档。
- POWER 文字最终要求是始终显示、保留最后显示的蓄力数值直到下次蓄力更新；只有 Slider 随键鼠蓄力显隐。用户已确认显示恢复正常，不再删除 castChargeText。Slider 复用张力条素材的步骤已给出，完成情况未明确确认。
- ReelingButtonHUD 已使用移动端输入组件 isActiveAndEnabled 与 reelingController.IsActive 联合控制按钮显示，用户报告步骤完成；手机实际显示仍待设备验证。
- FishingLoopController 已实现落水最终倍率计算、成功上岸 Mathf.RoundToInt(基础分 × 锁定倍率)、scoreTuningProfile 缺失检查；源码已确认，用户报告各步骤完成，不等于全流程/全部平台验证通过。
- 用户允许的一次性维护：Codex 已处理 FishingLoopTest 场景合并冲突并做对象/引用静态检查；另按明确要求给 FishingLoopController 添加英文 Region，未改变逻辑。这些不改变后续默认教学边界。
- HookFlightProfile 已创建，正式飞行配置与 CastTuningProfile 的移动端手势/旧原型配置分离。PC 用蓄力、移动端用手势，统一输出 power；不能把移动端称作蓄力。
- FishingHookController 已改为 Launch(float power, float launchSpeedMultiplier = 1f)。固定角度、力度映射基础初速度，装备倍率作用于初速度且不写共享 Profile；装备系统本身尚未实现。使用恒定重力解析计算飞行时间和距离，不再由 Inspector 指定固定时长。
- 旧 5～25 仅是 Play Test 地图的临时对照，不是正式地图的最终范围或验收标准。当前源码已移除旧距离调参字段；flightDuration 是计算得到的运行字段。保存场景 YAML 尚可见旧 minimumCastDistance / maximumCastDistance / flightDuration 数据，但不再是当前源码的生效调参来源，不应手工批量清理场景。
- HookVisual 已从根对象分离，局部零位置/旋转、单位缩放，SpriteRenderer 配置保留；根对象保留玩法和物理组件、0.2 缩放。场景已绑定 flightProfile 与 hookVisual，静态接线检查完成。
- 当前 UpdateFlight 已计算虚拟高度并向世界 +Y 偏移图像，落水复位并先改 State 再发 Landed。前进和高度都投影到屏幕 Y，因此不会形成侧视弯曲轨迹；用户已明确需要的是明显腾空感，当前反馈仍不足。先前将这版称为可见弧线已更正。
- 下一具体步骤：在正交方案内继续讨论/落实水面投影标记、图像高度偏移和适度缩放的最小腾空表现；具体素材和参数尚待选定。先补 Dock() 对 hookVisual 局部偏移的复位（当前保存源码尚未加入），同时核对中断后再次 Dock 不残留显示状态。不要从 Profile 创建或 HookVisual 拆分重新开始。
- 后续顺序：正交腾空表现与飞行收尾 → 倍率/落水反馈 → 加速实际向岸距离和擦边奖励 → 鱼生成避遮挡 → 最小行为树/动画 → 成功失败结束反馈、说明、重开、Android 交付。PC Full Charge Duration 的 Profile 迁移仍待做；奖励数值和时间耗尽的待结算处理仍待决定。
- 验证边界：本次仅源码/场景针对性读取和文档更新，未运行编译、Play Mode、Android、WebGL 或系统回归；落水一次、GameOver 中断、不同力度、装备倍率、腾空复位仍需对应步骤的最小验证。

下方保留 2026-09-07 交接语境，其中旧“下一步”、删除 POWER、倍率待接入、飞行算法未定等描述已由本节取代。

## 直接从这里接手

先读根 `AGENTS.md` 和本文件；需要完整计划时再读 `docs/reference/2026-09-07-progress-and-plan.md`。旧 `2026-09-07-conversation-handoff.md` 是本轮开始前的快照，里面“从 TransitionTo 开始”已经过时。不要重新创建冷却、ScoreTuningProfile、距离文字或蓄力 Slider。

当前任务是继续教学开发，不是代写游戏。用户已认可重新整理后的实施顺序。下一小步：收尾 `FishingSessionHUD` 中蓄力条的显隐，去除对旧蓄力文字的依赖；之后补落水最终倍率锁定与上岸倍率结算。不要一次给出全部阶段代码。

## 必须延续的协作方式

- 简体中文解释；用户亲自写代码和操作 Unity；一次一个适量步骤，解释为什么、改哪里、如何观察，然后等反馈。
- 注释、Header、Tooltip、日志使用英文。用户学习中需要时解释布尔属性、取反、每帧更新等，不默认先考问题，也不要反复讲已理解内容。
- 用户非常不喜欢每次说“好了”就扫描、编译或重复测试。只有新步骤依赖现有代码，或出现具体故障时，才针对性读取。用户已确认的效果直接推进。
- UI 信息归 `FishingSessionHUD`，挂在现有独立职责对象上；不要为倍率、距离、PC 蓄力再拆重复 HUD 控制器，也不要随意挂脚本到 Canvas。
- 不重复保存同一个运行数据，不保留两套调参字段却只用一套。Profile 迁移要同步消费者并保留用户实调参数。
- 当前教学要求优先于 AGENTS.md 的一般直接修改授权。可直接维护文档，但不自行编辑游戏代码/场景，不提交、推送、开多代理或发送外部消息。
- Claude 是正式设计主要作者；记录用户直接确认的差异，不宣称 Claude 已批准或已收到说明。

## 已完成和验证边界

- 六态流程、鱼竞赛、提竿、收线/张力/断线、基础分/钩数/计时/GameOver 已有。历史 M7 未开始等清单已过时。
- 时机圈 revision 9 已由用户明确测试通过；不重新教学或覆盖时长/带宽调参。
- PostAttemptCooldown 已从 Profile 接入 TransitionTo、ReadyToCast 倒计时与输入门控；Striking/Reeling 返回 ReadyToCast 冷却、期间可移动，初次开局无冷却。用户反馈试玩通过。
- 张力显示和移动加速按钮沿用前次已完成内容；不把它们扩大为 Android 全流程通过。
- PC 蓄力输入已写入保存源码；用户确认竖条手动调 Value 会变、Play Mode 按住空格时 Value 会增长。不再从“Slider 是否会变化”开始排查。
- 本次交接只写文档，没有运行编译、Play Mode、Android、WebGL 或系统回归。旧 build 结果不代表当前版本；普通“好了”通常只表示完成当前操作。

## 当前源码实际状态（最近一次局部读取）

### KeyboardMouseFishingInputSource.cs

路径：`Assets/Scripts/Input/KeyboardMouseFishingInputSource.cs`。

- 原固定 `castPower` 已移除；现在 `fullChargeDuration`、`castChargeElapsed`、`isChargingCast` 管蓄力。脚本默认 fullChargeDuration 为 2 秒，实际值看 Inspector。
- `IsChargingCast` 与 `CastCharge01` 为只读属性。
- ReadyToCast 开放输入时：按下开始，按住累计 Time.deltaTime，满蓄力封顶，松开先清空蓄力再 `RaiseCastPerformed(power)`。
- SetCastEnabled(false) 与 OnDisable 清空蓄力。提竿/加速语义与先释放再加速机制保留。
- 以前悬空的 Min/Tooltip 描述“虚拟位置速度”，不是蓄力参数；最近读取已移除，不需再次处理。

### FishingSessionHUD.cs

路径：`Assets/Scripts/UI/FishingSessionHUD.cs`。

- 现有字段名是 `keyboardMouseFishingInputSource`，不要给代码时误用之前示例的 `keyboardMouseInput`。
- 已有 `multiplierText`、`castDistanceText`、`castChargeSlider`，仍有 `castChargeText`。
- 倍率显示读取 `loopController.CurrentDistanceMultiplier`；距离读取 `loopController.CurrentCastDistance`。
- showCastCharge 检查键鼠引用非空、isActiveAndEnabled、IsChargingCast。
- **待修：** 当前 `if (castChargeText.gameObject.activeSelf != showCastCharge)` 分支同时设置文字和 Slider 显隐。Slider 依赖文字状态，不是独立判断。用户最终要蓄力条，下一步移除旧文字字段/更新及不需要的场景文字，改用 Slider 自己的 activeSelf 判定，保留有效 Slider 引用。不要直接删对象却留下代码引用。
- showCastCharge 为 true 时，已有 `castChargeSlider.value = keyboardMouseFishingInputSource.CastCharge01`。
- 保存的 FishingLoopTest 中 Slider/键鼠引用已有；Slider 为 Bottom To Top、0..1、Whole Numbers 关闭、Handle Rect 清空。用户已布局，不重置位置/缩放。

### FishingLoopController.cs / ReelingController.cs

路径均为 `Assets/Scripts/Controller/`。

- ReelingController 已有 `DistanceToShore => Mathf.Abs(transform.position.y - shoreTarget.position.y)`，复用正式岸边引用。
- FishingLoopController 已有 `scoreTuningProfile`、`castDistance`、`CastDistance => castDistance`、`CurrentCastDistance => reelingController.DistanceToShore`、`CurrentDistanceMultiplier { get; private set; } = 1f`。
- **注意命名含义：** CurrentCastDistance 是当前鱼钩到岸距离，Reeling 中继续减少；castDistance 是 Casting 更新、落水保存的距离。不要因为名字相近创建另一份重复字段。
- EnterCasting 重置 castDistance 和倍率；UpdateCasting 更新距离、用 Profile 算倍率。
- **待补：** HandleHookLanded 只更新 castDistance / LandingPosition，没有用最终距离重算 CurrentDistanceMultiplier。须在进入 Baiting 前计算一次，避免上一帧倍率成为最终值。
- **待补：** HandleRetrievalCompleted 仍为 `int caughtScore = HookedFish.ScoreValue;`，随后 AddScore；倍率还没有用于结算。应使用锁定倍率并只在成功上岸一次结算，空钩不加分。
- **待补：** Start 只检查 loopProfile，没有 scoreTuningProfile 缺失检查。

### ScoreTuningProfile.cs

路径：`Assets/Scripts/ProfileCreator/ScoreTuningProfile.cs`；配置资产在既有 `Assets/Engineer Folder/Profiles/`。

- 用户已创建；distanceForMaxMultiplier 默认 10、maxDistanceMultiplier 默认 2；GetDistanceMultiplier 用 Clamp01(distance / threshold) 再 Lerp(1, max, progress)。实际数值以资产为准。
- 用户已认可线性封顶的可调原型。public 实例方法通过小写字段 `scoreTuningProfile.GetDistanceMultiplier(...)` 调用，不通过类名调用。
- 结算建议 Mathf.RoundToInt；注意恰好 .5 时是就近偶数，不误称永远向上。

## 用户已经确认的规则

1. PC 按住空格蓄力，松开抛竿；满值不自动释放。移动端继续手势输入。
2. 蓄力显示是竖条，只在键鼠路径蓄力时出现，不是额外百分比文字需求。
3. 倍率随抛竿实时增长，**落水时锁定**，不是咬钩时锁定。成功上岸用锁定倍率计分。
4. 距离 HUD 在 Casting 与 Reeling 都实时变化；收线距离减少不影响锁定倍率。
5. 倍率需要伴随 VFX，尚未实现、具体表现待选；不把纯文字显示当作反馈完成。

## 最新设计讨论：Flight Duration

用户认为固定 Flight Duration 生硬，要求飞行时间由模拟产生，不希望凭空规定时长。

当前正式 `FishingHookController` 仍在组件保存 Minimum Cast Distance、Maximum Cast Distance、Flight Duration，以力度插值距离并按固定时长飞行。这是现状，不是新方案。PC Full Charge Duration 也仍在输入组件；迁移到 Profile 尚未做。

Codex 提议：力度决定初速度，固定发射角度与重力产生飞行时间/轨迹；俯视 2D 的 Y 表示离岸距离，另用虚拟高度表现腾空。**用户尚未回答是否同时做可见弧线，也未选择算法。** 最后要求先重新规划和交接，不要假定已批准整套弹道实现。

讨论/实现边界：

- 保留正式 FishingHookController 的 Dock/Fly/Land 和 Landed 事件；不要把 CastBallController/GyroscopeCastTest 原型直接替换进正式流程。
- 若增加虚拟高度，UI 距离、计分、碰撞的逻辑坐标不能误含腾空显示偏移。
- 飞行 Profile 归属先检查现有 CastTuningProfile 消费者，其包含手势与原型用途，不能直接混用。
- 调参迁移同步消费者与资产引用，保留现值，不把旧距离/时长和新模拟配置留成两套生效来源。
- 验收需覆盖近/远蓄力、落水事件一次、倍率使用最终逻辑距离、GameOver 中断后不继续落水推进。按教学步骤最小观察，不立即启动全项目回归。

## 后续顺序（已获用户认可）

1. 收尾蓄力 HUD 独立显隐、落水倍率锁定、上岸倍率结算和引用检查。
2. 讨论飞行模拟最小范围与 Profile 迁移；确定后再实施，不预设可见弧线已获批。
3. 倍率/落水最小反馈。
4. 加速实际向岸距离奖励、每障碍每次成功擦边一次的奖励；仅挂鱼 Reeling 累计，成功上岸统一结算。数值及时间耗尽时待结算处理仍待确定。
5. 鱼生成避视觉遮挡：考虑鱼体/边距，有限重试，空间不足少生成；不是禁整条航道。
6. 最小鱼行为树和 Idle/Swim/Hooked 动画，FishBiteRaceController 保持唯一咬钩裁决方。
7. 成功/失败/结束反馈、操作说明、重开与 Android 设备交付。

旧“两天试玩”不滚动重置；实际截止日期未重新确认。完整暂停/再校准、诱饵摆动、随机障碍、复杂鱼群、WebGL 专项和系统性回归仍延期，不擅自恢复或删去。

## 可直接贴到新对话的启动消息

请读取 AGENTS.md 和 docs/reference/2026-09-07-development-handoff.md，接手 Seven Seas。继续中文教学，我亲自写代码和操作 Unity，代码注释全部英文，一次一个适量步骤，不要每次“好了”都重新检查。时机圈和冷却已通过，PC 空格蓄力与竖向蓄力条已接入，运行时 Value 增长已确认。距离/倍率由 FishingSessionHUD 显示。先接续 HUD 显隐收尾，再补落水最终倍率锁定与上岸结算；不要重建已存在的类、Profile 或 UI。随后讨论模拟飞行时间及 Profile 迁移，可见弧线与具体算法尚未确定。先不要代写游戏代码。

## 2026-09-08 补充：编辑器布局刷新问题

用户已确认：将 Game 与 Scene 并排保持可见，并固定 Game 为 1440×2304 竖屏预设后，解决了“改完代码返回 Unity，Scene 中 HUD 乱掉，切 Game 才恢复”的现象。保留这个编辑器布局即可，不重新摆 UI 或修改游戏布局代码。判断为与隐藏 Game View 后 Canvas/尺寸刷新有关，未锁定具体 Unity bug；没有独立复现或修改代码。
