namespace Vividia.Ui;

public sealed class SettingRow : TableLayoutPanel
{
    private readonly Label _caption = new();
    private readonly ThemedSlider _slider = new();
    private readonly ThemedNumericField _numeric;
    private readonly int _scale;
    private bool _syncing;

    public event EventHandler? ValueChanged;

    public event EventHandler? ValueCommitted;

    public SettingRow(string caption, int minimum, int maximum, int neutral, int scale = 1, int decimals = 0)
    {
        _scale = scale;

        ColumnCount = 2;
        RowCount = 2;
        Height = 66;
        Dock = DockStyle.Top;
        Margin = new Padding(0, 4, 0, 10);
        Tag = "surface";
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
        RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _caption.Text = caption;
        _caption.Dock = DockStyle.Fill;
        _caption.TextAlign = ContentAlignment.MiddleLeft;
        _caption.Margin = new Padding(0);

        _slider.Minimum = minimum;
        _slider.Maximum = maximum;
        _slider.NeutralMark = neutral;
        _slider.Dock = DockStyle.Fill;
        _slider.Margin = new Padding(0, 6, 12, 6);

        _numeric = new ThemedNumericField(minimum, maximum, scale, decimals)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
        };

        _slider.ValueChanged += (_, _) =>
        {
            if (_syncing)
                return;

            _syncing = true;
            _numeric.Value = _slider.Value;
            _syncing = false;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        };

        _slider.ValueCommitted += (_, _) => ValueCommitted?.Invoke(this, EventArgs.Empty);

        _numeric.ValueChanged += (_, _) =>
        {
            if (_syncing)
                return;

            _syncing = true;
            _slider.Value = _numeric.Value;
            _syncing = false;
            ValueChanged?.Invoke(this, EventArgs.Empty);
            ValueCommitted?.Invoke(this, EventArgs.Empty);
        };

        Controls.Add(_caption, 0, 0);
        SetColumnSpan(_caption, 2);
        Controls.Add(_slider, 0, 1);
        Controls.Add(_numeric, 1, 1);

        Value = neutral;
    }

    public int Value
    {
        get => _slider.Value;
        set
        {
            _syncing = true;
            _slider.Value = value;
            _numeric.Value = value;
            _syncing = false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        _slider.Enabled = enabled;
        _numeric.Enabled = enabled;
    }
}
