using MA_FH5Trainer.Resources.Input;

namespace MA_FH5Trainer.Services.Handling.Abstractions;

public interface IGamepadReader
{
    bool IsAvailable { get; }
    int? CurrentUserIndex { get; }
    bool TryGetState(out XInputState state);
    bool IsButtonDown(XInputState state, XInputButtons button);
}
