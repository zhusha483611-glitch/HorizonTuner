using System.Windows;
using HorizonTuner.Resources.Config;
using HorizonTuner.Resources.Keybinds;
using HorizonTuner.Views.Windows;

namespace HorizonTuner.Tests;

public class UiLifecycleAndConfigTests
{
    [Fact]
    public async Task AppShutdownActions_RunAsync_ExecutesStepsInOrder()
    {
        var calls = new List<string>();

        await AppShutdownActions.RunAsync(
            () =>
            {
                calls.Add("StopAll");
                return Task.CompletedTask;
            },
            () => calls.Add("Disconnect"),
            () =>
            {
                calls.Add("StopHost");
                return Task.CompletedTask;
            },
            () => calls.Add("DisposeHost"));

        Assert.Equal(["StopAll", "Disconnect", "StopHost", "DisposeHost"], calls);
    }

    [Fact]
    public void WindowStatePersistence_NormalizeForSave_MinimizedBecomesNormal()
    {
        Assert.Equal(WindowState.Normal, WindowStatePersistence.NormalizeForSave(WindowState.Minimized));
        Assert.Equal(WindowState.Maximized, WindowStatePersistence.NormalizeForSave(WindowState.Maximized));
    }

    [Fact]
    public void HotkeyStoragePaths_UseLocalApplicationDataInsteadOfTemp()
    {
        var baseDir = @"C:\Users\Test\AppData\Local";
        var path = HotkeyStoragePaths.GetSavePath("Velocity", baseDir);

        Assert.Contains("MA_FH5Trainer", path);
        Assert.Contains("GlobalHotkeys", path);
        Assert.DoesNotContain("Temp", path, StringComparison.OrdinalIgnoreCase);
    }
}
