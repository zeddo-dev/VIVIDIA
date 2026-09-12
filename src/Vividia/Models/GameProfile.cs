namespace Vividia.Models;

public sealed class GameProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "New profile";

    public string ProcessName { get; set; } = "";

    public string? ExecutablePath { get; set; }

    public bool Enabled { get; set; } = true;

    public ColorSettings Settings { get; set; } = ColorSettings.Neutral;

    public bool AllDisplays { get; set; } = true;

    public List<string> DisplayKeys { get; set; } = new();

    public bool Matches(string processName) =>
        Enabled
        && !string.IsNullOrWhiteSpace(ProcessName)
        && string.Equals(
            NormalizeProcessName(ProcessName),
            NormalizeProcessName(processName),
            StringComparison.OrdinalIgnoreCase);

    public static string NormalizeProcessName(string value)
    {
        var name = value.Trim();
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];
        return name;
    }
}
