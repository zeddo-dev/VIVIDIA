using System.Globalization;
using Vividia.Core;
using Vividia.Display;
using Vividia.Models;
using Vividia.Storage;
using Vividia.Ui;
using Vividia.Watch;

namespace Vividia.SelfTest;

internal static class Program
{
    private static int _failures;

    [STAThread]
    private static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        bool skipWatcher = args.Contains("--no-watch");

        TestDisplayEnumeration(out var displays);
        TestEdidParser();
        TestRampMath();
        TestGammaReadWrite(displays);
        TestVibrance(displays);
        TestProfileListLogic();
        TestSettingRowRoundTrip();
        TestValuesSurviveWindowShow();
        TestPreviewToggle();
        TestDialogSizes();
        TestWheelAndCommit();
        TestFooterAndNumericInput();
        TestActiveProfileReapplyAndHitArea();
        TestSidebarWithManyProfiles();

        if (!skipWatcher)
            TestForegroundWatcher();

        Console.WriteLine();
        Console.WriteLine(_failures == 0 ? "All checks passed." : $"Checks failed: {_failures}");
    }

    private static void Check(bool condition, string message)
    {
        if (condition)
        {
            Console.WriteLine($"    OK   {message}");
        }
        else
        {
            _failures++;
            Console.WriteLine($"    FAIL {message}");
        }
    }

    private static void TestDisplayEnumeration(out List<DisplayTarget> displays)
    {
        Console.WriteLine("== Displays ==");
        displays = DisplayEnumerator.GetActiveDisplays();
        if (displays.Count == 0)
            Console.WriteLine("  none (EnumDisplayDevices returned no attached monitor)");

        foreach (var d in displays)
        {
            Console.WriteLine($"  {d.DeviceName} | {d.Label}");
            Console.WriteLine($"    key={d.Key}");
        }
    }

    private static void TestEdidParser()
    {
        Console.WriteLine();
        Console.WriteLine("== EDID parser ==");

        var edid = new byte[128];

        edid[54] = 0; edid[55] = 0; edid[56] = 0; edid[57] = 0xFC; edid[58] = 0;
        string name = "Odyssey G5";
        for (int i = 0; i < name.Length; i++)
            edid[59 + i] = (byte)name[i];
        edid[59 + name.Length] = 0x0A;

        var parsed = EdidReader.ParseName(edid);
        Check(parsed == name, $"model name parsed from descriptor: '{parsed}'");
        Check(EdidReader.DescribeVendor("SAC2750") == "Samsung", "vendor code SAC resolves to Samsung");
        Check(EdidReader.ParseName(new byte[64]) == null, "short EDID block returns null instead of throwing");
    }

    private static void TestRampMath()
    {
        Console.WriteLine();
        Console.WriteLine("== LUT maths ==");

        var neutral = GammaRamp.Build(50, 50, 1.0);
        var identity = GammaRamp.Identity();
        int maxDiff = 0;
        for (int i = 0; i < 256; i++)
            maxDiff = Math.Max(maxDiff, Math.Abs(neutral.Red[i] - identity.Red[i]));

        Check(maxDiff == 0, $"neutral (50/50/1.00) equals the identity ramp (max difference {maxDiff})");

        var dark = GammaRamp.Build(30, 50, 1.0);
        var bright = GammaRamp.Build(70, 50, 1.0);
        Check(dark.Red[128] < neutral.Red[128] && bright.Red[128] > neutral.Red[128],
            $"brightness moves midpoint: 30 -> {dark.Red[128]}, 50 -> {neutral.Red[128]}, 70 -> {bright.Red[128]}");

        var lowGamma = GammaRamp.Build(50, 50, 0.60);
        var highGamma = GammaRamp.Build(50, 50, 2.20);
        Check(lowGamma.Red[128] < neutral.Red[128] && highGamma.Red[128] > neutral.Red[128],
            $"gamma moves midpoint: 0.60 -> {lowGamma.Red[128]}, 1.00 -> {neutral.Red[128]}, 2.20 -> {highGamma.Red[128]}");

        var lowContrast = GammaRamp.Build(50, 20, 1.0);
        var highContrast = GammaRamp.Build(50, 80, 1.0);
        Check(lowContrast.Red[64] > neutral.Red[64] && highContrast.Red[64] < neutral.Red[64],
            $"contrast squeezes/expands around mid: 20 -> {lowContrast.Red[64]}, 50 -> {neutral.Red[64]}, 80 -> {highContrast.Red[64]}");

        var brightGamma = GammaRamp.Build(50, 50, 2.20);
        var brightGammaHighContrast = GammaRamp.Build(50, 90, 2.20);
        Check(Math.Abs(brightGamma.Red[128] - brightGammaHighContrast.Red[128]) < 600,
            $"contrast keeps the midpoint at high gamma: 50 -> {brightGamma.Red[128]}, 90 -> {brightGammaHighContrast.Red[128]}");

        var beyondLimit = GammaRamp.Build(94, 100, 2.80);
        Check(beyondLimit.MaxDeviationFromLinear() > GammaRamp.WindowsDeviationLimit,
            $"extreme values exceed the Windows limit ({beyondLimit.MaxDeviationFromLinear():F3} > {GammaRamp.WindowsDeviationLimit})");

        var constrained = beyondLimit.ConstrainToWindowsLimit();
        Check(constrained.MaxDeviationFromLinear() <= GammaRamp.WindowsDeviationLimit,
            $"constraining brings them back under it ({constrained.MaxDeviationFromLinear():F3})");

        bool monotonic = true;
        for (int i = 1; i < 256; i++)
        {
            if (constrained.Red[i] < constrained.Red[i - 1])
                monotonic = false;
        }

        Check(monotonic, "the constrained ramp is still monotonic");

        var lower = GammaRamp.Build(93, 100, 2.80).ConstrainToWindowsLimit();
        Check(constrained.Red[64] >= lower.Red[64],
            $"raising brightness past the limit still moves the curve up ({lower.Red[64]} -> {constrained.Red[64]})");

        Check(constrained.MaxDeviationFromLinear() > GammaRamp.WindowsDeviationLimit - 0.01,
            $"the constrained ramp uses as much of the allowed range as it can ({constrained.MaxDeviationFromLinear():F3})");

        int flat = 0;
        for (int i = 0; i < 256; i++)
        {
            if (brightGammaHighContrast.Red[i] == 65535)
                flat++;
        }

        Check(flat < 70, $"high gamma plus high contrast does not flatten the top of the curve ({flat} of 256 points clipped)");
    }

    private static void TestGammaReadWrite(List<DisplayTarget> displays)
    {
        Console.WriteLine();
        Console.WriteLine("== LUT read/write ==");
        Console.WriteLine($"  GdiIcmGammaRange unlocked: {GammaRampApi.IsGammaRangeUnlocked()}");

        foreach (var display in displays)
        {
            var original = GammaRampApi.Read(display.DeviceName);
            if (original == null)
            {
                Console.WriteLine($"  {display.DeviceName}: cannot read the LUT");
                continue;
            }

            Console.WriteLine($"  {display.DeviceName}: original LUT read, midpoint = {original.Red[128]}");

            var test = GammaRamp.Build(35, 55, 1.20);
            bool written = GammaRampApi.Write(display.DeviceName, test);
            Thread.Sleep(400);

            var readBack = GammaRampApi.Read(display.DeviceName);
            bool restored = GammaRampApi.Write(display.DeviceName, original);
            var afterRestore = GammaRampApi.Read(display.DeviceName);

            Check(written && readBack != null && Math.Abs(readBack.Red[128] - test.Red[128]) <= 512,
                $"test ramp applied (midpoint {readBack?.Red[128]}, expected {test.Red[128]})");
            Check(restored && afterRestore != null && afterRestore.Red[128] == original.Red[128],
                $"original ramp restored (midpoint {afterRestore?.Red[128]})");
        }
    }

    private static void TestVibrance(List<DisplayTarget> displays)
    {
        Console.WriteLine();
        Console.WriteLine("== Saturation (NVAPI / ADL) ==");
        SaturationApi.EnsureInitialized();
        Console.WriteLine($"  available: {SaturationApi.Available} via {SaturationApi.ProviderName}");
        Console.WriteLine($"  NVIDIA (NVAPI): {VibranceApi.Available}{(VibranceApi.LastError == null ? "" : ": " + VibranceApi.LastError)}");
        Console.WriteLine($"  AMD (ADL): {AdlApi.Available}{(AdlApi.LastError == null ? "" : ": " + AdlApi.LastError)}");

        foreach (var adapter in AdlApi.DescribeAdapters())
            Console.WriteLine($"    ADL adapter {adapter}");

        foreach (var display in displays)
        {
            var range = SaturationApi.GetRange(display.DeviceName);
            if (range == null)
            {
                Console.WriteLine($"  {display.DeviceName}: saturation unavailable");
                continue;
            }

            var r = range.Value;
            Console.WriteLine($"  {display.DeviceName}: min={r.Minimum} max={r.Maximum} default={r.Default} current={r.Current}");
            Check(SaturationApi.UiToLevel(50, r) == r.Default, $"UI 50 maps to the driver default ({r.Default})");
            Check(SaturationApi.UiToLevel(0, r) == r.Minimum && SaturationApi.UiToLevel(100, r) == r.Maximum,
                $"UI ends map to min/max ({r.Minimum}/{r.Maximum})");
            Check(SaturationApi.LevelToUi(r.Current, r) is >= 0 and <= 100,
                $"current level {r.Current} maps back to UI {SaturationApi.LevelToUi(r.Current, r)}");
        }
    }

    private static void TestProfileListLogic()
    {
        Console.WriteLine();
        Console.WriteLine("== Profile list (add / duplicate / delete) ==");

        string configFile = Paths.ConfigFile;
        string? backup = File.Exists(configFile) ? File.ReadAllText(configFile) : null;

        try
        {
            var config = new AppConfig();
            using var engine = new ProfileEngine(config);
            using var form = new MainForm(config, engine);

            form.AddProfileDirect();
            form.AddProfileDirect();
            Check(config.Profiles.Count == 2, $"two profiles added (count = {config.Profiles.Count})");

            var displays = DisplayEnumerator.GetActiveDisplays();
            var primary = displays.FirstOrDefault(d => d.IsPrimary) ?? displays.FirstOrDefault();
            if (primary != null)
            {
                var fresh = config.Profiles[0];
                Check(!fresh.AllDisplays && fresh.DisplayKeys.Contains(primary.Key),
                    $"a new profile targets the primary display ({primary.MonitorName})");
            }

            var second = config.Profiles[1];
            form.SelectProfile(second);
            form.DuplicateProfile();
            Check(config.Profiles.Count == 3, $"duplicate added (count = {config.Profiles.Count})");

            form.RemoveProfile();
            Check(config.Profiles.Count == 2, $"delete removes exactly one profile (count = {config.Profiles.Count})");
            Check(config.Profiles.Contains(second), "delete removed the duplicate, not the source profile");

            form.SelectProfile(config.Profiles[0]);
            form.RemoveProfile();
            form.RemoveProfile();
            Check(config.Profiles.Count == 0, $"all profiles can be deleted (count = {config.Profiles.Count})");

            form.AddProfileDirect();
            Check(config.Profiles.Count == 1, "a profile can be added again after the list was emptied");

            var saved = ConfigStore.Load();
            Check(saved.Profiles.Count == 1, $"config file matches the list (count = {saved.Profiles.Count})");
        }
        finally
        {
            if (backup != null)
                File.WriteAllText(configFile, backup);
            else if (File.Exists(configFile))
                File.Delete(configFile);
        }

        Console.WriteLine();
        Console.WriteLine("== Theme ==");
        Check(Theme.Resolve(AppTheme.Dark).IsDark, "dark theme resolves to the dark palette");
        Check(!Theme.Resolve(AppTheme.Light).IsDark, "light theme resolves to the light palette");
        Console.WriteLine($"  system preference: {(Theme.SystemPrefersDark() ? "dark" : "light")}");
    }

    private static void TestSettingRowRoundTrip()
    {
        Console.WriteLine();
        Console.WriteLine("== Setting row round-trip ==");

        var brightness = new SettingRow("Brightness", 0, 100, 50);
        foreach (int value in new[] { 0, 1, 37, 49, 50, 62, 99, 100 })
        {
            brightness.Value = value;
            Check(brightness.Value == value, $"brightness {value} -> {brightness.Value}");
        }

        var gamma = new SettingRow("Gamma", 30, 280, 100, scale: 100, decimals: 2);
        foreach (int value in new[] { 30, 85, 100, 115, 187, 280 })
        {
            gamma.Value = value;
            Check(gamma.Value == value, $"gamma raw {value} -> {gamma.Value}");
        }

        gamma.Value = 115;
        gamma.Width = 420;
        gamma.Width = 640;
        Check(gamma.Value == 115, $"gamma survives a resize: {gamma.Value}");

        brightness.Value = 62;
        brightness.Width = 380;
        Check(brightness.Value == 62, $"brightness survives a resize: {brightness.Value}");

        var slider = brightness.Controls.OfType<ThemedSlider>().First();
        slider.Focus();
        SendWheel(slider, 120);
        SendWheel(slider, -120);
        Check(brightness.Value == 62, $"the wheel never changes a slider value: {brightness.Value}");
    }

    private static void SendWheel(Control control, int delta)
    {
        var method = typeof(Control).GetMethod("OnMouseWheel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(control, new object[] { new MouseEventArgs(MouseButtons.None, 0, 10, 10, delta) });
    }

    private static void TestValuesSurviveWindowShow()
    {
        Console.WriteLine();
        Console.WriteLine("== Values after the window is shown ==");

        string configFile = Paths.ConfigFile;
        string? backup = File.Exists(configFile) ? File.ReadAllText(configFile) : null;

        try
        {
            var profile = new GameProfile
            {
                Name = "Sample",
                ProcessName = "sample",
                Settings = new ColorSettings { Brightness = 62, Contrast = 55, Gamma = 1.15, Vibrance = 78 },
            };
            var config = new AppConfig { Profiles = { profile } };

            using var engine = new ProfileEngine(config);
            using var form = new MainForm(config, engine);
            form.Show();

            for (int i = 0; i < 20; i++)
            {
                Application.DoEvents();
                Thread.Sleep(25);
            }

            form.Hide();

            Console.WriteLine($"  after show: brightness={profile.Settings.Brightness} contrast={profile.Settings.Contrast} " +
                              $"gamma={profile.Settings.Gamma} vibrance={profile.Settings.Vibrance}");

            Check(profile.Settings.Brightness == 62, $"brightness kept ({profile.Settings.Brightness})");
            Check(profile.Settings.Contrast == 55, $"contrast kept ({profile.Settings.Contrast})");
            Check(Math.Abs(profile.Settings.Gamma - 1.15) < 0.0001, $"gamma kept ({profile.Settings.Gamma})");
            Check(profile.Settings.Vibrance == 78, $"vibrance kept ({profile.Settings.Vibrance})");
        }
        finally
        {
            if (backup != null)
                File.WriteAllText(configFile, backup);
            else if (File.Exists(configFile))
                File.Delete(configFile);
        }
    }

    private static void TestPreviewToggle()
    {
        Console.WriteLine();
        Console.WriteLine("== Preview toggle ==");

        string configFile = Paths.ConfigFile;
        string? backup = File.Exists(configFile) ? File.ReadAllText(configFile) : null;

        try
        {
            var profile = new GameProfile
            {
                Name = "Neutral",
                ProcessName = "neutral-sample",
                Settings = ColorSettings.Neutral,
            };
            var config = new AppConfig { Profiles = { profile } };

            using var engine = new ProfileEngine(config);
            Check(!engine.PreviewActive, "preview is off to begin with");

            engine.PreviewProfile(profile);
            Check(engine.PreviewActive, "preview turns on");
            Check(engine.ActiveProfile == profile, "previewed profile is the active one");

            profile.Settings.Brightness = 55;
            engine.UpdateConfig(config);
            Check(engine.PreviewActive, "preview survives an edit of the profile");

            engine.StopPreview();
            Check(!engine.PreviewActive, "pressing again turns the preview off");
            Check(engine.ActiveProfile == null, "colours go back to the baseline");
        }
        finally
        {
            if (backup != null)
                File.WriteAllText(configFile, backup);
            else if (File.Exists(configFile))
                File.Delete(configFile);
        }
    }

    private static void TestDialogSizes()
    {
        Console.WriteLine();
        Console.WriteLine("== Dialog sizes ==");

        string configFile = Paths.ConfigFile;
        string? backup = File.Exists(configFile) ? File.ReadAllText(configFile) : null;

        try
        {
            using var newProfile = new NewProfileForm();
            ShowBriefly(newProfile);
            Check(newProfile.ClientSize.Height is > 140 and < 320,
                $"new profile dialog height follows content ({newProfile.ClientSize.Height})");

            using var settings = new SettingsForm(new AppConfig());
            ShowBriefly(settings);
            Check(settings.ClientSize.Height is > 300 and < 700,
                $"settings window height follows content ({settings.ClientSize.Height})");
        }
        finally
        {
            if (backup != null)
                File.WriteAllText(configFile, backup);
            else if (File.Exists(configFile))
                File.Delete(configFile);
        }
    }

    private static void ShowBriefly(Form form, bool hide = true)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-4000, -4000);
        form.ShowInTaskbar = false;
        form.Show();

        for (int i = 0; i < 10; i++)
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }

        if (hide)
            form.Hide();
    }

    private static void TestWheelAndCommit()
    {
        Console.WriteLine();
        Console.WriteLine("== Wheel over sliders / applying on release ==");

        using var host = new ScrollHost { Size = new Size(400, 200) };
        var row = new SettingRow("Brightness", 0, 100, 50) { Width = 360 };
        var filler = new Panel { Dock = DockStyle.Top, Height = 600 };
        host.Content.Controls.Add(filler);
        host.Content.Controls.Add(row);
        host.AttachWheel(host.Content);

        var slider = row.Controls.OfType<ThemedSlider>().First();
        int before = row.Value;
        SendWheel(slider, -120);
        Check(host.Content.Top < 0, $"wheel over a slider scrolls the page ({host.Content.Top})");
        Check(row.Value == before, $"and leaves the value alone ({row.Value})");

        int scrolled = host.Content.Top;
        SendWheel(filler, -120);
        Check(host.Content.Top < scrolled, $"wheel elsewhere scrolls too ({host.Content.Top})");

        string configFile = Paths.ConfigFile;
        string? backup = File.Exists(configFile) ? File.ReadAllText(configFile) : null;

        try
        {
            var profile = new GameProfile { Name = "Neutral", ProcessName = "neutral-sample", Settings = ColorSettings.Neutral };
            var config = new AppConfig { Profiles = { profile } };

            using var engine = new ProfileEngine(config);
            engine.PreviewProfile(profile);

            int applied = 0;
            engine.StateChanged += (_, _) => applied++;

            profile.Settings.Brightness = 60;
            engine.UpdateConfig(config, applyNow: false);
            Check(applied == 0, $"dragging a slider does not re-apply the profile ({applied} updates)");

            engine.UpdateConfig(config);
            Check(applied == 1, $"releasing the slider applies it once ({applied} updates)");

            engine.StopPreview();
        }
        finally
        {
            if (backup != null)
                File.WriteAllText(configFile, backup);
            else if (File.Exists(configFile))
                File.Delete(configFile);
        }
    }

    private static void TestFooterAndNumericInput()
    {
        Console.WriteLine();
        Console.WriteLine("== Footer height and numeric input ==");

        string configFile = Paths.ConfigFile;
        string? backup = File.Exists(configFile) ? File.ReadAllText(configFile) : null;

        try
        {
            var profile = new GameProfile { Name = "Sample", ProcessName = "sample" };
            var config = new AppConfig { Profiles = { profile } };

            using var engine = new ProfileEngine(config);
            using var form = new MainForm(config, engine);
            ShowBriefly(form, hide: false);

            form.SetWarningsVisibleForTest(true);
            Application.DoEvents();
            int withWarning = form.FooterHeight;

            form.SetWarningsVisibleForTest(false);
            Application.DoEvents();
            int withoutWarning = form.FooterHeight;

            form.Hide();

            Check(withoutWarning < withWarning,
                $"footer shrinks without a warning ({withWarning} -> {withoutWarning})");
        }
        finally
        {
            if (backup != null)
                File.WriteAllText(configFile, backup);
            else if (File.Exists(configFile))
                File.Delete(configFile);
        }

        Check(ThemedNumericField.KeepDigits("6a2", 0) == "62", "letters are dropped from a whole-number field");
        Check(ThemedNumericField.KeepDigits("1,15", 2) == "1.15", "a comma becomes a dot in the gamma field");
        Check(ThemedNumericField.KeepDigits("1.1.5", 2) == "1.15", "only the first separator survives");
        Check(ThemedNumericField.KeepDigits("1.15", 0) == "115", "a whole-number field keeps no separator");
        Check(!ThemedNumericField.Accepts('a', "", 0, 0), "a letter is refused");
        Check(ThemedNumericField.Accepts('7', "", 0, 0), "a digit is accepted");
        Check(ThemedNumericField.Accepts('.', "1", 1, 2), "a dot is accepted in the gamma field");
        Check(!ThemedNumericField.Accepts('.', "1", 1, 0), "a dot is refused in a whole-number field");
    }

    private static void TestActiveProfileReapplyAndHitArea()
    {
        Console.WriteLine();
        Console.WriteLine("== Active profile re-apply / checkbox hit area ==");

        string configFile = Paths.ConfigFile;
        string? backup = File.Exists(configFile) ? File.ReadAllText(configFile) : null;

        try
        {
            var profile = new GameProfile
            {
                Name = "Self",
                ProcessName = "vividia",
                Settings = ColorSettings.Neutral,
            };
            var config = new AppConfig { Profiles = { profile } };

            using var engine = new ProfileEngine(config);
            engine.ApplyProfileForTest(profile, "vividia");

            int applied = 0;
            engine.StateChanged += (_, _) => applied++;

            profile.Settings.Brightness = 58;
            engine.UpdateConfig(config);
            Check(applied == 1, $"editing the profile of the focused application re-applies it ({applied})");

            profile.Settings.Brightness = 61;
            engine.UpdateConfig(config, applyNow: false);
            Check(applied == 1, $"dragging still does not re-apply ({applied})");
        }
        finally
        {
            if (backup != null)
                File.WriteAllText(configFile, backup);
            else if (File.Exists(configFile))
                File.Delete(configFile);
        }

        using var check = new ThemedCheckBox { Text = "Profile enabled", Width = 560 };
        check.ApplyPalette(Theme.Dark);

        Check(check.IsInsideHotArea(new Point(8, 12)), "the box itself is clickable");
        Check(check.IsInsideHotArea(new Point(70, 12)), "the caption is clickable");
        Check(!check.IsInsideHotArea(new Point(400, 12)), "empty space to the right is not clickable");
    }

    private static void TestSidebarWithManyProfiles()
    {
        Console.WriteLine();
        Console.WriteLine("== Sidebar with many profiles ==");

        string configFile = Paths.ConfigFile;
        string? backup = File.Exists(configFile) ? File.ReadAllText(configFile) : null;

        try
        {
            var config = new AppConfig();
            for (int i = 1; i <= 20; i++)
                config.Profiles.Add(new GameProfile { Name = "Profile " + i, ProcessName = "sample" + i });

            using var engine = new ProfileEngine(config);
            using var form = new MainForm(config, engine);
            form.ClientSize = new Size(940, 620);
            ShowBriefly(form, hide: false);

            bool scrolls = form.SidebarScrolls;
            bool clear = form.SettingsTileIsClear;
            bool bar = form.SidebarScrollBarVisible;
            bool uniform = form.SidebarColorsUniform;
            form.ScrollSidebar(-120);
            bool wheel = form.SidebarScrollOffset > 0;
            form.Hide();

            Check(scrolls, "twenty profiles make the sidebar scrollable");
            Check(clear, "the profile tiles never cover the settings gear");
            Check(!bar, "the sidebar shows no scroll bar");
            Check(wheel, "the wheel scrolls the sidebar anyway");
            Check(uniform, "the sidebar is one colour throughout");
        }
        finally
        {
            if (backup != null)
                File.WriteAllText(configFile, backup);
            else if (File.Exists(configFile))
                File.Delete(configFile);
        }
    }

    private static void TestForegroundWatcher()
    {
        Console.WriteLine();
        Console.WriteLine("== Foreground watcher (5 seconds, switch windows) ==");

        using var watcher = new ForegroundWatcher();
        int events = 0;

        watcher.ForegroundChanged += (_, e) =>
        {
            events++;
            Console.WriteLine($"  event {events}: process = '{e.ProcessName}' (pid {e.ProcessId})");
        };

        watcher.Start();

        var stop = new System.Windows.Forms.Timer { Interval = 5000 };
        stop.Tick += (_, _) => { stop.Stop(); Application.ExitThread(); };
        stop.Start();
        Application.Run();

        Check(events > 0, $"foreground events received: {events}");
    }
}
