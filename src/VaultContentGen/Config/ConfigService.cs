using System.Text.Json;

namespace VaultContentGen.Config;

public class ConfigService(string configPath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static ConfigService CreateDefault() =>
        new(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VaultContentGen", "config.json"));

    public AppConfig Load()
    {
        if (!File.Exists(configPath))
            return new AppConfig();

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, JsonOptions));
    }
}