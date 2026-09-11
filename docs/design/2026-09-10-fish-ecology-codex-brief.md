# 鱼群生态 —— 实现工单（交 Codex）

日期：2026-09-10。设计来源：同目录 `2026-09-10-fish-ecology-design.md`（以下称「设计」）。
性质：**尚未实现的工单**，不是发布日志。设计已由用户逐项确认；实现细节与类名由实现者定，但**规则、参数归属和验收条件不得自行更改**。

规则以设计为准，本文件只讲**改哪里、怎么分批、哪里有坑**。

---

## 0. 先读这三个陷阱

这三条是静态读代码就能确认的真实风险，每一条都会造成静默错误或可刷漏洞。**动手前先确认理解。**

### 陷阱 1：`CancelRetrieval()` 会清零张力 —— 挣扎不能做成顶层 State

`ReelingController.CancelRetrieval()` 把 `Tension01` 置 0 并把 `catchTensionMultiplier` 重置为 1
（[ReelingController.cs:63-68](../../Assets/Scripts/Controller/ReelingController.cs:63)），
而 `FishingLoopController.ExitReeling()` 会调用它（[FishingLoopController.cs:760](../../Assets/Scripts/Controller/FishingLoopController.cs:760)）。

> **后果**：若把二次挣扎做成 `Reeling -> Struggling -> Reeling` 的顶层状态，玩家每次挣扎都会**白嫖一次张力清零**。张力快满了？触发挣扎就归零。这是可刷的免死金牌。

附带地，`FishEcologyController.HandleStateChanged` 在 state 不是 Reeling 时会**终止全部捕食与逃离导航**
（[FishEcologyController.cs:35](../../Assets/Scripts/Controller/FishEcologyController.cs:35)），离开 Reeling 会打断正在追击的捕食者。

**做法**：挣扎必须是 **Reeling 内部的子阶段**，`CurrentState` 全程保持 `Reeling`。
给 `ReelingController` 增加保留张力的暂停 / 恢复接口（不要复用 `CancelRetrieval`），
给 `StrikeController` 增加在 Reeling 期间可重入的检定入口。

### 陷阱 2：单一外部导航槽争用 —— 惊吓会让鱼永远不参赛

`FishController.BeginApproach` 在 `externalNavigation == true` 时**直接 return**
（[FishController.cs:158](../../Assets/Scripts/Controller/FishController.cs:158)）。惊吓走的正是外部导航。

> **后果**：惊吓尚未结束就 `BeginRace`，那些鱼会**静默地永不参赛**。不报错，只是没鱼来。

另外，现在逃离由 `FishEcologyController` 拥有、追饵由 `FishBiteRaceController` 拥有，两个组件写同一条鱼。

**做法**：Baiting 期间由 **`FishBiteRaceController` 单独持有**「惊吓 -> 抽签 -> 参赛」的完整序列，
惊吓结束后显式 `EndExternalNavigation()` 再 `BeginApproach()`。
`FishEcologyController` 继续只管 Reeling 期间的逃离与捕食，**不要**让它插手 Baiting。
一个阶段一个所有者。

### 陷阱 3：猎物类别判定有两处，漏一处就静默失效

Small 的猎物判定同时存在于：

- [FishEcologyController.cs:76](../../Assets/Scripts/Controller/FishEcologyController.cs:76) —— 决定是否发起追击
- [FishingLoopController.cs:418](../../Assets/Scripts/Controller/FishingLoopController.cs:418) —— `TryReplaceHookedFish` 的权威校验

> **后果**：只改前者，大鱼会追中鱼但永远吃不到（协调层拒绝替换），表现为「大鱼贴着猎物不动」。只改后者则追击根本不会发起。

**做法**：把类别判定收敛为 `FishEcologyProfile.IsPrey(category)` 单一来源，两处都调它。

---

## 1. 分批

**第 3 批必须单独提交，不要和前面混在一个改动里。**

| 批次 | 内容 | 规模 | 依赖 |
|---|---|---|---|
| **B0** | 取消钩数机制 + 单局时长可调（默认 60 秒） | 小，独立 | 无 |
| **B1** | 捕食关系调整（Special 退出、Large 吃 Medium、概率分列） | 小 | 陷阱 3 |
| **B2** | 饱食度数据 + 参赛抽签 + 接近速度 + 捕食欲望缩放 | 中 | B1 |
| **B3** | 落钩惊吓 | 中 | B2、陷阱 2 |
| **B4** | 二次挣扎 | 大，动状态机 | 陷阱 1 |
| **B5**（第二批，可缺省） | 饱食度随时间衰减、惊吓按分类分级 | 小 | 用户已明确推迟 |

