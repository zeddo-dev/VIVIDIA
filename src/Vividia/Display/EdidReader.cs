using Microsoft.Win32;

namespace Vividia.Display;

public static class EdidReader
{
    public static string? GetMonitorName(string deviceInterfacePath)
    {
        var (hardwareId, instanceId) = ParseInterfacePath(deviceInterfacePath);
        if (hardwareId == null || instanceId == null)
            return null;

        var edid = ReadEdid(hardwareId, instanceId);
        return edid == null ? null : ParseName(edid);
    }

    public static string? GetHardwareId(string deviceInterfacePath) => ParseInterfacePath(deviceInterfacePath).HardwareId;

    private static (string? HardwareId, string? InstanceId) ParseInterfacePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (null, null);

        var parts = path.TrimStart('\\', '?', '.').Split('#');
        if (parts.Length < 3 || !parts[0].Equals("DISPLAY", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        return (parts[1], parts[2]);
    }

    private static byte[]? ReadEdid(string hardwareId, string instanceId)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Enum\DISPLAY\{hardwareId}\{instanceId}\Device Parameters");
            return key?.GetValue("EDID") as byte[];
        }
        catch
        {
            return null;
        }
    }

    public static string? ParseName(byte[] edid)
    {
        if (edid.Length < 128)
            return null;

        for (int offset = 54; offset + 18 <= 128; offset += 18)
        {
            if (edid[offset] != 0 || edid[offset + 1] != 0 || edid[offset + 2] != 0)
                continue;
            if (edid[offset + 3] != 0xFC)
                continue;

            var chars = new List<char>();
            for (int i = offset + 5; i < offset + 18; i++)
            {
                byte b = edid[i];
                if (b == 0x0A)
                    break;
                chars.Add((char)b);
            }

            string name = new string(chars.ToArray()).Trim();
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }

        return null;
    }

    public static string DescribeVendor(string hardwareId)
    {
        if (hardwareId.Length < 3)
            return hardwareId;

        string code = hardwareId[..3].ToUpperInvariant();
        return Vendors.TryGetValue(code, out var vendor) ? vendor : code;
    }

    private static readonly Dictionary<string, string> Vendors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SAM"] = "Samsung",
        ["SAC"] = "Samsung",
        ["GSM"] = "LG",
        ["LGD"] = "LG",
        ["DEL"] = "Dell",
        ["AUS"] = "ASUS",
        ["ACI"] = "ASUS",
        ["ACR"] = "Acer",
        ["BNQ"] = "BenQ",
        ["AOC"] = "AOC",
        ["MSI"] = "MSI",
        ["GBT"] = "Gigabyte",
        ["PHL"] = "Philips",
        ["VSC"] = "ViewSonic",
        ["HWP"] = "HP",
        ["HPN"] = "HP",
        ["LEN"] = "Lenovo",
        ["IVM"] = "iiyama",
        ["NVD"] = "NVIDIA",
    };
}
