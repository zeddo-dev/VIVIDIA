namespace Vividia.Models;

public enum AppTheme
{
    System,
    Light,
    Dark,
}

public sealed class AppConfig
{
    public List<GameProfile> Profiles { get; set; } = new();

    public bool AutoStart { get; set; }

    public bool StartMinimized { get; set; }

    public AppTheme Theme { get; set; } = AppTheme.System;
}
