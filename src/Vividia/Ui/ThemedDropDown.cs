using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public sealed class ThemedDropDown : Control, IThemedControl
{
    private const int CornerRadius = 8;

    private readonly List<string> _items = new();
    private ThemePalette _palette = Theme.Dark;
    private int _selectedIndex = -1;
    private bool _hovered;
    private DropDownList? _openList;

    public event EventHandler? SelectedIndexChanged;

    public ThemedDropDown()
    {
        Size = new Size(ThemedButton.StandardWidth, ThemedButton.StandardHeight);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public void SetItems(params string[] items)
    {
        _items.Clear();
        _items.AddRange(items);
        Invalidate();
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = _items.Count == 0 ? -1 : Math.Clamp(value, 0, _items.Count - 1);
            if (clamped == _selectedIndex)
                return;

            _selectedIndex = clamped;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = Parent?.BackColor ?? palette.Background;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedPath(bounds, CornerRadius);

        using (var brush = new SolidBrush(_palette.SurfaceAlt))
            g.FillPath(brush, path);

        using (var pen = new Pen(_hovered || _openList != null ? _palette.Secondary : _palette.Border, 1))
            g.DrawPath(pen, path);

        string text = _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : "";
        var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        using (var brush = new SolidBrush(_palette.Text))
            g.DrawString(text, Font, brush, new RectangleF(12, 0, Width - 36, Height), format);

        using var chevron = new Pen(_palette.SubtleText, 1.6f);
        int cx = Width - 16;
        int cy = Height / 2 - 1;
        g.DrawLines(chevron, new[] { new Point(cx - 4, cy - 1), new Point(cx, cy + 3), new Point(cx + 4, cy - 1) });
    }

    internal static GraphicsPath RoundedPath(Rectangle bounds, int radius)
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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || _items.Count == 0)
            return;

        if (_openList != null)
        {
            _openList.Close();
            return;
        }

        var list = new DropDownList(_items, _selectedIndex, _palette, Font, Width);
        list.ItemChosen += index =>
        {
            SelectedIndex = index;
        };
        list.Closed += (_, _) =>
        {
            _openList = null;
            Invalidate();
        };

        _openList = list;
        list.ShowBelow(this);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }

    private sealed class DropDownList : Form
    {
        private const int ItemHeight = 30;
        private const int Inset = 6;

        private readonly List<string> _items;
        private readonly ThemePalette _palette;
        private int _hoveredIndex = -1;
        private int _selectedIndex;

        public event Action<int>? ItemChosen;

        public DropDownList(List<string> items, int selectedIndex, ThemePalette palette, Font font, int width)
        {
            _items = items;
            _palette = palette;
            _selectedIndex = selectedIndex;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Font = font;
            BackColor = palette.Surface;
            Size = new Size(width, items.Count * ItemHeight + Inset * 2);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public void ShowBelow(Control owner)
        {
            var origin = owner.PointToScreen(new Point(0, owner.Height + 4));
            Location = origin;

            using var path = RoundedPath(new Rectangle(0, 0, Width, Height), CornerRadius);
            Region = new Region(path);

            Show(owner.FindForm());
            Activate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_palette.Surface);

            using (var border = new Pen(_palette.Border, 1))
            using (var path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
                g.DrawPath(border, path);

            for (int i = 0; i < _items.Count; i++)
            {
                var row = new Rectangle(Inset, Inset + i * ItemHeight, Width - Inset * 2, ItemHeight);

                if (i == _hoveredIndex)
                {
                    using var hover = new SolidBrush(_palette.SurfaceAlt);
                    using var path = RoundedPath(row, 6);
                    g.FillPath(hover, path);
                }

                if (i == _selectedIndex)
                {
                    using var marker = new SolidBrush(_palette.Accent);
                    g.FillRectangle(marker, row.X + 2, row.Y + row.Height / 2 - 6, 3, 12);
                }

                using var text = new SolidBrush(_palette.Text);
                var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                g.DrawString(_items[i], Font, text, new RectangleF(row.X + 12, row.Y, row.Width - 16, row.Height), format);
            }
        }

        private int IndexAt(Point location)
        {
            int index = (location.Y - Inset) / ItemHeight;
            return index >= 0 && index < _items.Count ? index : -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = IndexAt(e.Location);
            if (index == _hoveredIndex)
                return;

            _hoveredIndex = index;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int index = IndexAt(e.Location);
            if (index < 0)
                return;

            _selectedIndex = index;
            ItemChosen?.Invoke(index);
            Close();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
                case Keys.Down:
                    _hoveredIndex = Math.Min(_items.Count - 1, _hoveredIndex + 1);
                    Invalidate();
                    break;
                case Keys.Up:
                    _hoveredIndex = Math.Max(0, _hoveredIndex - 1);
                    Invalidate();
                    break;
                case Keys.Enter when _hoveredIndex >= 0:
                    ItemChosen?.Invoke(_hoveredIndex);
                    Close();
                    break;
            }
        }
    }
}
