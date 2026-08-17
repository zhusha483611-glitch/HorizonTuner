using HorizonTuner.Resources.Config;

namespace HorizonTuner.Services.Handling.Abstractions;

public interface IHandlingAutoConfigStore
{
    HandlingAutoConfig Get();
    void Save(HandlingAutoConfig config);
}
