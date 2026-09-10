# 鱼群生态设计修订

日期：2026-09-10。设计者：Claude。性质：**设计修订，尚未实现**。
基线：2026-08-29-fishing-loop-design.md revision 9 + 2026-09-06 变更简报 + 2026-09-09 已实现的逃离/捕食第一批。
授权来源：用户在 2026-09-09/10 会话中提出五条生态需求并逐项确认取舍；同一会话追加取消钩数机制的决定。

本文件只描述规则与参数归属。实现工单见同目录 `2026-09-10-fish-ecology-codex-brief.md`。

---

## 0. 与现状的差异总览

| 项 | 现状（已实现） | 本修订 |
|---|---|---|
| 个体差异 | 无。四类 prefab `swimSpeed` 全为 2 | 新增每条鱼的饱食度，驱动抢饵意愿与速度 |
| 落钩反应 | 无。落钩直接开赛 | 新增惊吓退散，退散结束后开赛 |
| 抢饵资格 | 6m 内候选全部参赛 | 按饱食度抽参赛，含保底 |
| 逃离 | 仅 Reeling、仅 Small | 保留；另新增 Baiting 惊吓（全类别） |
| 捕食者 | Large + Special | **Large only**（Special 退出） |
| 猎物 | 仅挂钩的 Small | 挂钩的 Small **和 Medium**，概率分列 |
| Medium | 不吃不被吃 | 不吃，**会被吃**（纯猎物） |
| Special | 参与捕食 | **不吃也不被吃**，饱食度偏高 |
| 大鱼难度 | 仅加速张力倍率 1.3 | 追加 Reeling 中途二次挣扎 |
| 失败代价 | 扣一钩，钩尽 GameOver | **取消钩数**；仅时间为限制资源 |
| GameOver 入口 | 钩尽 或 超时 | **仅超时** |

**已记录决定的反转**：`specialCanPredate` 当前资产值为 1，findings.md:17 记为「用户明确决定」。本修订按用户新决定反转为 false，属设计修订，不是缺陷修复。

**文档与资产不一致的更正**：progress.md 记 `predationProbability = 0.3`，实际资产值为 **0.5**。本修订以资产值为准继承。

---

## 1. 饱食度（Satiety）

### 1.1 数据

每条鱼实例持有 `Satiety`，取值 0 到 1，0 = 最饿，1 = 最饱。

- **生成时**按分类抽一次（在 `FishSpawner` 配置移动参数的同一时机）。
- **整局固定**，不按竿重抽。（用户决定：每条鱼有稳定「性格」，可读性优于纯噪声。）
- **捕食成功后**该捕食者饱食度置为 `satietyAfterFeeding`（默认 1.0）。
- **可选衰减** `satietyDecayPerSecond`：单局仅 90 秒，衰减在一局内影响很小，列为第二批；第一批不实现时该字段留 0。

### 1.2 影响一：抢饵参赛意愿

落钩惊吓结束、竞赛开始前，对每条候选鱼抽一次：

```text
joinChance = Lerp(joinChanceAtEmpty, joinChanceAtFull, satiety)
若 category == Special:
    joinChance = Max(joinChance, specialMinJoinChance)
```

- 抽中：正常 `BeginApproach` 参赛。
- 未抽中：**弃权**，不参赛，掉头游回 Idle。
- **每竿抽一次**，不逐帧抽。抽签结果在本竿内固定。

**保底**：若候选集非空但抽签后参赛数低于 `guaranteedParticipants`（默认 1），按饱食度升序补足最饿的若干条强制参赛。

> 理由：玩家抛得准却因为不可见的随机数一无所获，是无反馈惩罚。保底把「空钩」限制为「鱼太远、游不到、超时」这类玩家能理解的原因。

### 1.3 影响二：接近速度

参赛鱼在 `Approaching` 状态下的移动速度：

```text
approachSpeed = swimSpeed * Lerp(approachSpeedAtEmpty, approachSpeedAtFull, satiety)
```

仅作用于追饵，不影响 Idle、惊吓和捕食追击（后两者有各自倍率）。

### 1.4 影响三：大鱼捕食欲望

见 3.2。

### 1.5 可见性要求（非可选）

饱食度对玩家不可见，因此**弃权必须有可见表现**：被惊吓后不返回、掉头游开。玩家要能读出「它们不想吃」，而不是「什么都没发生」。
不新增 HUD 元素，用行为表达。

