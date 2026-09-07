# 项目接手笔记

更新：2026-09-06。依据当前 C#、场景 YAML、Profile 和历史记录静态核对；本次未运行 Unity Play Mode、编译或设备测试。实现存在不等于已通过运行验证。

## 项目与开发方式

- Seven Seas：基于 Angler Dangler 的课程钓鱼原型。作业要求见根目录 README：单人、方向控制与单按钮、移动特色机制、复古精灵风格，最终面向 itch.io WebGL。
- Unity 6000.3.23f1；URP 17.3.0、Input System 1.20.0、Cinemachine 3.1.7。
- task_plan.md 与设计文档约定由用户亲自编写玩法代码和操作场景，Codex 逐步指导、检查。历史上的一次代写例外不应自动扩大为持续授权。
- 历史进度截至 09-03，部分已落后于代码；后续结合本笔记核对，不能直接把旧待办当成未实现。

## 启动与入口

- 完整入口：Assets/Scenes/BootStrap.unity → AppRoot / SceneLoader → MainMenu → FishingLoopTest。
- Editor 可直接打开 Assets/Scenes/FishingLoopTest.unity；FishingSceneEntryGuard 对非移动平台跳过姿态校准。
- 移动入口要求 AppRoot 和姿态校准服务；AppRoot 跨场景持有传感器、校准与场景加载服务。
- AttitudeControlTest、GyroscopeCastTest 是独立输入实验场景。CastBallController 的横向物理抛物线不是正式钓鱼飞行实现。

## 模块地图

脚本根目录：Assets/Scripts/。

| 入口 | 职责 |
|---|---|
| Controller/FishingLoopController.cs | 六态协调、功能启停、事件解释、扣钩/计分/结束 |
| Input/FishingInputSource.cs、FishingInputRouter.cs | Move / Cast / Strike / Accelerate 语义；按平台选择键鼠或移动实现 |
| Controller/ShoreLaneController.cs | 横向速度控制、松手停止、岸边限位 |
| Controller/CastGestureDetector.cs | 角速度阈值、滤波、峰值力度；提竿采用即时 GestureTriggered |
| Controller/FishingHookController.cs | Dock / Launch / Landed；目前沿世界 +Y 线性飞行 |
| Controller/FishBiteRaceController.cs、FishController.cs | 搜索候选鱼、唯一获胜者、8 秒超时、鱼跟随钩 |
| Controller/StrikeController.cs | 缩圈判定、失败尝试冷却、成功/超时事件 |
| Controller/ReelingController.cs | 自动回收、横向闪避、碰撞、张力、上岸事件 |
| Tracker/*.cs | ScoreTracker、HookTracker（默认 5 钩）、SessionTimer（默认 90 秒） |
| UI/*.cs、Controller/FishingCameraController.cs | 状态显示、张力条、缩圈、相机跟随与抖动 |

状态链：ReadyToCast → Casting → Baiting → Striking → Reeling → ReadyToCast。
诱鱼超时进入空钩 Reeling；提竿超时、撞石或断线扣钩；钩耗尽或计时结束进入 GameOver。

键鼠 A/D 移动，Space 根据状态负责抛竿、提竿或按住加速。进入收线时有先松开再加速的保护。

调参资产实际位于 Assets/Engineer Folder/Profiles/，不是旧文档里的路径。
当前 Reel 配置：回收 2、加速回收 4、横移 4、衰减 0.8、加速张力增长 1.2、安全横移速度 1.33、最大评估速度 4、最大横移增长 1.4。
张力按实际限位后的横移速度计算，叠加加速增长与被动衰减后积分，并钳制至 0..1。
当前 Strike 判定带为 0.442..0.624，时窗 1.5 秒、冷却 0.35 秒；不要用设计初始示例值覆盖已调参数。

## 已核实的进度差异

- M7 不再是“未开始”：计分、扣钩、计时、GameOver 逻辑和 FishingSessionHUD 已实现，场景已有相关非空引用。完整运行验收仍待核对。
- M6 已有上岸返回 ReadyToCast、成功计分、最大张力一次性失败保护。
- EventSystem 已恢复原名；Canvas 下已有独立 TensionBarHUD 容器与 Slider。
- TensionBarHUD.cs 已存在，但其脚本 GUID 未在所查场景/Prefab 中引用，目标容器只有 RectTransform。实时张力绑定仍是待办；脚本也尚未实现状态可见性和颜色变化。
- 移动语义输入和平台路由已实现并在主测试场景接线；本次未找到主场景直接绑定 PressAccelerate / ReleaseAccelerate 的记录，移动加速按钮需进一步核对。

## 后续最短推进顺序

1. 完成张力 HUD 的脚本挂载、ReelingController/Slider 引用、状态显示和反馈；Play Mode 验证张力变化与一次性断线扣钩。
2. 做完整循环回归：成功上岸加分、空钩无分、提竿失败扣钩、碰撞/断线一次扣钩、最后一钩与时间耗尽结束。
3. 优先检查 GameOver 取消行为：ExitCasting / ExitBaiting 为空，EnterGameOver 未取消飞行或鱼竞赛。虽然上层事件有状态守卫，底层仍可能继续飞行、游动或挂鱼。属于源码发现，尚未运行复现。
4. 再核对移动端倾斜、反向提竿、加速按钮与校准；WebGL 发布目标需要单独设备/浏览器验收，不能由 Android 路径推定通过。
5. 最后处理已延期项：正式 2.5D 抛物线、诱饵摆动、Strike Profile 抖动参数接入、暂停与校准复用、表现和发布。

## 验证与仓库注意

- 本次未找到项目自有的自动化测试套件；名称含 Test 的主要是原型与 Editor 工具。
- 旧构建通过记录只能代表当时状态，本次不作当前编译通过声明。
- 常规 git status 被 LFS 临时目录写入权限阻断。禁用过滤器的诊断列出的图片/视频差异可能是指针与实体文件差异，不应据此判定用户实际修改了这些资产。未更改 Git 配置或二进制文件。
- 本次仅新增这份接手文档，未改游戏代码、场景或参数。
