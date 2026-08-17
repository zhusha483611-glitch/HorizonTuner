using HorizonTuner.Resources.Config;
using HorizonTuner.Services.Handling.Abstractions;

namespace HorizonTuner.Services.Handling.Implementations;

public sealed class DefaultHandlingAutoConfigStore : IHandlingAutoConfigStore
{
    public HandlingAutoConfig Get()
    {
        return HandlingAutoConfigManager.Get();
    }

    public void Save(HandlingAutoConfig config)
    {
        HandlingAutoConfigManager.Save(config);
    }
}