---

## 2. 落钩惊吓（Startle）

### 2.1 时序

```text
Hook Landed
  -> 落钩瞬间抓取候选集（半径 startleRadius）      候选在惊吓前锁定
  -> 候选全部惊吓退散（全类别一致，本批不分级）
  -> 惊吓结束（到达退散点 或 startleDuration 到期，取先到者）
  -> 对候选逐条抽参赛（见 1.2）
  -> 竞赛开始，baitingDuration 从此刻起计
```

### 2.2 规则

- 惊吓方向为远离鱼钩；目标距离 `startleDistance`；速度 `swimSpeed * startleSpeedMultiplier`。
- 惊吓**对所有分类一致**（用户决定：分级表现留作后续）。
- 惊吓期间不判定咬钩。
- 已被惊吓但无法导航（被障碍卡住等）的鱼，超时后按原地处理，仍参与抽签。

### 2.3 候选集必须在惊吓前锁定

若先惊吓再抓候选，鱼已被推出半径，候选会锐减甚至为空。语义上候选集等于「听见落水声的鱼」，因此在落钩瞬间锁定。
`startleRadius` 起始值与现有 `attractionRadius`（6）一致，保持两者语义统一。

### 2.4 惊吓必须短

单局 90 秒，一次完整抛竿循环约 10 到 18 秒，全局只有 **6 到 9 竿**。Baiting 每增加 1 秒，约损失 7% 的可玩竿数。
因此 `startleDistance` 取 1.2、`startleDuration` 取 0.5，惊吓加返回合计约 1.0 到 1.2 秒。
**调参时优先压缩惊吓时长，不要靠加大 baitingDuration 补偿。**

---

## 3. 捕食关系

### 3.1 关系表

| 捕食者 | 猎物 | 说明 |
|---|---|---|
| Large | 挂钩的 Small、挂钩的 Medium | 通吃 |
| Medium | 无 | 纯猎物 |
| Small | 无 | 纯猎物 |
| Special | 无 | 不吃也不被吃 |

- 捕食**只针对已挂钩的鱼**，不捕食自由游动的鱼（沿用现有规则，避免与竞赛争夺同一条鱼的所有权）。
- **每竿最多完成一次替换**（沿用）。
- 每条捕食者**每竿只抽一次**，成功失败都记录，不重抽（沿用）。
- Special 既不是捕食者也不是猎物；`IsPredator(Special)` 与 `IsPrey(Special)` 均为 false。

### 3.2 概率

```text
baseProb  = 猎物为 Small  -> predationProbabilitySmallPrey  (0.5)
            猎物为 Medium -> predationProbabilityMediumPrey (0.2)

finalProb = baseProb * Lerp(predationSatietyScaleAtEmpty,
                            predationSatietyScaleAtFull,
                            捕食者.Satiety)
```

- 吃中鱼概率显著低于吃小鱼。**理由**：玩家丢一条中鱼的痛感远高于小鱼，而且捕食是玩家无法拒绝的强制替换。
- 饱食度缩放实现「大鱼越饿越想捕食」。

### 3.3 强制升级的设计立场

玩家钩到 Medium 被 Large 抢走，会被换成一条更难拉、还会二次挣扎的鱼。这是**风险与收益同时上升的强制事件**，不是纯奖励：

- 收益：基础分 10 变 25，加时 4 秒变 6 秒。
- 风险：张力倍率 1.0 变 1.3，且新增一次挣扎判定，有丢鱼可能。

保持这个张力是有意的，但概率必须低（0.2 起步）。若试玩显示「钩到中鱼反而更差」，先调低 `predationProbabilityMediumPrey`，不要削弱 Large 的难度。

---

## 4. 大鱼难度与二次挣扎

### 4.1 难度只加一层

现有难度表达仅有加速张力倍率（Small 0.8 / Medium 1.0 / Large 1.3 / Special 1.3）。
本修订**只追加二次挣扎一层**，不同时叠加收线减速或张力衰减惩罚。

理由：Large 权重仅 10/80，一局平均出现约 3 条。三层惩罚叠加会把「高光时刻」变成「看到就想放弃」。

### 4.2 触发

- 每类鱼的挣扎次数由 `<category>StruggleCount` 配置：Small 0 / Medium 0 / **Large 1** / **Special 1**。
- 触发点在**进入 Reeling 时一次性掷定**，不逐帧判定：

