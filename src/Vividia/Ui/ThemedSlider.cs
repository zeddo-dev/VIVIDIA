using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public sealed class ThemedSlider : Control, IThemedControl
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private bool _dragging;
    private bool _hovered;
    private ThemePalette _palette = Theme.Dark;

    public event EventHandler? ValueChanged;

    public event EventHandler? ValueCommitted;

    public ThemedSlider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Height = 24;
        TabStop = true;
    }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        Invalidate();
    }

    public int Minimum
    {
        get => _minimum;
        set { _minimum = value; Value = _value; Invalidate(); }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = value; Value = _value; Invalidate(); }
    }

    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, _minimum, _maximum);
            if (clamped == _value)
                return;

            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int? NeutralMark { get; set; }

    private Rectangle TrackRectangle => new(7, Height / 2 - 1, Math.Max(1, Width - 14), 2);

    private int ValueToX(int value)
    {
        var track = TrackRectangle;
        double ratio = _maximum == _minimum ? 0 : (value - _minimum) / (double)(_maximum - _minimum);
        return track.X + (int)Math.Round(ratio * track.Width);
    }

    private int XToValue(int x)
    {
        var track = TrackRectangle;
        double ratio = Math.Clamp((x - track.X) / (double)track.Width, 0, 1);
        return _minimum + (int)Math.Round(ratio * (_maximum - _minimum));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var track = TrackRectangle;
        int handleX = ValueToX(_value);

        Color fillColour = Enabled ? _palette.Accent : _palette.SubtleText;

        using (var trackBrush = new SolidBrush(_palette.Border))
            g.FillRectangle(trackBrush, track);

        if (NeutralMark is int mark)
        {
            int markX = ValueToX(mark);
            using var markPen = new Pen(Color.FromArgb(120, _palette.SubtleText), 1);
            g.DrawLine(markPen, markX, track.Y - 4, markX, track.Bottom + 4);
        }

        using (var fillBrush = new SolidBrush(fillColour))
            g.FillRectangle(fillBrush, track.X, track.Y, Math.Max(0, handleX - track.X), track.Height);

        int centre = Height / 2;
        bool lit = Enabled && (_hovered || _dragging);

        if (lit)
        {
            using var outerGlow = new SolidBrush(Color.FromArgb(45, _palette.Accent));
            g.FillEllipse(outerGlow, handleX - 13, centre - 13, 26, 26);
            using var midGlow = new SolidBrush(Color.FromArgb(90, _palette.Accent));
            g.FillEllipse(midGlow, handleX - 10, centre - 10, 20, 20);
            using var innerGlow = new SolidBrush(Color.FromArgb(140, _palette.Accent));
            g.FillEllipse(innerGlow, handleX - 8, centre - 8, 16, 16);
        }

        int radius = lit ? 7 : 6;
        var handle = new Rectangle(handleX - radius, centre - radius, radius * 2, radius * 2);
        using (var handleBrush = new SolidBrush(fillColour))
            g.FillEllipse(handleBrush, handle);

        if (Focused)
        {
            using var ring = new Pen(Color.FromArgb(90, _palette.Accent), 3);
            g.DrawEllipse(ring, handleX - 8, Height / 2 - 8, 16, 16);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        Focus();
        _dragging = true;
        Value = XToValue(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
            Value = XToValue(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_dragging)
        {
            _dragging = false;
            Invalidate();
            ValueCommitted?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Left: Value -= 1; break;
            case Keys.Right: Value += 1; break;
            case Keys.Home: Value = _minimum; break;
            case Keys.End: Value = _maximum; break;
            default: return;
        }

        ValueCommitted?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }

    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}
