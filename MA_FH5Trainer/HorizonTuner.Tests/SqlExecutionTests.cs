using HorizonTuner.Cheats;
using HorizonTuner.Cheats.ForzaHorizon5;
using HorizonTuner.ViewModels.Pages;

namespace HorizonTuner.Tests;

public class SqlExecutionTests
{
    [Fact]
    public async Task AutoshowSqlRunner_WaitsForScanAndQueryCompletion()
    {
        var fake = new FakeSqlCheat
        {
            WereScansSuccessful = false
        };

        await AutoshowSqlRunner.RunAsync(fake, "SELECT 1");

        Assert.Equal(["Scan", "Query:SELECT 1"], fake.Calls);
    }

    [Fact]
    public void CleanupScope_Dispose_RunsTrackedActions()
    {
        var calls = new List<string>();

        using (var scope = new CleanupScope())
        {
            scope.Push(() => calls.Add("first"));
            scope.Push(() => calls.Add("second"));
        }

        Assert.Equal(["second", "first"], calls);
    }

    private sealed class FakeSqlCheat : ISqlCheat
    {
        public bool WereScansSuccessful { get; set; }
        public List<string> Calls { get; } = [];

        public Task SqlExecAobScan()
        {
            Calls.Add("Scan");
            WereScansSuccessful = true;
            return Task.CompletedTask;
        }

        public Task QueryAsync(string command)
        {
            Calls.Add($"Query:{command}");
            return Task.CompletedTask;
        }
    }
}
