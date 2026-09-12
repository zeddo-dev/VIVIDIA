using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Vividia.Display;

public static class GammaRampApi
{
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateDC(string? lpszDriver, string lpszDevice, string? lpszOutput, IntPtr lpInitData);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool SetDeviceGammaRamp(IntPtr hdc, ushort[] lpRamp);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool GetDeviceGammaRamp(IntPtr hdc, ushort[] lpRamp);

    public static GammaRamp? Read(string deviceName)
    {
        IntPtr hdc = CreateDC(null, deviceName, null, IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            return null;

        try
        {
            var flat = new ushort[768];
            return GetDeviceGammaRamp(hdc, flat) ? GammaRamp.FromFlatArray(flat) : null;
        }
        finally
        {
            DeleteDC(hdc);
        }
    }

    public static bool Write(string deviceName, GammaRamp ramp)
    {
        IntPtr hdc = CreateDC(null, deviceName, null, IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            return false;

        try
        {
            return SetDeviceGammaRamp(hdc, ramp.ToFlatArray());
        }
        finally
        {
            DeleteDC(hdc);
        }
    }

    public static bool IsGammaRangeUnlocked()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ICM");
            return key?.GetValue("GdiIcmGammaRange") is int value && value >= 256;
        }
        catch
        {
            return false;
        }
    }
}
