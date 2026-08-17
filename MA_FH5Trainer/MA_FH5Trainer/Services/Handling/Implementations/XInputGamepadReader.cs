using HorizonTuner.Resources.Input;
using HorizonTuner.Services.Handling.Abstractions;

namespace HorizonTuner.Services.Handling.Implementations;

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
