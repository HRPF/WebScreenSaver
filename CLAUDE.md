# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Windows screensaver application that supports **multiple web-based screensavers** managed through a main configuration page. The C# WinForms host installs system-wide keyboard and mouse hooks to detect user input and exit the screensaver. Each screensaver is a subfolder under `web/` containing its own `index.html`, `config.html`, and assets.

## Build and Run

- Build: `dotnet build`
- Run with specific mode (uses launch profiles):
  - Configuration (主配置页): `dotnet run --launch-profile "配置WebScreenSaver"`
  - Fullscreen screensaver: `dotnet run --launch-profile "运行WebScreenSaver"`
  - Preview mode (unsupported): `dotnet run --launch-profile "预览WebScreenSaver"`
- Publish as single executable: `dotnet publish -c Release`
  - Output is a self-contained WinExe (WebScreenSaver.exe) in `bin\Release\net8.0-windows\publish\`
  - To install as a screensaver, rename `.exe` to `.scr` and place in `C:\Windows\System32\`

## Project Structure

```
├── ConfigManager.cs          ← 配置持久化 + 屏保自动发现
├── ConfigForm.cs             ← 主配置窗体（WebView2 承载主配置页）
├── MainForm.cs               ← 全屏幕保窗体（WebView2 + 键盘/鼠标钩子）
├── Program.cs                ← 入口点（/c → ConfigForm, /s → MainForm）
├── config.json               ← 持久化文件：当前选中的屏保 ID
│
└── web/                      ← 主配置页 + 各屏保目录
    ├── index.html            ← 主配置页（屏保列表 + 预览 + 操作按钮）
    ├── main-config.js        ← 主配置页逻辑
    ├── main-config.css       ← 主配置页样式
    │
    ├── Clock/                ← 时钟屏保（首个内置屏保）
    │   ├── index.html        ← 时钟屏保入口
    │   ├── config.html       ← 时钟配置页（样式、颜色、字体等）
    │   ├── config.js         ← 时钟静态配置对象
    │   ├── clock.js          ← 时钟核心逻辑
    │   ├── style.css         ← 时钟样式
    │   ├── config-ui.js      ← 时钟配置页交互逻辑
    │   └── config.css        ← 时钟配置页样式
    │
    └── NewScreensaver/       ← 新增屏保示例目录结构
        ├── index.html        ← 屏保入口（必须）
        └── config.html       ← 屏保配置页（可选）
```

## Architecture

### Backend (C# WinForms)
- `Program.cs`: Entry point with command-line argument handling. `/s` runs fullscreen screensaver (`MainForm`); `/c` and no args open the main configuration page (`ConfigForm`).
- `ConfigForm.cs`: Windowed form (1200x800) hosting a WebView2 that loads the main config page (`web/index.html`). Communicates with the page via `chrome.webview.postMessage`:
  - Sends `{ type: 'init', screensavers: [...], currentSelection: '...' }` on page load
  - Handles `selectScreensaver` → persists to `config.json`
  - Handles `navigateToConfig` → navigates WebView2 to the screensaver's `config.html`
  - Handles `closeConfig` → closes the form
- `MainForm.cs`: Fullscreen form with WebView2 control, low-level keyboard/mouse hooks, and mouse movement detection. Loads the selected screensaver's `index.html` using virtual host mapping.
- `ConfigManager.cs`: Static helper that reads/writes `config.json` and discovers available screensavers by scanning `web/` subdirectories. Each subfolder containing `index.html` is treated as a valid screensaver.
- All WebView2 pages are served via `SetVirtualHostNameToFolderMapping` mapping `screensaver.local` → `web/` folder, providing a consistent same-origin environment that enables iframe previews and `postMessage` communication.

### Frontend (HTML/JavaScript/CSS)
- **Main config page** (`web/index.html`): Two-column layout — left sidebar lists available screensavers (auto-discovered by folder scanning), right area shows an iframe preview and action buttons ("配置" and "应用").
- **Each screensaver** is a self-contained subfolder. The convention requires at minimum an `index.html` entry point. An optional `config.html` provides per-screensaver settings.

### Configuration Persistence
- `config.json` (alongside the executable) stores `{ "selectedScreensaver": "Clock" }`
- The selected screensaver is saved when the user clicks "应用" in the main config page
- Screensaver discovery is automatic: any subfolder under `web/` containing `index.html` appears in the list

## Adding a New Screensaver

1. Create a new folder under `web/` (e.g., `web/MyScreensaver/`)
2. Add at minimum an `index.html` file (the screensaver entry point)
3. Optionally add a `config.html` for user-configurable settings
4. Rebuild (`dotnet build`)
5. The new screensaver automatically appears in the main configuration page

## Important Notes

- `MainForm.Designer.cs` is excluded from compilation (`<Compile Remove="MainForm.Designer.cs" />`); all UI initialization is done manually.
- The project targets `net8.0-windows` and requires the Windows Desktop runtime.
- The `.csproj` configures `PublishSingleFile` and `SelfContained` for deployment. Web files are copied using a wildcard pattern (`web\**\*`).
- The only NuGet dependency is `Microsoft.Web.WebView2` (version 1.0.3800.47).
- Launch profiles are defined in `Properties\launchSettings.json` for easy testing of different modes.
- The screensaver exits on any keyboard input (including function keys) or mouse click/wheel movement; mouse movement beyond a 10-pixel threshold also triggers exit.
- Preview mode (`/p`) is not yet implemented.
