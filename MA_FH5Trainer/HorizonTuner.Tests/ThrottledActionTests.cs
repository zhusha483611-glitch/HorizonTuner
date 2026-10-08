using HorizonTuner.Utilities;

namespace HorizonTuner.Tests;

/// <summary>
/// ThrottledAction 的单元测试
/// 注意：由于 ThrottledAction 依赖 WPF Dispatcher，部分测试在控制台测试环境中可能无法完全验证
/// 建议在集成测试或 UI 测试环境中验证完整功能
/// </summary>
public class ThrottledActionTests
{
    [Fact]
    public void ThrottledAction_InvokeWithinInterval_Throttles()
    {
        var callCount = 0;
        var throttled = new ThrottledAction(() => callCount++, TimeSpan.FromMilliseconds(100));

        throttled.Invoke();
        throttled.Invoke();
        throttled.Invoke();

        // 第一次调用应该立即执行
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ThrottledAction_Cancel_StopsPending()
    {
        var callCount = 0;
        var throttled = new ThrottledAction(() => callCount++, TimeSpan.FromMilliseconds(100));

        throttled.Invoke();
        throttled.Cancel();

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ThrottledAction_ExceptionInAction_DoesNotThrow()
    {
        var throttled = new ThrottledAction(() => throw new InvalidOperationException("Test"), TimeSpan.FromMilliseconds(100));

        var exception = Record.Exception(() => throttled.Invoke());

        Assert.Null(exception);
    }

    [Fact]
    public void ThrottledAction_Generic_InvokeWithinInterval_Throttles()
    {
        var lastValue = 0;
        var throttled = new ThrottledAction<int>(v => lastValue = v, TimeSpan.FromMilliseconds(100));

        throttled.Invoke(1);
        throttled.Invoke(2);
        throttled.Invoke(3);

        Assert.Equal(1, lastValue);
    }

    [Fact]
    public void ThrottledAction_Generic_Cancel_StopsPending()
    {
        var lastValue = 0;
        var throttled = new ThrottledAction<int>(v => lastValue = v, TimeSpan.FromMilliseconds(100));

        throttled.Invoke(1);
        throttled.Invoke(2);
        throttled.Cancel();

        Assert.Equal(1, lastValue);
    }
}