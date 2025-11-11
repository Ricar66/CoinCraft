namespace CoinCraft.Services;

public sealed class ConfigService
{
    public string ConfigPath { get; }
    public ConfigService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDir = Path.Combine(appData, "CoinCraft");
        Directory.CreateDirectory(dataDir);
        ConfigPath = Path.Combine(dataDir, "config.json");
    }

    public void Save(string json) => File.WriteAllText(ConfigPath, json);
    public string LoadOrDefault() => File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath) : "{}";
}
