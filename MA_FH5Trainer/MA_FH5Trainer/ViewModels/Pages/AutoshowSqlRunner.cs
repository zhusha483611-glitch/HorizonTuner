using HorizonTuner.Cheats.ForzaHorizon5;

namespace HorizonTuner.ViewModels.Pages;

public static class AutoshowSqlRunner
{
    public static async Task RunAsync(ISqlCheat sqlCheat, string command)
    {
        ArgumentNullException.ThrowIfNull(sqlCheat);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        if (!sqlCheat.WereScansSuccessful)
        {
            await sqlCheat.SqlExecAobScan();
        }

        if (sqlCheat.WereScansSuccessful)
        {
            await sqlCheat.QueryAsync(command);
        }
    }
}
