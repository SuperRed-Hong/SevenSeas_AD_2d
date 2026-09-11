# 移动端菜单误触与重复音效

## 现象与原因

- 用户确认地图/开场镜头部分完成，随后反馈移动端点其他位置也触发 Start，且一次触摸按下/抬起各响一次。
- 正式场景的 StartButton 是 NavigateButton 的 Prefab 实例。Image 使用 100×160 的 `start animation_01.png`，六帧动画实际不透明内容只占像素 X=21..74、Y=84..99（图片左上为原点）。RectTransform 为500×800、Scale=2.89，因此完整透明图片的点击矩形几乎覆盖屏幕；Image默认矩形射线检测不会排除透明像素。
- `UiButtonAudioFeedback.OnPointerEnter` 未区分设备，触屏落指会发送 Enter，播放 Select；抬指触发 Button.onClick，再播放 Confirm。同帧去重不能消除跨帧两声。

## 修改

- Start场景实例新增 `UiButtonHitArea`，通过 `ICanvasRaycastFilter` 按归一化矩形过滤命中；X=0.19、Y=0.3625、Width=0.58、Height=0.125（左下原点），覆盖全部动画帧并留少量触摸边距。保留原图片、动画、尺寸、位置和Prefab本体，不开启贴图Read/Write，也不逐像素查询。
- 同对象和子Graphic的命中都受过滤。后续替换成不同布局的图片时，应在StartButton的 `Normalized Rect` 重新调整交互区域；此配置不是自动识别任意美术的算法。
- 音效忽略Input System Touch指针及旧输入系统的触摸Enter；鼠标/笔悬停和键盘选择仍保留。只有Button确认有效点击后播放Confirm/Back，拖出或禁用不额外发声。

## 验证

- 临时工程使用Unity6000.3.23f1、当前正式场景与实际Start Prefab，并注入真实Input System Touchscreen事件；禁用实际鼠标输入干扰，测试环境显式允许无焦点接收注入事件，未修改正式项目输入设置。
- 640×480下13项通过：临时关闭过滤器复现透明区命中、启用后排除、可见区域命中、四处外围排除、空白点击无事件/音效、落指无声音、抬指一次点击/Confirm、拖出/拖入不激活、鼠标悬停去重、键盘选择、禁用按钮。
- 1440×2304下相同13项全部通过。音效次数按实际路由待播放事件记录，不代表手机扬声器听感；Android/WebGL设备仍需复测。
- 临时证据位于 `C:/Users/73400/AppData/Local/Temp/SevenSeasIntro-643247fabe084c548704eb55e2099f1b`，文件 `mobile-ui-640-results.txt`、`mobile-ui-results.txt` 与 `mobile-ui-portrait.log`。

## 可复用候选

- 原问题：带透明留白的动画UI有巨大隐形点击区；可迁移部分为视觉范围与交互范围分离的归一化射线过滤。
- 项目耦合：Start六帧美术对应的矩形。边界：只支持矩形交互区，不自动识别不规则轮廓或新图片布局。
- 验证欠账：真实设备与第二种UI布局接入。后续练习：亲手实现过滤器，在另一种带留白图片上校准范围，并验证缩放、子Graphic和拖入/拖出行为。当前仅资产候选，不视为已独立提炼或用户已掌握。
