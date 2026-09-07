# Seven Seas 开发交接 — 2026-09-07

## 交接原因与证据边界

原任务「熟悉项目并接手开发」出现历史内容缺失、side chat 无法打开的用户报告。历史读取接口也曾返回过早的消息并标记进行中，与当前上下文不一致；尚不能确认记录永久删除或具体根因。旧任务 ID：01a07841-6328-7c42-8465-132bd8f94c1c。

本交接整合可用对话与本次少量源码核对，不是逐字聊天备份。优先使用用户最新反馈和保存的代码，不把旧进度清单当成当前实现。本次仅创建交接文档，没有改游戏代码或场景，没有运行编译、Play Mode 或设备测试。

## 新对话必须延续的协作方式

- 中文教学，用户亲自写代码和操作 Unity；一次给一个完整但适量的步骤，解释实现思路、改哪里和关键原因，等待反馈。不要直接实现整个阶段。
- 所有代码注释使用英文；代码中的 Header、Tooltip、日志也保持英文。
- 不要每次用户说“好了”都重新扫描项目、编译或要求截图。用户明确表示这样太慢。只有新步骤确实依赖现有实现时才读取相关文件。
- 用户已理解的概念不要重讲、不要默认先考问题。用户说功能通过后直接进入下一步。
- 用户暂缓系统性测试，当前以自己体验可玩为准；延期测试不能标成通过。
- 用户不希望 HUD 脚本随意挂 Canvas，使用独立职责对象和现有项目结构。
- Claude 是主要设计文档作者；Codex 负责实现教学与技术衔接。不要擅改玩法或宣称已向 Claude 汇报。
- Profile 调参避免重复配置来源。之前曾因旧 TargetBandInner/Outer 与新 TargetBandWidth 同时存在而引起用户质疑；迁移必须同步消费者、删除旧逻辑，不能让两套字段并存却只用一套。
- 根 AGENTS.md 的协作授权仍在；但当前明确教学请求优先。不要擅自提交、推送、覆盖用户场景或开多代理。

## 最重要：立即接续的位置

时机圈 revision 9 用户已明确表示功能完成、测试通过。下一步是 **完成一次尝试结束后返回 ReadyToCast 的抛竿冷却**。

旧对话已经给过完整接入说明，但用户尚未报告这一步完成。本次 2026-09-07 的局部源码读取确认它是“只接了一部分”，不要让用户从头创建重复类：

- `Assets/Scripts/ProfileCreator/FishingLoopProfile.cs` 已存在，只有 PostAttemptCooldown，默认 0.8 秒；配置资产 `Assets/Engineer Folder/Profiles/FishingLoopProfile.asset` 已存在。
- `Assets/Scripts/Controller/FishingLoopController.cs` 已加入 loopProfile 引用、castCooldownRemaining、IsCastCoolingDown，以及 Start 中 Profile 缺失检查。
- **TransitionTo 仍是旧逻辑**：ExitState → CurrentState = nextState → EnterState；尚未设置冷却。
- **UpdateReadyToCast 仍为空**。
- **EnterReadyToCast 仍立即开启 castGestureDetector 和 SetCastEnabled(true)**。
- **HandleCastDetected 仍只检查 ReadyToCast，没有检查 IsCastCoolingDown**。
- 未发现 SetCastingAvailable 方法；本次没有核对场景的 loopProfile 引用是否已拖入。

建议新对话第一步：简短说明“Profile 和字段已有，接下来只改 TransitionTo，让从 Striking/Reeling 返回 ReadyToCast 时设置冷却”。给该方法的小段替换代码，英文注释解释必须在覆盖 CurrentState 前记录来源。后续再接 Update、输入门控和场景引用，不一次倾倒全部改动。

### 冷却的已确认规则

- 保留 Striking 内失败点击冷却 AttemptCooldown（0.35 秒），错误点击不直接扣钩；超时才结束尝试并扣钩。
- 另加 PostAttemptCooldown（默认 0.8 秒）：从 Reeling 返回 ReadyToCast，包括成功上岸、空钩、碰撞、断线；用户额外明确要求 Striking 超时返回 ReadyToCast 也有。
- 首次进入游戏 ReadyToCast 不冷却；Striking 成功进入 Reeling 不加等待；进入 GameOver 不安排下一次抛竿。
- 不增加第七个状态。在 ReadyToCast 内用控制器倒计时（Time.deltaTime），冷却期间可左右移动，局内时间继续走。
- 同时关闭语义 Cast 输入和 CastGestureDetector；HandleCastDetected 再加冷却守卫。倒计时归零后一起恢复。
- 剩余时间属于控制器运行状态，不放共享 Profile。
- 起始钩数、局时未来可迁到 FishingLoopProfile，但本步不要先复制配置，须后续一起迁移 HookTracker/SessionTimer 消费者。

