namespace HorizonTuner;

public static class AppShutdownActions
{
    public static async Task RunAsync(Func<Task> stopAllAsync, Action disconnectFromGame, Func<Task> stopHostAsync, Action disposeHost)
    {
        ArgumentNullException.ThrowIfNull(stopAllAsync);
        ArgumentNullException.ThrowIfNull(disconnectFromGame);
        ArgumentNullException.ThrowIfNull(stopHostAsync);
        ArgumentNullException.ThrowIfNull(disposeHost);

        await stopAllAsync();
        disconnectFromGame();
        await stopHostAsync();
        disposeHost();
    }
}
