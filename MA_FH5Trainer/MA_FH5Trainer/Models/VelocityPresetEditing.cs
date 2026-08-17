namespace MA_FH5Trainer.Models;

public static class VelocityPresetEditing
{
    public const double GammaMin = 0.1;
    public const double GammaMax = 10;
    public const double ScalePercentMin = 0;
    public const double ScalePercentMax = 100;

    public static bool TryValidate(VelocityPresetEdit edit, out string error)
    {
        if (!IsBetween(edit.Stage1Gamma, GammaMin, GammaMax) ||
            !IsBetween(edit.Stage2Gamma, GammaMin, GammaMax) ||
            !IsBetween(edit.Stage3Gamma, GammaMin, GammaMax))
        {
            error = "Gamma 范围需要在 0.1 - 10 之间";
            return false;
        }

        if (!IsBetween(edit.Stage1ScalePercent, ScalePercentMin, ScalePercentMax) ||
            !IsBetween(edit.Stage2ScalePercent, ScalePercentMin, ScalePercentMax) ||
            !IsBetween(edit.Stage3ScalePercent, ScalePercentMin, ScalePercentMax))
        {
            error = "比例范围需要在 0% - 100% 之间";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void ApplyEdits(VelocityPreset preset, VelocityPresetEdit edit)
    {
        preset.Stage1Gamma = edit.Stage1Gamma;
        preset.Stage2Gamma = edit.Stage2Gamma;
        preset.Stage3Gamma = edit.Stage3Gamma;
        preset.Stage1Scale = ToScale01(edit.Stage1ScalePercent);
        preset.Stage2Scale = ToScale01(edit.Stage2ScalePercent);
        preset.Stage3Scale = ToScale01(edit.Stage3ScalePercent);
    }

    public static VelocityPreset CreateNew(string name, VelocityPresetEdit edit, DateTime createdTimeLocal)
    {
        return new VelocityPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Stage1Gamma = edit.Stage1Gamma,
            Stage2Gamma = edit.Stage2Gamma,
            Stage3Gamma = edit.Stage3Gamma,
            Stage1Scale = ToScale01(edit.Stage1ScalePercent),
            Stage2Scale = ToScale01(edit.Stage2ScalePercent),
            Stage3Scale = ToScale01(edit.Stage3ScalePercent),
            CreatedTime = createdTimeLocal,
            UseCount = 0,
            LastUsedTimeUtc = null
        };
    }

    private static double ToScale01(double percent)
    {
        var v = percent / 100d;
        return Math.Clamp(v, 0d, 1d);
    }

    private static bool IsBetween(double v, double min, double max)
    {
        return v >= min && v <= max;
    }
}