## 已完成或已报告状态

### 张力 UI 与收线

- TensionBarHUD、收线张力与断线、上岸计分、钩数、计时、GameOver 基础循环已有。
- 用户希望条体按进度揭示更多图像而非拉伸尺寸；Fill 使用 Image Filled / Vertical / Bottom，指针由 Slider value 驱动。
- 曾有指针终点和图像高度不匹配；调整 Fill Area / Handle Slide Area 一致后用户确认正常。
- 用户采用 GalaxyPad Portrait 1440x2304（5:8）调布局；TensionSlider scale 3.45，位置用户已调整。不要重置布局。
- 加速与横移张力相加；横移风险应依据限位后的实际横移速度，不依据输入意图。用户曾把横移张力参数调至 0.9，后续数值以当前资产为准。

### Android 加速按钮

- 用户创建了圆形 AccelerateButton，Knob Sprite，Simple Image，约 160x160。
- 使用 Event Trigger：Pointer Down → MobileFishingInputSource.PressAccelerate；Pointer Up → ReleaseAccelerate。Button OnClick 留空，因为需要按住持续加速和松手停止。
- Navigation 的 Visualize 箭头只是 UI 导航显示；触屏按钮可设 None。
- `Assets/Scripts/UI/ReelingButtonHUD.cs` 已由用户创建、挂到独立 HUD 对象，用 ReelingController.IsActive 显示子按钮，隐藏前 ReleaseAccelerate。
- 用户确认完成这些操作；不要据此宣称 Android 实机全流程已验证。

### 时机圈 revision 9

- 每次 Striking 进入时随机目标带中心，一次尝试内固定。用 Width 与中心生成实际 Inner/Outer；HUD 读取控制器产生的边界。
- progress01 = Clamp01(elapsedTime / safeDuration)，RingRadius01 = Abs(1 - 2 * progress01)，实现 1 → 0 → 1 双程。
- 隐藏命中宽容值扩展显示带两边，不改变显示带。
- 设计建议：width 0.16，中心 0.25..0.70，forgiveness 0.04，attemptCooldown 0.35，双程总时长 2 秒。
- **本次看到 StrikeWindowProfile.cs 的 windowDuration 默认值已经是 3f**，不能拿文档 2 秒覆盖用户调参。实际运行值以引用的 .asset 为准，本次未读取该资产参数。
- 老固定带字段已从本次读取的 Profile 中移除。用户明确报告本块功能测试通过；Codex 本次没有重跑。

## 剩余主要待办与规则

用户在旧对话提出试玩日只剩两天，目标 Android 手机/平板，最不满意“整体看起来不像完成的游戏，表现和反馈不足”。这是当时的相对期限，新对话不要把“两天”无限重置；必要时确认实际试玩日期。

### 1. 技巧得分与 UI 表现

已讨论认可的方向：

`Total = FishBaseScore * LongitudinalDistanceMultiplier + NearMissBonuses + AcceleratedRetrievalDistance * BonusPerUnit`

- FishBaseScore 随鱼类型变化。
- 距离奖励用钓到鱼的位置到岸的纵向距离，不计算反复左右拖动距离，也避免抛远与同一收线距离重复奖励。
- 技巧分通过极限避障：挂鱼且 Reeling，靠近障碍外侧安全带、未碰撞并成功越过障碍后奖励；每个障碍每次尝试仅一次，不是一靠近就给分。
- 加速奖励依据实际向岸收线距离，不是单纯按住按钮时间。
- 这些作为本次待结算奖励，成功上岸才一次计入 ScoreTracker；碰撞或断线失去本次待结算奖励。局时结束时待结算奖励作废是提议，尚需确认。
- UI 显示待结算增长和上岸基础分/倍率/技巧奖励/总分动效；UI 不是第二个计分源。
- 系数、距离曲线、取整尚未最终定值，先提出明确可调原型，不宣称已设计完毕。调参放 Profile。

### 2. 鱼生成避开视觉遮挡

