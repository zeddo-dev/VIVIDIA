using System.Diagnostics;
using Vividia.Watch;

namespace Vividia.Ui;

public sealed class RunningApplication
{
    public required string ProcessName { get; init; }
    public required string DisplayName { get; init; }
    public string? ExecutablePath { get; init; }
}

public sealed class ProcessPickerForm : Form
{
    private readonly ListView _list = new();
    private readonly ImageList _icons = new() { ImageSize = new Size(20, 20), ColorDepth = ColorDepth.Depth32Bit };

    public RunningApplication? Selected { get; private set; }

    public ProcessPickerForm()
    {
        Text = "Running applications";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 480);
        MinimizeBox = false;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9f);
        Icon = Branding.CreateIcon(32);

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.SmallImageList = _icons;
        _list.Columns.Add("Process", 190);
        _list.Columns.Add("Window", 330);
        _list.DoubleClick += (_, _) => Accept();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(10),
        };

        var select = new ThemedButton("Select", accent: true) { Margin = new Padding(6, 0, 0, 0) };
        var cancel = new ThemedButton("Cancel") { Margin = new Padding(6, 0, 0, 0) };
        var refresh = new ThemedButton("Refresh") { Margin = new Padding(6, 0, 0, 0) };
        select.Click += (_, _) => Accept();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        refresh.Click += (_, _) => FillList();

        buttons.Controls.AddRange(new Control[] { select, cancel, refresh });
        Controls.Add(_list);
        Controls.Add(buttons);

        FillList();
        Theme.Apply(this, Theme.Current);
    }

    private void Accept()
    {
        if (_list.SelectedItems.Count == 0)
            return;

        Selected = _list.SelectedItems[0].Tag as RunningApplication;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void FillList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        _icons.Images.Clear();

        foreach (var entry in CollectApplications())
        {
            var item = new ListViewItem(entry.Application.ProcessName) { Tag = entry.Application };
            item.SubItems.Add(entry.WindowTitle);

            if (entry.Application.ExecutablePath != null)
                AttachIcon(item, entry.Application.ExecutablePath);

            _list.Items.Add(item);
        }

        _list.EndUpdate();
    }

    private void AttachIcon(ListViewItem item, string executablePath)
    {
        try
        {
            if (!_icons.Images.ContainsKey(executablePath))
            {
                using var icon = Icon.ExtractAssociatedIcon(executablePath);
                if (icon == null)
                    return;

                _icons.Images.Add(executablePath, icon.ToBitmap());
            }

            item.ImageKey = executablePath;
        }
        catch
        {
        }
    }

    private static List<(RunningApplication Application, string WindowTitle)> CollectApplications()
    {
        var result = new List<(RunningApplication, string)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            string title;
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                    continue;

                title = process.MainWindowTitle;
                if (string.IsNullOrWhiteSpace(title))
                    continue;
            }
            catch
            {
                continue;
            }

            if (!seen.Add(process.ProcessName))
                continue;

            string? path = ProcessPath.TryGet(process.Id);
            result.Add((new RunningApplication
            {
                ProcessName = process.ProcessName,
                DisplayName = string.IsNullOrWhiteSpace(title) ? process.ProcessName : title,
                ExecutablePath = path,
            }, title));
        }

        return result
            .OrderBy(entry => entry.Item1.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
