using System.Runtime.InteropServices;
using Microsoft.Win32;
using Vividia.Models;

namespace Vividia.Ui;

public sealed record ThemePalette(
    Color Sidebar,
    Color Background,
    Color Surface,
    Color SurfaceAlt,
    Color InputBackground,
    Color Text,
    Color SubtleText,
    Color Border,
    Color Accent,
    Color AccentText,
    Color Secondary,
    Color Warning,
    bool IsDark);

public static class Theme
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? subAppName, string? subIdList);

    public static ThemePalette Dark { get; } = new(
        Sidebar: Color.FromArgb(23, 20, 42),
        Background: Color.FromArgb(30, 26, 51),
        Surface: Color.FromArgb(39, 33, 66),
        SurfaceAlt: Color.FromArgb(52, 44, 87),
        InputBackground: Color.FromArgb(25, 21, 50),
        Text: Color.FromArgb(237, 235, 247),
        SubtleText: Color.FromArgb(167, 161, 198),
        Border: Color.FromArgb(61, 53, 102),
        Accent: Color.FromArgb(95, 227, 169),
        AccentText: Color.FromArgb(16, 37, 28),
        Secondary: Color.FromArgb(169, 139, 234),
        Warning: Color.FromArgb(240, 192, 102),
        IsDark: true);

    public static ThemePalette Light { get; } = new(
        Sidebar: Color.FromArgb(232, 230, 243),
        Background: Color.FromArgb(244, 245, 248),
        Surface: Color.White,
        SurfaceAlt: Color.FromArgb(238, 236, 248),
        InputBackground: Color.FromArgb(241, 240, 249),
        Text: Color.FromArgb(29, 26, 46),
        SubtleText: Color.FromArgb(107, 102, 136),
        Border: Color.FromArgb(216, 212, 232),
        Accent: Color.FromArgb(34, 179, 131),
        AccentText: Color.White,
        Secondary: Color.FromArgb(124, 92, 214),
        Warning: Color.FromArgb(160, 92, 18),
        IsDark: false);

    public static ThemePalette Current { get; private set; } = Dark;

    public static ThemePalette Resolve(AppTheme theme) => theme switch
    {
        AppTheme.Light => Light,
        AppTheme.Dark => Dark,
        _ => SystemPrefersDark() ? Dark : Light,
    };

    public static void SetCurrent(AppTheme theme) => Current = Resolve(theme);

    public static bool SystemPrefersDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void Apply(Form form, ThemePalette palette)
    {
        form.BackColor = palette.Background;
        form.ForeColor = palette.Text;
        ApplyTitleBar(form, palette.IsDark);

        foreach (Control control in form.Controls)
            ApplyToControl(control, palette);
    }

    private static void ApplyScrollBars(Control control, bool dark)
    {
        try
        {
            if (control.IsHandleCreated)
                SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
        }
        catch
        {
        }
    }

    public static void ApplyTitleBar(Form form, bool dark)
    {
        try
        {
            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
        catch
        {
        }
    }

    private static void ApplyToControl(Control control, ThemePalette palette)
    {
        string role = control.Tag as string ?? "";
        ApplyScrollBars(control, palette.IsDark);

        switch (control)
        {
            case IThemedControl themed:

                themed.ApplyPalette(palette);
                return;

            case Button button:
                bool accent = role == "accent-button";
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = accent ? palette.Accent : palette.Border;
                button.FlatAppearance.MouseOverBackColor = accent ? palette.Accent : palette.SurfaceAlt;
                button.BackColor = accent ? palette.Accent : palette.Surface;
                button.ForeColor = accent ? palette.AccentText : palette.Text;
                button.UseVisualStyleBackColor = false;
                break;

            case TextBox textBox:
                textBox.BorderStyle = BorderStyle.FixedSingle;
                textBox.BackColor = palette.SurfaceAlt;
                textBox.ForeColor = palette.Text;
                break;

            case NumericUpDown numeric:
                numeric.BorderStyle = BorderStyle.FixedSingle;
                numeric.BackColor = palette.SurfaceAlt;
                numeric.ForeColor = palette.Text;
                break;

            case ComboBox combo:
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = palette.SurfaceAlt;
                combo.ForeColor = palette.Text;
                break;

            case ListView listView:
                listView.BackColor = palette.Surface;
                listView.ForeColor = palette.Text;
                listView.BorderStyle = BorderStyle.FixedSingle;
                break;

            case CheckedListBox checkedList:
                checkedList.BackColor = palette.Surface;
                checkedList.ForeColor = palette.Text;
                checkedList.BorderStyle = BorderStyle.None;
                break;

            case LinkLabel link:

                link.BackColor = Color.Transparent;
                link.ForeColor = palette.SubtleText;
                link.LinkColor = palette.Accent;
                link.ActiveLinkColor = palette.Accent;
                link.VisitedLinkColor = palette.Accent;
                link.LinkBehavior = LinkBehavior.HoverUnderline;
                break;

            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = role switch
                {
                    "subtle" => palette.SubtleText,
                    "warning" => palette.Warning,
                    "accent" => palette.Accent,
                    _ => palette.Text,
                };
                break;

            case CheckBox checkBox:
                checkBox.BackColor = Color.Transparent;
                checkBox.ForeColor = palette.Text;
                checkBox.FlatStyle = FlatStyle.Flat;
                checkBox.FlatAppearance.BorderColor = palette.Border;
                checkBox.FlatAppearance.CheckedBackColor = palette.Accent;
                break;

            default:
                control.BackColor = role switch
                {
                    "sidebar" => palette.Sidebar,
                    "surface" => palette.Surface,
                    _ => palette.Background,
                };
                control.ForeColor = palette.Text;
                break;
        }

        foreach (Control child in control.Controls)
            ApplyToControl(child, palette);
    }
}

public interface IThemedControl
{
    void ApplyPalette(ThemePalette palette);
}
