using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WebClockScreensaver;

public class ConfigForm : Form
{
    private WebView2 webView = null!;
    private string webFolder = null!;
    private Button btnBack = null!;
    private Panel toolbar = null!;

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
            if (e.KeyCode == Keys.Escape) HandleBack();
        };

        // 顶部工具栏
        toolbar = new Panel
        {
            Height = 40,
            Dock = DockStyle.Top,
            BackColor = System.Drawing.Color.FromArgb(20, 20, 40),
        };

        btnBack = new Button
        {
            Text = "← 返回",
            FlatStyle = FlatStyle.Flat,
            ForeColor = System.Drawing.Color.White,
            BackColor = System.Drawing.Color.Transparent,
            FlatAppearance = { BorderSize = 0 },
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Size = new System.Drawing.Size(80, 40),
            Location = new System.Drawing.Point(4, 0),
            Cursor = Cursors.Hand,
        };
        btnBack.Click += (_, _) => HandleBack();

        var lblTitle = new Label
        {
            Text = "屏保配置",
            ForeColor = System.Drawing.Color.White,
            BackColor = System.Drawing.Color.Transparent,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new System.Drawing.Font("Microsoft YaHei UI", 10),
        };

        toolbar.Controls.Add(btnBack);
        toolbar.Controls.Add(lblTitle);

        Controls.Add(toolbar);
    }

    private void HandleBack()
    {
        var url = webView.CoreWebView2?.Source;
        if (url != null && url != "https://screensaver.local/index.html")
        {
            webView.CoreWebView2.Navigate("https://screensaver.local/index.html");
        }
        else
        {
            Close();
        }
    }

    private async void InitializeWebView()
    {
        try
        {
            webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(webView);

            var env = await CoreWebView2Environment.CreateAsync();
            await webView.EnsureCoreWebView2Async(env);

            webFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");

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
        if (!e.IsSuccess) return;
        var url = webView.CoreWebView2.Source;

        if (url == "https://screensaver.local/index.html")
        {
            btnBack.Visible = false;
            SendInitData();
        }
        else if (url.EndsWith("/config.html"))
        {
            btnBack.Visible = true;
            InjectSettingsToPage(url);
        }
    }

    private void SendInitData()
    {
        var screensavers = ConfigManager.DiscoverScreensavers();
        var current = ConfigManager.GetSelectedScreensaver();
        var settings = ConfigManager.GetSettings(current);

        var data = new { type = "init", screensavers, currentSelection = current, settings };
        string json = System.Text.Json.JsonSerializer.Serialize(data);
        webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    private void InjectSettingsToPage(string url)
    {
        // 从 URL 提取屏保 ID: "https://screensaver.local/Clock/config.html" → "Clock"
        string prefix = "https://screensaver.local/";
        string suffix = "/config.html";
        if (!url.StartsWith(prefix) || !url.EndsWith(suffix)) return;
        string id = url.Substring(prefix.Length, url.Length - prefix.Length - suffix.Length);

        var settings = ConfigManager.GetSettings(id);
        if (settings.HasValue)
        {
            var msg = new { type = "loadSettings", settings = settings.Value };
            string json = System.Text.Json.JsonSerializer.Serialize(msg);
            webView.CoreWebView2.PostWebMessageAsJson(json);
        }
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
                case "saveSettings":
                    HandleSaveSettings(root);
                    break;
                case "requestSettings":
                    HandleRequestSettings(root);
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
                string configPath = Path.Combine(webFolder, id, "config.html");
                if (File.Exists(configPath))
                {
                    webView.CoreWebView2.Navigate($"https://screensaver.local/{id}/config.html");
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

    private void HandleRequestSettings(System.Text.Json.JsonElement root)
    {
        if (root.TryGetProperty("id", out var idProp))
        {
            string id = idProp.GetString() ?? "";
            if (!string.IsNullOrEmpty(id))
            {
                var settings = ConfigManager.GetSettings(id);
                if (settings.HasValue)
                {
                    var msg = new { type = "loadSettings", settings = settings.Value };
                    string json = System.Text.Json.JsonSerializer.Serialize(msg);
                    webView.CoreWebView2.PostWebMessageAsJson(json);
                }
            }
        }
    }

    private void HandleSaveSettings(System.Text.Json.JsonElement root)
    {
        if (root.TryGetProperty("id", out var idProp) &&
            root.TryGetProperty("settings", out var settingsProp))
        {
            string id = idProp.GetString() ?? "";
            if (!string.IsNullOrEmpty(id))
            {
                ConfigManager.SaveSettings(id, settingsProp);
            }
        }
    }
}
