using MA_FH5Trainer.Resources.Config;

namespace MA_FH5Trainer.Services.Handling.Abstractions;

public interface IHandlingAutoConfigStore
{
    HandlingAutoConfig Get();
    void Save(HandlingAutoConfig config);
}
