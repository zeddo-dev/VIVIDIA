namespace Vividia.Display;

public static class SaturationApi
{
    public static void EnsureInitialized()
    {
        VibranceApi.EnsureInitialized();
        AdlApi.EnsureInitialized();
    }

    public static bool Available
    {
        get
        {
            EnsureInitialized();
            return VibranceApi.Available || AdlApi.Available;
        }
    }

    public static string ProviderName
    {
        get
        {
            EnsureInitialized();
            if (VibranceApi.Available && AdlApi.Available)
                return "NVAPI + ADL";

            return VibranceApi.Available ? "NVAPI" : AdlApi.Available ? "ADL" : "none";
        }
    }

    public static string? LastError => VibranceApi.LastError ?? AdlApi.LastError;

    public static SaturationRange? GetRange(string deviceName)
    {
        EnsureInitialized();
        return VibranceApi.GetRange(deviceName) ?? AdlApi.GetRange(deviceName);
    }

    public static bool SetLevel(string deviceName, int level)
    {
        EnsureInitialized();

        if (VibranceApi.GetRange(deviceName) != null)
            return VibranceApi.SetLevel(deviceName, level);

        return AdlApi.SetLevel(deviceName, level);
    }

    public static int UiToLevel(int ui, SaturationRange range)
    {
        ui = Math.Clamp(ui, 0, 100);
        return ui <= 50
            ? (int)Math.Round(range.Minimum + (ui / 50.0) * (range.Default - range.Minimum))
            : (int)Math.Round(range.Default + ((ui - 50) / 50.0) * (range.Maximum - range.Default));
    }

    public static int LevelToUi(int level, SaturationRange range)
    {
        if (level <= range.Default)
        {
            int span = range.Default - range.Minimum;
            return span == 0 ? 50 : (int)Math.Round((level - range.Minimum) * 50.0 / span);
        }

        int upper = range.Maximum - range.Default;
        return upper == 0 ? 50 : 50 + (int)Math.Round((level - range.Default) * 50.0 / upper);
    }
}
