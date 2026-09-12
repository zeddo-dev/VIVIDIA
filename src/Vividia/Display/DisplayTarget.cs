namespace Vividia.Display;

public sealed class DisplayTarget
{
    public required string DeviceName { get; init; }

    public required string Key { get; init; }

    public required string MonitorName { get; init; }

    public Rectangle Bounds { get; init; }

    public int RefreshRate { get; init; }

    public bool IsPrimary { get; init; }

    public int Number { get; init; }

    public string ShortLabel => $"{Number}. {MonitorName}";

    public string Label
    {
        get
        {
            string resolution = Bounds.Width > 0 ? $"{Bounds.Width}×{Bounds.Height}" : "";
            string rate = RefreshRate > 0 ? $" @ {RefreshRate} Hz" : "";
            string primary = IsPrimary ? "  (primary)" : "";
            return $"{Number}. {MonitorName}  {resolution}{rate}{primary}";
        }
    }

    public override string ToString() => Label;
}
