using System.Text.Json;
using Vividia.Models;

namespace Vividia.Storage;

public static class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(Paths.ConfigFile))
                return new AppConfig();

            var json = File.ReadAllText(Paths.ConfigFile);
            return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        File.WriteAllText(Paths.ConfigFile, JsonSerializer.Serialize(config, Options));
    }
}
