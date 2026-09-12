namespace Vividia.Ui;

public sealed class NewProfileForm : Form
{
    private readonly ThemedTextField _processBox = new();
    private readonly ThemedTextField _nameBox = new();

    public string ProcessName => Models.GameProfile.NormalizeProcessName(_processBox.Text);

    public string ProfileName => string.IsNullOrWhiteSpace(_nameBox.Text)
        ? (string.IsNullOrWhiteSpace(ProcessName) ? "New profile" : ProcessName)
        : _nameBox.Text.Trim();

    public string? ExecutablePath { get; private set; }

    public NewProfileForm()
    {
        Text = "New profile";
        Icon = Branding.CreateIcon(32);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 250);
        Font = new Font("Segoe UI", 9f);

        BuildLayout();
        Theme.Apply(this, Theme.Current);
    }

    private void BuildLayout()
    {
        var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Spacing.WindowPadding) };
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(Spacing.SectionPadding),
            Tag = "surface",
        };

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

        var pick = new ThemedButton("Pick running…") { Dock = DockStyle.Fill, Margin = new Padding(0) };
        pick.Click += (_, _) => PickProcess();
        processRow.Controls.Add(_processBox, 0, 0);
        processRow.Controls.Add(pick, 1, 0);

        _nameBox.Dock = DockStyle.Top;
        _nameBox.Margin = new Padding(0, 0, 0, Spacing.GroupGap);

        var buttons = Spacing.ButtonRow(
            CreateButton("Create", accent: true, DialogResult.OK),
            CreateButton("Cancel", accent: false, DialogResult.Cancel));

        body.Controls.Add(buttons);
        body.Controls.Add(Spacing.Gap(Spacing.GroupGap));
        body.Controls.Add(_nameBox);
        body.Controls.Add(Spacing.Caption("Profile name"));
        body.Controls.Add(Spacing.Gap(Spacing.GroupGap));
        body.Controls.Add(processRow);
        body.Controls.Add(Spacing.Caption("Process name (without .exe)"));

        outer.Controls.Add(body);
        Controls.Add(outer);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        FitToContent();
    }

    private void FitToContent()
    {
        var outer = Controls.Cast<Control>().FirstOrDefault();
        var body = outer?.Controls.Cast<Control>().FirstOrDefault();
        if (outer == null || body == null)
            return;

        PerformLayout();
        outer.PerformLayout();
        body.PerformLayout();

        int bottom = body.Controls.Cast<Control>()
            .Where(child => child.Visible)
            .Select(child => child.Bottom + child.Margin.Bottom)
            .DefaultIfEmpty(0)
            .Max();

        ClientSize = new Size(ClientSize.Width, bottom + body.Padding.Bottom + outer.Padding.Vertical);
    }

    private ThemedButton CreateButton(string text, bool accent, DialogResult result)
    {
        var button = new ThemedButton(text, accent);
        button.Click += (_, _) =>
        {
            if (result == DialogResult.OK && string.IsNullOrWhiteSpace(ProcessName))
            {
                MessageBox.Show(this, "Enter a process name or pick a running application.",
                    "New profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = result;
            Close();
        };
        return button;
    }

    private void PickProcess()
    {
        using var picker = new ProcessPickerForm();
        if (picker.ShowDialog(this) != DialogResult.OK || picker.Selected == null)
            return;

        ExecutablePath = picker.Selected.ExecutablePath;
        _processBox.Text = picker.Selected.ProcessName;
        _nameBox.Text = picker.Selected.ProcessName;
    }
}