```text
triggerAt01 = Random.Range(struggleTriggerRange01.x, struggleTriggerRange01.y)
进度 = 已收回距离 / 落钩时的总距离
```

  默认区间 `(0.35, 0.65)`。首尾各留出余量，避免刚进收线或即将靠岸时突然打断。
- 捕食替换发生后，若新的挂钩鱼有挣扎次数而本竿尚未掷定，则在替换时掷定；若已过触发点则本竿不再挣扎。

### 4.3 挣扎期间的行为

进入挣扎窗口时：

| 项 | 处理 |
|---|---|
| 鱼钩前进 | **冻结**（收线暂停） |
| 张力累积 | **冻结**，且**保持当前值不清零** |
| 移动输入 | **禁用**（`SetMoveEnabled(false)`） |
| 加速输入 | **禁用**，并要求重新按下 |
| 判定 | 复用 `StrikeController`，配独立的 `StrikeWindowProfile` 资产 |
| HUD | 复用现有时机圈 |

**冻结移动是必需的，不是保险措施**：移动端上挑手势与倾斜转向共用姿态传感器，不冻结就会互相干扰。冻结后两者在时间上互斥，冲突消失。

### 4.4 输入

复用现有 Strike 通路（用户决定）：

- PC：空格。
- 移动端：上挑手势（`strikeGestureDetector.GestureTriggered`）。

**待确认的约束（推断，证据等级：代码注释）**：PC 上 Strike 与 Accelerate 很可能绑定同一个空格键。
依据是 `KeyboardMouseFishingInputSource` 中那句注释「Entering Reeling while Space is still held must not immediately activate acceleration」及 `accelerateRequiresRelease` 的存在——进入 Reeling 的前一个状态正是 Striking，空格会「还按着」最自然的解释就是提竿也用空格。
**但这一条无法从静态资产确认**：`InputSystem_Actions.inputactions.meta` 的 `internalIDToNameTable` 为空，场景里的 `strikeAction` / `accelerateAction` 只有 fileID，解析不出 action 名。实施前须在编辑器内核对。

若同键成立：玩家进入挣扎时通常正按住空格加速，而 Strike 走 `WasPressedThisFrame()`，按住状态不会触发，因此**必须先松开再重按**。

- 这个手感是可接受的（「松手，再猛提一杆」），但**挣扎窗口时长必须容纳松手加反应加重按**。
- 挣扎用的 `StrikeWindowProfile` 起始 `windowDuration` 取 **1.4 秒**，不沿用提竿的 2.0 秒，也不得低于 1.0 秒。
- 挣扎开始时必须主动置位「需要重新按下加速」，让松手成为被要求的动作而不是意外。

若核对后发现两者**不同键**，1.4 秒可以适当下调（下限仍为 1.0 秒），其余规则不变。移动端无论如何都要冻结移动输入，因为上挑与倾斜共用姿态传感器，这一条与 PC 绑定无关。

### 4.5 结果

| 结果 | 处理 |
|---|---|
| 成功 | 恢复收线、张力累积、移动与加速输入。张力沿用挣扎前的值。本竿该次挣扎不再触发。 |
| 失败或超时 | **鱼逃脱**：挂钩鱼 `ResetToIdle()`，`HookedFish` 置空，**收线以空钩继续到岸**，不结算分数、不加时。**不判定为一次失败尝试**，不触发 `AttemptFailed`。 |

- 失败路径与现有「Baiting 超时进入空钩 Reeling」语义一致，玩家仍需把空钩收回岸。
- 已累计的加速奖励距离在无鱼后不再增长（现有 `HandleAcceleratedDistanceMoved` 已按 `HookedFish != null` 门控），并在无鱼上岸时不结算。
- 逃脱的鱼不视为进食，饱食度不变。

---

## 5. Special

- 不捕食，不被捕食。
- 饱食度抽样区间显著偏高（`satietyRangeSpecial` 默认 `(0.60, 1.00)`）。
- **弃权封顶 60%**：`specialMinJoinChance = 0.40`，保证可钓。
- 仍会被惊吓，仍可抢饵，仍有一次挣扎。

**稀有度复利警告**：Special 权重 5/80，25 条鱼中约 1.6 条，进入 6 米候选圈本就少见。若再无参赛下限，很可能整局遇不到。分数 50、加时 6 秒并不足以补偿「整局钓不到」。
`specialMinJoinChance` 是这条规则的安全阀，**调参时不得下调到 0.25 以下**。

