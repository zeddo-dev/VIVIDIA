using System.Globalization;

namespace Vividia.Ui;

public sealed class ThemedNumericField : UnderlinedField
{
    private readonly int _minimum;
    private readonly int _maximum;
    private readonly int _scale;
    private readonly int _decimals;

    private int _value;
    private bool _updatingText;

    public event EventHandler? ValueChanged;

    public ThemedNumericField(int minimum, int maximum, int scale = 1, int decimals = 0)
    {
        _minimum = minimum;
        _maximum = maximum;
        _scale = scale;
        _decimals = decimals;

        Width = 64;
        Input.TextAlign = HorizontalAlignment.Center;
        Input.KeyDown += OnInputKeyDown;
        Input.KeyPress += OnInputKeyPress;
        Input.TextChanged += OnInputTextChanged;
        Input.Leave += (_, _) => CommitText();

        _value = minimum;
        UpdateText();
    }

    protected override int TextPadding => 4;

    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, _minimum, _maximum);
            if (clamped == _value)
            {
                UpdateText();
                return;
            }

            _value = clamped;
            UpdateText();
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateText()
    {
        _updatingText = true;
        Input.Text = _decimals > 0
            ? (_value / (double)_scale).ToString("F" + _decimals, CultureInfo.CurrentCulture)
            : _value.ToString(CultureInfo.CurrentCulture);
        _updatingText = false;
    }

    internal static bool Accepts(char character, string current, int selectionStart, int decimals)
    {
        if (char.IsControl(character))
            return true;

        if (char.IsDigit(character))
            return true;

        if (decimals == 0)
            return false;

        if (character != '.' && character != ',')
            return false;

        return !current.Contains('.') || selectionStart <= current.IndexOf('.');
    }

    internal static string KeepDigits(string text, int decimals)
    {
        var kept = new System.Text.StringBuilder();
        bool separatorUsed = false;

        foreach (char character in text)
        {
            if (char.IsDigit(character))
            {
                kept.Append(character);
                continue;
            }

            if (decimals == 0 || separatorUsed)
                continue;

            if (character == '.' || character == ',')
            {
                kept.Append('.');
                separatorUsed = true;
            }
        }

        return kept.ToString();
    }

    private void OnInputKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == ',')
            e.KeyChar = '.';

        e.Handled = !Accepts(e.KeyChar, Input.Text, Input.SelectionStart, _decimals);
    }

    private void OnInputTextChanged(object? sender, EventArgs e)
    {
        if (_updatingText)
            return;

        string cleaned = KeepDigits(Input.Text, _decimals);
        if (cleaned == Input.Text)
            return;

        int caret = Math.Max(0, Input.SelectionStart - (Input.Text.Length - cleaned.Length));
        _updatingText = true;
        Input.Text = cleaned;
        Input.SelectionStart = Math.Min(caret, cleaned.Length);
        _updatingText = false;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;

        CommitText();
        e.SuppressKeyPress = true;
    }

    private void CommitText()
    {
        if (_updatingText)
            return;

        string text = Input.Text.Trim().Replace(',', '.');
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            Value = (int)Math.Round(parsed * _scale);
        else
            UpdateText();
    }
}