B0 放最前是因为它完全独立，且能立刻简化状态机（GameOver 只剩超时一个入口），后面几批的测试都受益。

---

## 2. 各批的改动面

### B0 —— 取消钩数

影响面已确认很小，只有 2 个脚本 7 处引用。

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Tracker/HookTracker.cs` | 删除组件 |
| `FishingLoopController.cs:38` | 移除 `hookTracker` 字段 |
| `FishingLoopController.cs:525-541` | `LoseHookAndFinishAttempt` 改为「失败并返回待抛竿」：不扣钩，恒定 `TransitionTo(ReadyToCast)`；**保留** `AttemptFailed?.Invoke(...)` |
| `CatchResult.cs` | `AttemptFailureReason` 的 `ReelingFailure` 拆为 `ReelingObstacle` 与 `LineSnap` |
| `ReelingController.cs` | 撞石与断线两条路径分别上报，不再共用同一个无参 `AttemptFailed` |
| `FishingLoopController.TransitionTo` | 按**结果**查表取冷却，六档（见设计 6.3） |
| `FishingLoopProfile.cs` | 单一 `postAttemptCooldown` 改为六档冷却配置 |
| `FishingSessionHUD.cs:8, 108` | 移除 `hookTracker` 字段与 `Hooks:` 行 |
| 场景 `FishingLoopTest.unity` | 移除 HookTracker 对象与两处引用；**不要批量重写场景 YAML** |

**保留不动**：`AttemptFailureReason`、`AttemptFailureResult`、`FishingAudioFeedback.HandleFailure`、`FishingHaptics.HandleFailure`。失败仍要有反馈。

**冷却分档的两个已确认前提，别跳过：**

1. `TransitionTo` 现有的 `shouldStartCooldown`（`FishingLoopController.cs:159`）只看目标状态和来源状态，
   **成功上岸和断线走的是同一条 `Reeling -> ReadyToCast`**，现有信息无法区分。
   必须把「本次转移的结果」显式带进来（传参或先记录），不要靠来源状态猜。
2. `ReelingController` 的撞石（`OnTriggerEnter2D`）与断线（`Tension01 >= 1`）目前调的是同一个
   无参 `ReportAttemptFailed()`，上层只能得到一个 `ReelingFailure`。要分档就必须先让这两条路径可区分。

**枚举拆分是安全的**：`FishingAudioFeedback.HandleFailure` 与 `FishingHaptics.HandleFailure`
都**不读** `Reason` 字段，没有 switch 需要补分支。（拆完后它们具备了做差异化音效的条件，
但那是后续，本批不做。）

**六档取值见设计 6.3。** 跨度约束：最轻与最重至少差 1.2 秒，任一档不超过 3.0 秒。
不要把六个值调成彼此接近——那样分档就只是看不见的复杂度。

### B0b —— 单局时长可调（同批，规则见设计 6.4）

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Tracker/LocalPlayerProgress.cs` | 新增单局时长覆盖值的读写（PlayerPrefs，沿用现有键名风格 `SevenSeas.*`）；含「未设置」状态与清除 |
| `Assets/Scripts/Tracker/SessionTimer.cs` | `BeginSession()` 读取覆盖值；无覆盖值时回落到序列化的 `sessionDuration`。`ResetTimer()` 同步处理 |
| `Assets/Scripts/UI/DeveloperPanel.cs` | 新增时长调节项（30 到 180，步进 15）、当前值显示、恢复默认 |
| 四个玩法场景 | `sessionDuration` 由 90 改为 **60**（`FishingLoopTest`、`Level 1`、`Level 2`、`Level 3` 各一份） |

**两个已确认的坑：**

1. `sessionDuration` 在**四个场景各存一份，当前都是 90**。只改 `FishingLoopTest` 不会影响正式随机地图。
2. **`DeveloperPanel` 只在 `FishingLoopTest` 里**（经 `MenuSettingsPanel` 打开），`Level 1/2/3` 没有。所以**不能**让调节项直接持有场景内的 `SessionTimer` 引用——正式地图里那个面板不存在。走 PlayerPrefs，`SessionTimer` 自己读，两个问题一起解决，也不用给 Level 1/2/3 补面板。