---

## 6. 取消钩数机制

### 6.1 规则

- 移除 `HookTracker` 及其全部引用。
- 三条失败路径（落点障碍、提竿超时、收线失败）**不再扣钩**，一律返回 `ReadyToCast`。
- `GameOver` **只剩超时一条入口**（`SessionTimer.Expired`）。
- HUD 移除 `Hooks:` 行。
- `AttemptFailed` 事件、`AttemptFailureReason` 与音效、震动反馈**全部保留**。失败仍要有反馈，只是不再扣资源。

### 6.2 无限续时检查（已验证不成立）

时间奖励为 Small +2 / Medium +4 / Large +6 / Special +6 秒，而一次完整抛竿循环约 10 到 18 秒。即使每竿必中 Large，时间仍净减少。

**结论：时间只会单调减少，不存在靠钓鱼无限续局的漏洞。** 后续调整时间奖励时必须重新核对这条。

### 6.3 失败代价的重新安置

钩数取消后，失败的唯一代价是浪费的时间（约占单局 10% 到 18%）加上 0.8 秒冷却。这个代价是真实的，但**不可见**，玩家感觉不到被惩罚。

#### 现有 `postAttemptCooldown` 不能直接当惩罚用

先澄清一个容易误记的点：`postAttemptCooldown` **不是失败专用的**。
`TransitionTo` 的条件只看「从 Casting / Striking / Reeling 返回 ReadyToCast」
（`FishingLoopController.cs:159`），**成功上岸与空钩返回也会触发同一个 0.8 秒**。
它目前的角色是「每竿之间的节奏喘息」，不表达代价。

#### 按结果分档（用户 2026-09-10 决定）

把单一冷却拆成按结果查表：成功与失败分离，失败内部再按玩家失误的性质分档。

| 结果 | 冷却 | 定位 | 理由 |
|---|---|---|---|
| 收鱼上岸 | 0.8 | 节奏 | 不是惩罚 |
| 空钩上岸 | 0.8 | 节奏 | 没有玩家失误，鱼不咬不该罚 |
| 提竿超时 | 1.0 | 轻 | 已经损失一条到嘴的鱼，不宜双重惩罚 |
| 落点撞石 | 1.5 | 中 | 抛竿失误。时间代价最低、最容易乱抛，需要额外刹车 |
| 收线撞石 | 1.8 | 中重 | 走位失误，部分受地图运气影响 |
| 断线 | 2.5 | 重 | 完全自作自受，且正好对冲 6.5 提到的「加速变强势」 |

**档位差必须够大才有意义。** 相差 0.3 到 0.4 秒玩家根本感知不到，那就只是看不见的调参复杂度。
上表最轻与最重相差 1.5 秒，是能感觉出来的量级。**调参时保持这个跨度，不要把六个值收敛到彼此接近。**

#### 前置改动：`AttemptFailureReason` 需要拆分

当前 `ReelingController` 对**撞石**和**断线**触发的是同一个 `AttemptFailed`，
`FishingLoopController` 统一映射成 `AttemptFailureReason.ReelingFailure`，两者无法区分。
要实现上表，必须把 `ReelingFailure` 拆成 `ReelingObstacle` 与 `LineSnap`。

**这个拆分是安全的**：现有两个消费方 `FishingAudioFeedback.HandleFailure` 与
`FishingHaptics.HandleFailure` 都**不读** `Reason` 字段，没有 switch 需要补分支。
拆完后它们也具备了做差异化反馈的条件（断线和撞石本就该有不同音效），但那属于后续，不在本批范围。

#### 冷却作为惩罚手段的局限（实施后需实测）

必须说清楚：**冷却是一种偏弱的惩罚**。
`EnterReadyToCast` 在冷却期间仍然 `SetMoveEnabled(true)` 并启用 `shoreLaneController`，
玩家可以继续左右选位——也就是说这段时间**本来就要花在瞄准下一竿上**，并非纯粹的死时间。

所以 2.5 秒的断线惩罚，实际痛感可能明显小于账面。
**验收时要实测断线是否真的让人不想再乱加速**；若无感，再考虑对断线单独改用扣时间
（需给 `SessionTimer` 加减时接口）。不要靠继续加长冷却来补——超过 3 秒会开始像卡顿而不是惩罚。

