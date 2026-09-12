namespace Vividia.Ui;

public sealed class ScrollHost : Panel
{
    private readonly ThemedScrollBar _bar = new();

    public ScrollHost()
    {
        DoubleBuffered = true;
        Content = new BufferedPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = new Point(0, 0),
        };
        Content.SizeChanged += (_, _) => UpdateMetrics();

        _bar.Dock = DockStyle.Right;
        _bar.ValueChanged += (_, _) => ScrollTo(_bar.Value);

        Controls.Add(Content);
        Controls.Add(_bar);

        MouseWheel += (_, e) => ScrollBy(e.Delta);
    }

    public Panel Content { get; }

    public int MaximumContentWidth { get; set; } = int.MaxValue;

    public bool ScrollBarVisible
    {
        get => _bar.Visible;
        set
        {
            if (_bar.Visible == value)
                return;

            _bar.Visible = value;
            UpdateMetrics();
        }
    }

    public void ScrollBy(int delta) => _bar.Value -= Math.Sign(delta) * 60;

    public void ScrollToTop() => _bar.Value = 0;

    private void ScrollTo(int offset)
    {
        if (Content.Top == -offset)
            return;

        Content.Top = -offset;
        Content.Update();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        int barWidth = _bar.Visible ? _bar.Width : 0;
        int available = Math.Max(50, ClientSize.Width - barWidth - Padding.Horizontal);
        int width = Math.Min(available, MaximumContentWidth);

        if (Content.Width != width)
            Content.Width = width;

        Content.Left = Padding.Left + (available - width) / 2;

        int viewport = Math.Max(1, ClientSize.Height);
        _bar.SetMetrics(Content.Height, viewport);
        ScrollTo(_bar.Value);
    }

    public void AttachWheel(Control control)
    {
        control.MouseWheel += (_, e) => ScrollBy(e.Delta);

        foreach (Control child in control.Controls)
            AttachWheel(child);
    }
}
