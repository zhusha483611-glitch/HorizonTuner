namespace HorizonTuner.Services.Handling.Curves;

public static class TriggerMath
{
    public static byte PercentToTriggerByte(double percent)
    {
        var p = Math.Clamp(percent, 0d, 100d);
        return (byte)Math.Clamp((int)Math.Round(p / 100d * 255d), 0, 255);
    }

    public static double CalculateTriggerU(byte trigger, byte threshold)
    {
        if (trigger <= threshold)
        {
            return 0d;
        }

        if (threshold >= 254)
        {
            return 1d;
        }

        return Math.Clamp((trigger - threshold) / (double)(255 - threshold), 0d, 1d);
    }
}
