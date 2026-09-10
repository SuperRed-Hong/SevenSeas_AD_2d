# Intro 运镜开始播放 Wave

- 用户要求 Wave 环境循环在 IntroCamera 开始运镜时激活并渐入。
- FishingIntroController 在 BeginIntro 接管完成后、首次 MoveTo 前发出 MovementStarted；FishingAudioFeedback 显式引用场景 intro 并订阅，开放原环境音门控，沿用 ambientFadeInSeconds（当前1.5秒）非缩放时间渐入。
- 主菜单不提前播放 Wave；音乐原流程保留。Casting兼容入口保留，但已经播放的环境循环不会重播或重置渐入。静音/暂停期间只记录启动资格，恢复后按原逻辑渐入；GameOver停止。禁用时解除订阅，重新启用可从Intro.HasStartedMovement恢复资格。
- FishingLoopTest音频组件绑定Intro 2147000000。Runtime编译通过、目标脚本差异检查通过；实际开场听感/跳过/静音恢复待Play Mode试听。未提交推送。

## 现场未生效后的更正

- 用户反馈没有变化，再读当前保存的场景发现音频组件已无intro字段，无法订阅事件；不推断引用何时或被谁覆盖。仅验证一次磁盘接线不足以证明实际运行生效。
- 补回当前唯一Intro引用；OnEnable在显式引用缺失时搜索同一场景（含未激活对象）的唯一FishingIntroController，拒绝含糊的多实例，不跨场景绑定。
- Editor/Development Build在事件到达时记录Intro Wave requested，包括clip、静音/暂停/结束状态、AudioSource.isPlaying、渐入时长及目标音量。当前目标为0.3×0.35=0.105，保持用户音量参数。
- 本次编译通过。实际用户运行的事件日志及听感尚待验证，不能宣称已经听到Wave。
