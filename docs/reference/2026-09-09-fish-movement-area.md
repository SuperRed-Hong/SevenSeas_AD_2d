# 鱼移动矩形与生成避让

用户要求：岸边人物继续使用线段限位；鱼使用独立、可视化矩形；生成避免Tilemap与鱼重叠。

## 接线和调整

- FishingLoopTest/GameplayRoot/FishMovementArea：BoxCollider2D表达游动边界，青色Gizmo始终显示于开启Gizmos的Scene视图。选中后用Edit Collider，或修改Size/Offset。不是Game视图中的UI边框。
- 初始矩形覆盖原两个生成区域的并集：中心(-2.175,12.04)、尺寸(15.56,30.1)，可由用户按三张地图调整。
- 两个FishSpawner的movementArea均引用此矩形；各自原BoxCollider2D继续表示出生区域。出生需同时满足游动范围和出生取样范围。未接movementArea的其他场景保留以出生区域作移动范围的旧行为。
- ShoreLaneController左右岸边限位不变。自由游动、追饵、逃离与捕食移动受矩形约束；挂钩后仍交由收线流程带动。

## 生成与地图

- FishNavigationGeometry按完整胶囊/碰撞形状及padding检查矩形和TilemapCollider2D，支持同对象Tilemap的CompositeCollider2D；普通标记障碍继续识别。
- IsSpawnPoseAllowed额外拒绝与任意活动FishController所属碰撞体重叠，跨生成器生效；保留同生成器中心间距配置。普通导航不将其他鱼作为静态障碍，避免新增鱼群互相堵塞规则。
- 随机选图后立即ProcessTilemapChanges，再同步Physics2D，避免等到LateUpdate才生成地图碰撞形状。只有当前激活地图参与查询；无碰撞的装饰Tile不属于物理障碍。
- 生成尝试次数仍有上限，空间不足会少生成并警告，不强行生成到障碍中。

## 验证与范围

- 当前源码MSBuild通过，正式场景本地fileID完整，无重复。未改Unity或项目包版本。
- Unity6000.3.23f1独立工程真实Physics2D通过8项：水域允许、完整鱼身边界限制、其他鱼重叠拒绝、导航不被其他鱼阻挡、停用鱼忽略、无障碍标记Tilemap阻止生成、Tilemap阻止游动扫掠、停用地图忽略。
- 证据：系统临时目录SevenSeasFishAreaResults.txt与SevenSeasFishAreaNative.log。测试使用当前FishController/FishNavigationGeometry源码和生成的真实Tilemap碰撞体；并非正式场景完整试玩。三个实际地图生成数量和运行手感待验证。
- 当前三份进度文档仍在Git冲突状态，本轮不覆盖，暂存本记录；未提交推送。

## 后续修复：鱼钩漏判Tilemap

- 用户复现鱼钩落在Tilemap上没有失败反馈。原因是此前只有鱼导航识别Tilemap，FishingLoopController落点调用的ReelingObstacle.ContainsPoint及收线Trigger仍只识别ReelingObstacle标记。
- 将判定集中到ReelingObstacle.IsBlockingCollider，统一用于鱼导航、落点、线段/圆扫掠与ReelingController触发碰撞。保持既有ObstacleLanding流程：扣一钩，剩余钩大于0回ReadyToCast，否则GameOver；不另加反弹动画。
- 当前源码编译通过。真实Unity Physics2D共14项通过（原8项及新增落点、线段、圆扫掠、收线碰撞分类、水域允许、停用地图不触发落点失败6项）。证据在系统临时SevenSeasFishAreaResults.txt、SevenSeasHookTilemapNative.log。正式场景完整失败反馈动画/设备运行仍待试玩，未提交推送。
