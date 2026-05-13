using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebClockScreensaver
{
    public partial class MainForm : Form
    {
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;

        // Windows API 钩子相关
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        // 钩子类型常量
        private const int WH_KEYBOARD_LL = 13;    // 低级键盘钩子
        private const int WH_MOUSE_LL = 14;       // 低级鼠标钩子

        // 键盘消息常量
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        // 鼠标消息常量
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEWHEEL = 0x020A;

        // 钩子句柄
        private IntPtr keyboardHook = IntPtr.Zero;
        private IntPtr mouseHook = IntPtr.Zero;

        // 钩子委托实例
        private HookProc keyboardProc;
        private HookProc mouseProc;

        // 鼠标移动检测（窗体 + 定时器）
        private Point lastMousePos;
        private const int MOUSE_MOVE_THRESHOLD = 10;  // 阈值改为10像素
        private System.Windows.Forms.Timer mouseTimer;

        public MainForm()
        {
            InitializeComponent();
            InitializeWebView();

            // 初始化鼠标位置
            lastMousePos = Control.MousePosition;

            // 启用键盘预览（备用）
            this.KeyPreview = true;

            // 隐藏鼠标光标
            this.Cursor = Cursors.No;
            Cursor.Hide();
        }

        private void InitializeComponent()
        {
            // 窗体设置
            this.Text = "时钟屏保";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.TopMost = true;

            // 窗体事件
            this.Load += MainForm_Load;
            this.FormClosed += MainForm_FormClosed;
            this.MouseMove += MainForm_MouseMove;   // 启用窗体级别的鼠标移动检测

            // 定时器：定期检查鼠标全局位置（备用）
            mouseTimer = new System.Windows.Forms.Timer();
            mouseTimer.Interval = 500;
            mouseTimer.Tick += (s, e) => CheckMouseMovement();
            mouseTimer.Start();
        }

        // 窗体加载时安装钩子
        private void MainForm_Load(object sender, EventArgs e)
        {
            InstallHooks();
        }

        // 窗体关闭时卸载钩子
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UninstallHooks();
            mouseTimer?.Stop();
            mouseTimer?.Dispose();
        }

        // 安装Windows钩子
        private void InstallHooks()
        {
            try
            {
                // 创建委托实例
                keyboardProc = new HookProc(KeyboardHookProc);
                mouseProc = new HookProc(MouseHookProc);

                // 获取当前进程模块句柄
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    // 安装键盘钩子
                    keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc,
                        GetModuleHandle(curModule.ModuleName), 0);
                    // 安装鼠标钩子
                    mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc,
                        GetModuleHandle(curModule.ModuleName), 0);

                    if (keyboardHook == IntPtr.Zero || mouseHook == IntPtr.Zero)
                    {
                        MessageBox.Show("无法安装系统钩子，将使用备用检测模式", "警告",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装钩子失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 卸载Windows钩子
        private void UninstallHooks()
        {
            if (keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(keyboardHook);
                keyboardHook = IntPtr.Zero;
            }

            if (mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }
        }

        // 键盘钩子处理函数（保留原逻辑）
        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                // F1-F12 或任意按键均退出
                if (vkCode >= 0x70 && vkCode <= 0x7B) // F1-F12
                {
                    ExitScreensaver();
                    return (IntPtr)1;
                }

                // 检查常规按键
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    // 允许某些系统键（如Alt+Tab）但不退出
                    // 这里我们让所有按键都退出
                    ExitScreensaver();
                    return (IntPtr)1; // 阻止事件传递
                }
            }

            // 传递给下一个钩子
            return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        // 鼠标钩子处理函数：仅处理点击、滚轮等非移动事件，移动事件由窗体和定时器处理
        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                // 只处理鼠标点击和滚轮事件，移动事件忽略（交给窗体和定时器）
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN ||
                    msg == WM_MBUTTONDOWN || msg == WM_MOUSEWHEEL)
                {
                    ExitScreensaver();
                    return (IntPtr)1; // 阻止事件传递
                }
                // 其他鼠标消息（包括WM_MOUSEMOVE）直接传递，不做任何处理
            }
            // 传递给下一个钩子
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        // 备用鼠标移动检测（窗体级别）
        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (lastMousePos.IsEmpty)
            {
                lastMousePos = e.Location;
                return;
            }

            if (Math.Abs(e.X - lastMousePos.X) > MOUSE_MOVE_THRESHOLD ||
                Math.Abs(e.Y - lastMousePos.Y) > MOUSE_MOVE_THRESHOLD)
            {
                ExitScreensaver();
            }
            // 注意：这里不更新lastMousePos，因为定时器会基于全局位置更新；
            // 如果希望实时更新位置防止连续触发，也可以更新，但定时器逻辑会覆盖。
            // 为简化，我们让定时器统一维护lastMousePos，所以这里不更新。
        }

        // 定时器检测：基于屏幕坐标的鼠标移动
        private void CheckMouseMovement()
        {
            Point currentPos = Control.MousePosition;
            if (!lastMousePos.IsEmpty &&
                (Math.Abs(currentPos.X - lastMousePos.X) > MOUSE_MOVE_THRESHOLD ||
                 Math.Abs(currentPos.Y - lastMousePos.Y) > MOUSE_MOVE_THRESHOLD))
            {
                ExitScreensaver();
            }
            lastMousePos = currentPos;
        }

        // 退出屏幕保护程序
        private void ExitScreensaver()
        {
            // 确保只调用一次
            if (!this.IsDisposed && !this.Disposing)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    Application.Exit();
                });
            }
        }

        // 初始化WebView2（保持不变）
        private async void InitializeWebView()
        {
            try
            {
                webView = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView.Dock = DockStyle.Fill;
                webView.AllowExternalDrop = false;
                this.Controls.Add(webView);

                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
                await webView.EnsureCoreWebView2Async(env);

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

                // 禁用浏览器功能
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                // 页面加载完成后注入已保存的配置
                webView.CoreWebView2.NavigationCompleted += (s, e2) =>
                {
                    if (e2.IsSuccess)
                    {
                        var settings = ConfigManager.GetSettings(selected);
                        if (settings.HasValue)
                        {
                            var msg = new { type = "loadSettings", settings = settings.Value };
                            string json = System.Text.Json.JsonSerializer.Serialize(msg);
                            webView.CoreWebView2.PostWebMessageAsJson(json);
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化WebView2失败: {ex.Message}");
            }
        }

        // Windows API 声明
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // 鼠标钩子结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}