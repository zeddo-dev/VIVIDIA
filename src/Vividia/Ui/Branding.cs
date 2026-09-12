using System.Drawing.Drawing2D;

namespace Vividia.Ui;

public static class Branding
{
    private static Image? _logo;
    private static bool _logoLoaded;

    public static Color Accent { get; } = Color.FromArgb(95, 227, 169);

    public static Image? Logo
    {
        get
        {
            if (_logoLoaded)
                return _logo;

            _logoLoaded = true;
            _logo = LoadLogo();
            return _logo;
        }
    }

    private static Image? LoadLogo()
    {
        var embedded = LoadEmbeddedMark();
        if (embedded != null)
            return embedded;

        foreach (var path in CandidatePaths())
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var image = Image.FromStream(stream);
                return new Bitmap(image);
            }
            catch
            {
            }
        }

        return null;
    }

    private static Image? LoadEmbeddedMark()
    {
        try
        {
            using var stream = typeof(Branding).Assembly.GetManifestResourceStream("Vividia.mark.png");
            if (stream == null)
                return null;

            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> CandidatePaths()
    {
        string baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "assets", "logo.png");
        yield return Path.Combine(baseDir, "logo.png");

        yield return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "assets", "logo.png"));
        yield return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "assets", "logo.png"));
    }

    public static Icon CreateIcon(int size = 32)
    {
        var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (Logo != null)
                DrawLogoFitted(g, Logo, size);
            else
                DrawFallbackMark(g, size);
        }

        IntPtr handle = bitmap.GetHicon();
        using var icon = Icon.FromHandle(handle);
        var copy = (Icon)icon.Clone();
        bitmap.Dispose();
        return copy;
    }

    private static void DrawLogoFitted(Graphics g, Image logo, int size)
    {
        var source = logo.Width > logo.Height * 1.4
            ? new Rectangle(0, 0, logo.Height, logo.Height)
            : new Rectangle(0, 0, logo.Width, logo.Height);

        g.DrawImage(logo, new Rectangle(0, 0, size, size), source, GraphicsUnit.Pixel);
    }

    private static void DrawFallbackMark(Graphics g, int size)
    {
        float s = size;
        using var bright = new SolidBrush(Color.FromArgb(140, 240, 197));
        using var deep = new SolidBrush(Accent);

        var left = new[]
        {
            new PointF(0.06f * s, 0.12f * s),
            new PointF(0.30f * s, 0.12f * s),
            new PointF(0.60f * s, 0.90f * s),
            new PointF(0.44f * s, 0.90f * s),
        };
        g.FillPolygon(deep, left);

        var rightOuter = new[]
        {
            new PointF(0.74f * s, 0.12f * s),
            new PointF(0.94f * s, 0.12f * s),
            new PointF(0.62f * s, 0.90f * s),
            new PointF(0.52f * s, 0.90f * s),
        };
        g.FillPolygon(bright, rightOuter);

        var rightInner = new[]
        {
            new PointF(0.60f * s, 0.12f * s),
            new PointF(0.70f * s, 0.12f * s),
            new PointF(0.46f * s, 0.72f * s),
            new PointF(0.40f * s, 0.56f * s),
        };
        g.FillPolygon(bright, rightInner);

        using var arcPen = new Pen(Accent, Math.Max(1f, 0.05f * s));
        g.DrawArc(arcPen, 0.10f * s, 0.04f * s, 0.80f * s, 0.80f * s, 200, 140);
    }
}
