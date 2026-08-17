using MA_FH5Trainer.Resources.Config;
using MA_FH5Trainer.Services.Handling.Abstractions;

namespace MA_FH5Trainer.Services.Handling.Implementations;

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