**不要**把这个值放进任何 Profile。它是开发调试设置，不是玩法配置（AGENTS.md 第 6 节：Profile 只存配置）。

**生效时机**：下一局，不改动进行中的对局。

### B1 —— 捕食关系

| 文件 | 改动 |
|---|---|
| `FishEcologyProfile.cs` | `specialCanPredate` 改为 false（同时改 `.asset` 值）；`IsPredator` 去掉 Special 分支；**新增 `IsPrey(category)`** 返回 Small 或 Medium；`predationProbability` 拆成 `predationProbabilitySmallPrey`(0.5) 与 `predationProbabilityMediumPrey`(0.2) |
| `FishEcologyController.cs:76` | `HasCategory(prey, Small)` 改为 `ecologyProfile.IsPrey(...)` |
| `FishingLoopController.cs:418` | 同上，改用 `IsPrey` |
| `FishEcologyController.UpdatePredation` | 按猎物类别取对应概率 |
| `Assets/Engineer Folder/Profiles/FishEcologyProfile.asset` | 同步字段 |

**注意**：`predationProbability` 现资产值是 **0.5**，不是 progress.md 写的 0.3。以资产为准。

### B2 —— 饱食度

| 文件 | 改动 |
|---|---|
| `FishController.cs` | 新增 `Satiety` 属性与设置入口；追饵速度乘上倍率（当前 `Update()` 的 Approaching 分支用的是裸 `swimSpeed`） |
| `FishSpawner.cs` | 生成时按分类抽饱食度，在 `ConfigureMovement` 同一时机写入 |
| `FishBiteRaceController.cs` | `BeginRace` 中对候选逐条抽参赛，未中者不 `BeginApproach` 并使其掉头游开；实现保底补足 |
| `FishEcologyController.cs` | 捕食概率乘饱食度缩放；替换成功后把捕食者饱食度置为 `satietyAfterFeeding` |
| `FishEcologyProfile.cs` + `.asset` | 新增设计 8.1 的字段 |

**保底逻辑**：候选非空但参赛数低于 `guaranteedParticipants` 时，按饱食度升序补足最饿的。不要写成「全弃权才保底」。

**弃权表现是验收项**：未参赛的鱼必须可见地掉头离开，不能原地不动。

### B3 —— 落钩惊吓

| 文件 | 改动 |
|---|---|
| `FishBiteRaceController.cs` | `BeginRace` 拆成「落钩即锁定候选 -> 惊吓 -> 结束惊吓 -> 抽签 -> 开赛」；`baitingDuration` 从开赛计时，不含惊吓 |
| `FishingLoopController.EnterBaiting` (:679) | 触发新的落钩序列 |
| `FishEcologyProfile.cs` + `.asset` | 新增 startle 四个字段 |

**顺序不能反**：候选集在惊吓**之前**锁定（语义 = 听见落水声的鱼）。先惊吓再抓候选会把鱼推出半径，候选锐减。

**每条鱼惊吓结束的条件**：到达退散点 **或** `startleDuration` 到期，取先到者。被障碍卡住的鱼按超时处理，仍参与抽签。

**结束时必须显式 `EndExternalNavigation()`**，否则撞上陷阱 2。

`HookLandingVFX` 已存在，惊吓应与该表现同一拍。

### B4 —— 二次挣扎

| 文件 | 改动 |
|---|---|
| `ReelingController.cs` | 新增**保留张力**的暂停 / 恢复（不要复用 `CancelRetrieval`）；暴露收线进度 01 供触发判定 |
| `StrikeController.cs` | 支持用另一份 `StrikeWindowProfile` 发起检定；结果需能区分「提竿」与「挣扎」两个来源 |
| `FishingLoopController.cs` | Reeling 内的挣扎子阶段：掷定触发点、到点暂停收线并起检定、处理成败；**`CurrentState` 全程保持 `Reeling`** |
| `FishingLoopController` 的 strike 回调 | 现有 `HandleStrikeSucceeded` / `HandleStrikeTimedOut` / `HandleStrikeAttemptRejected` 全都以 `if (CurrentState != Striking) return` 开头，会**吞掉挣扎的回调**。必须显式支持 Reeling 期间的重入 |
| `ReelTuningProfile.cs` + `.asset` | 新增设计 8.3 的字段 |
| 新建 `StruggleWindowProfile.asset` | 复制 StrikeWindowProfile，`windowDuration` 改 1.4 |

