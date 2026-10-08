using HorizonTuner.Services.Handling.Curves;

namespace HorizonTuner.Tests;

public class TriggerMathTests
{
    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(50.0, 128)]
    [InlineData(100.0, 255)]
    public void PercentToTriggerByte_ValidPercent_MapsToByte(double percent, byte expected)
    {
        Assert.Equal(expected, TriggerMath.PercentToTriggerByte(percent));
    }

    [Theory]
    [InlineData(-20.0)]
    [InlineData(150.0)]
    public void PercentToTriggerByte_OutOfRange_Clamps(double percent)
    {
        var result = TriggerMath.PercentToTriggerByte(percent);

        Assert.InRange(result, (byte)0, (byte)255);
        Assert.Equal(percent <= 0 ? (byte)0 : (byte)255, result);
    }

    [Theory]
    [InlineData(50, 100)]
    [InlineData(100, 100)]
    [InlineData(200, 255)]
    public void CalculateTriggerU_TriggerBelowOrAtThreshold_ReturnsZero(byte trigger, byte threshold)
    {
        Assert.Equal(0d, TriggerMath.CalculateTriggerU(trigger, threshold));
    }

    [Fact]
    public void CalculateTriggerU_ThresholdNearMax_ReturnsOne()
    {
        // threshold >= 254 时直接饱和为 1
        Assert.Equal(1d, TriggerMath.CalculateTriggerU(255, 254));
    }

    [Fact]
    public void CalculateTriggerU_FullPressAtMaxThreshold_ReturnsOne()
    {
        Assert.Equal(1d, TriggerMath.CalculateTriggerU(255, 255));
    }

    [Fact]
    public void CalculateTriggerU_LinearMapping_ReturnsExpected()
    {
        // (200 - 100) / (255 - 100) = 100 / 155
        var expected = 100d / 155d;
        Assert.Equal(expected, TriggerMath.CalculateTriggerU(200, 100), 9);
    }

    [Fact]
    public void CalculateTriggerU_MidRange_IsStrictlyIncreasing()
    {
        Assert.True(TriggerMath.CalculateTriggerU(180, 100) > TriggerMath.CalculateTriggerU(160, 100));
    }
}