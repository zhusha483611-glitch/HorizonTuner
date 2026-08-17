namespace MA_FH5Trainer.Services.Handling.Curves;

public static class VelocityCurves
{
    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * Math.Clamp(t, 0d, 1d);
    }

    private static double SmoothStep(double t)
    {
        t = Math.Clamp(t, 0d, 1d);
        return t * t * (3d - 2d * t);
    }

    public static float CalculateLinearBoost(float baseBoostValue, double scalePercent, double gamma, double u)
    {
        var scale = Math.Clamp(scalePercent, 0d, 100d) / 100d;
        var g = Math.Clamp(gamma, 1d, 4d);

        var maxDelta = Math.Clamp(baseBoostValue - 1f, 0f, 1f);
        var powerShaped = Math.Pow(Math.Clamp(u, 0d, 1d), g);
        var shaped = SmoothStep(powerShaped);
        var boost = 1d + maxDelta * scale * shaped;
        return (float)Math.Clamp(boost, 1d, 2d);
    }

    public static float CalculateMultiStageBoostWithTargets(
        float baseBoostValue,
        double stage1Gamma,
        double stage2Gamma,
        double stage3Gamma,
        double stage1End,
        double stage2End,
        double stage1TargetFrac,
        double stage2TargetFrac,
        double u)
    {
        u = double.IsFinite(u) ? Math.Clamp(u, 0d, 1d) : 0d;

        var end1 = double.IsFinite(stage1End) ? Math.Clamp(stage1End, 0.05, 0.60) : 0.30;
        var end2 = double.IsFinite(stage2End) ? Math.Clamp(stage2End, end1 + 0.05, 0.95) : 0.70;

        var g1 = Math.Clamp(SanitizeDouble(stage1Gamma, 1.1), 0.6, 4.0);
        var g2 = Math.Clamp(SanitizeDouble(stage2Gamma, 1.5), 0.8, 4.0);
        var g3 = Math.Clamp(SanitizeDouble(stage3Gamma, 2.4), 1.0, 6.0);

        var maxDelta = Math.Clamp(baseBoostValue - 1f, 0f, 1f);
        var f1 = Math.Clamp(SanitizeDouble(stage1TargetFrac, 0.75), 0.10, 0.95);
        var f2 = Math.Clamp(SanitizeDouble(stage2TargetFrac, 0.95), f1 + 0.05, 0.99);

        var b0 = 1d;
        var b1 = 1d + maxDelta * f1;
        var b2 = 1d + maxDelta * f2;
        var b3 = 1d + maxDelta;

        double boost;
        if (u <= end1)
        {
            var t = u / end1;
            var shaped = SmoothStep(Math.Pow(t, g1));
            boost = Lerp(b0, b1, shaped);
        }
        else if (u <= end2)
        {
            var t = (u - end1) / (end2 - end1);
            var shaped = SmoothStep(Math.Pow(t, g2));
            boost = Lerp(b1, b2, shaped);
        }
        else
        {
            var t = (u - end2) / (1d - end2);
            var shaped = SmoothStep(Math.Pow(t, g3));
            boost = Lerp(b2, b3, shaped);
        }

        return (float)Math.Clamp(boost, 1d, 2d);
    }

    public static float CalculateMultiStageBoost(
        float baseBoostValue,
        double stage1Gamma,
        double stage2Gamma,
        double stage3Gamma,
        double stage1Scale,
        double stage2Scale,
        double stage3Scale,
        double u)
    {
        var w1 = Math.Max(0d, SanitizeDouble(stage1Scale, 0.3));
        var w2 = Math.Max(0d, SanitizeDouble(stage2Scale, 0.5));
        var w3 = Math.Max(0d, SanitizeDouble(stage3Scale, 0.7));
        var wSum = w1 + w2 + w3;
        if (wSum <= 0d)
        {
            w1 = 0.3;
            w2 = 0.5;
            w3 = 0.7;
            wSum = w1 + w2 + w3;
        }

        var end1 = 0.30;
        var end2 = 0.70;

        var f1 = Math.Clamp(0.55 + 0.35 * (w1 / wSum), 0.55, 0.90);
        var f2 = Math.Clamp(0.85 + 0.14 * ((w1 + w2) / wSum), f1 + 0.05, 0.99);

        return CalculateMultiStageBoostWithTargets(
            baseBoostValue,
            stage1Gamma,
            stage2Gamma,
            stage3Gamma,
            end1,
            end2,
            f1,
            f2,
            u
        );
    }

    private static double SanitizeDouble(double value, double fallback)
    {
        return double.IsFinite(value) ? value : fallback;
    }
}
