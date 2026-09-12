using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Vividia.Watch;

public static class ProcessPath
{
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    public static string? TryGet(int processId)
    {
        IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (handle != IntPtr.Zero)
        {
            try
            {
                int capacity = 1024;
                var buffer = new StringBuilder(capacity);
                if (QueryFullProcessImageName(handle, 0, buffer, ref capacity))
                    return buffer.ToString();
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        try
        {
            return Process.GetProcessById(processId).MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    public static string? TryGetByName(string processName)
    {
        try
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                var path = TryGet(process.Id);
                if (!string.IsNullOrEmpty(path))
                    return path;
            }
        }
        catch
        {
        }

        return null;
    }
}
