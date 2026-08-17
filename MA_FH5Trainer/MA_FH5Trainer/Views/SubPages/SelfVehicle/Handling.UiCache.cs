using HorizonTuner.Services.Handling.Curves;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private double GetDetourApplyHzEstimate()
    {
        var hz = _detourApplyHzEma;
        if (!double.IsFinite(hz) || hz <= 0d)
        {
            return 60d;
        }

        return Math.Clamp(hz, 20d, 240d);
    }

    private void UpdateDetourApplyHzEstimate(int applyCounter, DateTime nowUtc)
    {
        if (nowUtc < _detourApplyHzLastUtc)
        {
            _detourApplyHzLastUtc = nowUtc;
            _detourApplyHzLastCounter = applyCounter;
            return;
        }

        var dt = (nowUtc - _detourApplyHzLastUtc).TotalSeconds;
        if (dt <= 0.05d)
        {
            return;
        }

        var delta = applyCounter - _detourApplyHzLastCounter;
        if (delta < 0)
        {
            _detourApplyHzLastUtc = nowUtc;
            _detourApplyHzLastCounter = applyCounter;
            return;
        }

        var hzInstant = delta / dt;
        if (double.IsFinite(hzInstant) && hzInstant > 0d)
        {
            hzInstant = Math.Clamp(hzInstant, 20d, 240d);
            const double alpha = 0.20d;
            _detourApplyHzEma = _detourApplyHzEma * (1d - alpha) + hzInstant * alpha;
        }

        _detourApplyHzLastUtc = nowUtc;
        _detourApplyHzLastCounter = applyCounter;
    }

    private float ToBoostPerApply(float boostPerSecond)
    {
        var bps = double.IsFinite(boostPerSecond) ? Math.Clamp(boostPerSecond, 1f, 2f) : 1f;
        var hz = GetDetourApplyHzEstimate();
        var delta = bps - 1d;
        var perApply = Expm1(Log1p(delta) / hz) + 1d;
        return (float)Math.Clamp(perApply, 1d, 2d);
    }

    private static double Log1p(double x)
    {
        if (!double.IsFinite(x))
        {
            return double.NaN;
        }

        if (x == 0d)
        {
            return 0d;
        }

        if (x <= -1d)
        {
            return double.NegativeInfinity;
        }

        if (Math.Abs(x) < 1e-4)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            var x4 = x3 * x;
            var x5 = x4 * x;
            return x - x2 / 2d + x3 / 3d - x4 / 4d + x5 / 5d;
        }

        return Math.Log(1d + x);
    }

    private static double Expm1(double x)
    {
        if (!double.IsFinite(x))
        {
            return x;
        }

        if (x == 0d)
        {
            return 0d;
        }

        if (Math.Abs(x) < 1e-4)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            var x4 = x3 * x;
            var x5 = x4 * x;
            return x + x2 / 2d + x3 / 6d + x4 / 24d + x5 / 120d;
        }

        return Math.Exp(x) - 1d;
    }

    private static float KmhToMph(float kmh)
    {
        return kmh * 0.621371f;
    }

    private static double ClampPercent(double value)
    {
        return Math.Clamp(value, 0, 100);
    }

    private static double SanitizeDouble(double value, double fallback)
    {
        return double.IsFinite(value) ? value : fallback;
    }

    private byte GetThrottleThresholdByte()
    {
        var percent = ClampPercent(ViewModel.ThrottleTriggerThresholdPercent);
        return TriggerMath.PercentToTriggerByte(percent);
    }

    private byte GetBrakeThresholdByte()
    {
        var percent = ClampPercent(ViewModel.BrakeTriggerThresholdPercent);
        return TriggerMath.PercentToTriggerByte(percent);
    }

    private void UpdateCachedUiValues()
    {
        UpdateCachedVelocityValues();
        UpdateCachedWheelspeedValues();
        UpdateCachedJumpValue();
        UpdateCachedSuperBrakeStrength();

        _brakeAssistNormalStrengthPercent = ClampPercent(BrakeAssistNormalStrengthBox.Value ?? 0);
        _brakeAssistPanicThresholdPercent = ClampPercent(BrakeAssistPanicThresholdBox.Value ?? 0);
        _brakeAssistPanicStrengthPercent = ClampPercent(BrakeAssistPanicStrengthBox.Value ?? 0);
        _velocityLinearScalePercent = ClampPercent(VelLinearScale.Value ?? 40);
        _velocityLinearGamma = Math.Clamp(VelLinearGamma.Value ?? 2.2, 1.0, 4.0);

        _velocityStage1Gamma = SanitizeDouble(VelStage1Gamma.Value ?? 3.0, 3.0);
        _velocityStage2Gamma = SanitizeDouble(VelStage2Gamma.Value ?? 1.8, 1.8);
        _velocityStage3Gamma = SanitizeDouble(VelStage3Gamma.Value ?? 1.3, 1.3);
        _velocityStage1Scale = SanitizeDouble((VelStage1Scale.Value ?? 30) / 100d, 0.3);
        _velocityStage2Scale = SanitizeDouble((VelStage2Scale.Value ?? 50) / 100d, 0.5);
        _velocityStage3Scale = SanitizeDouble((VelStage3Scale.Value ?? 70) / 100d, 0.7);
    }

    private void UpdateCachedVelocityValues()
    {
        var delta = VelocityStrength.Value ?? 0d;
        delta = double.IsFinite(delta) ? Math.Clamp(delta, 0d, 1d) : 0d;
        _velocityBoostValue = 1f + Convert.ToSingle(delta);
        _velocityLimitValue = Convert.ToSingle(VelocityLimit.Value);
    }

    private void UpdateCachedWheelspeedValues()
    {
        _wheelspeedModeValue = (byte)WheelspeedModeBox.SelectedIndex;
        _wheelspeedBoostValue = Convert.ToSingle(WheelspeedValueBox.Value);
        _wheelspeedLimitValue = Convert.ToSingle(WheelspeedLimit.Value);
    }

    private void UpdateCachedJumpValue()
    {
        _jumpBoostValue = Convert.ToSingle(JumpSlider.Value);
    }

    private void UpdateCachedSuperBrakeStrength()
    {
        _superBrakeStrengthLevel = (int)Math.Round(StopSlider.Value);
    }
}
