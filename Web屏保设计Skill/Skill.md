---
名称: Web屏保设计
描述: 创建独具特色、设计品质卓越且可直接用于个人电脑的适合全屏显示的Web屏保。根据用户要求构建Web屏保时（例如：简约时钟、粒子动画、仪表盘、图片幻灯片等等），请使用此技能。生成在严格满足用户要求的前提下，又富有创意且精雕细琢的代码与 UI 设计，避免落入泛化的 AI 美学俗套。
---

本技能指导创建独具特色、可用于个人电脑的本地屏保页面，避免泛化的“AI 垃圾”美学。实现真正可运行的代码，在美学细节和创意选择上给予特别关注。

用户会提供Web前端需求：需要构建的组件、页面、视觉风格等。其中可能包含有关目的、受众或技术约束的背景信息。

## 设计思维

在编码之前，理解上下文并确定一个鲜明的美学方向：

- **目的**：这个界面解决什么问题？谁在使用它？
- **基调**：选择一个极端方向：极简主义、极繁主义混乱、复古未来主义、有机/自然、奢华/精致、俏皮/玩具感、编辑/杂志风格、粗野主义/原始、装饰艺术/几何感、柔和/粉彩、工业/实用主义等。从中汲取灵感，但设计的风格要忠实于美学方向。
- **约束**：技术需求（框架、性能、可访问性）。
- **差异化**：是什么让它令人难忘？人们会记住哪一点？

**关键**：选择一个清晰的概念方向并精准执行。大胆的极繁主义和精致的极简主义都可行 —— 关键在于意图性，而非强度。

然后实现可运行的代码（HTML/CSS/JS、React、Vue 等），要求：
- 达到生产级别且功能完整
- 视觉上引人注目且令人难忘，但要记住这始终是一个屏保
- 具有统一的美学观点
- 每个细节都经过精心打磨
- 不应该具备交互功能（鼠标、键盘等）

## 项目约定

### 目录结构约定

生成的屏保放在一个独立文件夹中，文件夹以屏保名称命名：

```
web/
└── 你的屏保文件夹/           ← 需要生成的内容
    ├── index.html           ← 屏保入口文件（必须）
    ├── config.html          ← 样式配置页（可选）
    ├── config.js            ← 默认配置对象（可选，配合 config.html 使用）
    └── 其他资源（CSS、JS、图片等）

settings/                    ← 运行时自动生成（用户保存配置后）
└── 你的屏保文件夹.json       ← 持久化的样式设置，由 C# 后端读写
```

### index.html —— 屏保入口（必须）

#### 基本要求

- 全屏自适应，填满整个浏览器视口
- 无滚动条
- 不自带任何 UI 控件（按钮、菜单等）
- 所有资源使用相对路径引用

#### 接收已保存的配置

屏保运行在主程序 WebView2 中，C# 后端在页面加载完成后会自动注入已保存的样式配置。屏保需要监听 `chrome.webview` 消息以接收配置：

```javascript
// 监听 C# 后端注入的已保存配置
if (typeof chrome !== 'undefined' && chrome.webview) {
    chrome.webview.addEventListener('message', function(e) {
        if (e.data.type === 'loadSettings') {
            // e.data.settings 包含完整的配置对象
            // 合并到当前配置并重新渲染
            config = { ...defaultConfig, ...(window.appConfig || {}), ...e.data.settings };
            render();
        }
    });
}
```

如果没有已保存的配置（首次运行），C# 不会发送 `loadSettings` 消息，屏保应使用 `config.js` 中的 `window.appConfig` 默认值。

#### 预览模式支持

屏保需要检测 URL 参数 `preview=true`，在预览模式下调整行为：

```javascript
const urlParams = new URLSearchParams(window.location.search);
const isPreviewMode = urlParams.has('preview');

if (isPreviewMode) {
    // 通知父窗口预览已就绪
    window.parent.postMessage({ type: 'previewReady' }, '*');
}
```

预览模式下应：
- 跳过开场动画或长过渡动画, 立即显示完整内容（可选）
- 发送 `previewReady` 消息通知父窗口

#### 运行时配置更新（预览 iframe）

当屏保在配置页的预览 iframe 中运行时，需要监听来自父窗口的实时配置更新：

```javascript
window.addEventListener('message', function(event) {
    if (event.data && event.data.type === 'updateConfig') {
        // event.data.config 包含新的配置对象
        applyNewConfig(event.data.config);
    }
});
```

**两种消息机制的说明：**
- `chrome.webview.addEventListener('message', ...)` — 来自 C# 后端，用于加载已保存的配置（全屏屏保和配置页均可用）
- `window.addEventListener('message', ...)` — 来自父窗口（配置页），用于实时预览更新（仅在预览 iframe 中生效）

两者不冲突，应同时实现。

#### 主屏保程序如何找到入口、打开网页

