# webgl-dist —— GitHub Pages 发布目录

这个文件夹的内容会被 `.github/workflows/deploy-webgl.yml` 原样发布到
**https://superred-hong.github.io/SevenSeas_AD_2d/**

顶层页面，不是 iframe。所以 iOS Safari 的 Permissions Policy 限制不存在，
`deviceorientation` / `AttitudeSensor` 正常可用。

---

## 怎么发一个新版本

1. Unity 里 **File → Build Settings → WebGL → Build**，输出到任意临时目录
2. 把输出目录里的**全部内容**（`index.html`、`Build/`、`TemplateData/`、`StreamingAssets/` 如果有）
   复制进这个文件夹，覆盖旧的
3. `git add webgl-dist && git commit && git push`
4. Actions 自动部署，一分钟左右生效

推上去之后去 Actions 页面看一眼绿灯。workflow 里有两道检查会在
构建没进仓库、或者压缩设置不对的时候**直接报错**，不会默默发一个打不开的页面。

---

## 两个必须保持的设置

### 1. Decompression Fallback 必须开着

**Player Settings → Publishing Settings → Decompression Fallback → 勾上**

项目现在是 Brotli 压缩（`webGLCompressionFormat: 0`）。GitHub Pages 是纯静态
托管，不会给 `.br` 文件发 `Content-Encoding: br` 响应头，浏览器拿到的是一坨
没法解析的字节，游戏会卡在加载界面报
`Unable to parse Build/xxx.framework.js.br`。

开了 Decompression Fallback，Unity 会把解压器打进 loader,由 JS 自己解压，
在任何静态托管上都能跑。代价是启动慢一点点。

> itch.io 不需要这个设置，因为 itch 的服务器自己处理了编码。
> 但开着对 itch 也无害（用不到就不会走那条路），所以一个设置两边通用。

### 2. `.gitignore` 末尾的三行否定规则不能删

仓库的 `.gitignore` 里有 `[Bb]uild/`，而 Unity 的输出子目录**正好叫 `Build/`**。
没有末尾那三行否定，`webgl-dist/Build/` 会被 git **静默忽略**——
你会推上去一个只有 `index.html` 没有游戏的空壳，而且 git 不会给任何提示。

验证方法（没有输出说明没被忽略，是对的）：

```bash
git check-ignore -v webgl-dist/Build/WebGL.loader.js
```

---

## 首次启用（只需做一次）

GitHub 仓库 → **Settings → Pages → Build and deployment → Source** 选
**GitHub Actions**（不是 Deploy from a branch）。

---

## 和 itch.io 的关系

两边可以同时存在，互不影响：

| | itch.io 内嵌 | 这里（GitHub Pages） |
|---|---|---|
| 运行环境 | 跨域 iframe | 顶层页面 |
| iOS 体感 | ❌ 被 Permissions Policy 挡住（缺 `magnetometer`） | ✅ 无限制 |
| Android 体感 | ✅ | ✅ |
| 触屏按钮 | ✅ | ✅ |

建议 itch 页面留着当门面，正文里放一句
「iPhone 玩家点这里获得体感版」指向上面那个链接。

玩家在 Safari 里打开后点 **分享 → 添加到主屏幕**，就是全屏无浏览器边框，
体验接近原生 App。

---

## 体积提醒

每个 WebGL 包约 18–19 MB，直接提交进仓库。多发几版仓库会变大，
git 历史里的旧包不会自动清理。如果以后觉得太重，可以改成
workflow 从 Release 附件拉包，或者换 Netlify / Cloudflare Pages 拖拽部署。
现阶段直接提交最省事。
