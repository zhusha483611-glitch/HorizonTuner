using System;
using HorizonTuner.Models;

namespace HorizonTuner.Resources.Config;

public static class HandlingAutoConfigMigration
{
    public static bool NormalizeConfig(HandlingAutoConfig config)
    {
        var changed = false;

        var chaseKp = double.IsFinite(config.VelocityChaseKp) ? config.VelocityChaseKp : 1.2;
        chaseKp = Math.Clamp(chaseKp, 0.1, 10.0);
        if (chaseKp != config.VelocityChaseKp)
        {
            config.VelocityChaseKp = chaseKp;
            changed = true;
        }

        var chaseMinDelta = double.IsFinite(config.VelocityChaseMinDelta) ? config.VelocityChaseMinDelta : 0.01;
        chaseMinDelta = Math.Clamp(chaseMinDelta, 0.0, 1.0);
        if (chaseMinDelta != config.VelocityChaseMinDelta)
        {
            config.VelocityChaseMinDelta = chaseMinDelta;
            changed = true;
        }

        changed |= ApplyVelocityCurveNormalization(
            config,
            config.VelocityStage1End,
            config.VelocityStage2End,
            config.VelocityStage1TargetFrac,
            config.VelocityStage2TargetFrac,
            0.26,
            0.65,
            0.78,
            0.97,
            (c, s1, s2, f1, f2) =>
            {
                c.VelocityStage1End = s1;
                c.VelocityStage2End = s2;
                c.VelocityStage1TargetFrac = f1;
                c.VelocityStage2TargetFrac = f2;
            }
        );

        changed |= ApplyVelocityCurveNormalization(
            config,
            config.VelocityS1Stage1End,
            config.VelocityS1Stage2End,
            config.VelocityS1Stage1TargetFrac,
            config.VelocityS1Stage2TargetFrac,
            0.28,
            0.68,
            0.72,
            0.95,
            (c, s1, s2, f1, f2) =>
            {
                c.VelocityS1Stage1End = s1;
                c.VelocityS1Stage2End = s2;
                c.VelocityS1Stage1TargetFrac = f1;
                c.VelocityS1Stage2TargetFrac = f2;
            }
        );

        changed |= ApplyVelocityCurveNormalization(
            config,
            config.VelocityS2Stage1End,
            config.VelocityS2Stage2End,
            config.VelocityS2Stage1TargetFrac,
            config.VelocityS2Stage2TargetFrac,
            0.26,
            0.65,
            0.78,
            0.97,
            (c, s1, s2, f1, f2) =>
            {
                c.VelocityS2Stage1End = s1;
                c.VelocityS2Stage2End = s2;
                c.VelocityS2Stage1TargetFrac = f1;
                c.VelocityS2Stage2TargetFrac = f2;
            }
        );

        changed |= ApplyVelocityCurveNormalization(
            config,
            config.VelocityAStage1End,
            config.VelocityAStage2End,
            config.VelocityAStage1TargetFrac,
            config.VelocityAStage2TargetFrac,
            0.22,
            0.60,
            0.84,
            0.98,
            (c, s1, s2, f1, f2) =>
            {
                c.VelocityAStage1End = s1;
                c.VelocityAStage2End = s2;
                c.VelocityAStage1TargetFrac = f1;
                c.VelocityAStage2TargetFrac = f2;
            }
        );

        if (config.CustomVelocityPresets == null)
        {
            config.CustomVelocityPresets = new List<VelocityPreset>();
            changed = true;
        }

        foreach (var preset in config.CustomVelocityPresets)
        {
            changed |= NormalizeVelocityPreset(preset);
        }

        return changed;
    }

    private static bool ApplyVelocityCurveNormalization(
        HandlingAutoConfig config,
        double stage1End,
        double stage2End,
        double stage1TargetFrac,
        double stage2TargetFrac,
        double defaultStage1End,
        double defaultStage2End,
        double defaultStage1TargetFrac,
        double defaultStage2TargetFrac,
        Action<HandlingAutoConfig, double, double, double, double> setter)
    {
        var normalized = NormalizeVelocityCurveParams(
            stage1End,
            stage2End,
            stage1TargetFrac,
            stage2TargetFrac,
            defaultStage1End,
            defaultStage2End,
            defaultStage1TargetFrac,
            defaultStage2TargetFrac
        );

        if (!normalized.Changed)
        {
            return false;
        }

        setter(config, normalized.Stage1End, normalized.Stage2End, normalized.Stage1TargetFrac, normalized.Stage2TargetFrac);
        return true;
    }

    private static (double Stage1End, double Stage2End, double Stage1TargetFrac, double Stage2TargetFrac, bool Changed) NormalizeVelocityCurveParams(
        double stage1End,
        double stage2End,
        double stage1TargetFrac,
        double stage2TargetFrac,
        double defaultStage1End,
        double defaultStage2End,
        double defaultStage1TargetFrac,
        double defaultStage2TargetFrac)
    {
        var s1 = double.IsFinite(stage1End) ? stage1End : defaultStage1End;
        var s2 = double.IsFinite(stage2End) ? stage2End : defaultStage2End;
        s1 = Math.Clamp(s1, 0.05, 0.60);
        s2 = Math.Clamp(s2, s1 + 0.05, 0.95);

        var f1 = double.IsFinite(stage1TargetFrac) ? stage1TargetFrac : defaultStage1TargetFrac;
        var f2 = double.IsFinite(stage2TargetFrac) ? stage2TargetFrac : defaultStage2TargetFrac;
        f1 = Math.Clamp(f1, 0.10, 0.94);
        f2 = Math.Clamp(f2, f1 + 0.05, 0.99);

        var changed = s1 != stage1End ||
                      s2 != stage2End ||
                      f1 != stage1TargetFrac ||
                      f2 != stage2TargetFrac;

        return (s1, s2, f1, f2, changed);
    }

    public static bool NormalizeVelocityPreset(VelocityPreset preset)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(preset.Id))
        {
            preset.Id = Guid.NewGuid().ToString("N");
            changed = true;
        }

        if (preset.CreatedTime == default)
        {
            preset.CreatedTime = DateTime.Now;
            changed = true;
        }

        if (preset.UseCount < 0)
        {
            preset.UseCount = 0;
            changed = true;
        }

        var normalizedCurve = NormalizeVelocityCurveParams(
            preset.Stage1End,
            preset.Stage2End,
            preset.Stage1TargetFrac,
            preset.Stage2TargetFrac,
            VelocityPresetEditing.DefaultStage1End,
            VelocityPresetEditing.DefaultStage2End,
            VelocityPresetEditing.DefaultStage1TargetFrac,
            VelocityPresetEditing.DefaultStage2TargetFrac);

        if (normalizedCurve.Changed)
        {
            preset.Stage1End = normalizedCurve.Stage1End;
            preset.Stage2End = normalizedCurve.Stage2End;
            preset.Stage1TargetFrac = normalizedCurve.Stage1TargetFrac;
            preset.Stage2TargetFrac = normalizedCurve.Stage2TargetFrac;
            changed = true;
        }

        if (preset.LastUsedTimeUtc is { } lastUsedUtc)
        {
            var utc = lastUsedUtc.Kind == DateTimeKind.Utc ? lastUsedUtc : DateTime.SpecifyKind(lastUsedUtc, DateTimeKind.Utc);
            if (utc > DateTime.UtcNow.AddDays(1))
            {
                utc = DateTime.UtcNow;
            }

            if (utc != preset.LastUsedTimeUtc)
            {
                preset.LastUsedTimeUtc = utc;
                changed = true;
            }
        }

        return changed;
    }
}
