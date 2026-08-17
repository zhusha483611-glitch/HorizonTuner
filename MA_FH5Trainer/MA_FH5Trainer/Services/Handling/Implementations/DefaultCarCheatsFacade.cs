using HorizonTuner.Cheats.ForzaHorizon5;
using HorizonTuner.Services.Handling.Abstractions;
using static HorizonTuner.Resources.Cheats;

namespace HorizonTuner.Services.Handling.Implementations;

public sealed class DefaultCarCheatsFacade : ICarCheatsFacade
{
    private readonly CarCheats _carCheats = GetClass<CarCheats>();

    public UIntPtr LocalPlayerHookDetourAddress => _carCheats.LocalPlayerHookDetourAddress;
    public UIntPtr AccelDetourAddress => _carCheats.AccelDetourAddress;
    public UIntPtr GravityDetourAddress => _carCheats.GravityDetourAddress;
    public UIntPtr NoWaterDragDetourAddress => _carCheats.NoWaterDragDetourAddress;
    public UIntPtr NoClipDetourAddress => _carCheats.NoClipDetourAddress;

    public bool IsLocalPlayerHookActive() => _carCheats.IsLocalPlayerHookActive();
    public bool TryReapplyLocalPlayerHook() => _carCheats.TryReapplyLocalPlayerHook();

    public Task<bool> CheatLocalPlayer() => _carCheats.CheatLocalPlayer();
    public Task<bool> CheatAccel() => _carCheats.CheatAccel();
    public Task CheatGravity() => _carCheats.CheatGravity();
    public Task CheatNoWaterDrag() => _carCheats.CheatNoWaterDrag();
    public Task CheatNoClip() => _carCheats.CheatNoClip();
}
