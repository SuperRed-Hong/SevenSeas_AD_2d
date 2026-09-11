# 地图与开场镜头交接（2026-09-09）

## 接手范围

用户要求本轮先更新文档并交接，不立即实施镜头。下一 chat 专门接手地图与开场镜头；原 chat 继续障碍 Prefab Editor 工具教学，不要代写或改动教学文件。

工作目录：D:/UnityProject/SevenSeas_AD_2d。记录时本地分支 main，HEAD a9811ba（update doc），大量最新功能和用户美术/场景修改尚在工作区，部分新脚本未跟踪。必须从当前工作区接手，不能从远端或旧 main 新建干净副本后假设拥有全部进度。先读 AGENTS.md、三份进度文档、本交接及实际源码。

Unity 6000.3.23f1，Cinemachine 3.1.7；不自行升级。Git索引仍把 task_plan.md、findings.md、progress.md 标记为 UU；当前文件未发现冲突标记，但不能据此宣称Git冲突已解决。本轮只追加文档，不暂存、提交或推送，不覆盖其他修改。

## 用户已决定的新流程（尚未实现）

原因：打开游戏时主菜单已经通过摄像机暴露了本局随机地图，与用户希望的开场展示意图冲突。

1. 游戏开场相机看一个与当前随机地图无关的空镜，承载开始界面。
2. 点击 Start 进入游玩流程后，相机缓慢移到游戏地图上方。
3. 开始一次从地图上方向下移动的 Overview Roll 展示；此阶段切成电影机画幅。
4. 镜头缓慢向下巡览，最终到 ShorePlayer 所在平台。
5. 再交回此前的正常游戏镜头流程。

这里的 Roll 按用户描述指“由上向下巡览”，不要擅自理解为绕相机前轴滚转。需确认具体运动轨迹，但不要默认加旋转。此次请求并未要求改变三张地图的等概率随机规则，也未明确必须改成多个Unity场景文件。

## 下一 chat 先讨论的细节

- 空镜内容：独立海面/布景、位置和取景范围，由谁提供美术；无论选哪张地图都不应提前露出实际地图、鱼群或障碍。
- “电影机画幅”：目标比例、上下黑边还是其他遮幅、出现与退出动画；移动端竖屏和安全区域怎么处理。不能擅定16:9或2.35:1。
- 移向地图上方、向下巡览、平台停留、交回镜头各阶段时长和缓动；需放可调配置。
- 每局/Play Again是否都播放，可否跳过，跳过按钮/输入与取消流程。
- 首次教程、Android校准在巡览前还是后；保留既有教程完成和校准规则，先确认编排。
- 演出期间计时、输入、鱼Idle的具体门控。建议计时及玩法输入在镜头交接完成后开始，这是实施建议而非新批准规则。
- 地图仍可在菜单背后提前选好，也可延迟到Start；用户已明确的是“不提前展示”，没有指定初始化时刻。若延迟，必须同步重排鱼生成，不能让鱼先按错误地图出生。

## 当前实际实现

### 地图选择与碰撞

- 正式场景 Assets/Scenes/FishingLoopTest.unity；三图都在 MainLanscape/Obstacles/Lanscape_TileMap 下，名为 TileMapGrid_1、TileMapGrid_2、TileMapGrid_3。
- FishingMapSelector 接在常开独立根 FishingSceneEntry，Awake执行顺序-1000；Initialize幂等，从完整maps数组等概率选一张，允许重复，同一场景实例不重复抽。Play Again目前重载游戏场景，产生新一局。
- Initialize覆盖三个Grid的激活状态，并启用地图容器及其祖先链，再ProcessTilemapChanges和Physics2D.SyncTransforms，先于FishSpawner.Start。编辑器预览开关不决定候选地图；入口对象自身应保持启用。
- 发生过两次真实接线问题：场景中缺选择器组件；后又出现mapContainer为fileID 0，日志报requires a map container并提前退出，保留了两张预览地图。已补接线；现在父引用为空可从maps[0].parent推导，并验证三图共同父对象。不要只凭旧文档或测试就认定现场正常。
- 当前重要引用：选择器2111000001→入口GO1636518285；mapContainer Transform314732374；三图GO273462853/314236851/1890288895。接手时再次核实，不能盲写ID。
- 三图之外的公共Smallobstacles不会随Grid自动切换。地图专属障碍必须归属相应地图根；移动/归类先核对用户布局。

### 人、鱼范围与生成