```c#
            // 获取web文件夹路径
            string webFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");
            string selected = ConfigManager.GetSelectedScreensaver();

            // 使用虚拟主机映射，统一同源策略，支持 iframe + postMessage
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "screensaver.local", webFolder,
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

            string indexPath = Path.Combine(webFolder, selected, "index.html");
            if (File.Exists(indexPath))
            {
                webView.Source = new Uri($"https://screensaver.local/{selected}/index.html");
            }
            else
            {
                webView.NavigateToString($"<html><body style='background:black;color:white;font-size:48px;text-align:center;padding-top:200px'>屏保加载失败<br>未找到 {selected}/index.html</body></html>");
            }
```

### config.html —— 样式配置页（可选）

#### 用途

允许用户在主配置页中点击"样式设置"按钮后，在主程序 WebView2 中打开此页面修改屏保的颜色、速度、密度等参数。

#### 配置持久化（WebView2 模式）

主程序将配置页加载在 WebView2 中，通过 `chrome.webview.postMessage` 让 C# 后端代为读写磁盘文件。

**保存配置到 C# 后端：**

```javascript
// 检测是否在 WebView2 中运行
const isWebView = typeof chrome !== 'undefined' && chrome.webview;

function saveConfig(configObject) {
    if (isWebView) {
        chrome.webview.postMessage(JSON.stringify({
            type: 'saveSettings',
            id: screensaverId,   // 屏保文件夹名称
            settings: configObject
        }));
        alert('设置已保存！');
    }
}
```

**加载已保存的配置（C# 后端注入）：**

```javascript
if (isWebView) {
    chrome.webview.addEventListener('message', function(e) {
        if (e.data.type === 'loadSettings') {
            // 使用 C# 注入的已保存配置回填表单
            currentConfig = { ...defaultConfig, ...e.data.settings };
            updateForm();
            updatePreview();
        }
    });
}
```

配置存储在 `settings/{screensaverId}.json` 文件中，与主程序 `config.json` 同级。

#### 预览 iframe 实时更新

配置页通常包含一个预览 iframe（加载 `index.html?preview=true`），配置变化时通过 `window.postMessage` 实时同步到预览：

```javascript
function updatePreview() {
    if (previewFrame && previewFrame.contentWindow) {
        previewFrame.contentWindow.postMessage({
            type: 'updateConfig',
            config: currentConfig
        }, '*');
    }
}
```

#### 总结：完整消息流

```
保存配置：
  config-ui.js → chrome.webview.postMessage({ type: "saveSettings", id, settings })
    → C# ConfigManager.SaveSettings() → 写入 settings/{id}.json

加载已保存配置：
  C# → PostWebMessageAsJson({ type: "loadSettings", settings: {...} })
    → config-ui.js chrome.webview message 监听 → 回填表单
  
预览 iframe 更新：
  config-ui.js → previewFrame.contentWindow.postMessage({ type: "updateConfig", config })
    → clock.js window.addEventListener('message', ...) → 重新渲染
```

## 兼容性

- 屏保运行在 WebView2（基于 Chromium）环境，支持 ES6+、CSS3+
- 可直接使用 CSS `var()`、`calc()` 等现代特性
- 可使用 Canvas、WebGL、CSS3D 等渲染方式

## 前端美学指南

重点关注：

- **字体排印**：选择优美、独特、有趣的字体。避免使用 Arial 和 Inter 等泛用字体；选择能够提升前端美感的独特字体方案。将富有特色的展示字体与精致的正文字体搭配使用。
- **色彩与主题**：坚持统一的美学风格。使用 CSS 变量保证一致性。主色调搭配鲜明的点缀色，效果优于谨慎、均匀分布的调色板。
- **动效**：利用动画实现效果。优先使用纯 CSS 解决方案处理 HTML。
- **空间构图**：不拘一格的布局、不对称、重叠、对角线流动、打破网格的元素。充足的留白或受控的密度。
- **背景与视觉细节**：营造氛围和深度，也可以使用纯色背景。添加与整体美学相匹配的情境效果和纹理。运用创意形式，如渐变网格、噪点纹理、几何图案、分层透明度、戏剧性阴影、装饰性边框和颗粒叠加层。

**严禁**使用泛化的 AI 生成美学，例如过度使用的字体家族（Inter、Roboto、Arial、系统字体）、陈词滥调的配色方案（特别是白色背景上的紫色渐变）、可预测的布局和组件模式，以及缺乏上下文特色的千篇一律设计。

进行创造性诠释，做出让人感觉真正为上下文量身定制的大胆选择。每次的设计都不应雷同。在亮色与暗色主题、不同字体、不同美学风格之间变化。不要在多次生成中趋同于常见的选择。

**重要提示**：将实现的复杂度、美学愿景与屏保用途这三者相匹配。极繁主义的设计需要包含大量动效和效果的精细代码。极简或精致的设计则需要克制、精确，并仔细关注间距、字体排印和微妙的细节。优雅源于对设想的精准执行。

请记住：Claude 拥有非凡的创造能力。不要有所保留，展示出突破常规、全心投入到一个独特愿景中时所能真正创造出的作品。