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
            string arg = args.Length > 0 ? args[0].ToLower().Trim() : "";

            if (arg.StartsWith("/s"))  // 屏保模式
            {
                Application.Run(new MainForm());
            }
            else if (arg.StartsWith("/p"))  // 预览模式
            {
                MessageBox.Show("暂不支持小窗预览。点击预览按钮进入全屏预览。", "WebScreenSaver");
            }
            else
            {
                // /c、无参数或未知参数 → 打开主配置页
                Application.Run(new ConfigForm());
            }
        }
    }
}
