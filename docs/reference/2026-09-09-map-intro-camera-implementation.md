# 地图与开场镜头实施记录

## 用户确认的规则

- 先从三图等概率选本局地图，再从另外两张中随机选菜单地图；允许相邻两局重复。
- 菜单与游戏为两块独立场地。菜单有水面、地图和正常游动的鱼，没有 ShorePlayer 或岸边平台。
- 首次教程和必要的移动端校准完成后，才开始演出；保留原有完成/取消规则。
- 菜单移向游戏地图上方，再从上往下巡览，到 ShorePlayer 平台后交回正常镜头。不旋转相机。
- 每局播放，包括 Play Again；演出中有 SKIP 按钮。
- 上下各占屏幕高度 10% 的黑边，巡览收紧取景，退出时恢复正常画幅。
- 鱼继续游动；镜头交接完成后才启动玩法计时、输入和 HUD。

## 实现与调参入口

- `FishingSceneEntry` 新增 `FishingIntroController`，引用 `FishingIntroProfile`、选择器、既有相机、水面 Mesh、两鱼群生成器、鱼移动区域和 ShorePlayer。没有移动或重排用户已有场景对象。
- `FishingMapSelector.ChooseMenuMap()` 在已选本局地图之外抽取展示地图。正式场地仍只启用一张地图。
- 开场组件在 Awake（执行顺序 -750）创建 `MenuScenery (runtime)`；只复制候选 Grid 和水面视觉。菜单区向世界 Y 正方向平移至少 55 单位，按地图边界自动增大间距。延展现有水面覆盖两区间的镜头路径，不复制水面碰撞。
- `FishSpawner.CreateAmbientCopy()` 复制生成配置并绑定独立移动矩形，不订阅正式 Loop。正式鱼仍在正式地图生成。演出完成时停用菜单场地，避免全局鱼生态查询收集菜单鱼。
- `Cameras/IntroCamera` 是独立的 Cinemachine 电影镜头。开始演出时从 MenuCamera 的位置/Lens无缝接管，再负责入场、匀速下巡和平台停留；MenuCamera保持菜单用途。交接时IntroCamera先对齐Overview的位置/Lens，再切换；保留退出相机的最终姿态，避免混合期间重置造成回跳。黑边由独立Overlay Canvas绘制，不是相机自动产生的效果。
- 黑边、取景和运动参数在 `Assets/Engineer Folder/Profiles/FishingIntroProfile.asset`：入场 2 秒、巡览速度 `Survey Speed = 2` 世界单位/秒、平台停留 0.6 秒、交回 0.8 秒；地图顶端展开黑边约 0.35 秒。跳过以 0.35 秒交回。全部使用非缩放时间。巡览用 MoveTowards 匀速移动，时长由距离÷速度决定，速度越小越慢；入场/交回仍使用原时长与缓动。
- 参数演变：最初下巡5秒，用户更正为8秒后仍不满意，最终要求改成速度控制。已同步迁移实际Profile资产至surveySpeed，不用FormerlySerializedAs把旧秒数误当作速度。旧计时验收仅对应当时版本，不代表最新速度版已验收。
- 巡览 Orthographic Size 默认 7（原菜单 9、正常 Overview 12.4）；Bar Height为每一条黑边占屏幕高度的比例，0.1表示上下各10%，剩余80%可见。黑边是 Overlay UI 遮幅，不改变屏幕分辨率或游戏 Camera viewport。SKIP 按钮位于安全区域右上角。
- `FishingSceneEntryGuard` 在演出确认完成后才释放玩法；重复 Start/Skip 不重复记局。演出被禁用时清理黑边和菜单场地，但不记为完成、不启动游戏。
- 用户反馈黑边与巡览不生效后，实际检查发现保存场景的EntryGuard.intro为fileID 0，导致走旧TransitionToOverview。现已补回引用，并在Awake从同一GameObject恢复遗漏的Intro组件引用；存在Intro但未准备好时明确报错并阻止开始，不再静默跳过演出。

## 验证记录

- 首次 Runtime MSBuild 编译通过（既有 MSB3277 程序集版本告警）。
- 场景静态检查：本地 fileID 无缺失或重复，场景差异限于开场组件与入口引用。
- Unity 6000.3.23f1 临时副本运行了当前正式场景、实际鱼 Prefab/Tilemap/水面与 Cinemachine：首轮 7 次加载、103 条断言通过，覆盖三张正式地图、独立菜单地图、菜单鱼实际移动、入口取消、正常播放、入场/巡览/平台阶段跳过、重复 Start/Skip、一次记局、菜单场地退出、相机交接不回跳、正常追钩/Overview 切换及演出禁用不启动玩法。
- 补充零时长配置、Time.timeScale=0 两轮检查通过。首次零时长测试误把“镜头完成”当作“整个入口完成”，在菜单 0.35 秒淡出结束前断言启动；修正测试等待两个条件后通过，无需改变玩法启动门控。
- 1440×2304 原生 ScreenCapture 核对菜单无人物/平台、巡览黑边和 SKIP、结束后的正常画面。720×1152 下新开场遮幅/按钮正常，但现有菜单布局存在裁切；未在本任务中重排原菜单，低分辨率菜单适配仍需单独处理。
- 测试副本的六份受影响 C#、场景和 Profile 与工作区 SHA256 一致；副本使用独立 company/product，避免改玩家实际存档。未启动当前编辑器内的 Play Mode，未执行 Android/WebGL 或完整钓鱼结算/排行榜回归。
- 临时证据目录：`C:/Users/73400/AppData/Local/Temp/SevenSeasIntro-643247fabe084c548704eb55e2099f1b`。`first-pass-results.txt`、`intro-results.txt`、`intro2.log`、`edge-final.log` 和 `menu-screen.png`/`survey-screen.png`/`gameplay-screen.png`。原生导入日志另有 Unity Search 初始化和长路径下 2D Tooling 编辑器资源导入异常，未阻止上述场景检查；不把临时环境日志称为全项目零错误。

## 适用边界与后续练习

- 装饰编辑现状：`FishingSceneEntry/MenuScenery (runtime)` 为 Play 时临时副本，退出 Play 不保存。编辑态将装饰放到原 `TileMapGrid_1/2/3` 对应根的子层级，运行时会随整棵地图复制；这些装饰也会出现在正式地图。根外的对象不会自动复制。只用于菜单的独立装饰/编辑态菜单预览目前没有作者工具，不能把临时副本误称为可持久编辑场地。

- 当前为同一正交 2D 场景、垂直地图、可复用水面材质的实现。运行时菜单布景不保存为第二套场景资产；菜单鱼只承担氛围表现。
- 可复用候选：演出完成/取消的一次性启动门控、物理空间分离的菜单布景、可调遮幅与安全区按钮。尚未在第二个项目验证，不称为通用模块或已掌握能力。
- 后续练习：在干净场景亲手重建“菜单→可跳过演出→计时开始”，说明为何相机混合期间不能重置退出镜头，并验证重复点击、取消和不同屏幕比例。
