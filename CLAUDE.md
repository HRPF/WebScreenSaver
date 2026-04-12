# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Windows screensaver application that displays an analog/digital clock using WebView2 to render HTML/CSS/JavaScript. The C# WinForms host installs system-wide keyboard and mouse hooks to detect user input and exit the screensaver. The frontend is a configurable clock with both analog and digital display modes.

## Build and Run

- Build: `dotnet build`
- Run with specific mode (uses launch profiles):
  - Preview mode (small window): `dotnet run --launch-profile "预览WebScreenSaver"`
  - Fullscreen screensaver: `dotnet run --launch-profile "运行WebScreenSaver"`
  - Configuration: `dotnet run --launch-profile "配置WebScreenSaver"`
- Publish as single executable: `dotnet publish -c Release`
  - Output is a self-contained WinExe (WebScreenSaver.exe) in `bin\Release\net8.0-windows\publish\`
  - To install as a screensaver, rename `.exe` to `.scr` and place in `C:\Windows\System32\`

## Architecture

### Backend (C# WinForms)
- `Program.cs`: Entry point with command-line argument handling (`/c` config, `/s` screensaver, `/p` preview)
- `MainForm.cs`: Fullscreen form with WebView2 control, low-level keyboard/mouse hooks, and mouse movement detection
- System hooks (`SetWindowsHookEx`) capture global input events; any key press or mouse click exits the screensaver
- The WebView2 instance loads `web\index.html` from the application's output directory

### Frontend (HTML/JavaScript/CSS)
- Located in the `web\` folder, copied to output on build
- `index.html`: Container for digital clock, analog clock face, and date display
- `style.css`: Responsive styling with vw/vmin units for scaling
- `config.js`: User-configurable settings (clock type, colors, format, etc.)
- `clock.js`: Core logic for updating both clock types, with smooth animations for analog hands

### Configuration
- Modify `web\config.js` to change clock appearance, colors, format, etc.
- The configuration is static; there is no runtime UI for settings (the `/c` argument opens `index.html` in the default browser)
- The `web\` folder must be present alongside the executable (copied automatically via `.csproj`)

## Important Notes

- `MainForm.Designer.cs` is excluded from compilation (`<Compile Remove="MainForm.Designer.cs" />`); all UI initialization is done manually in `MainForm.InitializeComponent()`.
- The project targets `net8.0-windows` and requires the Windows Desktop runtime.
- The `.csproj` configures `PublishSingleFile` and `SelfContained` for deployment.
- The only NuGet dependency is `Microsoft.Web.WebView2` (version 1.0.3800.47).
- Launch profiles are defined in `Properties\launchSettings.json` for easy testing of different modes.
- The screensaver exits on any keyboard input (including function keys) or mouse click/wheel movement; mouse movement beyond a 10-pixel threshold also triggers exit.
- The analog clock performs an initial animation to “catch up” to the current time, then updates at intervals defined in `config.updateInterval` (default 1 second).