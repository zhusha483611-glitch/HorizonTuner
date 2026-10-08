using System.Windows.Input;
using HorizonTuner.Resources.Keybinds;

namespace HorizonTuner.Tests;

public class HotkeyInfrastructureTests
{
    [Fact]
    public async Task AsyncSingleRunner_PreventsConcurrentExecutionUntilInnerTaskCompletes()
    {
        var runner = new AsyncSingleRunner();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;

        var first = runner.RunAsync(async () =>
        {
            Interlocked.Increment(ref callCount);
            started.SetResult();
            await release.Task;
        });

        await started.Task;
        var secondRan = await runner.RunAsync(() =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        });

        Assert.False(secondRan);
        Assert.Equal(1, callCount);

        release.SetResult();
        Assert.True(await first);
    }

    [Fact]
    public void HotkeyRegistry_Register_IgnoresDuplicateNames()
    {
        var registry = new HotkeyRegistry();
        var first = new GlobalHotkey("Velocity", ModifierKeys.None, Key.Q, () => { });
        var duplicate = new GlobalHotkey("Velocity", ModifierKeys.Control, Key.W, () => { });

        Assert.True(registry.Register(first));
        Assert.False(registry.Register(duplicate));
        Assert.Single(registry.Snapshot());
    }
}
