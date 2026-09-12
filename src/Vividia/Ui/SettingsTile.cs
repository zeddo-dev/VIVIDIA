using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public sealed class SettingsTile : Control, IThemedControl
{
    private ThemePalette _palette = Theme.Dark;
    private bool _hovered;

    public SettingsTile()
    {
        Size = new Size(72, 56);
        Margin = new Padding(0, 4, 0, 8);
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

        if (_hovered)
        {
            using var fill = new SolidBrush(_palette.SurfaceAlt);
            using var path = RoundedRect(box, 8);
            g.FillPath(fill, path);
        }

        var colour = _hovered ? _palette.Secondary : _palette.SubtleText;
        DrawGear(g, box, colour);
    }

    private static void DrawGear(Graphics g, Rectangle box, Color colour)
    {
        float cx = box.X + box.Width / 2f;
        float cy = box.Y + box.Height / 2f;
        float outer = 11f;
        float inner = 7.5f;

        using var pen = new Pen(colour, 2f);
        g.DrawEllipse(pen, cx - inner, cy - inner, inner * 2, inner * 2);

        for (int i = 0; i < 8; i++)
        {
            double angle = Math.PI / 4 * i;
            float x1 = cx + (float)(Math.Cos(angle) * inner);
            float y1 = cy + (float)(Math.Sin(angle) * inner);
            float x2 = cx + (float)(Math.Cos(angle) * outer);
            float y2 = cy + (float)(Math.Sin(angle) * outer);
            g.DrawLine(pen, x1, y1, x2, y2);
        }

        using var hub = new SolidBrush(colour);
        g.FillEllipse(hub, cx - 2.5f, cy - 2.5f, 5f, 5f);
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
