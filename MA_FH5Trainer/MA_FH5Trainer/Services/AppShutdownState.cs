using System.Threading;

namespace MA_FH5Trainer.Services;

public static class AppShutdownState
{
    private static int s_isShuttingDown;

    public static bool IsShuttingDown => Volatile.Read(ref s_isShuttingDown) != 0;

    public static void BeginShutdown()
    {
        Interlocked.Exchange(ref s_isShuttingDown, 1);
    }
}
