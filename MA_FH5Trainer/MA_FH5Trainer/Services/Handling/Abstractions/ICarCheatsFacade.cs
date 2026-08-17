namespace MA_FH5Trainer.Services.Handling.Abstractions;

public interface ICarCheatsFacade
{
    UIntPtr LocalPlayerHookDetourAddress { get; }
    UIntPtr AccelDetourAddress { get; }
    UIntPtr GravityDetourAddress { get; }
    UIntPtr NoWaterDragDetourAddress { get; }
    UIntPtr NoClipDetourAddress { get; }

    bool IsLocalPlayerHookActive();
    bool TryReapplyLocalPlayerHook();

    Task<bool> CheatLocalPlayer();
    Task<bool> CheatAccel();
    Task CheatGravity();
    Task CheatNoWaterDrag();
    Task CheatNoClip();
}