**挣扎开始时**：暂停收线（保留张力）、`SetMoveEnabled(false)`、`SetAccelerateEnabled(false)`。
**挣扎结束时**：恢复收线、`SetMoveEnabled(true)`、`SetAccelerateEnabled(true)`。

后者会把 `accelerateRequiresRelease` 置位，强制玩家松手重按 —— **这是设计要求的行为，不是 bug，不要绕过。**

**PC 输入约束（推断，需你核对后确认）**：Strike 与 Accelerate 很可能绑同一个空格键。
依据只有 [KeyboardMouseFishingInputSource.cs:131](../../Assets/Scripts/Input/KeyboardMouseFishingInputSource.cs:131) 那句注释
「Entering Reeling while Space is still held must not immediately activate acceleration」及 `accelerateRequiresRelease` 的存在。
**静态资产无法确认**：`InputSystem_Actions.inputactions.meta` 的 `internalIDToNameTable` 为空，
场景里 `strikeAction` / `accelerateAction`（`FishingLoopTest.unity:70165-70166`）只有 fileID，解析不出 action 名。

**请在编辑器里打开 InputActionReference 核对，并把结论写进实现记录。**

- 若同键：玩家进入挣扎时正按住空格，而 Strike 走 `WasPressedThisFrame()`，按住不触发，必须松手重按。`windowDuration` 1.4 秒就是为容纳这个动作，**不要压到 1.0 秒以下**。
- 若不同键：1.4 秒可适当下调，下限仍为 1.0 秒，其余规则不变。

移动端冻结移动输入与此无关，无论哪种结论都必须做——上挑与倾斜共用姿态传感器。

**失败路径**：`HookedFish.ResetToIdle()`、`HookedFish = null`、恢复收线、**空钩继续到岸**。
**不要**调用失败结算，**不要**触发 `AttemptFailed`，**不要**结束 Reeling。

**捕食替换与挣扎的交互**：替换发生时若新鱼有挣扎次数而本竿尚未掷定，则在替换时掷定；已过触发点则本竿不再挣扎。

---

## 3. 临时统计（实施期需要，交付前移除或关掉）

设计 9.1 要求空钩率可观测。加一个开发期计数：连续 20 竿中的空钩数、全员弃权次数、惊吓到开赛的实际耗时。
放在开发面板或 `Debug.Log` 均可，**不要进 Profile**（Profile 只存配置，不存运行状态）。

目标：空钩率不高于 25%。超标先压 `startleDuration` 与 `joinChanceAtFull`，不要直接加大 `baitingDuration`。

---

## 4. 纪律提醒（AGENTS.md）

- 重命名序列化字段要处理兼容性，检查场景与 Prefab 引用；移动资产保留 `.meta` 与 GUID。
- 不批量重写场景 YAML，不重建已布置场景来解决局部接线问题。
- 尊重现有未提交修改（当前 `Assets/Editor/ObstablePrefabGeneratorWindow.cs` 有未提交改动）。
- 现有已调参数以资产实际值为准，不要用文档示例值覆盖。
- 编译通过不等于场景接线正确；每批分别记录「源码实现 / 场景接线 / 编译通过 / Play Mode 通过 / 设备通过」。
- 按 AGENTS.md 第 9 节更新 `task_plan.md`、`findings.md`、`progress.md`。

---

## 5. 验收

完整清单见设计第 10 节。以下是**最容易被漏掉、必须逐条实测**的几项：

1. 挣扎期间与结束后 `Tension01` 连续，不清零。（对应陷阱 1）
2. 惊吓结束后的鱼能真正参赛。（对应陷阱 2）
3. 大鱼能**吃到**中鱼，不是只追不吃。（对应陷阱 3）
4. 挣扎失败后收线继续、不扣任何东西、不结束本竿。
5. GameOver 只能由超时进入；三条失败路径都不结束对局。
6. Android 真机：上挑手势与倾斜转向不互相干扰。**Editor 通过不能推断真机通过。**
7. 连续 20 竿的空钩率。

验收由 Claude 执行；提交时请说明每项属于「源码已实现 / 场景已接线 / 编译通过 / Play Mode 通过 / 设备通过」中的哪一级，未运行的检查明确标记待验证。
