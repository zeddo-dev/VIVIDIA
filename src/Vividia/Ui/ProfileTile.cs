using System.Drawing.Drawing2D;
using Vividia.Models;

namespace Vividia.Ui;

public sealed class ProfileTile : Control, IThemedControl
{
    private ThemePalette _palette = Theme.Dark;
    private bool _selected;
    private bool _hovered;

    public ProfileTile(GameProfile profile, Image? icon)
    {
        Profile = profile;
        Icon = icon;
        Size = new Size(72, 56);
        Margin = new Padding(0, 4, 0, 4);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public GameProfile Profile { get; }

    public Image? Icon { get; set; }

    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
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

        using (var fill = new SolidBrush(_selected || _hovered ? _palette.SurfaceAlt : _palette.Surface))
        using (var path = RoundedRect(box, 8))
            g.FillPath(fill, path);

        using (var border = new Pen(_selected ? _palette.Accent : _hovered ? _palette.Secondary : _palette.Border, _selected ? 2 : 1))
        using (var path = RoundedRect(box, 8))
            g.DrawPath(border, path);

        if (_selected)
        {
            using var marker = new SolidBrush(_palette.Accent);
            g.FillRectangle(marker, 0, box.Y + 8, 3, box.Height - 16);
        }

        if (Icon != null)
        {
            var iconRect = new Rectangle(box.X + 7, box.Y + 7, 30, 30);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(Icon, iconRect);
        }
        else
        {
            string initials = GetInitials(Profile.Name);
            using var font = new Font("Segoe UI", 14f, FontStyle.Bold);
            using var brush = new SolidBrush(_palette.SubtleText);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(initials, font, brush, box, format);
        }

        if (!Profile.Enabled)
        {
            using var veil = new SolidBrush(Color.FromArgb(150, _palette.Sidebar));
            using var path = RoundedRect(box, 8);
            g.FillPath(veil, path);
        }
    }

    private static string GetInitials(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            return "?";

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[1][0]))
            : trimmed[..Math.Min(2, trimmed.Length)].ToUpperInvariant();
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
