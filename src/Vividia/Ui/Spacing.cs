namespace Vividia.Ui;

public static class Spacing
{
    public const int WindowPadding = 16;

    public const int SectionPadding = 22;

    public const int SectionGap = 18;

    public const int GroupGap = 26;

    public const int ItemGap = 12;

    public const int LabelGap = 8;

    public const int ButtonGap = 10;

    public static Panel BuildSection(string title, IReadOnlyList<Control> children)
    {
        var panel = new BufferedPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(SectionPadding, 16, SectionPadding, SectionPadding),
            Margin = new Padding(0, 0, 0, SectionGap),
            Tag = "surface",
        };

        for (int i = children.Count - 1; i >= 0; i--)
        {
            children[i].Dock = DockStyle.Top;
            panel.Controls.Add(children[i]);
        }

        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
        });

        return panel;
    }

    public static Label Caption(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = 24,
        Margin = new Padding(0, 0, 0, LabelGap),
        Tag = "subtle",
    };

    public static Panel Gap(int height) => new BufferedPanel { Dock = DockStyle.Top, Height = height, Tag = "surface" };

    public static FlowLayoutPanel ButtonRow(params Control[] buttons)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Tag = "surface",
        };

        foreach (var button in buttons)
        {
            button.Margin = new Padding(0, 0, ButtonGap, 0);
            row.Controls.Add(button);
        }

        return row;
    }
}
