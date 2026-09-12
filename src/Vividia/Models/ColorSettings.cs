using System.Text.Json.Serialization;

namespace Vividia.Models;

public sealed class ColorSettings
{
    public int Brightness { get; set; } = 50;
    public int Contrast { get; set; } = 50;
    public double Gamma { get; set; } = 1.0;
    public int Vibrance { get; set; } = 50;

    public static ColorSettings Neutral => new();

    public ColorSettings Clone() => new()
    {
        Brightness = Brightness,
        Contrast = Contrast,
        Gamma = Gamma,
        Vibrance = Vibrance,
    };

    [JsonIgnore]
    public bool IsNeutral =>
        Brightness == 50 && Contrast == 50 && Vibrance == 50 && Math.Abs(Gamma - 1.0) < 0.001;

    public void Clamp()
    {
        Brightness = Math.Clamp(Brightness, 0, 100);
        Contrast = Math.Clamp(Contrast, 0, 100);
        Vibrance = Math.Clamp(Vibrance, 0, 100);
        Gamma = Math.Clamp(Gamma, 0.30, 2.80);
    }
}
