using MA_FH5Trainer.Models;
using MA_FH5Trainer.ViewModels.Windows;

namespace MA_FH5Trainer.Tests;

public class PresetHotnessTests
{
    [Fact]
    public void CalculateHotScore_UseCountZero_ReturnsZero()
    {
        var preset = new VelocityPreset
        {
            UseCount = 0,
            CreatedTime = DateTime.Now
        };

        var score = PresetHotness.CalculateHotScore(preset, DateTime.UtcNow);
        Assert.Equal(0, score);
    }

    [Fact]
    public void CalculateHotScore_MoreRecent_IsHotter_WithSameUseCount()
    {
        var nowUtc = DateTime.UtcNow;

        var older = new VelocityPreset
        {
            UseCount = 10,
            CreatedTime = DateTime.Now,
            LastUsedTimeUtc = nowUtc.AddDays(-10)
        };

        var recent = new VelocityPreset
        {
            UseCount = 10,
            CreatedTime = DateTime.Now,
            LastUsedTimeUtc = nowUtc.AddHours(-1)
        };

        var olderScore = PresetHotness.CalculateHotScore(older, nowUtc);
        var recentScore = PresetHotness.CalculateHotScore(recent, nowUtc);

        Assert.True(recentScore > olderScore);
    }

    [Fact]
    public void CalculateHotScore_HigherUseCount_IsHotter_WithSameLastUsed()
    {
        var nowUtc = DateTime.UtcNow;

        var low = new VelocityPreset
        {
            UseCount = 3,
            CreatedTime = DateTime.Now,
            LastUsedTimeUtc = nowUtc.AddDays(-1)
        };

        var high = new VelocityPreset
        {
            UseCount = 12,
            CreatedTime = DateTime.Now,
            LastUsedTimeUtc = nowUtc.AddDays(-1)
        };

        var lowScore = PresetHotness.CalculateHotScore(low, nowUtc);
        var highScore = PresetHotness.CalculateHotScore(high, nowUtc);

        Assert.True(highScore > lowScore);
    }
}

