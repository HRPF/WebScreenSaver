using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WebClockScreensaver
{
    public partial class MainForm : Form
    {
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;

        public MainForm()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private void InitializeComponent()
        {
            // 窗体设置
            this.Text = "时钟屏保";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.TopMost = true;

            // 按任意键或移动鼠标退出
            this.KeyDown += (s, e) => Application.Exit();
            this.MouseMove += MainForm_MouseMove;

            // 计时器：5秒后检测鼠标位置（屏保常见做法）
            System.Windows.Forms.Timer mouseTimer = new System.Windows.Forms.Timer();
            mouseTimer.Interval = 5000;
            mouseTimer.Tick += (s, e) => CheckMouseMovement();
            mouseTimer.Start();
        }

        // 鼠标移动处理
        private Point lastMousePos;
        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (lastMousePos.IsEmpty)
            {
                lastMousePos = e.Location;
                return;
            }

            // 如果鼠标移动超过10像素，退出屏保
            if (Math.Abs(e.X - lastMousePos.X) > 10 ||
                Math.Abs(e.Y - lastMousePos.Y) > 10)
            {
                Application.Exit();
            }
        }

        private void CheckMouseMovement()
        {
            // 定期检查鼠标是否移动（备用检测）
            Point currentPos = Control.MousePosition;
            if (!lastMousePos.IsEmpty &&
                (Math.Abs(currentPos.X - lastMousePos.X) > 10 ||
                 Math.Abs(currentPos.Y - lastMousePos.Y) > 10))
            {
                Application.Exit();
            }
            lastMousePos = currentPos;
        }

        // 初始化WebView2
        private async void InitializeWebView()
        {
            try
            {
                webView = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView.Dock = DockStyle.Fill;
                this.Controls.Add(webView);

                // 创建WebView2环境
                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
                await webView.EnsureCoreWebView2Async(env);

                // 获取web文件夹路径
                string webFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");
                string indexPath = Path.Combine(webFolder, "index.html");

                if (File.Exists(indexPath))
                {
                    // 加载本地HTML文件
                    webView.Source = new Uri(indexPath);
                }
                else
                {
                    // 如果文件不存在，显示默认内容
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
    }
}