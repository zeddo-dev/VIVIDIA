using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Vividia.Watch;

public sealed class ForegroundChangedEventArgs : EventArgs
{
    public required string ProcessName { get; init; }
    public required int ProcessId { get; init; }
}

public sealed class ForegroundWatcher : IDisposable
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private readonly WinEventProc _callback;
    private readonly System.Windows.Forms.Timer _fallbackTimer;
    private IntPtr _hook;
    private int _lastProcessId = -1;

    public event EventHandler<ForegroundChangedEventArgs>? ForegroundChanged;

    public ForegroundWatcher()
    {
        _callback = OnWinEvent;
        _fallbackTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _fallbackTimer.Tick += (_, _) => Poll(force: false);
    }

    public void Start()
    {
        _hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
            _callback, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        _fallbackTimer.Start();
        Poll(force: true);
    }

    public void Refresh() => Poll(force: true);

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) => Poll(force: false);

    private void Poll(bool force)
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return;

        GetWindowThreadProcessId(hwnd, out uint pid);
        int processId = (int)pid;
        if (!force && processId == _lastProcessId)
            return;

        _lastProcessId = processId;

        string name;
        try
        {
            name = Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            name = "";
        }

        ForegroundChanged?.Invoke(this, new ForegroundChangedEventArgs
        {
            ProcessName = name,
            ProcessId = processId,
        });
    }

    public void Dispose()
    {
        _fallbackTimer.Stop();
        _fallbackTimer.Dispose();
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
