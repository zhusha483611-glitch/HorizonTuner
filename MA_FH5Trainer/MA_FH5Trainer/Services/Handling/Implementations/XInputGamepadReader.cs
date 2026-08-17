using MA_FH5Trainer.Resources.Input;
using MA_FH5Trainer.Services.Handling.Abstractions;

namespace MA_FH5Trainer.Services.Handling.Implementations;

public sealed class XInputGamepadReader : IGamepadReader
{
    public bool IsAvailable => XInput.IsAvailable;
    public int? CurrentUserIndex => XInput.CurrentUserIndex;

    public bool TryGetState(out XInputState state)
    {
        return XInput.TryGetState(out state);
    }

    public bool IsButtonDown(XInputState state, XInputButtons button)
    {
        return XInput.IsButtonDown(state, button);
    }
}
