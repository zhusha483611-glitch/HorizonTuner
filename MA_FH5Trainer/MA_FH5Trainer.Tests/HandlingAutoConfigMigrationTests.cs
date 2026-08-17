using MA_FH5Trainer.Models;
using MA_FH5Trainer.Resources.Config;

namespace MA_FH5Trainer.Tests;

public class HandlingAutoConfigMigrationTests
{
    [Fact]
    public void NormalizeConfig_NullPresetList_InitializesList()
    {
        var config = new HandlingAutoConfig
        {
            CustomVelocityPresets = null!
        };

        var changed = HandlingAutoConfigMigration.NormalizeConfig(config);

        Assert.True(changed);
        Assert.NotNull(config.CustomVelocityPresets);
    }

    [Fact]
    public void NormalizeVelocityPreset_MissingId_AssignsId()
    {
        var preset = new VelocityPreset
        {
            Id = "",
            Name = "A",
            CreatedTime = DateTime.Now
        };

        var changed = HandlingAutoConfigMigration.NormalizeVelocityPreset(preset);

        Assert.True(changed);
        Assert.False(string.IsNullOrWhiteSpace(preset.Id));
    }

    [Fact]
    public void NormalizeVelocityPreset_NegativeUseCount_ClampsToZero()
    {
        var preset = new VelocityPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "A",
            CreatedTime = DateTime.Now,
            UseCount = -10
        };

        var changed = HandlingAutoConfigMigration.NormalizeVelocityPreset(preset);

        Assert.True(changed);
        Assert.Equal(0, preset.UseCount);
    }

    [Fact]
    public void NormalizeVelocityPreset_FutureLastUsed_ClampsToNowUtc()
    {
        var preset = new VelocityPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "A",
            CreatedTime = DateTime.Now,
            UseCount = 1,
            LastUsedTimeUtc = DateTime.UtcNow.AddDays(2)
        };

        HandlingAutoConfigMigration.NormalizeVelocityPreset(preset);

        Assert.True(preset.LastUsedTimeUtc <= DateTime.UtcNow.AddSeconds(2));
    }
}

