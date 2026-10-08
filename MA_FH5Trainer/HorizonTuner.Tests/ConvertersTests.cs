using HorizonTuner.Converters;
using System.Globalization;
using System.Windows.Data;

namespace HorizonTuner.Tests;

public class ConvertersTests
{
    #region EnabledIfAnyOnMultiConverter Tests

    [Fact]
    public void EnabledIfAnyOnMultiConverter_ValidInput_ReturnsExpected()
    {
        var converter = new EnabledIfAnyOnMultiConverter();

        // 第一个值为true，后面任意一个为true
        var result1 = converter.Convert(new object[] { true, false, true, false }, typeof(bool), null!, CultureInfo.InvariantCulture);
        Assert.True((bool)result1);

        // 第一个值为true，后面全为false
        var result2 = converter.Convert(new object[] { true, false, false, false }, typeof(bool), null!, CultureInfo.InvariantCulture);
        Assert.False((bool)result2);

        // 第一个值为false
        var result3 = converter.Convert(new object[] { false, true, true, true }, typeof(bool), null!, CultureInfo.InvariantCulture);
        Assert.False((bool)result3);
    }

    [Fact]
    public void EnabledIfAnyOnMultiConverter_InvalidInput_ReturnsFalse()
    {
        var converter = new EnabledIfAnyOnMultiConverter();

        // 少于2个值
        var result1 = converter.Convert(new object[] { true }, typeof(bool), null!, CultureInfo.InvariantCulture);
        Assert.False((bool)result1);

        // 第一个值不是bool
        var result2 = converter.Convert(new object[] { "invalid", true, false }, typeof(bool), null!, CultureInfo.InvariantCulture);
        Assert.False((bool)result2);
    }

    [Fact]
    public void EnabledIfAnyOnMultiConverter_ConvertBack_ThrowsNotImplemented()
    {
        var converter = new EnabledIfAnyOnMultiConverter();

        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack(true, new[] { typeof(bool), typeof(bool) }, null!, CultureInfo.InvariantCulture));
    }

    #endregion

    #region AndBooleanMultiConverter Tests

    [Fact]
    public void AndBooleanMultiConverter_AllTrue_ReturnsTrue()
    {
        var converter = new AndBooleanMultiConverter();

        // 注意：实际实现只接受2个值
        var result = converter.Convert(new object[] { true, true }, typeof(bool), null!, CultureInfo.InvariantCulture);

        Assert.True((bool)result);
    }

    [Fact]
    public void AndBooleanMultiConverter_OneFalse_ReturnsFalse()
    {
        var converter = new AndBooleanMultiConverter();

        var result = converter.Convert(new object[] { true, false, true }, typeof(bool), null!, CultureInfo.InvariantCulture);

        Assert.False((bool)result);
    }

    [Fact]
    public void AndBooleanMultiConverter_EmptyArray_ReturnsFalse()
    {
        var converter = new AndBooleanMultiConverter();

        var result = converter.Convert(Array.Empty<object>(), typeof(bool), null!, CultureInfo.InvariantCulture);

        Assert.False((bool)result);
    }

    #endregion

    #region MultiplyConverter Tests

    [Theory]
    [InlineData(2.0, 3.0, 6.0)]
    [InlineData(0.0, 5.0, 0.0)]
    [InlineData(-2.0, 4.0, -8.0)]
    public void MultiplyConverter_ValidInput_ReturnsProduct(double value, double parameter, double expected)
    {
        var converter = new MultiplyConverter();

        var result = converter.Convert(value, typeof(double), parameter, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void MultiplyConverter_NonNumericValue_ThrowsException()
    {
        var converter = new MultiplyConverter();

        // 实际实现会抛出异常，不是返回0
        Assert.Throws<FormatException>(() =>
            converter.Convert("invalid", typeof(double), 2.0, CultureInfo.InvariantCulture));
    }

    #endregion

    #region BoolParamConverter Tests

    [Fact]
    public void BoolParamConverter_TrueValue_ReturnsFirstString()
    {
        var converter = new BoolParamConverter();

        var result = converter.Convert(new object[] { true, "first", "second" }, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("first", result);
    }

    [Fact]
    public void BoolParamConverter_FalseValue_ReturnsSecondString()
    {
        var converter = new BoolParamConverter();

        var result = converter.Convert(new object[] { false, "first", "second" }, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("second", result);
    }

    [Fact]
    public void BoolParamConverter_InvalidInput_ReturnsEmptyObject()
    {
        var converter = new BoolParamConverter();

        var result = converter.Convert(new object[] { true }, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.IsType<object>(result);
    }

    #endregion
}