- 用户说“不能生成在障碍物正下方”指鱼被岩石画面遮住，不是禁止整条向岸路线或整列位置。
- 排除区域应覆盖视觉障碍范围、鱼体尺寸和安全边距，不只测试鱼中心。
- 保留鱼间距、限制生成重试次数；空间不足允许少生成。
- 鱼游动过程也避开遮挡可另讨论，不自动扩张为完整寻路系统。

### 3. 鱼 AI 与动画

- 用户要求最小 AI behavior tree 和 Idle / Swim to / Be hooked 等动画状态。
- 当前 FishController 有 Idle/Approaching/Hooked 逻辑，Idle 还缺游动表现；FishBiteRaceController 必须仍是唯一咬钩获胜者决定者。
- 原型建议优先级：Hooked > 追逐被选中的饵 > Idle/Wander。先小型行为树和最小 Animator，不引入完整 BT 编辑器、群集模拟或复杂寻路。

### 4. 试玩呈现与进入/再来一局

- 加速反馈、近失奖励提示、上岸计分动效、失败/结束反馈、简短操作说明及直接重开入口。
- 旧 PausePanelButton 实际回 MainMenu，并非完整暂停；不要误报已经有完整重开/暂停 UX。
- 抛竿弧线、复杂暂停、额外个人校准、饵抖动、随机关卡、WebGL专项等保留延期，不悄悄恢复成当前优先项。

建议顺序：收尾冷却 → 技巧计分核心与最小反馈 → 遮挡生成 → 最小鱼行为/动画 → 补齐试玩呈现，预留构建与 Android 体验时间。不要重启已经通过的时机圈教学。

## 架构与文件入口

- 根 AGENTS.md：长期约定；README.md：课程约束。
- `docs/design/2026-08-29-fishing-loop-design.md`：Claude 的 revision 9 设计。
- `docs/design/2026-08-29-fishing-loop-teaching-plan.md`：教学路线，旧条目可能滞后。
- `docs/design/2026-09-06-android-playtest-implementation-plan.md`：近期实施计划。
- `docs/design/2026-09-06-claude-design-change-brief.md`：给 Claude 的设计改动说明，已准备、没有代用户发送。
- `task_plan.md`、`findings.md`、`progress.md`：混有历史记录；本交接补充最近未完整落盘的对话。
- `docs/reference/project-handoff.md`：更早的源码地图，不要与本交接的最新进度混淆。
- **当前 Profile 脚本目录是 `Assets/Scripts/ProfileCreator/`**，不是旧记录中的 Controller/ProfileCreator；本次实际查找确认。
- Profile 资产目录：`Assets/Engineer Folder/Profiles/`。
- 正式玩法场景 `Assets/Scenes/FishingLoopTest.unity`；另有 FishingLoopTest2，过去 Build Settings/SceneCatalog 指向前者，后续按实际资产核对。
- FishingLoopController 协调状态与结果；StrikeController 管提竿；ReelingController 管收线/实际速度/张力；FishingInputSource 是输入语义边界；FishingInputRouter 选平台。
- HookTracker / ScoreTracker / SessionTimer 是局内数据源；HUD 读数据；FishController 管单条鱼、FishBiteRaceController 管唯一获胜者。
- 正式 FishingHookController 与 CastBallController/GyroscopeCastTest 原型不要混用。
- 保留 .meta/GUID 和用户未提交改动。不要因文件移动而重建资产。

## 验证边界

- 本次只静态读取冷却相关源码、Profile 和进度文档。
- 时机圈、张力显示正常是用户报告；没有替用户运行。
- 更早一次 dotnet build 曾 0 errors / 2 CS0649 SceneCatalog warnings；不代表当前版本编译已通过。
- Android、WebGL、完整核心流程回归未在本交接确认通过。无需接手第一轮就启动整套测试。

## 新对话启动提示词

请先读 AGENTS.md 和 docs/reference/2026-09-07-conversation-handoff.md，接手 Seven Seas。继续中文教学模式，我亲自写代码；代码注释全部英文，一次一个适量步骤，不要每步都重新检查或启动测试。时机圈已由我测试通过，现在接续 FishingLoopController 的 post-attempt cooldown。Profile、字段和 Start 检查已有，TransitionTo/UpdateReadyToCast/输入门控仍待接入；先教我改 TransitionTo，不要从头重做，不要直接修改游戏代码。
