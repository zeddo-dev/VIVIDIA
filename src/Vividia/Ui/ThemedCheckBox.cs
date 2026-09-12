using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public sealed class ThemedCheckBox : Control, IThemedControl
{
    private ThemePalette _palette = Theme.Dark;
    private bool _checked;
    private bool _hovered;

    public event EventHandler? CheckedChanged;

    public ThemedCheckBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Height = 24;
        Width = 220;
        TabStop = true;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;

            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = Parent?.BackColor ?? palette.Surface;
        Invalidate();
    }

    private Rectangle BoxRectangle => new(0, (Height - 16) / 2, 16, 16);

    private Rectangle HotArea
    {
        get
        {
            var box = BoxRectangle;
            int textWidth = string.IsNullOrEmpty(Text)
                ? 0
                : TextRenderer.MeasureText(Text, Font).Width;

            int width = Math.Min(Width, box.Right + (textWidth > 0 ? 8 + textWidth + 4 : 0));
            return new Rectangle(0, 0, width, Height);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var box = BoxRectangle;
        Color boxColor = !Enabled ? _palette.Border
            : _checked ? _palette.Accent
            : _hovered ? _palette.SurfaceAlt
            : Color.Transparent;

        if (boxColor != Color.Transparent)
        {
            using var fill = new SolidBrush(boxColor);
            g.FillRectangle(fill, box);
        }

        Color outline = !Enabled ? _palette.Border : _checked ? _palette.Accent : _palette.Border;
        using (var border = new Pen(outline, 1))
            g.DrawRectangle(border, box);

        if (_checked)
        {
            using var tick = new Pen(Enabled ? _palette.AccentText : _palette.SubtleText, 2);
            g.DrawLines(tick, new[]
            {
                new Point(box.X + 3, box.Y + 8),
                new Point(box.X + 7, box.Y + 12),
                new Point(box.X + 13, box.Y + 4),
            });
        }

        var textColor = Enabled ? _palette.Text : _palette.SubtleText;
        using (var brush = new SolidBrush(textColor))
        {
            var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var textRect = new RectangleF(box.Right + 8, 0, Width - box.Right - 8, Height);
            g.DrawString(Text, Font, brush, textRect, format);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!Enabled || e.Button != MouseButtons.Left || !HotArea.Contains(e.Location))
            return;

        Focus();
        Checked = !Checked;
    }

    protected override bool IsInputKey(Keys keyData) => keyData == Keys.Space || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space)
            Checked = !Checked;
    }

    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        bool inside = HotArea.Contains(e.Location);
        Cursor = inside && Enabled ? Cursors.Hand : Cursors.Default;

        if (inside == _hovered)
            return;

        _hovered = inside;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Cursor = Cursors.Default;
        _hovered = false;
        Invalidate();
    }

    internal bool IsInsideHotArea(Point location) => HotArea.Contains(location);

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}
