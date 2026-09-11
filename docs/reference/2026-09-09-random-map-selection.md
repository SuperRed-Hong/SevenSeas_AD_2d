# 每局随机地图

## 最新阅读入口与流程变更

当前选择器位于常开FishingSceneEntry，已支持空mapContainer从候选Grid共同父对象恢复；下文初版“挂在Lanscape_TileMap”的记录仅为历史。原生空引用Play生命周期测试亦通过。三份进度文档现已追加汇总，但Git索引仍UU，未宣称冲突已解决。

用户最新要求将主菜单改为与随机地图无关的空镜，Start后缓慢移向地图上方，以电影画幅由上向下巡览，到ShorePlayer平台后交还既有游戏镜头。该流程尚未实现；下一chat先讨论参数和教程/校准编排，参见[地图与开场镜头交接](2026-09-09-map-intro-camera-handoff.md)。原chat继续Editor工具教学。

## 当前现场故障更正：Map Container 空引用

- 用户再次报告两张地图同时显示。此时磁盘场景选择器存在、三张maps引用完整，但mapContainer为fileID 0；最新Editor.log明确出现FishingMapSelector requires a map container，Initialize提前退出。这是本次直接证据，不能用先前隔离测试通过代替现场排查，也不推断谁修改了引用。
- 补回场景mapContainer到Lanscape_TileMap Transform 314732374。代码在此引用缺失时由maps[0]的父对象推导，再逐一验证所有候选确实同父，避免空引用让编辑器预览状态直接进入游戏。
- 本次Runtime编译通过；真实Unity测试故意清空mapContainer，64种地图/祖先开关组合、512断言通过。证据为系统临时SevenSeasMapResults.txt和SevenSeasMapNullContainer.log。仍不代表当前正式场景视觉已验收。

- 用户将三张地图整合到 FishingLoopTest 的 Lanscape_TileMap 下，要求每次游玩随机启用一张。
- 新增 FishingMapSelector，挂在 Lanscape_TileMap，显式引用 TileMapGrid_1、TileMapGrid_2、TileMapGrid_3；等概率选择，允许连续重复。
- 在执行顺序 -1000 的 Awake 选图，早于场景进入逻辑及 FishSpawner.Start。未选 Grid 整体停用，其子对象和碰撞体同时停用，然后同步 Physics2D。暂停/恢复不会重选；现有 Play Again 重载游戏场景，重新选图。
- 不修改地图位置、美术、障碍物配置或独立 Level 场景。若某地图专属内容放在 Grid 外，不由此组件控制。
- 当前源码 MSBuild 编译通过；正式场景本地 fileID 无重复或缺失，差异检查通过。尚未运行正式场景 Play Mode，需复测三张地图的生成、追饵和障碍行为。Console 输出 Fishing map selected 可确认选中地图；Inspector Maps 数组可查看接线。
- 当前 Git 将 task_plan.md、findings.md、progress.md 标为未解决冲突，本轮不覆盖这些文档；以本文件临时记录实现和验证范围，待冲突处理后汇入进度。

## 编辑器预览状态与运行初始化分离（后续修复）

- 用户要求编辑器手动开关地图不影响每局随机结果。复查当前磁盘场景发现：Lanscape_TileMap 只有 Transform，未找到 FishingMapSelector 的 GUID；前述接线描述已经不代表当前场景事实，无法仅凭文件判断接线何时丢失。
- 现在将 FishingMapSelector 接到常开、独立根对象 FishingSceneEntry，显式引用 Lanscape_TileMap 与三个 Grid。地图关闭不再导致选择器的 Awake 被一起跳过。
- Awake（执行顺序 -1000）调用幂等 Initialize：先校验完整候选数组，从全部候选等概率抽一张，覆盖三个 Grid 的 activeSelf，再开启地图容器及其祖先 MainLanscape / Obstacles，处理所选 Tilemap 碰撞重建并同步 Physics2D。未选地图整体停用；编辑时三个 Grid 任意开关、地图父级关闭均不改变候选范围。没有修改菜单或 GameplayRoot 的开关。
- 同一场景实例重复 Initialize、暂停或重新启用组件不重抽；Play Again 仍通过场景重载重新抽取。编辑器可继续自由预览，但 FishingSceneEntry 是运行入口，应保持启用。未设计运行中手动隐藏所选地图后的自动修复，也未支持关闭 Scene Reload 的快速进入模式；当前项目设置仍重载场景。
- 验证：隔离 Unity 6000.3.23f1 原生环境中，以当前源码通过 64 种状态组合（3 个 Grid 的 8 种开关 × 3 个祖先的 8 种开关）、512 个断言：唯一活动地图、只有所选 Tilemap 参与真实 Physics2D 查询、固定随机种子不受预览状态影响、重复初始化不消耗随机数。另通过原生 Play Mode 生命周期检查：所有地图和祖先关闭时，Awake 仍完成选图与碰撞同步，默认执行顺序的 Start 已能读取有效地图及碰撞。
- 证据位于本机临时目录 SevenSeasMapResults.txt、SevenSeasMapChecks.log、SevenSeasMapPlayChecks.log；隔离工程内测试源码与项目 FishingMapSelector.cs 的 SHA256 一致。正式场景本地 fileID 无重复或缺失，选图脚本 GUID 接线存在且唯一。完整正式场景游玩、Android/WebGL 尚未验证。场景原有其他改动存在四处行尾空格，本次新增块无行尾空格，未为此改动用户其他区域。
