using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public sealed class ThemedButton : Control, IThemedControl
{
    public const int StandardWidth = 136;
    public const int StandardHeight = 32;
    private const int CornerRadius = 8;

    private ThemePalette _palette = Theme.Dark;
    private bool _hovered;
    private bool _pressed;

    public ThemedButton(string text, bool accent = false, bool warning = false)
    {
        Text = text;
        Accent = accent;
        Warning = warning;
        Size = new Size(StandardWidth, StandardHeight);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public bool Accent { get; }

    public bool Warning { get; }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = Parent?.BackColor ?? palette.Surface;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Rounded(bounds, CornerRadius);

        Color fill = Accent
            ? (_pressed ? Darken(_palette.Accent, 0.85f) : _hovered ? Lighten(_palette.Accent, 0.12f) : _palette.Accent)
            : (_pressed ? _palette.SurfaceAlt : _hovered ? _palette.SurfaceAlt : _palette.Surface);

        Color border = Accent ? fill
            : Warning ? _palette.Warning
            : _hovered ? _palette.Secondary : _palette.Border;

        Color text = !Enabled ? _palette.SubtleText
            : Accent ? _palette.AccentText
            : Warning ? _palette.Warning
            : _palette.Text;

        using (var brush = new SolidBrush(Enabled ? fill : _palette.Surface))
            g.FillPath(brush, path);

        using (var pen = new Pen(Enabled ? border : _palette.Border, 1))
            g.DrawPath(pen, path);

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };

        using (var brush = new SolidBrush(text))
            g.DrawString(Text, Font, brush, new RectangleF(4, 0, Width - 8, Height), format);
    }

    private static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color Lighten(Color color, float amount) => Color.FromArgb(
        color.A,
        (int)Math.Min(255, color.R + 255 * amount),
        (int)Math.Min(255, color.G + 255 * amount),
        (int)Math.Min(255, color.B + 255 * amount));

    private static Color Darken(Color color, float factor) => Color.FromArgb(
        color.A, (int)(color.R * factor), (int)(color.G * factor), (int)(color.B * factor));

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _pressed = true; Invalidate(); }

    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }

    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
}
