# 2026-09-09 main 合并报告

用户要求：将当前交接分支合入远端 main，并汇报冲突。

## 合并输入

- 目标：origin/main，`0d76fc6`。
- 来源：codex/parallel-development-handoff-20260909，`8711115`。
- 拉取时双方分别有14和4个独有提交。以main为基线在独立工作树做普通merge，不强推，不用整树覆盖。

## 4个文本冲突及处理

| 文件 | 原因 | 处理 |
|---|---|---|
| Assets/Prefabs/LargeFish_Variant.prefab | main新增剪影Animator，交接分支增加胶囊尺寸 | 保留Animator整套新增块及引用，保留Capsule尺寸0.32×0.09 |
| Assets/Prefabs/MediumFish_Variant.prefab | 同上 | 保留Animator，并保留Capsule宽度0.2及继承高度 |
| Assets/Prefabs/SmallFish_Variant.prefab | 同上 | 保留Animator，并保留Capsule尺寸0.11×0.04 |
| Assets/Art Asset Folder/Fonts/PressStart2P/Press_Start_2P/PressStart2P-Regular SDF.asset | 动态字形表、图集数据分叉；main已有旧冲突标记 | 采用交接分支完整字体资产，保持GUID及材质/图集自洽；该版本此前通过原生暂停页渲染检查 |

字体的旧冲突标记在此次merge前就存在于origin/main，涉及历史`ab85d99`；不是将本次新冲突误留在结果中。合并后Assets/Packages/ProjectSettings无冲突标记，Git未解决冲突列表为空。

## 1处运行兼容修复

main新增的L/M/S动画循环写根SpriteRenderer.m_Sprite，原FishAppearance.Reveal只写一次图片，因此合并后会被下一帧剪影覆盖。

在FishAppearance只暂停该SpriteRenderer同对象上的Animator：选到有效揭示图才暂停，RestoreHiddenAppearance/OnDisable恢复此前启用状态。原来禁用的Animator不会被强行启用，子对象水花/涟漪Animator不受影响。保留完整剪影动画资源，没有删除动画来规避兼容。

## 自动合并保留内容

- main的Level 1/2/3、鱼钩美术、装饰/木障碍、鱼动画帧、像素化shader/材质和WebGL设置。
- main原有device-simulator.devices 1.0.1依赖按远端保留；未自行升级Unity或其他包。
- 交接分支的正式FishingLoopTest功能、胶囊导航、逃离/捕食、结算/仓库、音频/震动、暂停风格与全局UI音效。

## 验证与范围

- 使用现有同版本Unity引用，对合并工作树源码运行Runtime/Editor MSBuild：0错误，仅既有MSB3277依赖告警；没有修改生成csproj或原工作区源码。
- FishingLoopTest、Leaderboard和Level 1/2/3的本地fileID检查均无重复/缺失。
- Animator与揭示兼容的独立Unity验证结果记录在progress.md最新条目。
- 未声称完整合并工程已通过正式场景Play Mode、Android或WebGL构建。新增Level场景按main保留，未擅自把它们重建为正式流程或逐个接入所有新系统。
- 原工作区保持在交接分支，集成提交在独立工作树；后续如要试玩合并结果，应使用main版本。
