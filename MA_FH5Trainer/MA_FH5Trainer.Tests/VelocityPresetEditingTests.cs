using MA_FH5Trainer.Models;

namespace MA_FH5Trainer.Tests;

public class VelocityPresetEditingTests
{
    [Fact]
    public void TryValidate_InvalidGamma_Fails()
    {
        var edit = new VelocityPresetEdit(
            0.01,
            1.0,
            1.0,
            30,
            50,
            70);

        var ok = VelocityPresetEditing.TryValidate(edit, out var error);

        Assert.False(ok);
        Assert.Contains("Gamma", error);
    }

    [Fact]
    public void TryValidate_InvalidScalePercent_Fails()
    {
        var edit = new VelocityPresetEdit(
            1.0,
            1.0,
            1.0,
            -1,
            50,
            70);

        var ok = VelocityPresetEditing.TryValidate(edit, out var error);

        Assert.False(ok);
        Assert.Contains("比例", error);
    }

    [Fact]
    public void ApplyEdits_DoesNotTouchStatsFields()
    {
        var preset = new VelocityPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "P",
            Stage1Gamma = 3,
            Stage2Gamma = 2,
            Stage3Gamma = 1,
            Stage1Scale = 0.3,
            Stage2Scale = 0.5,
            Stage3Scale = 0.7,
            CreatedTime = DateTime.Now.AddDays(-10),
            UseCount = 12,
            LastUsedTimeUtc = DateTime.UtcNow.AddHours(-2)
        };

        var id = preset.Id;
        var useCount = preset.UseCount;
        var lastUsed = preset.LastUsedTimeUtc;
        var created = preset.CreatedTime;

        var edit = new VelocityPresetEdit(1.1, 1.2, 1.3, 10, 20, 30);
        VelocityPresetEditing.ApplyEdits(preset, edit);

        Assert.Equal(id, preset.Id);
        Assert.Equal(useCount, preset.UseCount);
        Assert.Equal(lastUsed, preset.LastUsedTimeUtc);
        Assert.Equal(created, preset.CreatedTime);
        Assert.Equal(1.1, preset.Stage1Gamma);
        Assert.Equal(0.10, preset.Stage1Scale, 3);
    }

    [Fact]
    public void CreateNew_ResetsStatsAndGeneratesId()
    {
        var edit = new VelocityPresetEdit(1.1, 1.2, 1.3, 10, 20, 30);
        var created = DateTime.Now;
        var preset = VelocityPresetEditing.CreateNew("X", edit, created);

        Assert.False(string.IsNullOrWhiteSpace(preset.Id));
        Assert.Equal("X", preset.Name);
        Assert.Equal(0, preset.UseCount);
        Assert.Null(preset.LastUsedTimeUtc);
        Assert.Equal(created, preset.CreatedTime);
        Assert.Equal(0.20, preset.Stage2Scale, 3);
    }
}

