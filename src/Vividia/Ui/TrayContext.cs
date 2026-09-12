using Vividia.Core;
using Vividia.Models;

namespace Vividia.Ui;

public sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly ProfileEngine _engine;
    private readonly MainForm _form;
    private readonly ToolStripMenuItem _pauseItem;

    public TrayContext(AppConfig config, ProfileEngine engine)
    {
        _engine = engine;
        _form = new MainForm(config, engine);

        _pauseItem = new ToolStripMenuItem("Pause (keep baseline colours)") { CheckOnClick = true };
        _pauseItem.CheckedChanged += (_, _) =>
        {
            _engine.Paused = _pauseItem.Checked;
            UpdateTooltip();
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Open VIVIDIA", null, (_, _) => ShowForm()));
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication()));

        _tray = new NotifyIcon
        {
            Icon = Branding.CreateIcon(32),
            Visible = true,
            Text = "VIVIDIA",
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => ShowForm();

        _engine.StateChanged += (_, _) => UpdateTooltip();
        UpdateTooltip();

        if (!config.StartMinimized)
            ShowForm();
    }

    private void ShowForm()
    {
        _form.Show();
        _form.WindowState = FormWindowState.Normal;
        _form.BringToFront();
        _form.Activate();
    }

    private void UpdateTooltip()
    {
        string state = _engine.Paused
            ? "paused"
            : _engine.ActiveProfile == null ? "baseline" : _engine.ActiveProfile.Name;

        string text = "VIVIDIA: " + state;
        _tray.Text = text.Length > 62 ? text[..62] : text;
    }

    private void ExitApplication()
    {
        _tray.Visible = false;
        _engine.Dispose();
        _form.Dispose();
        _tray.Dispose();
        ExitThread();
    }
}
