using System.Runtime.InteropServices;

namespace Vividia.Display;

public static class DisplayEnumerator
{
    private const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
    private const int DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;
    private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;
    private const int ENUM_CURRENT_SETTINGS = -1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    public static List<DisplayTarget> GetActiveDisplays()
    {
        var result = new List<DisplayTarget>();
        int number = 0;

        for (uint adapterIndex = 0; ; adapterIndex++)
        {
            var adapter = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
            if (!EnumDisplayDevices(null, adapterIndex, ref adapter, 0))
                break;

            if ((adapter.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0)
                continue;

            number++;
            result.Add(BuildTarget(adapter, number));
        }

        return result;
    }

    private static DisplayTarget BuildTarget(DISPLAY_DEVICE adapter, int number)
    {
        string key = adapter.DeviceName;
        string? edidName = null;
        string? hardwareId = null;

        var monitor = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
        if (EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0) && !string.IsNullOrWhiteSpace(monitor.DeviceID))
            key = monitor.DeviceID;

        var monitorInterface = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
        if (EnumDisplayDevices(adapter.DeviceName, 0, ref monitorInterface, EDD_GET_DEVICE_INTERFACE_NAME))
        {
            edidName = EdidReader.GetMonitorName(monitorInterface.DeviceID);
            hardwareId = EdidReader.GetHardwareId(monitorInterface.DeviceID);
        }

        var settings = new DEVMODE { dmSize = (ushort)Marshal.SizeOf<DEVMODE>() };
        var bounds = Rectangle.Empty;
        int refreshRate = 0;
        if (EnumDisplaySettings(adapter.DeviceName, ENUM_CURRENT_SETTINGS, ref settings))
        {
            bounds = new Rectangle(settings.dmPositionX, settings.dmPositionY,
                (int)settings.dmPelsWidth, (int)settings.dmPelsHeight);
            refreshRate = (int)settings.dmDisplayFrequency;
        }

        return new DisplayTarget
        {
            DeviceName = adapter.DeviceName,
            Key = key,
            MonitorName = ResolveName(edidName, hardwareId, monitor.DeviceString, adapter.DeviceString),
            Bounds = bounds,
            RefreshRate = refreshRate,
            IsPrimary = (adapter.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
            Number = number,
        };
    }

    private static string ResolveName(string? edidName, string? hardwareId, string? driverString, string adapterString)
    {
        if (!string.IsNullOrWhiteSpace(edidName))
            return edidName;

        if (!string.IsNullOrWhiteSpace(hardwareId))
        {
            string vendor = EdidReader.DescribeVendor(hardwareId);
            return vendor.Equals(hardwareId, StringComparison.OrdinalIgnoreCase)
                ? hardwareId
                : $"{vendor} {hardwareId}";
        }

        if (!string.IsNullOrWhiteSpace(driverString))
            return driverString;

        return adapterString;
    }
}
