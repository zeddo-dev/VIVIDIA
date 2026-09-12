using NvAPIWrapper;
using NvAPIWrapper.Display;

namespace Vividia.Display;

public readonly record struct SaturationRange(int Minimum, int Maximum, int Default, int Current);

public static class VibranceApi
{
    private static bool _initialized;

    public static bool Available { get; private set; }

    public static string? LastError { get; private set; }

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;
        try
        {
            NVIDIA.Initialize();
            Available = true;
        }
        catch (Exception ex)
        {
            Available = false;
            LastError = ex.Message;
        }
    }

    private static NvAPIWrapper.Display.Display? FindDisplay(string deviceName)
    {
        if (!Available)
            return null;

        try
        {
            return NvAPIWrapper.Display.Display.GetDisplays()
                .FirstOrDefault(d => string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public static SaturationRange? GetRange(string deviceName)
    {
        var display = FindDisplay(deviceName);
        if (display == null)
            return null;

        try
        {
            var dvc = display.DigitalVibranceControl;
            return new SaturationRange(dvc.MinimumLevel, dvc.MaximumLevel, dvc.DefaultLevel, dvc.CurrentLevel);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public static bool SetLevel(string deviceName, int level)
    {
        var display = FindDisplay(deviceName);
        if (display == null)
            return false;

        try
        {
            var dvc = display.DigitalVibranceControl;
            dvc.CurrentLevel = Math.Clamp(level, dvc.MinimumLevel, dvc.MaximumLevel);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }
}
