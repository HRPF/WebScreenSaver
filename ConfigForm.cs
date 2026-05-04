using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WebClockScreensaver;

public class ConfigForm : Form
{
    private WebView2 webView = null!;
    private bool initialized = false;

    public ConfigForm()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private void InitializeComponent()
    {
        Text = "屏保配置";
        Size = new System.Drawing.Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new System.Drawing.Size(900, 600);
        BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
        };
    }

    private async void InitializeWebView()
    {
        try
        {
            webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(webView);

            var env = await CoreWebView2Environment.CreateAsync();
            await webView.EnsureCoreWebView2Async(env);

            string webFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "screensaver.local", webFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            webView.CoreWebView2.Navigate("https://screensaver.local/index.html");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化WebView2失败: {ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!initialized && e.IsSuccess)
        {
            initialized = true;
            SendInitData();
        }
    }

    private void SendInitData()
    {
        var screensavers = ConfigManager.DiscoverScreensavers();
        var current = ConfigManager.GetSelectedScreensaver();

        var data = new { type = "init", screensavers, currentSelection = current };
        string json = System.Text.Json.JsonSerializer.Serialize(data);
        webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string json = e.TryGetWebMessageAsString();
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp)) return;
            string type = typeProp.GetString() ?? "";

            switch (type)
            {
                case "selectScreensaver":
                    HandleSelectScreensaver(root);
                    break;
                case "navigateToConfig":
                    HandleNavigateToConfig(root);
                    break;
                case "closeConfig":
                    Close();
                    break;
            }
        }
        catch
        {
            // 忽略格式异常的消息
        }
    }

    private void HandleSelectScreensaver(System.Text.Json.JsonElement root)
    {
        if (root.TryGetProperty("id", out var idProp))
        {
            string id = idProp.GetString() ?? "";
            if (!string.IsNullOrEmpty(id))
                ConfigManager.SetSelectedScreensaver(id);
        }
    }

    private void HandleNavigateToConfig(System.Text.Json.JsonElement root)
    {
        if (root.TryGetProperty("id", out var idProp))
        {
            string id = idProp.GetString() ?? "";
            if (!string.IsNullOrEmpty(id))
            {
                string configPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "web", id, "config.html");
                if (File.Exists(configPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(configPath)
                    {
                        UseShellExecute = true
                    });
                }
                else
                {
                    var msg = new { type = "configNotFound", screensaverId = id };
                    string json = System.Text.Json.JsonSerializer.Serialize(msg);
                    webView.CoreWebView2.PostWebMessageAsJson(json);
                }
            }
        }
    }
}
