namespace MA_FH5Trainer.Models;

public sealed record VelocityPresetEdit(
    double Stage1Gamma,
    double Stage2Gamma,
    double Stage3Gamma,
    double Stage1ScalePercent,
    double Stage2ScalePercent,
    double Stage3ScalePercent);

