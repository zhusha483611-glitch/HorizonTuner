using MA_FH5Trainer.Cheats.ForzaHorizon5;
using MA_FH5Trainer.Services.Handling.Abstractions;
using static MA_FH5Trainer.Resources.Cheats;

namespace MA_FH5Trainer.Services.Handling.Implementations;

public sealed class DefaultMiscCheatsFacade : IMiscCheatsFacade
{
    private readonly MiscCheats _miscCheats = GetClass<MiscCheats>();

    public UIntPtr NameDetourAddress => _miscCheats.NameDetourAddress;
    public UIntPtr UnbreakableSkillScoreDetourAddress => _miscCheats.UnbreakableSkillScoreDetourAddress;
    public UIntPtr SkillScoreMultiplierDetourAddress => _miscCheats.SkillScoreMultiplierDetourAddress;

    public Task CheatName() => _miscCheats.CheatName();
    public Task CheatUnbreakableSkillScore() => _miscCheats.CheatUnbreakableSkillScore();
    public Task CheatSkillScoreMultiplier() => _miscCheats.CheatSkillScoreMultiplier();
}
