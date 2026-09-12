using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public sealed class ThemedScrollBar : Control, IThemedControl
{
    private ThemePalette _palette = Theme.Dark;
    private int _value;
    private int _contentHeight = 1;
    private int _viewportHeight = 1;
    private bool _dragging;
    private int _dragOffset;
    private bool _hovered;

    public event EventHandler? ValueChanged;

    public ThemedScrollBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Width = 10;
    }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = Parent?.BackColor ?? palette.Background;
        Invalidate();
    }

    public int MaximumValue => Math.Max(0, _contentHeight - _viewportHeight);

    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, 0, MaximumValue);
            if (clamped == _value)
                return;

            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetMetrics(int contentHeight, int viewportHeight)
    {
        _contentHeight = Math.Max(1, contentHeight);
        _viewportHeight = Math.Max(1, viewportHeight);
        Value = Math.Min(_value, MaximumValue);
        Invalidate();
    }

    private Rectangle ThumbRectangle
    {
        get
        {
            int trackHeight = Height;
            int thumbHeight = Math.Max(28, (int)(trackHeight * (_viewportHeight / (double)_contentHeight)));
            thumbHeight = Math.Min(thumbHeight, trackHeight);

            int travel = trackHeight - thumbHeight;
            int offset = MaximumValue == 0 ? 0 : (int)(travel * (_value / (double)MaximumValue));
            return new Rectangle(2, offset, Width - 4, thumbHeight);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        if (MaximumValue <= 0)
            return;

        var thumb = ThumbRectangle;
        using var brush = new SolidBrush(_hovered || _dragging ? _palette.SubtleText : _palette.Border);
        using var path = Rounded(thumb, (Width - 4) / 2);
        g.FillPath(brush, path);
    }

    private static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(1, radius * 2);
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
        if (e.Button != MouseButtons.Left || MaximumValue <= 0)
            return;

        var thumb = ThumbRectangle;
        if (thumb.Contains(e.Location))
        {
            _dragging = true;
            _dragOffset = e.Y - thumb.Y;
        }
        else
        {
            SetValueFromThumbTop(e.Y - thumb.Height / 2);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
            SetValueFromThumbTop(e.Y - _dragOffset);
    }

    private void SetValueFromThumbTop(int thumbTop)
    {
        int travel = Height - ThumbRectangle.Height;
        if (travel <= 0)
            return;

        double ratio = Math.Clamp(thumbTop / (double)travel, 0, 1);
        Value = (int)Math.Round(ratio * MaximumValue);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }
}
