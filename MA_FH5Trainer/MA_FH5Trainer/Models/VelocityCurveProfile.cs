namespace HorizonTuner.Models;

public sealed record VelocityCurveProfile(
    double Stage1Gamma,
    double Stage2Gamma,
    double Stage3Gamma,
    double Stage1Scale,
    double Stage2Scale,
    double Stage3Scale,
    double Stage1End,
    double Stage2End,
    double Stage1TargetFrac,
    double Stage2TargetFrac);
