using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public abstract class UnderlinedField : Control, IThemedControl
{
    protected readonly TextBox Input = new() { BorderStyle = BorderStyle.None };

    private ThemePalette _palette = Theme.Dark;

    protected UnderlinedField()
    {
        Height = 30;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        Input.GotFocus += (_, _) => Invalidate();
        Input.LostFocus += (_, _) => Invalidate();
        Controls.Add(Input);
    }

    protected ThemePalette Palette => _palette;

    public virtual void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = Parent?.BackColor ?? palette.Surface;
        Input.BackColor = palette.InputBackground;
        Input.ForeColor = palette.Text;
        Invalidate();
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        BackColor = Parent?.BackColor ?? _palette.Surface;
        Input.BackColor = _palette.InputBackground;
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int height = Input.PreferredHeight;
        Input.SetBounds(TextPadding, Math.Max(0, (Height - height - 4) / 2), Math.Max(10, Width - TextPadding * 2), height);
    }

    protected virtual int TextPadding => 10;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        bool active = Input.Focused;

        var box = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var fill = new SolidBrush(Enabled ? _palette.InputBackground : _palette.Surface))
        using (var path = ThemedDropDown.RoundedPath(box, 6))
            g.FillPath(fill, path);

        using var pen = new Pen(active ? _palette.Accent : _palette.Border, active ? 2f : 1f);
        float y = Height - (active ? 2f : 1f);
        g.DrawLine(pen, 2, y, Width - 2, y);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Input.Enabled = Enabled;
        Input.BackColor = Enabled ? _palette.InputBackground : _palette.Surface;
        Input.ForeColor = Enabled ? _palette.Text : _palette.SubtleText;
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Input.Focus();
    }
}
