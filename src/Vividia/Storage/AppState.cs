using System.Text.Json;
using Vividia.Display;

namespace Vividia.Storage;

public sealed class AppState
{
    public bool ProfileApplied { get; set; }
    public string? ActiveProfileId { get; set; }
    public List<DisplayBaseline> Baselines { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppState Load()
    {
        try
        {
            if (!File.Exists(Paths.StateFile))
                return new AppState();

            var json = File.ReadAllText(Paths.StateFile);
            return JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(Paths.StateFile, JsonSerializer.Serialize(this, Options));
        }
        catch
        {
        }
    }
}
