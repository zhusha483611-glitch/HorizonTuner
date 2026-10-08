using HorizonTuner.Cheats;

namespace HorizonTuner.Resources;

public static class CheatLifecycle
{
    public static void CleanupAndResetAll(IEnumerable<object> instances)
    {
        foreach (var instance in instances)
        {
            if (instance is not ICheatsBase cheat)
            {
                continue;
            }

            cheat.Cleanup();
            cheat.Reset();
        }
    }
}