### 6.4 单局时长改为开发者面板可调（用户 2026-09-10 追加需求）

时间成为唯一限制资源后，单局时长直接决定整个体验的松紧，需要能针对不同 playtest 场景快速切换。

**规则**

- 默认单局时长由 90 秒改为 **60 秒**。
- 开发者面板新增单局时长调节项，范围 **30 到 180 秒**，步进 15 秒。
- 修改**在下一局生效**（`SessionTimer.BeginSession` 时读取），不修改进行中的对局。
- 该值是**开发调试设置，不是玩法配置**，因此存 PlayerPrefs（沿用 `LocalPlayerProgress` 的写法），不进 Profile。
- 提供「恢复默认」，回到 60 秒。

**现状与两个坑**

1. `sessionDuration` 是 `SessionTimer` 上的序列化字段，四个玩法场景（`FishingLoopTest`、`Level 1`、`Level 2`、`Level 3`）**各存一份，当前都是 90**。只改一个场景不会生效，必须四个都改成 60，或者让运行时覆盖值成为唯一来源。
2. **`DeveloperPanel` 只存在于 `FishingLoopTest`**（经 `MenuSettingsPanel` 打开），`Level 1/2/3` 里没有。因此调节项不能直接写场景内的 `SessionTimer` 引用——在正式随机地图里那个面板根本不存在。

**做法**：PlayerPrefs 存覆盖值，`SessionTimer.BeginSession()` 读取覆盖值（无覆盖值时回落到序列化的 60）。这样无论从哪个场景设置、加载到哪张地图都生效，也不需要给 Level 1/2/3 补面板。

**关联**：6.2 的「不存在无限续时漏洞」结论在 60 秒下依然成立，且余量更大。但若把单局时长调到 30 秒，一次抛竿循环（10 到 18 秒）就占掉三分之一以上，届时只够 2 到 3 竿——**30 秒档只适合测单竿手感，不适合测整体节奏或计分平衡。**

### 6.5 张力系统会不会失去意义

会有变化，但结论是可接受的。

钩数在时，断线是离散重罚。取消后，断线的代价变成「丢掉这条鱼，加上浪费的剩余收线时间，加上失败冷却」。由于加速能省时间，而时间是唯一资源，**激进加速的性价比整体上升**。

但收益随鱼种分化：断掉一条 Large 在 3 倍距离倍率下相当于放弃 75 分，远超省下的 1 到 3 秒；断掉一条 Small 只放弃 5 到 15 分。
而**鱼种在提竿成功时就已揭示**（`HandleStrikeSucceeded` 调用 `Reveal()`），玩家在收线前就知道钩到了什么。

因此预期玩家策略是：**小鱼猛拉，大鱼谨慎**。这是一个可读、需要识别鱼种的风险决策，属于设计收益而非退化。**予以保留，不做补偿性削弱。**

---

## 7. 状态转移表（修订后）

| 起点 | 事件 | 终点 | 冷却 |
|---|---|---|---|
| ReadyToCast | 抛竿 | Casting | 无 |
| Casting | 落点在障碍上 | ReadyToCast | **落点撞石 1.5** |
| Casting | 正常落水 | Baiting（惊吓、抽签、竞赛） | 无 |
| Baiting | 有鱼咬钩 | Striking | 无 |
| Baiting | 超时（含全员弃权后无鱼到达） | Reeling（空钩） | 无 |
| Striking | 带外尝试 | 留在 Striking | attemptCooldown |
| Striking | 成功 | Reeling | 无 |
| Striking | 超时 | ReadyToCast | **提竿超时 1.0** |
| **Reeling** | **到达挣扎触发点** | **留在 Reeling（挣扎子阶段）** | 无 |
| **Reeling 挣扎** | **成功** | **继续 Reeling，张力保留** | 无 |
| **Reeling 挣扎** | **失败或超时** | **继续 Reeling（空钩），不算失败尝试** | 无 |
| Reeling | 收鱼上岸 或 空钩上岸 | ReadyToCast | **0.8** |
| Reeling | 撞石 | ReadyToCast | **收线撞石 1.8** |
| Reeling | 断线 | ReadyToCast | **断线 2.5** |
| 任意 | 计时结束 | GameOver | 无 |

**挣扎不是顶层状态。** 原因见工单中的陷阱说明。

---

## 8. Profile 字段

### 8.1 FishEcologyProfile（新增）

