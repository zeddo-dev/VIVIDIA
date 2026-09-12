using Vividia.Core;
using Vividia.Display;
using Vividia.Models;
using Vividia.Storage;

namespace Vividia.Ui;

public sealed class MainForm : Form
{
    private readonly AppConfig _config;
    private readonly ProfileEngine _engine;

    private readonly Panel _sidebar = new() { Dock = DockStyle.Fill, Tag = "sidebar" };
    private readonly ScrollHost _tilesHost = new() { Dock = DockStyle.Fill, Tag = "sidebar" };
    private readonly FlowLayoutPanel _tiles = new()
    {
        Dock = DockStyle.Top,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Tag = "sidebar",
        Padding = new Padding(0, 12, 0, 0),
    };
    private readonly SettingsTile _settingsTile = new() { Dock = DockStyle.Bottom };

    private readonly Label _headerLabel = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ThemedTextField _nameBox = new();
    private readonly ThemedTextField _processBox = new();
    private readonly ThemedCheckBox _enabledBox = new() { Text = "Profile enabled" };
    private readonly ThemedCheckBox _allDisplaysBox = new() { Text = "Apply to all displays" };
    private readonly FlowLayoutPanel _displayPanel = new()
    {
        Dock = DockStyle.Top,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Tag = "surface",
        Margin = new Padding(0, Spacing.ItemGap, 0, 0),
    };

