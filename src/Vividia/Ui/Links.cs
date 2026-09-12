using System.Diagnostics;

namespace Vividia.Ui;

public static class Links
{
    public const string Author = "zeddo-dev";
    public const string GitHub = "https://github.com/zeddo-dev/VIVIDIA";
    public const string Donate = "https://buymeacoffee.com/zeddo";

    public static void Open(string url, IWin32Window? owner = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            if (owner != null)
                MessageBox.Show(owner, ex.Message, "VIVIDIA", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show(ex.Message, "VIVIDIA", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
