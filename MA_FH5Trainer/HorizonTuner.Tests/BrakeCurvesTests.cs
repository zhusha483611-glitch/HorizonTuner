using HorizonTuner.Services.Handling.Curves;

namespace HorizonTuner.Tests;

public class BrakeCurvesTests
{
    #region GetSuperBrakeMinBoost

    [Theory]
    [InlineData(1, 0.92)]
    [InlineData(2, 0.88)]
    [InlineData(3, 0.84)]
    [InlineData(4, 0.80)]
    [InlineData(5, 0.75)]
    public void GetSuperBrakeMinBoost_KnownLevels_ReturnExpected(int level, double expected)
    {
        Assert.Equal(expected, BrakeCurves.GetSuperBrakeMinBoost(level), 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void GetSuperBrakeMinBoost_UnknownLevel_FallsBackToDefault(int level)
    {
        Assert.Equal(0.84, BrakeCurves.GetSuperBrakeMinBoost(level), 6);
    }

    #endregion

    #region CalculateSuperBrakeBoost

    [Fact]
    public void CalculateSuperBrakeBoost_ZeroT_ReturnsOne()
    {
        Assert.Equal(1d, BrakeCurves.CalculateSuperBrakeBoost(3, 0d), 6);
    }

    [Fact]
    public void CalculateSuperBrakeBoost_FullT_ReturnsMinBoost()
    {
        Assert.Equal(0.84d, BrakeCurves.CalculateSuperBrakeBoost(3, 1d), 6);
        Assert.Equal(0.75d, BrakeCurves.CalculateSuperBrakeBoost(5, 1d), 6);
    }

    [Fact]
    public void CalculateSuperBrakeBoost_OutOfRangeT_Clamps()
    {
        Assert.Equal(1d, BrakeCurves.CalculateSuperBrakeBoost(3, -0.5d), 6);
        Assert.Equal(0.84d, BrakeCurves.CalculateSuperBrakeBoost(3, 1.5d), 6);
    }

    [Fact]
    public void CalculateSuperBrakeBoost_MidT_IsLinearInterpolation()
    {
        // level 3: min=0.84, t=0.5 → 1 - 0.16*0.5 = 0.92
        Assert.Equal(0.92d, BrakeCurves.CalculateSuperBrakeBoost(3, 0.5d), 6);
    }

    #endregion

    #region CalculateBrakeAssist

    private const double NormalStrength = 20;
    private const double PanicThreshold = 85;
    private const double PanicStrength = 60;

    [Fact]
    public void CalculateBrakeAssist_ZeroT_ReturnsOne()
    {
        var (boost, _) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, 0d);

        Assert.Equal(1d, boost, 6);
    }

    [Fact]
    public void CalculateBrakeAssist_FullT_ReachesPanicMinBoost()
    {
        var (boost, _) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, 1d);

        // minBoostPanic = max(1 - 0.12 * 0.60, 0.88) = 0.928
        Assert.Equal(0.928d, boost, 6);
    }

    [Fact]
    public void CalculateBrakeAssist_OutOfRangeT_Clamps()
    {
        var (below, _) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, -1d);
        var (above, _) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, 2d);

        Assert.Equal(1d, below, 6);
        Assert.Equal(0.928d, above, 6);
    }

    [Fact]
    public void CalculateBrakeAssist_PanicThresholdReached_ReportsPanicOn()
    {
        var (_, diag) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, 0.9d);

        Assert.Contains("panic=ON", diag);
    }

    [Fact]
    public void CalculateBrakeAssist_BelowPanicThreshold_ReportsPanicOff()
    {
        var (_, diag) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, 0.5d);

        Assert.Contains("panic=OFF", diag);
    }

    [Fact]
    public void CalculateBrakeAssist_PanicZone_LowerBoostThanNormalZone()
    {
        var (before, _) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, 0.84d);
        var (after, _) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, 0.90d);

        Assert.True(after < before);
    }

    [Fact]
    public void CalculateBrakeAssist_AlwaysAtOrBelowOne()
    {
        var (boost, _) = BrakeCurves.CalculateBrakeAssist(NormalStrength, PanicThreshold, PanicStrength, 0.6d);

        Assert.InRange(boost, 0d, 1d);
    }

    #endregion
}