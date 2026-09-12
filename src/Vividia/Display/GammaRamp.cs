namespace Vividia.Display;

public sealed class GammaRamp
{
    public ushort[] Red { get; } = new ushort[256];
    public ushort[] Green { get; } = new ushort[256];
    public ushort[] Blue { get; } = new ushort[256];

    public static GammaRamp Identity()
    {
        var ramp = new GammaRamp();
        for (int i = 0; i < 256; i++)
        {
            ushort v = (ushort)(i * 257);
            ramp.Red[i] = ramp.Green[i] = ramp.Blue[i] = v;
        }
        return ramp;
    }

    public ushort[] ToFlatArray()
    {
        var flat = new ushort[768];
        Array.Copy(Red, 0, flat, 0, 256);
        Array.Copy(Green, 0, flat, 256, 256);
        Array.Copy(Blue, 0, flat, 512, 256);
        return flat;
    }

    public static GammaRamp FromFlatArray(ushort[] flat)
    {
        var ramp = new GammaRamp();
        Array.Copy(flat, 0, ramp.Red, 0, 256);
        Array.Copy(flat, 256, ramp.Green, 0, 256);
        Array.Copy(flat, 512, ramp.Blue, 0, 256);
        return ramp;
    }

    public const double WindowsDeviationLimit = 0.5;

    public double MaxDeviationFromLinear()
    {
        double max = 0;
        for (int i = 0; i < 256; i++)
        {
            double linear = i * 257 / 65535.0;
            double value = Red[i] / 65535.0;
            max = Math.Max(max, Math.Abs(value - linear));
        }

        return max;
    }

    public GammaRamp ConstrainToWindowsLimit(double limit = 0.498)
    {
        double deviation = MaxDeviationFromLinear();
        if (deviation <= limit)
            return this;

        double scale = limit / deviation;
        var constrained = new GammaRamp();

        for (int i = 0; i < 256; i++)
        {
            double linear = i * 257;
            double value = linear + (Red[i] - linear) * scale;
            ushort clamped = (ushort)Math.Clamp(Math.Round(value), 0, 65535);
            constrained.Red[i] = constrained.Green[i] = constrained.Blue[i] = clamped;
        }

        return constrained;
    }

    public static GammaRamp Build(int brightness, int contrast, double gamma)
    {
        double b = (brightness - 50) / 50.0;
        double c = contrast / 50.0;
        double invGamma = 1.0 / Math.Clamp(gamma, 0.30, 2.80);

        var ramp = new GammaRamp();
        for (int i = 0; i < 256; i++)
        {
            double v = i / 255.0;
            v = (v - 0.5) * c + 0.5;
            v += b * 0.5;
            v = Math.Pow(Math.Clamp(v, 0.0, 1.0), invGamma);

            ushort value = (ushort)Math.Clamp(Math.Round(v * 65535.0), 0, 65535);
            ramp.Red[i] = ramp.Green[i] = ramp.Blue[i] = value;
        }
        return ramp;
    }
}
