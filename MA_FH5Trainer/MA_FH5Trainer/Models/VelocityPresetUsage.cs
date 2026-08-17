namespace MA_FH5Trainer.Models;

public static class VelocityPresetUsage
{
    public static void MarkUsed(VelocityPreset preset, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(preset.Id))
        {
            preset.Id = Guid.NewGuid().ToString("N");
        }

        preset.UseCount = Math.Max(0, preset.UseCount) + 1;
        preset.LastUsedTimeUtc = nowUtc;
    }
}

