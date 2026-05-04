using System.Text.Json;

namespace WebClockScreensaver;

internal static class ConfigManager
{
    private static readonly string ConfigPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

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
}
