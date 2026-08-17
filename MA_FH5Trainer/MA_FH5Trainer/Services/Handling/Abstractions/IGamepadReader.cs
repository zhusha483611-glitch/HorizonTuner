using HorizonTuner.Resources.Input;

namespace HorizonTuner.Services.Handling.Abstractions;

public interface IGamepadReader
{
    bool IsAvailable { get; }
    int? CurrentUserIndex { get; }
    bool TryGetState(out XInputState state);
    bool IsButtonDown(XInputState state, XInputButtons button);
}
