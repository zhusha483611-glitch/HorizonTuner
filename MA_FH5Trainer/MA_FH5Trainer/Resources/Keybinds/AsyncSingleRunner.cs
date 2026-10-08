namespace HorizonTuner.Resources.Keybinds;

public sealed class AsyncSingleRunner
{
    private int _running;

    public async Task<bool> RunAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return false;
        }

        try
        {
            await action();
            return true;
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }
}
