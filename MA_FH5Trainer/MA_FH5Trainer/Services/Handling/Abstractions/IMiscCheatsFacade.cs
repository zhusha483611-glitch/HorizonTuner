namespace MA_FH5Trainer.Services.Handling.Abstractions;

public interface IMiscCheatsFacade
{
    UIntPtr NameDetourAddress { get; }
    UIntPtr UnbreakableSkillScoreDetourAddress { get; }
    UIntPtr SkillScoreMultiplierDetourAddress { get; }

    Task CheatName();
    Task CheatUnbreakableSkillScore();
    Task CheatSkillScoreMultiplier();
}
