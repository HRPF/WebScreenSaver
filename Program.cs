using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebClockScreensaver
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 处理命令行参数
            if (args.Length > 0)
            {
                string arg = args[0].ToLower().Trim();

                if (arg.StartsWith("/c"))  // 配置模式
                {
                    // 打开配置页面（config.html）
                    string configPath = Path.Combine(Application.StartupPath, "web", "config.html");
                    if (File.Exists(configPath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(configPath) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("这个屏幕保护程序没有可以设置的选项",
                                       "屏幕保护程序设置",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Information);
                    }
                }
                else if (arg.StartsWith("/s"))  // 屏保模式
                {
                    Application.Run(new MainForm());
                }
                else if (arg.StartsWith("/p"))  // 预览模式
                {
                    // 预览模式需要父窗口句柄，这里简化处理
                    MessageBox.Show("暂不支持小窗预览。点击预览按钮进入全屏预览。");
                }
                else
                {
                    // TODO 无参数时也进入配置页面
                    string webPath = Path.Combine(
                        Application.StartupPath, "web", "index.html");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(webPath) { UseShellExecute = true });
                }
            }
            else
            {
                // 无参数时显示配置
                string webPath = Path.Combine(
                    Application.StartupPath, "web", "index.html");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(webPath) { UseShellExecute = true });
            }
            //Application.Run(new MainForm());
        }
    }
}