| 字段 | 起始值 | 说明 |
|---|---|---|
| `satietyRangeSmall` | (0.00, 0.50) | 生成时抽样区间 |
| `satietyRangeMedium` | (0.10, 0.60) | |
| `satietyRangeLarge` | (0.20, 0.70) | |
| `satietyRangeSpecial` | (0.60, 1.00) | 明显偏高 |
| `satietyAfterFeeding` | 1.00 | 捕食成功后置值 |
| `satietyDecayPerSecond` | 0.00 | 第二批；单局 90 秒内影响很小 |
| `joinChanceAtEmpty` | 0.95 | satiety 为 0 时的参赛概率 |
| `joinChanceAtFull` | 0.15 | satiety 为 1 时的参赛概率 |
| `specialMinJoinChance` | 0.40 | Special 参赛下限，**不得低于 0.25** |
| `guaranteedParticipants` | 1 | 候选非空时的最少参赛数 |
| `approachSpeedAtEmpty` | 1.35 | 追饵速度倍率（相对 swimSpeed） |
| `approachSpeedAtFull` | 0.75 | |
| `startleRadius` | 6.0 | 与 `attractionRadius` 保持一致 |
| `startleDistance` | 1.2 | 退散目标距离 |
| `startleDuration` | 0.5 | 惊吓上限时长 |
| `startleSpeedMultiplier` | 2.0 | 退散速度倍率 |
| `predationProbabilitySmallPrey` | 0.50 | 继承现资产值 |
| `predationProbabilityMediumPrey` | 0.20 | |
| `predationSatietyScaleAtEmpty` | 1.00 | 越饿越想吃 |
| `predationSatietyScaleAtFull` | 0.10 | |

### 8.2 FishEcologyProfile（修改）

| 字段 | 现值 | 新值 |
|---|---|---|
| `specialCanPredate` | true | **false** |
| `predationProbability` | 0.5 | 拆分为按猎物分类的两个字段，原字段移除 |

`fleeEnterRadius`、`fleeExitRadius`、`fleeSpeedMultiplier`、`predatorDetectionRadius`、`predatorSpeedMultiplier`、`mouthContactTolerance`、`largeCanPredate` 全部保留当前值不动。

### 8.3 ReelTuningProfile（新增）

| 字段 | 起始值 | 说明 |
|---|---|---|
| `smallStruggleCount` | 0 | |
| `mediumStruggleCount` | 0 | |
| `largeStruggleCount` | 1 | |
| `specialStruggleCount` | 1 | |
| `struggleTriggerRange01` | (0.35, 0.65) | 按已收回距离比例 |

### 8.4 StrikeWindowProfile（新增资产实例，不改类）

新建 `StruggleWindowProfile.asset`，由 `StrikeController` 单独引用：

| 字段 | 起始值 | 与提竿用资产的差异 |
|---|---|---|
| `windowDuration` | **1.4** | 提竿为 2.0；必须容纳松手加重按，不得低于 1.0 |
| `targetBandWidth` | 0.15 | 相同 |
| `minBandCenterRadius` | 0.187 | 相同 |
| `maxBandCenterRadius` | 0.852 | 相同 |
| `hitForgivenessRadius` | 0.15 | 相同 |
| `attemptCooldown` | 0.35 | 相同 |

### 8.5 FishingLoopProfile（按结果分档的冷却）

原单一 `postAttemptCooldown` 改为按结果查表。字段名由实现者定，语义须与下表一致。

| 结果 | 起始值 | 说明 |
|---|---|---|
| 收鱼上岸 | 0.8 | 沿用现值 |
| 空钩上岸 | 0.8 | 沿用现值 |
| 提竿超时 | 1.0 | |
| 落点撞石 | 1.5 | |
| 收线撞石 | 1.8 | |
| 断线 | 2.5 | 最重 |

**跨度约束**：最轻与最重之间至少保持 1.2 秒差，否则分档不可感知；任一档不得超过 3.0 秒，否则读作卡顿。

配套：`AttemptFailureReason` 的 `ReelingFailure` 拆为 `ReelingObstacle` 与 `LineSnap`。

### 8.6 单局时长（PlayerPrefs，不进 Profile）

| 项 | 值 | 说明 |
|---|---|---|
| `SessionTimer.sessionDuration` | 90 改为 **60** | 四个玩法场景各改一份 |
| PlayerPrefs 覆盖键 | 30 到 180，步进 15 | 开发调试用，`BeginSession` 时读取，下一局生效 |