    private readonly ThemedButton _deleteButton = new("Delete profile");
    private readonly ThemedButton _previewButton = new("Preview", accent: true);
    private readonly EllipsisLabel _statusLabel = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.TopLeft,
        Tag = "subtle",
        Margin = new Padding(0, 0, Spacing.ItemGap, 0),
    };
    private readonly EllipsisLabel _warningLabel = new()
    {
        Dock = DockStyle.Fill,
        Visible = false,
        Tag = "warning",
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0, Spacing.GroupGap, Spacing.ItemGap, 0),
    };
    private readonly ThemedButton _fixGammaButton = new("Unlock range…", warning: true) { Visible = false };
    private readonly ThemedButton _donateButton = new("☕  Buy me a coffee", accent: true);
    private readonly ScrollHost _body = new() { Dock = DockStyle.Fill, Padding = new Padding(Spacing.WindowPadding, 8, 10, 8) };
    private TableLayoutPanel _footer = null!;

    private readonly List<Panel> _sections = new();
    private readonly List<Label> _sectionLabels = new();

    private SettingRow _brightness = null!;
    private SettingRow _contrast = null!;
    private SettingRow _gamma = null!;
    private SettingRow _vibrance = null!;

    private readonly List<ProfileTile> _profileTiles = new();
    private readonly List<ThemedCheckBox> _displayChecks = new();
    private List<DisplayTarget> _displays = new();
    private GameProfile? _current;
    private bool _loading;
    private bool _firstProfilePrompted;

    public MainForm(AppConfig config, ProfileEngine engine)
    {
        _config = config;
        _engine = engine;

        Text = "VIVIDIA";
        Icon = Branding.CreateIcon(32);
        ClientSize = new Size(1000, 720);
        MinimumSize = new Size(900, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        DoubleBuffered = true;

        BuildLayout();
        ReloadDisplays();
        ReloadProfiles();
        ApplyTheme();
        UpdateWarnings();
        UpdateStatus(new ProfileAppliedEventArgs
        {
            Profile = _engine.ActiveProfile,
            ForegroundProcess = _engine.LastForegroundProcess,
        });

        _engine.StateChanged += OnEngineStateChanged;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_COMPOSITED = 0x02000000;
            var parameters = base.CreateParams;
            parameters.ExStyle |= WS_EX_COMPOSITED;
            return parameters;
        }
    }

    private void OnEngineStateChanged(object? sender, ProfileAppliedEventArgs e)
    {
        if (IsHandleCreated)
            BeginInvoke(() => UpdateStatus(e));
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _settingsTile.Click += (_, _) => OpenSettings();
        new ToolTip().SetToolTip(_settingsTile, "Settings");

        _sidebar.Margin = Padding.Empty;
        _tilesHost.ScrollBarVisible = false;
        _tilesHost.Content.Tag = "sidebar";
        _tilesHost.Content.Controls.Add(_tiles);
        _tilesHost.AttachWheel(_tilesHost.Content);

        _sidebar.Controls.Add(_tilesHost);
        _sidebar.Controls.Add(_settingsTile);
        _tilesHost.SizeChanged += (_, _) => UpdateTileWidths();
        _sidebar.ClientSizeChanged += (_, _) => UpdateTileWidths();
        root.Controls.Add(_sidebar, 0, 0);
        root.Controls.Add(BuildContent(), 1, 0);

        Controls.Add(root);
    }

    private Control BuildContent()
    {
        var column = new TableLayoutPanel { ColumnCount = 1, RowCount = 3 };
        column.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        column.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        column.Controls.Add(BuildHeader(), 0, 0);

        BuildSections();
        column.Controls.Add(_body, 0, 1);
        column.Controls.Add(BuildFooter(), 0, 2);

        var host = new Panel { Dock = DockStyle.Fill };
        host.Controls.Add(column);
        host.SizeChanged += (_, _) => CentreColumn(host, column);
        CentreColumn(host, column);

        return host;
    }

    private static void CentreColumn(Control host, Control column)
    {
        const int maximumWidth = 1100;

        int width = Math.Min(host.ClientSize.Width, maximumWidth);
        column.SetBounds((host.ClientSize.Width - width) / 2, 0, width, host.ClientSize.Height);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(Spacing.WindowPadding, 0, Spacing.WindowPadding, 0) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ThemedButton.StandardWidth + 8));

        _headerLabel.Font = new Font("Segoe UI", 15f, FontStyle.Bold);

        _deleteButton.Anchor = AnchorStyles.None;
        _deleteButton.Click += (_, _) => RemoveProfile();

        header.Controls.Add(_headerLabel, 0, 0);
        header.Controls.Add(_deleteButton, 1, 0);
        return header;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(Spacing.WindowPadding, Spacing.ItemGap, Spacing.WindowPadding, 10),
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _fixGammaButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _fixGammaButton.Margin = new Padding(0, Spacing.GroupGap, 0, 0);
        _fixGammaButton.Height = 30;
        _fixGammaButton.Click += (_, _) => UnlockGammaRange();

        _donateButton.Size = new Size(160, 30);
        _donateButton.Margin = new Padding(0, 0, 0, 2);
        _donateButton.Click += (_, _) => Links.Open(Links.Donate, this);
        new ToolTip().SetToolTip(_donateButton, "Support VIVIDIA: " + Links.Donate);

        var donatePanel = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        donatePanel.Controls.Add(new Label
        {
            Text = "Enjoying VIVIDIA?",
            Tag = "subtle",
            AutoSize = false,
            Width = _donateButton.Width,
            Height = 20,
            TextAlign = ContentAlignment.TopCenter,
            Margin = new Padding(0, 0, 0, 4),
        });
        donatePanel.Controls.Add(_donateButton);

        footer.Controls.Add(_statusLabel, 0, 0);
        footer.Controls.Add(donatePanel, 1, 0);
        footer.Controls.Add(_warningLabel, 0, 1);
        footer.Controls.Add(_fixGammaButton, 1, 1);
        _footer = footer;
        return footer;
    }

    private void BuildSections()
    {
        var processRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            Height = ThemedButton.StandardHeight,
            Margin = new Padding(0, 0, 0, Spacing.GroupGap),
            Tag = "surface",
        };
        processRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        processRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ThemedButton.StandardWidth));
        _processBox.Dock = DockStyle.Fill;
        _processBox.Margin = new Padding(0, 0, Spacing.ButtonGap, 0);
        _processBox.TextChanged += (_, _) => OnEditorChanged();
        var pick = new ThemedButton("Pick running…") { Dock = DockStyle.Fill, Margin = new Padding(0) };
        pick.Click += (_, _) => PickProcess();
        processRow.Controls.Add(_processBox, 0, 0);
        processRow.Controls.Add(pick, 1, 0);

        _nameBox.Dock = DockStyle.Top;
        _nameBox.Margin = new Padding(0, 0, 0, Spacing.GroupGap);
        _nameBox.TextChanged += (_, _) => OnEditorChanged();

        _enabledBox.Dock = DockStyle.Top;
        _enabledBox.CheckedChanged += (_, _) => OnEditorChanged();

        var application = Spacing.BuildSection("Application", new Control[]
        {
            Spacing.Caption("Process name (without .exe)"),
            processRow,
            Spacing.Gap(Spacing.GroupGap),
            Spacing.Caption("Profile name"),
            _nameBox,
            Spacing.Gap(Spacing.GroupGap),
            _enabledBox,
        });

        _brightness = new SettingRow("Brightness", 0, 100, 50);
        _contrast = new SettingRow("Contrast", 0, 100, 50);
        _gamma = new SettingRow("Gamma", 30, 280, 100, scale: 100, decimals: 2);
        _vibrance = new SettingRow("Digital vibrance (%)", 0, 100, 50);

        foreach (var row in new[] { _brightness, _contrast, _gamma, _vibrance })
        {
            row.ValueChanged += (_, _) => OnEditorChanged(applyNow: false);
            row.ValueCommitted += (_, _) => OnEditorChanged();
        }

        _previewButton.Click += (_, _) => TogglePreview();
        var neutral = new ThemedButton("Reset to neutral");
        neutral.Click += (_, _) => ResetToNeutral();
        var colorActions = Spacing.ButtonRow(_previewButton, neutral);

        var color = Spacing.BuildSection("Color", new Control[]
        {
            _brightness, _contrast, _gamma, _vibrance,
            Spacing.Gap(Spacing.GroupGap),
            colorActions,
        });

        _allDisplaysBox.Dock = DockStyle.Top;
        _allDisplaysBox.CheckedChanged += (_, _) =>
        {
            UpdateDisplayCheckState();
            OnEditorChanged();
        };

        var identify = new ThemedButton("Identify");
        var refresh = new ThemedButton("Refresh");
        identify.Click += (_, _) => DisplayIdentifier.Show(_displays);
        refresh.Click += (_, _) => { ReloadDisplays(); LoadSelectedProfile(); ApplyTheme(); };
        var displayButtons = Spacing.ButtonRow(identify, refresh);

        var displays = Spacing.BuildSection("Displays", new Control[]
        {
            _allDisplaysBox,
            Spacing.Gap(Spacing.ItemGap),
            _displayPanel,
            Spacing.Gap(Spacing.GroupGap),
            displayButtons,
        });

        _sections.AddRange(new[] { application, color, displays });

        _body.Content.Controls.Add(displays);
        _body.Content.Controls.Add(color);
        _body.Content.Controls.Add(application);
        _body.AttachWheel(_body.Content);

        foreach (var section in _sections)
        {
            if (section.Controls.Count > 0 && section.Controls[0] is Label title)
                _sectionLabels.Add(title);
        }
    }

    private void ReloadDisplays()
    {
        _displays = DisplayEnumerator.GetActiveDisplays();

        _displayPanel.SuspendLayout();
        _displayPanel.Controls.Clear();
        _displayChecks.Clear();

        foreach (var display in _displays)
        {
            var check = new ThemedCheckBox
            {
                Text = display.Label,
                Width = 560,
                Tag = display,
                Margin = new Padding(0, 1, 0, 1),
            };
            check.CheckedChanged += (_, _) => OnEditorChanged();
            _displayChecks.Add(check);
            _displayPanel.Controls.Add(check);
        }

        if (_displays.Count == 0)
        {
            _displayPanel.Controls.Add(new Label
            {
                Text = "No displays detected",
                Tag = "subtle",
                AutoSize = true,
            });
        }

        _displayPanel.ResumeLayout();
    }

    private void UpdateDisplayCheckState()
    {
        foreach (var check in _displayChecks)
            check.Enabled = _current != null && !_allDisplaysBox.Checked;
    }

    private void ReloadProfiles()
    {
        _tiles.SuspendLayout();
        _tiles.Controls.Clear();
        _profileTiles.Clear();

        foreach (var profile in _config.Profiles)
        {
            var tile = CreateTile(profile);
            _profileTiles.Add(tile);
            _tiles.Controls.Add(tile);
        }

        var addTile = new AddTile();
        addTile.Click += (_, _) => AddProfile();
        new ToolTip().SetToolTip(addTile, "Add profile");
        _tiles.Controls.Add(addTile);

        _tiles.ResumeLayout();
        UpdateTileWidths();

        if (_profileTiles.Count > 0)
            SelectProfile(_profileTiles[0].Profile);
        else
            ShowEmptyState();
    }

    private void UpdateTileWidths()
    {
        int width = Math.Max(48, _tilesHost.Content.ClientSize.Width);
        _tiles.Width = width;

        foreach (Control tile in _tiles.Controls)
            tile.Width = width;
    }

    internal bool SidebarScrolls => _tilesHost.Content.Height > _tilesHost.ClientSize.Height;

    internal bool SidebarScrollBarVisible => _tilesHost.ScrollBarVisible;

    internal int SidebarScrollOffset => -_tilesHost.Content.Top;

    internal void ScrollSidebar(int delta) => _tilesHost.ScrollBy(delta);

    internal bool SidebarColorsUniform => ColorsMatch(_sidebar, Theme.Current.Sidebar);

    private static bool ColorsMatch(Control control, Color color)
    {
        if (control.BackColor != color)
            return false;

        foreach (Control child in control.Controls)
        {
            if (!ColorsMatch(child, color))
                return false;
        }

        return true;
    }

    internal bool SettingsTileIsClear => _tilesHost.Bottom <= _settingsTile.Top;

    private ProfileTile CreateTile(GameProfile profile)
    {
        var tile = new ProfileTile(profile, IconProvider.GetForProfile(profile));
        tile.Click += (_, _) => SelectProfile(profile);

        new ToolTip().SetToolTip(tile, profile.Name);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Duplicate", null, (_, _) => { SelectProfile(profile); DuplicateProfile(); });
        menu.Items.Add("Delete", null, (_, _) => { SelectProfile(profile); RemoveProfile(); });
        tile.ContextMenuStrip = menu;

        return tile;
    }

    internal void SelectProfile(GameProfile profile)
    {
        _current = profile;

        foreach (var tile in _profileTiles)
            tile.Selected = ReferenceEquals(tile.Profile, profile);

        LoadSelectedProfile();
    }

    private void ShowEmptyState()
    {
        _current = null;
        _headerLabel.Text = "NO PROFILES YET";
        ClearEditor();
        SetEditorEnabled(false);
    }

    private void SetEditorEnabled(bool enabled)
    {
        _nameBox.Enabled = _processBox.Enabled = _enabledBox.Enabled = enabled;
        _allDisplaysBox.Enabled = enabled;
        _deleteButton.Enabled = enabled;
        _previewButton.Enabled = enabled;
        _brightness.SetEnabled(enabled);
        _contrast.SetEnabled(enabled);
        _gamma.SetEnabled(enabled);
        _vibrance.SetEnabled(enabled);
        UpdateDisplayCheckState();

        var palette = Theme.Current;
        foreach (var label in _sectionLabels)
            label.ForeColor = enabled ? palette.Text : palette.SubtleText;

        foreach (var section in _sections)
            DimCaptions(section, enabled, palette);
    }

    private static void DimCaptions(Control parent, bool enabled, ThemePalette palette)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Label label && label.Tag as string == "subtle")
                label.ForeColor = enabled ? palette.SubtleText : Color.FromArgb(120, palette.SubtleText);

            if (child is ThemedButton button)
                button.Enabled = enabled;

            DimCaptions(child, enabled, palette);
        }
    }

    private void LoadSelectedProfile()
    {
        if (_current == null)
        {
            ShowEmptyState();
            return;
        }

        _loading = true;

        _headerLabel.Text = _current.Name.ToUpperInvariant();
        _nameBox.Text = _current.Name;
        _processBox.Text = _current.ProcessName;
        _enabledBox.Checked = _current.Enabled;
        _brightness.Value = _current.Settings.Brightness;
        _contrast.Value = _current.Settings.Contrast;
        _gamma.Value = (int)Math.Round(_current.Settings.Gamma * 100);
        _vibrance.Value = _current.Settings.Vibrance;
        _allDisplaysBox.Checked = _current.AllDisplays;

        foreach (var check in _displayChecks)
        {
            var display = (DisplayTarget)check.Tag!;
            check.Checked = _current.DisplayKeys.Contains(display.Key);
        }

        _loading = false;
        SetEditorEnabled(true);
    }

    private void OnEditorChanged(bool applyNow = true)
    {
        if (_loading || _current == null)
            return;

        _current.Name = string.IsNullOrWhiteSpace(_nameBox.Text) ? "Untitled" : _nameBox.Text.Trim();
        _current.ProcessName = GameProfile.NormalizeProcessName(_processBox.Text);
        _current.Enabled = _enabledBox.Checked;
        _current.Settings.Brightness = _brightness.Value;
        _current.Settings.Contrast = _contrast.Value;
        _current.Settings.Gamma = _gamma.Value / 100.0;
        _current.Settings.Vibrance = _vibrance.Value;
        _current.Settings.Clamp();
        _current.AllDisplays = _allDisplaysBox.Checked;
        _current.DisplayKeys = _displayChecks
            .Where(check => check.Checked)
            .Select(check => ((DisplayTarget)check.Tag!).Key)
            .ToList();

        _headerLabel.Text = _current.Name.ToUpperInvariant();

        foreach (var tile in _profileTiles)
        {
            if (ReferenceEquals(tile.Profile, _current))
            {
                tile.Invalidate();
                break;
            }
        }

        SaveConfig(applyNow);
    }

    private void SaveConfig(bool applyNow = true)
    {
        ConfigStore.Save(_config);
        _engine.UpdateConfig(_config, applyNow);
    }

    internal void AddProfile()
    {
        using var dialog = new NewProfileForm();
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var profile = new GameProfile
        {
            Name = dialog.ProfileName,
            ProcessName = dialog.ProcessName,
            ExecutablePath = dialog.ExecutablePath,
        };
        ApplyDefaultDisplay(profile);

        _config.Profiles.Add(profile);

        var tile = CreateTile(profile);
        _profileTiles.Add(tile);
        _tiles.Controls.Add(tile);
        _tiles.Controls.SetChildIndex(tile, _profileTiles.Count - 1);
        UpdateTileWidths();
        Theme.Apply(this, Theme.Current);

        SelectProfile(profile);
        SaveConfig();
        _body.ScrollToTop();
    }

    private void ApplyDefaultDisplay(GameProfile profile)
    {
        var primary = _displays.FirstOrDefault(d => d.IsPrimary) ?? _displays.FirstOrDefault();
        if (primary == null)
            return;

        profile.AllDisplays = false;
        profile.DisplayKeys = new List<string> { primary.Key };
    }

    internal void DuplicateProfile()
    {
        if (_current == null)
            return;

        var copy = new GameProfile
        {
            Name = _current.Name + " (copy)",
            ProcessName = _current.ProcessName,
            ExecutablePath = _current.ExecutablePath,
            Enabled = _current.Enabled,
            Settings = _current.Settings.Clone(),
            AllDisplays = _current.AllDisplays,
            DisplayKeys = new List<string>(_current.DisplayKeys),
        };

        _config.Profiles.Add(copy);
        var tile = CreateTile(copy);
        _profileTiles.Add(tile);
        _tiles.Controls.Add(tile);
        _tiles.Controls.SetChildIndex(tile, _profileTiles.Count - 1);
        UpdateTileWidths();
        Theme.Apply(this, Theme.Current);

        SelectProfile(copy);
        SaveConfig();
    }

    internal void RemoveProfile()
    {
        if (_current == null)
            return;

        var profile = _current;
        int index = _config.Profiles.IndexOf(profile);

        _config.Profiles.Remove(profile);
        IconProvider.Forget(profile);

        var tile = _profileTiles.FirstOrDefault(t => ReferenceEquals(t.Profile, profile));
        if (tile != null)
        {
            _profileTiles.Remove(tile);
            _tiles.Controls.Remove(tile);
            tile.Dispose();
        }

        _current = null;
        SaveConfig();

        if (_profileTiles.Count > 0)
        {
            int next = Math.Clamp(index, 0, _profileTiles.Count - 1);
            SelectProfile(_profileTiles[next].Profile);
        }
        else
        {
            ShowEmptyState();
        }
    }

    internal GameProfile AddProfileDirect(string name = "New profile", string processName = "")
    {
        var profile = new GameProfile { Name = name, ProcessName = processName };
        ApplyDefaultDisplay(profile);
        _config.Profiles.Add(profile);

        var tile = CreateTile(profile);
        _profileTiles.Add(tile);
        _tiles.Controls.Add(tile);
        _tiles.Controls.SetChildIndex(tile, _profileTiles.Count - 1);
        UpdateTileWidths();

        SelectProfile(profile);
        SaveConfig();
        return profile;
    }

    private void ClearEditor()
    {
        _loading = true;
        _nameBox.Text = "";
        _processBox.Text = "";
        _enabledBox.Checked = false;
        _brightness.Value = 50;
        _contrast.Value = 50;
        _gamma.Value = 100;
        _vibrance.Value = 50;
        _allDisplaysBox.Checked = true;
        foreach (var check in _displayChecks)
            check.Checked = false;
        _loading = false;
    }

    private void PickProcess()
    {
        using var picker = new ProcessPickerForm();
        if (picker.ShowDialog(this) != DialogResult.OK || picker.Selected == null || _current == null)
            return;

        _current.ExecutablePath = picker.Selected.ExecutablePath;
        _processBox.Text = picker.Selected.ProcessName;
        _nameBox.Text = picker.Selected.ProcessName;

        OnEditorChanged();
        RefreshCurrentIcon();
    }

    private void RefreshCurrentIcon()
    {
        if (_current == null)
            return;

        IconProvider.Forget(_current);
        var icon = IconProvider.GetForProfile(_current);

        foreach (var tile in _profileTiles)
        {
            if (ReferenceEquals(tile.Profile, _current))
            {
                tile.Icon = icon;
                tile.Invalidate();
                break;
            }
        }
    }

    private void TogglePreview()
    {
        if (_current == null)
            return;

        if (_engine.PreviewActive)
            _engine.StopPreview();
        else
            _engine.PreviewProfile(_current);

        UpdatePreviewButton();
    }

    private void UpdatePreviewButton()
    {
        _previewButton.Text = _engine.PreviewActive ? "Stop preview" : "Preview";
        _previewButton.Invalidate();
    }

    private void ResetToNeutral()
    {
        _brightness.Value = 50;
        _contrast.Value = 50;
        _gamma.Value = 100;
        _vibrance.Value = 50;
        OnEditorChanged();
    }

    private void OpenSettings()
    {
        using var settings = new SettingsForm(_config);
        settings.ThemeChanged += (_, _) => ApplyTheme();
        settings.ShowDialog(this);
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        Theme.SetCurrent(_config.Theme);
        Theme.Apply(this, Theme.Current);
        _statusLabel.ForeColor = Theme.Current.SubtleText;
        _warningLabel.ForeColor = Theme.Current.Warning;
        SetEditorEnabled(_current != null);
        Invalidate(true);
    }

    internal int FooterHeight => _footer.Height;

    internal void SetWarningsVisibleForTest(bool visible)
    {
        _warningLabel.Visible = visible;
        _fixGammaButton.Visible = visible;
        _footer.PerformLayout();
        PerformLayout();
    }

    private void UpdateWarnings()
    {
        var warnings = new List<string>();

        if (!SaturationApi.Available)
        {
            warnings.Add("No NVIDIA or AMD driver found: digital vibrance is unavailable. "
                       + "Brightness, contrast and gamma still work.");
        }

        if (!GammaRampApi.IsGammaRangeUnlocked())
        {
            warnings.Add("Windows caps the brightness, contrast and gamma range. "
                       + "Unlock it for extreme values (admin rights required).");
            _fixGammaButton.Visible = true;
        }

        _warningLabel.Text = string.Join(Environment.NewLine, warnings);
        _warningLabel.Visible = warnings.Count > 0;
    }

    private void UnlockGammaRange()
    {
        var answer = MessageBox.Show(this,
            "VIVIDIA will ask Windows for administrator rights and set\n"
            + @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ICM\GdiIcmGammaRange = 256." + "\n\n"
            + "Sign out and back in for it to take effect. Some machines only pick it up "
            + "after a full restart. Continue?",
            "Unlock gamma range", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

        if (answer != DialogResult.OK)
            return;

        try
        {
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = "add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ICM\" "
                          + "/v GdiIcmGammaRange /t REG_DWORD /d 256 /f",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            };

            using var process = System.Diagnostics.Process.Start(start);
            process?.WaitForExit();

            if (GammaRampApi.IsGammaRangeUnlocked())
            {
                _fixGammaButton.Visible = false;
                UpdateWarnings();
                MessageBox.Show(this, "Done. Sign out and back in to apply. If the range is still "
                    + "capped afterwards, restart the computer.", "VIVIDIA",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unlock gamma range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateStatus(ProfileAppliedEventArgs e)
    {
        string active = e.Profile == null ? "baseline settings" : $"profile \"{e.Profile.Name}\"";
        string preview = _engine.PreviewActive ? "   (preview)" : "";
        string rejected = _engine.LastApplyRejected
            ? "      This display ignores brightness, contrast and gamma (remote session, virtual display or HDR)"
            : _engine.LastValuesConstrained
                ? "      Capped by the Windows gamma limit. Unlock the range for the full effect"
                : "";
        string process = string.IsNullOrEmpty(e.ForegroundProcess) ? "none" : e.ForegroundProcess;
        _statusLabel.Text = $"Foreground: {process}      Applied: {active}{preview}{rejected}";
        UpdatePreviewButton();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnFormClosing(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (_firstProfilePrompted || _config.Profiles.Count > 0)
            return;

        _firstProfilePrompted = true;
        BeginInvoke(() => AddProfile());
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible)
            _engine.StopPreview();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
            _engine.StopPreview();
    }
}
