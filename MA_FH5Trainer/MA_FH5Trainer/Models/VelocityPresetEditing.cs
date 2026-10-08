namespace HorizonTuner.Models;

public static class VelocityPresetEditing
{
    public const double GammaMin = 0.1;
    public const double GammaMax = 10;
    public const double ScalePercentMin = 0;
    public const double ScalePercentMax = 100;
    public const double DefaultStage1End = 0.26;
    public const double DefaultStage2End = 0.65;
    public const double DefaultStage1TargetFrac = 0.78;
    public const double DefaultStage2TargetFrac = 0.97;

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
        return CreateNew(name, MergeCurveProfile(new VelocityPreset(), edit), createdTimeLocal);
    }

    public static VelocityPreset CreateNew(string name, VelocityCurveProfile profile, DateTime createdTimeLocal)
    {
        return new VelocityPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Stage1Gamma = profile.Stage1Gamma,
            Stage2Gamma = profile.Stage2Gamma,
            Stage3Gamma = profile.Stage3Gamma,
            Stage1Scale = profile.Stage1Scale,
            Stage2Scale = profile.Stage2Scale,
            Stage3Scale = profile.Stage3Scale,
            Stage1End = profile.Stage1End,
            Stage2End = profile.Stage2End,
            Stage1TargetFrac = profile.Stage1TargetFrac,
            Stage2TargetFrac = profile.Stage2TargetFrac,
            CreatedTime = createdTimeLocal,
            UseCount = 0,
            LastUsedTimeUtc = null
        };
    }

    public static VelocityCurveProfile GetCurveProfile(VelocityPreset preset)
    {
        return new VelocityCurveProfile(
            preset.Stage1Gamma,
            preset.Stage2Gamma,
            preset.Stage3Gamma,
            preset.Stage1Scale,
            preset.Stage2Scale,
            preset.Stage3Scale,
            preset.Stage1End,
            preset.Stage2End,
            preset.Stage1TargetFrac,
            preset.Stage2TargetFrac);
    }

    public static VelocityCurveProfile MergeCurveProfile(VelocityPreset preset, VelocityPresetEdit edit)
    {
        return new VelocityCurveProfile(
            edit.Stage1Gamma,
            edit.Stage2Gamma,
            edit.Stage3Gamma,
            ToScale01(edit.Stage1ScalePercent),
            ToScale01(edit.Stage2ScalePercent),
            ToScale01(edit.Stage3ScalePercent),
            preset.Stage1End,
            preset.Stage2End,
            preset.Stage1TargetFrac,
            preset.Stage2TargetFrac);
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

