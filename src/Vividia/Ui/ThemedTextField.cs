namespace Vividia.Ui;

public sealed class ThemedTextField : UnderlinedField
{
    public ThemedTextField()
    {
        Input.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
    }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => Input.Text;
        set => Input.Text = value ?? "";
    }

    public void SelectAllText()
    {
        Input.Focus();
        Input.SelectAll();
    }
}
