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

        // 鼠标移动检测
        private Point lastMousePos;
        private const int MOUSE_MOVE_THRESHOLD = 100;

        public MainForm()
        {
            InitializeComponent();
            InitializeWebView();

            // 初始化鼠标位置
            lastMousePos = Control.MousePosition;

            // 启用键盘预览
            this.KeyPreview = true;

            // 隐藏鼠标光标
            this.Cursor = Cursors.No;
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

            // 仍然保留窗体级别的事件处理作为备用
            //this.KeyDown += (s, e) => ExitScreensaver();
            //this.MouseMove += MainForm_MouseMove;
            //this.MouseClick += (s, e) => ExitScreensaver();
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

        // 键盘钩子处理函数
        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // 检查是否是功能键（F1-F12）或特殊键
                if (vkCode >= 0x70 && vkCode <= 0x7B) // F1-F12
                {
                    ExitScreensaver();
                    return (IntPtr)1; // 阻止事件传递
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

        // 鼠标钩子处理函数
        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();

                // 处理鼠标点击
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN ||
                    msg == WM_MBUTTONDOWN || msg == WM_MOUSEWHEEL)
                {
                    ExitScreensaver();
                    return (IntPtr)1; // 阻止事件传递
                }

                // 处理鼠标移动
                if (msg == WM_MOUSEMOVE)
                {
                    // 从lParam中提取鼠标位置
                    MSLLHOOKSTRUCT mouseInfo = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                    Point currentPos = new Point(mouseInfo.pt.x, mouseInfo.pt.y);

                    if (!lastMousePos.IsEmpty)
                    {
                        // 计算移动距离
                        int deltaX = Math.Abs(currentPos.X - lastMousePos.X);
                        int deltaY = Math.Abs(currentPos.Y - lastMousePos.Y);

                        if (deltaX > MOUSE_MOVE_THRESHOLD || deltaY > MOUSE_MOVE_THRESHOLD)
                        {
                            //System.Diagnostics.Debug.WriteLine($"MouseHookProc: msg={msg}, deltaPos=({deltaX},{deltaY})");
                            ExitScreensaver();
                            return (IntPtr)1; // 阻止事件传递
                        }
                    }

                    lastMousePos = currentPos;
                }
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

        // 初始化WebView2
        private async void InitializeWebView()
        {
            try
            {
                webView = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView.Dock = DockStyle.Fill;
                webView.AllowExternalDrop = false;
                this.Controls.Add(webView);

                // 禁用WebView2的交互
                //webView.IsTabStop = false;

                // 添加事件处理作为备用
                //webView.MouseMove += MainForm_MouseMove;
                //webView.KeyDown += (s, e) => ExitScreensaver();
                //webView.MouseClick += (s, e) => ExitScreensaver();

                // 创建WebView2环境
                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
                await webView.EnsureCoreWebView2Async(env);

                // 获取web文件夹路径
                string webFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");
                string indexPath = Path.Combine(webFolder, "index.html");

                if (File.Exists(indexPath))
                {
                    webView.Source = new Uri(indexPath);
                }
                else
                {
                    webView.NavigateToString("<html><body style='background:black;color:white;font-size:48px;text-align:center;padding-top:200px'>时钟屏保<br>请创建web/index.html文件</body></html>");
                }

                // 禁用浏览器功能
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
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