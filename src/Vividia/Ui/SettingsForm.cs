using Vividia.Models;
using Vividia.Storage;

namespace Vividia.Ui;

public sealed class SettingsForm : Form
{
    private static string AppVersion =>
        typeof(SettingsForm).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .Select(attribute => attribute.InformationalVersion.Split('+')[0])
            .FirstOrDefault() ?? "1.0";

    private readonly AppConfig _config;
    private readonly ThemedDropDown _themeBox = new();
    private readonly ThemedCheckBox _autoStartBox = new() { Text = "Start with Windows" };
    private readonly ThemedCheckBox _startMinimizedBox = new() { Text = "Start minimized to tray" };
    private bool _loading;

    public event EventHandler? ThemeChanged;

    public SettingsForm(AppConfig config)
    {
        _config = config;

        Text = "VIVIDIA settings";
        Icon = Branding.CreateIcon(32);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 560);
        Font = new Font("Segoe UI", 9f);

        BuildLayout();
        LoadValues();
        Theme.Apply(this, Theme.Current);
    }

    private void BuildLayout()
    {
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(Spacing.WindowPadding),
        };

        var about = BuildAbout();
        var startup = BuildStartup();
        var appearance = BuildAppearance();

        body.Controls.Add(about);
        body.Controls.Add(startup);
        body.Controls.Add(appearance);

        Controls.Add(body);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        FitToContent();
    }

    private void FitToContent()
    {
        var body = Controls.Cast<Control>().FirstOrDefault();
        if (body == null)
            return;

        PerformLayout();
        body.PerformLayout();

        int bottom = body.Controls.Cast<Control>()
            .Where(child => child.Visible)
            .Select(child => child.Bottom + child.Margin.Bottom)
            .DefaultIfEmpty(0)
            .Max();

        ClientSize = new Size(ClientSize.Width, bottom + body.Padding.Bottom);
    }

    private Control BuildAppearance()
    {
        _themeBox.SetItems("System", "Light", "Dark");
        _themeBox.Width = 160;
        _themeBox.Dock = DockStyle.Top;
        _themeBox.SelectedIndexChanged += (_, _) => OnThemeSelected();

        return Section("Appearance", new Control[]
        {
            Caption("Theme"),
            _themeBox,
        });
    }

    private Control BuildStartup()
    {
        _autoStartBox.Dock = DockStyle.Top;
        _autoStartBox.CheckedChanged += (_, _) => ApplyAutoStart();
        _startMinimizedBox.Dock = DockStyle.Top;
        _startMinimizedBox.Margin = new Padding(0, Spacing.ItemGap, 0, 0);
        _startMinimizedBox.CheckedChanged += (_, _) => ApplyStartMinimized();

        return Section("Startup", new Control[]
        {
            _autoStartBox,
            _startMinimizedBox,
        });
    }

    private Control BuildAbout()
    {
        var description = new Label
        {
            Text = "VIVIDIA applies per-application colour profiles (brightness, contrast, "
                 + "gamma and digital vibrance) and restores your usual settings the moment "
                 + "the application loses focus.",
            Dock = DockStyle.Top,
            Height = 60,
            Tag = "subtle",
        };

        var version = new Label
        {
            Text = "Version " + AppVersion,
            Dock = DockStyle.Top,
            Height = 22,
            Margin = new Padding(0, Spacing.ItemGap, 0, 0),
            Tag = "subtle",
        };

        var author = new Label
        {
            Text = "Author: " + Links.Author,
            Dock = DockStyle.Top,
            Height = 22,
            Margin = new Padding(0, Spacing.ItemGap, 0, 0),
        };

        var github = new LinkLabel
        {
            Text = Links.GitHub,
            Dock = DockStyle.Top,
            Height = 22,
            Margin = new Padding(0, Spacing.ItemGap, 0, 0),
        };
        github.LinkClicked += (_, _) => Links.Open(Links.GitHub, this);

        return Section("About", new Control[]
        {
            description,
            version,
            author,
            github,
        });
    }


    private static Panel Section(string title, IReadOnlyList<Control> children) =>
        Spacing.BuildSection(title, children);

    private static Label Caption(string text) => Spacing.Caption(text);

    private void LoadValues()
    {
        _loading = true;
        _themeBox.SelectedIndex = (int)_config.Theme;
        _autoStartBox.Checked = AutoStart.IsEnabled();
        _startMinimizedBox.Checked = _config.StartMinimized;
        _loading = false;
    }

    private void OnThemeSelected()
    {
        if (_loading)
            return;

        _config.Theme = (AppTheme)Math.Max(0, _themeBox.SelectedIndex);
        ConfigStore.Save(_config);
        Theme.SetCurrent(_config.Theme);
        Theme.Apply(this, Theme.Current);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyAutoStart()
    {
        if (_loading)
            return;

        try
        {
            AutoStart.Set(_autoStartBox.Checked);
            _config.AutoStart = _autoStartBox.Checked;
            ConfigStore.Save(_config);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Autostart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyStartMinimized()
    {
        if (_loading)
            return;

        _config.StartMinimized = _startMinimizedBox.Checked;
        ConfigStore.Save(_config);
    }
}
