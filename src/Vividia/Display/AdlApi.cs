using System.Runtime.InteropServices;

namespace Vividia.Display;

public static class AdlApi
{
    private const int ADL_OK = 0;
    private const int ADL_MAX_PATH = 256;
    private const int ADL_DISPLAY_COLOR_SATURATION = 1 << 2;
    private const int ADL_DISPLAY_DISPLAYINFO_DISPLAYCONNECTED = 0x00000001;
    private const int ADL_DISPLAY_DISPLAYINFO_DISPLAYMAPPED = 0x00000002;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct AdapterInfo
    {
        public int Size;
        public int AdapterIndex;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)] public string Udid;
        public int BusNumber;
        public int DeviceNumber;
        public int FunctionNumber;
        public int VendorId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)] public string AdapterName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)] public string DisplayName;
        public int Present;
        public int Exist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)] public string DriverPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)] public string DriverPathExt;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)] public string PnpString;
        public int OsDisplayIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ADLDisplayID
    {
        public int DisplayLogicalIndex;
        public int DisplayPhysicalIndex;
        public int DisplayLogicalAdapterIndex;
        public int DisplayPhysicalAdapterIndex;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct ADLDisplayInfo
    {
        public ADLDisplayID DisplayID;
        public int DisplayControllerIndex;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)] public string DisplayManufacturerName;
        public int DisplayType;
        public int DisplayOutputType;
        public int DisplayConnector;
        public int DisplayInfoMask;
        public int DisplayInfoValue;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr AdlMainMemoryAlloc(int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MainControlCreate(AdlMainMemoryAlloc callback, int enumConnectedAdapters, out IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MainControlDestroy(IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AdapterNumberOfAdaptersGet(IntPtr context, out int count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AdapterAdapterInfoGet(IntPtr context, IntPtr info, int inputSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DisplayDisplayInfoGet(IntPtr context, int adapterIndex, out int numDisplays, out IntPtr info, int forceDetect);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DisplayColorGet(IntPtr context, int adapterIndex, int displayIndex, int colorType,
        out int current, out int defaultValue, out int min, out int max, out int step);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DisplayColorSet(IntPtr context, int adapterIndex, int displayIndex, int colorType, int current);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string name);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr module, string name);

    private static readonly AdlMainMemoryAlloc Allocator = Marshal.AllocCoTaskMem;

    private static IntPtr _library;
    private static IntPtr _context;
    private static bool _initialised;

    private static MainControlCreate? _create;
    private static MainControlDestroy? _destroy;
    private static AdapterNumberOfAdaptersGet? _adapterCount;
    private static AdapterAdapterInfoGet? _adapterInfo;
    private static DisplayDisplayInfoGet? _displayInfo;
    private static DisplayColorGet? _colorGet;
    private static DisplayColorSet? _colorSet;

    public static bool Available { get; private set; }

    public static string? LastError { get; private set; }

    public static void EnsureInitialized()
    {
        if (_initialised)
            return;

        _initialised = true;

        try
        {
            _library = LoadLibrary("atiadlxx.dll");
            if (_library == IntPtr.Zero)
                _library = LoadLibrary("atiadlxy.dll");

            if (_library == IntPtr.Zero)
            {
                LastError = "atiadlxx.dll not found (no AMD driver)";
                return;
            }

            _create = Resolve<MainControlCreate>("ADL2_Main_Control_Create");
            _destroy = Resolve<MainControlDestroy>("ADL2_Main_Control_Destroy");
            _adapterCount = Resolve<AdapterNumberOfAdaptersGet>("ADL2_Adapter_NumberOfAdapters_Get");
            _adapterInfo = Resolve<AdapterAdapterInfoGet>("ADL2_Adapter_AdapterInfo_Get");
            _displayInfo = Resolve<DisplayDisplayInfoGet>("ADL2_Display_DisplayInfo_Get");
            _colorGet = Resolve<DisplayColorGet>("ADL2_Display_Color_Get");
            _colorSet = Resolve<DisplayColorSet>("ADL2_Display_Color_Set");

            if (_create == null || _adapterCount == null || _adapterInfo == null
                || _displayInfo == null || _colorGet == null || _colorSet == null)
            {
                LastError = "ADL entry points missing";
                return;
            }

            int status = _create(Allocator, 1, out _context);
            if (status != ADL_OK || _context == IntPtr.Zero)
            {
                LastError = $"ADL2_Main_Control_Create returned {status}";
                return;
            }

            Available = true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Available = false;
        }
    }

    private static T? Resolve<T>(string name) where T : Delegate
    {
        IntPtr address = GetProcAddress(_library, name);
        return address == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    public static SaturationRange? GetRange(string deviceName)
    {
        if (!TryResolveDisplay(deviceName, out int adapterIndex, out int displayIndex))
            return null;

        try
        {
            int status = _colorGet!(_context, adapterIndex, displayIndex, ADL_DISPLAY_COLOR_SATURATION,
                out int current, out int defaultValue, out int min, out int max, out _);

            if (status != ADL_OK)
            {
                LastError = $"ADL2_Display_Color_Get returned {status}";
                return null;
            }

            return new SaturationRange(min, max, defaultValue, current);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public static bool SetLevel(string deviceName, int level)
    {
        if (!TryResolveDisplay(deviceName, out int adapterIndex, out int displayIndex))
            return false;

        try
        {
            int status = _colorSet!(_context, adapterIndex, displayIndex, ADL_DISPLAY_COLOR_SATURATION, level);
            if (status == ADL_OK)
                return true;

            LastError = $"ADL2_Display_Color_Set returned {status}";
            return false;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    private static bool TryResolveDisplay(string deviceName, out int adapterIndex, out int displayIndex)
    {
        adapterIndex = -1;
        displayIndex = -1;

        EnsureInitialized();
        if (!Available)
            return false;

        try
        {
            if (_adapterCount!(_context, out int count) != ADL_OK || count <= 0)
                return false;

            int structSize = Marshal.SizeOf<AdapterInfo>();
            IntPtr buffer = Marshal.AllocHGlobal(structSize * count);

            try
            {
                if (_adapterInfo!(_context, buffer, structSize * count) != ADL_OK)
                    return false;

                for (int i = 0; i < count; i++)
                {
                    var adapter = Marshal.PtrToStructure<AdapterInfo>(buffer + i * structSize);
                    if (adapter.Present == 0)
                        continue;

                    if (!string.Equals(adapter.DisplayName, deviceName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (TryFindDisplay(adapter.AdapterIndex, out displayIndex))
                    {
                        adapterIndex = adapter.AdapterIndex;
                        return true;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }

        return false;
    }

    private static bool TryFindDisplay(int adapterIndex, out int displayIndex)
    {
        displayIndex = -1;

        if (_displayInfo!(_context, adapterIndex, out int numDisplays, out IntPtr info, 0) != ADL_OK
            || info == IntPtr.Zero || numDisplays <= 0)
        {
            return false;
        }

        try
        {
            int structSize = Marshal.SizeOf<ADLDisplayInfo>();
            const int connectedAndMapped = ADL_DISPLAY_DISPLAYINFO_DISPLAYCONNECTED | ADL_DISPLAY_DISPLAYINFO_DISPLAYMAPPED;

            for (int i = 0; i < numDisplays; i++)
            {
                var display = Marshal.PtrToStructure<ADLDisplayInfo>(info + i * structSize);
                if ((display.DisplayInfoValue & connectedAndMapped) != connectedAndMapped)
                    continue;

                if (display.DisplayID.DisplayLogicalAdapterIndex != adapterIndex)
                    continue;

                displayIndex = display.DisplayID.DisplayLogicalIndex;
                return true;
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(info);
        }

        return false;
    }

    public static List<string> DescribeAdapters()
    {
        var result = new List<string>();

        EnsureInitialized();
        if (!Available)
            return result;

        try
        {
            if (_adapterCount!(_context, out int count) != ADL_OK || count <= 0)
                return result;

            int structSize = Marshal.SizeOf<AdapterInfo>();
            IntPtr buffer = Marshal.AllocHGlobal(structSize * count);

            try
            {
                if (_adapterInfo!(_context, buffer, structSize * count) != ADL_OK)
                    return result;

                for (int i = 0; i < count; i++)
                {
                    var adapter = Marshal.PtrToStructure<AdapterInfo>(buffer + i * structSize);
                    if (adapter.Present == 0)
                        continue;

                    result.Add($"index {adapter.AdapterIndex}: {adapter.AdapterName.Trim()} -> {adapter.DisplayName.Trim()}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }

        return result;
    }

    public static void Shutdown()
    {
        if (_context != IntPtr.Zero && _destroy != null)
        {
            _destroy(_context);
            _context = IntPtr.Zero;
        }

        Available = false;
    }
}
