namespace MA_FH5Trainer.Services.Handling.Curves;

public static class BrakeCurves
{
    public static double CalculateSuperBrakeBoost(int strengthLevel, double t)
    {
        var tt = Math.Clamp(t, 0d, 1d);

        var minBoost = GetSuperBrakeMinBoost(strengthLevel);
        var boost = 1d - (1d - minBoost) * tt;
        return Math.Clamp(boost, minBoost, 1d);
    }

    public static double GetSuperBrakeMinBoost(int strengthLevel)
    {
        return strengthLevel switch
        {
            1 => 0.92d,
            2 => 0.88d,
            3 => 0.84d,
            4 => 0.80d,
            5 => 0.75d,
            _ => 0.84d
        };
    }

    public static (double Boost, string Diagnostics) CalculateBrakeAssist(
        double normalStrengthPercent,
        double panicThresholdPercent,
        double panicStrengthPercent,
        double t)
    {
        var normalStrength = Math.Clamp(normalStrengthPercent, 0d, 100d);
        var panicThreshold = Math.Clamp(panicThresholdPercent, 0d, 100d);
        var panicStrength = Math.Clamp(panicStrengthPercent, 0d, 100d);

        var t0 = Math.Clamp(panicThreshold / 100d, 0d, 1d);

        var minBoostNormal = 1d - 0.05d * (normalStrength / 100d);
        minBoostNormal = Math.Max(minBoostNormal, 0.95d);

        var minBoostPanic = 1d - 0.12d * (panicStrength / 100d);
        minBoostPanic = Math.Max(minBoostPanic, 0.88d);

        const double gammaNormal = 2.2d;
        const double gammaPanic = 1.4d;

        var eN = Math.Pow(Math.Clamp(t, 0d, 1d), gammaNormal);
        var boostN = 1d - (1d - minBoostNormal) * eN;

        var u = t0 >= 0.999d ? 0d : Math.Clamp((t - t0) / (1d - t0), 0d, 1d);
        var eP = Math.Pow(u, gammaPanic);
        var boostP = 1d - (1d - minBoostPanic) * eP;

        var w = SmoothStep(t0 - 0.03d, t0 + 0.03d, t);

        var boost = Lerp(boostN, boostP, w);
        boost = Math.Clamp(boost, minBoostPanic, 1d);

        var panicOn = t >= t0 && t0 < 0.999d;
        var diag =
            $"t={t:0.000} t0={t0:0.000} w={w:0.000} minN={minBoostNormal:0.000} minP={minBoostPanic:0.000} boostN={boostN:0.000} boostP={boostP:0.000} boost={boost:0.000} panic={(panicOn ? "ON" : "OFF")}";

        return (boost, diag);
    }

    private static double SmoothStep(double edge0, double edge1, double x)
    {
        if (edge0 >= edge1)
        {
            return x < edge0 ? 0d : 1d;
        }

        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0d, 1d);
        return t * t * (3d - 2d * t);
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }
}
