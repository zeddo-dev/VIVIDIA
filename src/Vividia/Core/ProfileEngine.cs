using Vividia.Display;
using Vividia.Models;
using Vividia.Storage;
using Vividia.Watch;

namespace Vividia.Core;

public sealed class ProfileAppliedEventArgs : EventArgs
{
    public GameProfile? Profile { get; init; }
    public string ForegroundProcess { get; init; } = "";
}

public sealed class ProfileEngine : IDisposable
{
    private readonly ColorController _controller = new();
    private readonly ForegroundWatcher _watcher = new();
    private readonly AppState _state;

    private AppConfig _config;
    private GameProfile? _activeProfile;
    private bool _paused;
    private bool _previewing;

    public event EventHandler<ProfileAppliedEventArgs>? StateChanged;

    public GameProfile? ActiveProfile => _activeProfile;

    public bool PreviewActive => _previewing;

    public bool LastApplyRejected { get; private set; }

    public bool LastValuesConstrained => _controller.LastValuesConstrained;
    public string LastForegroundProcess { get; private set; } = "";

    public bool Paused
    {
        get => _paused;
        set
        {
            if (_paused == value)
                return;

            _paused = value;
            if (_paused)
                RestoreBaseline();
            else
                _watcher.Refresh();
        }
    }

    public ProfileEngine(AppConfig config)
    {
        _config = config;
        _state = AppState.Load();
        _watcher.ForegroundChanged += OnForegroundChanged;
    }

    public void Start()
    {
        SaturationApi.EnsureInitialized();

        if (_state.ProfileApplied && _state.Baselines.Count > 0)
            RestoreBaseline();

        _watcher.Start();
    }

    public void UpdateConfig(AppConfig config, bool applyNow = true)
    {
        _config = config;

        if (!applyNow)
            return;

        if (_activeProfile != null && (_previewing || _activeProfile.Matches(LastForegroundProcess)))
        {
            var profile = _activeProfile;
            bool previewing = _previewing;
            RestoreBaseline();
            ApplyProfile(profile);
            _previewing = previewing;
            RaiseStateChanged();
            return;
        }

        _watcher.Refresh();
    }

    internal void ApplyProfileForTest(GameProfile profile, string foregroundProcess)
    {
        LastForegroundProcess = foregroundProcess;
        ApplyProfile(profile);
        RaiseStateChanged();
    }

    private void OnForegroundChanged(object? sender, ForegroundChangedEventArgs e)
    {
        bool processChanged = !string.Equals(LastForegroundProcess, e.ProcessName, StringComparison.OrdinalIgnoreCase);
        LastForegroundProcess = e.ProcessName;

        if (_previewing && !processChanged)
            return;

        _previewing = false;

        if (_paused)
            return;

        var profile = string.IsNullOrEmpty(e.ProcessName)
            ? null
            : _config.Profiles.FirstOrDefault(p => p.Matches(e.ProcessName));

        if (profile == null)
        {
            RestoreBaseline();
        }
        else if (!ReferenceEquals(profile, _activeProfile))
        {
            RestoreBaseline();
            ApplyProfile(profile);
        }

        StateChanged?.Invoke(this, new ProfileAppliedEventArgs
        {
            Profile = _activeProfile,
            ForegroundProcess = e.ProcessName,
        });
    }

    public void PreviewProfile(GameProfile profile)
    {
        RestoreBaseline();
        ApplyProfile(profile);
        _previewing = true;
        RaiseStateChanged();
    }

    public void StopPreview()
    {
        if (!_previewing)
            return;

        _previewing = false;
        RestoreBaseline();
        _watcher.Refresh();
        RaiseStateChanged();
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, new ProfileAppliedEventArgs
    {
        Profile = _activeProfile,
        ForegroundProcess = LastForegroundProcess,
    });

    private void ApplyProfile(GameProfile profile)
    {
        var displays = DisplayEnumerator.GetActiveDisplays();
        var targets = ResolveTargets(profile, displays);
        if (targets.Count == 0)
            return;

        _state.Baselines = displays.Select(_controller.Capture).ToList();
        _state.ProfileApplied = true;
        _state.ActiveProfileId = profile.Id;
        _state.Save();

        LastApplyRejected = !_controller.Apply(profile.Settings, targets);
        _activeProfile = profile;
    }

    private void RestoreBaseline()
    {
        if (_state.Baselines.Count == 0)
        {
            _activeProfile = null;
            return;
        }

        foreach (var baseline in _state.Baselines)
            _controller.Restore(baseline);

        _state.Baselines.Clear();
        _state.ProfileApplied = false;
        _state.ActiveProfileId = null;
        _state.Save();
        _activeProfile = null;
        LastApplyRejected = false;
    }

    public static List<DisplayTarget> ResolveTargets(GameProfile profile, List<DisplayTarget> displays)
    {
        if (profile.AllDisplays)
            return displays;

        return displays.Where(d => profile.DisplayKeys.Contains(d.Key)).ToList();
    }

    public void Dispose()
    {
        RestoreBaseline();
        _watcher.Dispose();
    }
}
