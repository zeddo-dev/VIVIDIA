using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public sealed class AddTile : Control, IThemedControl
{
    private ThemePalette _palette = Theme.Dark;
    private bool _hovered;

    public AddTile()
    {
        Size = new Size(72, 56);
        Margin = new Padding(0, 4, 0, 4);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Sidebar;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(_palette.Sidebar);

        var box = new Rectangle((Width - 44) / 2, (Height - 44) / 2, 44, 44);

        using (var fill = new SolidBrush(_hovered ? _palette.SurfaceAlt : _palette.Sidebar))
        using (var path = RoundedRect(box, 8))
            g.FillPath(fill, path);

        using (var border = new Pen(_hovered ? _palette.Secondary : _palette.Border, 1) { DashStyle = DashStyle.Dash })
        using (var path = RoundedRect(box, 8))
            g.DrawPath(border, path);

        using var plus = new Pen(_hovered ? _palette.Secondary : _palette.SubtleText, 2);
        int cx = box.X + box.Width / 2;
        int cy = box.Y + box.Height / 2;
        g.DrawLine(plus, cx - 9, cy, cx + 9, cy);
        g.DrawLine(plus, cx, cy - 9, cx, cy + 9);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
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

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }
}
