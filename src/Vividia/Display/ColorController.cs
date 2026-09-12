using Vividia.Models;

namespace Vividia.Display;

public sealed class ColorController
{
    public bool LastValuesConstrained { get; private set; }

    public bool Apply(ColorSettings settings, IEnumerable<DisplayTarget> targets)
    {
        var ramp = GammaRamp.Build(settings.Brightness, settings.Contrast, settings.Gamma);
        LastValuesConstrained = false;

        if (!GammaRampApi.IsGammaRangeUnlocked() && ramp.MaxDeviationFromLinear() > GammaRamp.WindowsDeviationLimit)
        {
            ramp = ramp.ConstrainToWindowsLimit();
            LastValuesConstrained = true;
        }

        bool accepted = true;

        foreach (var target in targets)
        {
            accepted &= GammaRampApi.Write(target.DeviceName, ramp);

            var range = SaturationApi.GetRange(target.DeviceName);
            if (range.HasValue)
                SaturationApi.SetLevel(target.DeviceName, SaturationApi.UiToLevel(settings.Vibrance, range.Value));
        }

        return accepted;
    }

    public DisplayBaseline Capture(DisplayTarget target)
    {
        var baseline = new DisplayBaseline
        {
            Key = target.Key,
            DeviceName = target.DeviceName,
            VibranceLevel = SaturationApi.GetRange(target.DeviceName)?.Current,
        };
        baseline.SetRamp(GammaRampApi.Read(target.DeviceName));
        return baseline;
    }

    public void Restore(DisplayBaseline baseline)
    {
        var ramp = baseline.GetRamp();
        if (ramp != null)
            GammaRampApi.Write(baseline.DeviceName, ramp);

        if (baseline.VibranceLevel.HasValue)
            SaturationApi.SetLevel(baseline.DeviceName, baseline.VibranceLevel.Value);
    }
}
