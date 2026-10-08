namespace HorizonTuner.Cheats.ForzaHorizon5;

public interface ISqlCheat
{
    bool WereScansSuccessful { get; }
    Task SqlExecAobScan();
    Task QueryAsync(string command);
}
