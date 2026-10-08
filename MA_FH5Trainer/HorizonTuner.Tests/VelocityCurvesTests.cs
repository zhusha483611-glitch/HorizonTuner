using HorizonTuner.Services.Handling.Curves;

namespace HorizonTuner.Tests;

public class VelocityCurvesTests
{
    #region CalculateLinearBoost

    [Fact]
    public void CalculateLinearBoost_ZeroU_ReturnsOne()
    {
        Assert.Equal(1f, VelocityCurves.CalculateLinearBoost(2f, 100, 1.0, 0d));
    }

    [Fact]
    public void CalculateLinearBoost_BaseBoostAtOrBelowOne_ReturnsOne()
    {
        Assert.Equal(1f, VelocityCurves.CalculateLinearBoost(1f, 100, 1.0, 1d));
        Assert.Equal(1f, VelocityCurves.CalculateLinearBoost(0.8f, 100, 1.0, 1d));
    }

    [Fact]
    public void CalculateLinearBoost_ZeroScale_ReturnsOne()
    {
        Assert.Equal(1f, VelocityCurves.CalculateLinearBoost(2f, 0, 1.0, 1d));
    }

    [Fact]
    public void CalculateLinearBoost_FullScaleAtMaxU_ReturnsMaxBoost()
    {
        Assert.Equal(2f, VelocityCurves.CalculateLinearBoost(2f, 100, 1.0, 1d));
        Assert.Equal(1.5f, VelocityCurves.CalculateLinearBoost(2f, 50, 1.0, 1d), 6);
    }

    [Fact]
    public void CalculateLinearBoost_HigherU_IsNeverLower()
    {
        var low = VelocityCurves.CalculateLinearBoost(2f, 80, 2.0, 0.2d);
        var high = VelocityCurves.CalculateLinearBoost(2f, 80, 2.0, 0.8d);

        Assert.True(high >= low);
        Assert.True(high > 1f);
    }

    #endregion

    #region CalculateMultiStageBoostWithTargets

    private const float Base = 2f;

    [Fact]
    public void CalculateMultiStageBoostWithTargets_ZeroU_ReturnsOne()
    {
        var result = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 0d);

        Assert.Equal(1f, result);
    }

    [Fact]
    public void CalculateMultiStageBoostWithTargets_AtStageEnds_ReachesTargetFractions()
    {
        var atEnd1 = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 0.30);
        var atEnd2 = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 0.70);

        Assert.Equal(1.78f, atEnd1, 4);
        Assert.Equal(1.97f, atEnd2, 4);
    }

    [Fact]
    public void CalculateMultiStageBoostWithTargets_MaxU_ReturnsMaxBoost()
    {
        var result = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 1d);

        Assert.Equal(2f, result);
    }

    [Fact]
    public void CalculateMultiStageBoostWithTargets_OutOfRangeU_Clamps()
    {
        var below = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, -1d);
        var above = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 1.5d);

        Assert.Equal(1f, below);
        Assert.Equal(2f, above);
    }

    [Fact]
    public void CalculateMultiStageBoostWithTargets_NonFiniteU_ReturnsOne()
    {
        var result = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, double.NaN);

        Assert.Equal(1f, result);
    }

    [Fact]
    public void CalculateMultiStageBoostWithTargets_BaseAtOne_AlwaysOne()
    {
        var result = VelocityCurves.CalculateMultiStageBoostWithTargets(1f, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 0.5d);

        Assert.Equal(1f, result);
    }

    [Fact]
    public void CalculateMultiStageBoostWithTargets_IsMonotonicAcrossStages()
    {
        var s1 = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 0.15);
        var s2 = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 0.50);
        var s3 = VelocityCurves.CalculateMultiStageBoostWithTargets(Base, 1.1, 1.5, 2.4, 0.30, 0.70, 0.78, 0.97, 0.85);

        Assert.True(s1 < s2);
        Assert.True(s2 < s3);
    }

    #endregion

    #region CalculateMultiStageBoost

    [Fact]
    public void CalculateMultiStageBoost_ZeroWeights_FallsBackToDefaults()
    {
        var result = VelocityCurves.CalculateMultiStageBoost(Base, 1.1, 1.5, 2.4, 0, 0, 0, 0.5d);

        // 默认权重 (0.3, 0.5, 0.7) 下结果应与等比例参数一致
        var reference = VelocityCurves.CalculateMultiStageBoost(Base, 1.1, 1.5, 2.4, 0.3, 0.5, 0.7, 0.5d);

        Assert.Equal(reference, result, 6);
    }

    [Fact]
    public void CalculateMultiStageBoost_ProportionalWeights_GiveSameBoost()
    {
        var full = VelocityCurves.CalculateMultiStageBoost(Base, 1.1, 1.5, 2.4, 30, 50, 70, 0.5d);
        var half = VelocityCurves.CalculateMultiStageBoost(Base, 1.1, 1.5, 2.4, 15, 25, 35, 0.5d);

        Assert.Equal(full, half, 6);
    }

    [Fact]
    public void CalculateMultiStageBoost_ResultStaysInSafeRange()
    {
        var result = VelocityCurves.CalculateMultiStageBoost(Base, 3.0, 1.8, 1.3, 0.3, 0.5, 0.7, 0.6d);

        Assert.InRange(result, 1f, 2f);
    }

    #endregion
}