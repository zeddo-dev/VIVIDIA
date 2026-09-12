namespace Vividia.Display;

public sealed class DisplayBaseline
{
    public string Key { get; set; } = "";
    public string DeviceName { get; set; } = "";

    public string? RampBase64 { get; set; }

    public int? VibranceLevel { get; set; }

    public GammaRamp? GetRamp()
    {
        if (string.IsNullOrEmpty(RampBase64))
            return null;

        var bytes = Convert.FromBase64String(RampBase64);
        if (bytes.Length != 768 * 2)
            return null;

        var flat = new ushort[768];
        Buffer.BlockCopy(bytes, 0, flat, 0, bytes.Length);
        return GammaRamp.FromFlatArray(flat);
    }

    public void SetRamp(GammaRamp? ramp)
    {
        if (ramp == null)
        {
            RampBase64 = null;
            return;
        }

        var flat = ramp.ToFlatArray();
        var bytes = new byte[flat.Length * 2];
        Buffer.BlockCopy(flat, 0, bytes, 0, bytes.Length);
        RampBase64 = Convert.ToBase64String(bytes);
    }
}