### 8.7 移除

`HookTracker.startingHooks` 随组件一并移除。

---

## 9. 风险与待观察

1. **空钩率是首要观察指标。** 饱食弃权、惊吓延迟、8 秒窗口三者叠加。实施时应加临时统计（连续 20 竿的空钩数），让调参有据。目标：空钩率不高于 25%。
2. **单局可玩竿数只有 6 到 9。** 任何延长 Baiting 的改动都要按「损失竿数」计价。
3. **随机源从 1 处增加到 5 处**（参赛、接近速度、惊吓、捕食判定、挣扎触发点）。每个随机都必须有可见表现，否则整套生态在玩家眼里就是「这游戏很卡」。
4. **`satietyDecayPerSecond` 在 90 秒单局内近乎无效**，列第二批，不要为它牺牲第一批的完成度。
5. **惊吓分级表现**（小鱼吓得远、大鱼几乎不动）已被用户明确推迟，不要在第一批里顺手实现。

---

## 10. 验收条件

按 AGENTS.md 第 8 节，区分「源码实现 / 场景接线 / 编译通过 / Play Mode 通过 / 设备通过」。

### 规则正确性

- [ ] 同一条鱼在一局内饱食度不变；捕食成功后置为 `satietyAfterFeeding`。
- [ ] 参赛抽签每竿一次，不逐帧重抽；暂停后恢复不重抽。
- [ ] 候选集非空时，参赛数不低于 `guaranteedParticipants`。
- [ ] 弃权的鱼有可见的掉头离开表现，不是原地不动。
- [ ] 候选集在惊吓**之前**锁定；惊吓结束后被惊吓的鱼能正常参赛，不被外部导航槽卡死。
- [ ] Large 可吃挂钩的 Small 与 Medium；Medium 不吃任何鱼；Special 既不吃也不被吃。
- [ ] 每竿最多一次捕食替换；替换后仍只有一个挂钩对象。

### 二次挣扎

- [ ] 挣扎期间 `Tension01` **不清零、不累积**；挣扎结束后从挣扎前的值继续。
- [ ] 挣扎期间鱼钩不前进，移动与加速输入禁用。
- [ ] 挣扎成功后需重新按下才能加速。
- [ ] 挣扎失败时鱼逃脱、空钩继续收线、**不触发 `AttemptFailed`**、不结算分数与加时。
- [ ] Small 与 Medium 不触发挣扎；Large 与 Special 每竿至多一次。
- [ ] PC 上按住空格进入挣扎，松手再按能正常判定；窗口时长足够完成该操作。

### 钩数取消与单局时长

- [ ] 四个玩法场景的 `sessionDuration` 均为 60。
- [ ] 开发者面板可在 30 到 180 之间调整单局时长，显示当前值。
- [ ] 修改后**当前对局不受影响**，下一局生效。
- [ ] 从 `FishingLoopTest` 设置的值，在 `Level 1/2/3` 加载时同样生效。
- [ ] 「恢复默认」回到 60 秒。
- [ ] 三条失败路径均返回 `ReadyToCast`，不结束对局。
- [ ] `GameOver` 只能由超时进入。
- [ ] 失败仍触发音效与震动反馈。
- [ ] HUD 无 `Hooks` 残留；场景无空引用报错。
- [ ] 六种结果各自使用对应档位的冷却，逐一实测（收鱼 / 空钩 / 提竿超时 / 落点撞石 / 收线撞石 / 断线）。
- [ ] 撞石与断线能被区分（`ReelingObstacle` 与 `LineSnap` 两个 Reason 分别到达）。
- [ ] **手感实测**：断线的 2.5 秒冷却是否真的让人不想乱加速。冷却期间仍可左右选位，痛感可能低于账面；若无感需上报，不要自行加长。

### 回归（不得被本次改动破坏）

- [ ] 成功上岸计分、加时、入库存正常；空钩无分。
- [ ] 撞石与断线仍各触发一次失败，不重复。
- [ ] 计时结束后不再推进玩法。
- [ ] 捕食替换保留本竿距离倍率、张力与加速累计距离。

### 平台

- [ ] Editor、Android 分别记录。**上挑手势与倾斜转向不打架**必须在真机验证，Editor 通过不能推断。
- [ ] WebGL 待验证。
