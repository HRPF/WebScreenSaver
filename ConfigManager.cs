using System.Text.Json;

namespace WebClockScreensaver;

internal static class ConfigManager
{
    private static readonly string ConfigPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private static readonly string SettingsDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings");

    public static string GetSelectedScreensaver()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return "Clock";

            var json = File.ReadAllText(ConfigPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("selectedScreensaver", out var prop))
                return prop.GetString() ?? "Clock";

            return "Clock";
        }
        catch
        {
            return "Clock";
        }
    }

    public static void SetSelectedScreensaver(string id)
    {
        var json = JsonSerializer.Serialize(new { selectedScreensaver = id });
        File.WriteAllText(ConfigPath, json);
    }

    public static List<string> DiscoverScreensavers()
    {
        string webFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");
        if (!Directory.Exists(webFolder))
            return new List<string>();

        return Directory.GetDirectories(webFolder)
            .Select(Path.GetFileName)
            .Where(name => File.Exists(Path.Combine(webFolder, name!, "index.html")))
            .Cast<string>()
            .ToList();
    }

    public static JsonElement? GetSettings(string screensaverId)
    {
        try
        {
            string path = Path.Combine(SettingsDir, $"{screensaverId}.json");
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void SaveSettings(string screensaverId, object settings)
    {
        Directory.CreateDirectory(SettingsDir);
        string path = Path.Combine(SettingsDir, $"{screensaverId}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(settings));
    }
}
