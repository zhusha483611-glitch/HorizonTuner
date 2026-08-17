using MA_FH5Trainer.Models;

namespace MA_FH5Trainer.Tests;

public class VelocityPresetUsageTests
{
    [Fact]
    public void MarkUsed_AssignsId_WhenMissing()
    {
        var preset = new VelocityPreset
        {
            Id = "",
            Name = "Test",
            CreatedTime = DateTime.Now
        };

        VelocityPresetUsage.MarkUsed(preset, DateTime.UtcNow);

        Assert.False(string.IsNullOrWhiteSpace(preset.Id));
    }

    [Fact]
    public void MarkUsed_IncrementsUseCount_AndSetsLastUsed()
    {
        var nowUtc = DateTime.UtcNow;
        var preset = new VelocityPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test",
            CreatedTime = DateTime.Now,
            UseCount = 5
        };

        VelocityPresetUsage.MarkUsed(preset, nowUtc);

        Assert.Equal(6, preset.UseCount);
        Assert.Equal(nowUtc, preset.LastUsedTimeUtc);
    }

    [Fact]
    public void MarkUsed_ClampsNegativeUseCount()
    {
        var preset = new VelocityPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test",
            CreatedTime = DateTime.Now,
            UseCount = -3
        };

        VelocityPresetUsage.MarkUsed(preset, DateTime.UtcNow);

        Assert.Equal(1, preset.UseCount);
    }
}

