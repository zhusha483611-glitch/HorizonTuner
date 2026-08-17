namespace HorizonTuner.Services.Handling.Abstractions;

public interface IMiscCheatsFacade
{
    UIntPtr NameDetourAddress { get; }
    UIntPtr UnbreakableSkillScoreDetourAddress { get; }
    UIntPtr SkillScoreMultiplierDetourAddress { get; }

    Task CheatName();
    Task CheatUnbreakableSkillScore();
    Task CheatSkillScoreMultiplier();
}