- ShoreLaneController继续使用左右岸边Transform限位。
- GameplayRoot/FishMovementArea用BoxCollider2D与青色Scene Gizmo表达独立鱼移动矩形；两FishSpawner引用2146000002。初始矩形中心(-2.175,12.04)、尺寸(15.56,30.1)，接手以实际Inspector最新值为准。
- Spawner自身Box仍为出生区域，movementArea为自由鱼的游动/追饵/逃离/捕食边界；挂钩后交由收线移动。
- FishSpawner→FishController.IsSpawnPoseAllowed→FishNavigationGeometry按完整鱼碰撞形状+padding检查范围、活动Tilemap及其他鱼，跨生成器防重叠。空间不足少生成并警告，不强行重叠。
- ReelingObstacle.IsBlockingCollider统一识别有效ReelingObstacle标记、TilemapCollider2D及同对象含TilemapCollider2D的CompositeCollider2D。鱼导航、鱼钩落点、线段/圆扫掠、收线Trigger已统一。落到地图障碍沿用ObstacleLanding扣钩→ReadyToCast/最后一钩GameOver；没有新增反弹动画。

### 现有镜头与开局调用链

- FishingCameraController引用menuCamera、overviewCamera、hookFollowCamera、CinemachineBrain。ShowMenu启用菜单镜头；TransitionToOverview仅ShowOverview后等待Brain混合；尚无巡览轨迹、电影画幅或平台停留阶段。
- FishingSceneEntryGuard.Awake：sceneUI.Initialize，loopController.PrepareForEntry禁用玩法/输入，GameplayRoot激活。鱼在Start生成，因此菜单期间鱼和地图已存在。
- Entry.Start调用ShowMenu；StartGame先检查首次教程，然后ContinueStart处理手机服务/校准。之后BeginAfterTransition并行启动TransitionToOverview与菜单淡出，等待结束才启用Loop、记录开始次数、显示Gameplay UI。
- Loop.Start调用sessionTimer.BeginSession；既有“演出期间别提前启用Loop”的门控值得保留。ShowOverview也用于正常玩法/结束，不能为了开场修改它而破坏普通抛竿/收线/失败镜头。
- FishingSceneUIController管理菜单/Gameplay Canvas显隐；MenuCanvasFader负责淡出。建议演出由独立表现组件承载，Entry编排交接，避免把镜头阶段塞进FishingLoopController。
- 完整启动仍由BootStrap/AppRoot/SceneLoader进入；不要绕过移动校准或把Start改成未配置的新场景名。

## 验证事实与欠账

- 地图隔离原生Unity测试覆盖64开关组合、512断言；还故意清空mapContainer重复验证。原生Play生命周期确认Awake准备地图碰撞早于默认Start。
- 鱼边界、跨鱼生成、活动/停用Tilemap、落点/扫掠等真实Physics2D共14项通过。源码MSBuild通过，非正式场景完整回归。
- 临时证据：SevenSeasMapResults.txt、SevenSeasMapNullContainer.log、SevenSeasMapNullPlay.log、SevenSeasFishAreaResults.txt、SevenSeasHookTilemapNative.log，位于本机系统临时目录，非仓库持久测试。
- 用户曾实际发现两图同显，已按日志修复；没有确认最新正式场景完整试玩通过。新空镜/巡览需求完全未实施，不能以已有测试当作其验收。
- 实施后验收：冷启动三图都不泄露到空镜；编辑器三图/父级任意开关仍只选一张；Start防连点；教程取消/校准取消不误启动；三图巡览起终点正确、电影画幅恢复；演出完成/跳过只开启一次玩法；计时门控；Play Again与排行榜返回；正常追钩、上岸、失败相机不回归；竖屏与Android/WebGL分别记录。

## Editor教学留在原chat

- 用户手写 Assets/Editor/ObstablePrefabGeneratorWindow.cs，命名中的Obstable保持现状，不替用户重命名/代写。
- 已完成窗口、选择变化重绘；GetSelectedSprites方法已写出：Sprite直取、Texture2D用LoadAllAssetsAtPath筛Sprite、Contains去重。
- 当前OnGUI仍调用旧GetFiltered并用数组Length，尚未接GetSelectedSprites。原chat下一小步：换成List<Sprite> sprites = GetSelectedSprites()，数量Length改Count，验证外层图片读取与去重。
- 后续才教学输出目录、按钮、每Sprite独立Prefab、SpriteRenderer+Collider2D+ReelingObstacle。用户已选亲手学习，不在镜头任务中批量生成或改写此工具。

## 可复制给下一chat的任务

从D:/UnityProject/SevenSeas_AD_2d当前main工作区接手，不从旧远端重开。阅读AGENTS.md、三份进度及本交接，核对实际场景。专门推进与随机地图无关的主菜单空镜、Start后移向地图上方、电影画幅从上到下Overview Roll、到ShorePlayer平台后交回既有游戏镜头。先讨论本文待决参数和教程/校准编排，再实施；保留地图/鱼边界/Tilemap避障修复及用户场景美术改动。不改障碍Prefab Editor教学文件，不擅自处理Git索引冲突或提交推送。区分静态、隔离测试、正式场景和设备验证，依据现场日志验收。
