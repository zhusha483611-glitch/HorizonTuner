using System.Collections.Generic;
using System.Threading;

namespace HorizonTuner.Services;

public static class ShutdownCoordinator
{
    private static readonly object s_lock = new();
    private static readonly Dictionary<Guid, Func<Task>> s_stoppers = new();

    public static IDisposable Register(Func<Task> stopper)
    {
        ArgumentNullException.ThrowIfNull(stopper);

        lock (s_lock)
        {
            var id = Guid.NewGuid();
            s_stoppers.Add(id, stopper);
            return new Registration(id);
        }
    }

    public static async Task StopAllAsync(int timeoutMs = 3000)
    {
        List<Func<Task>> stopActions;
        lock (s_lock)
        {
            stopActions = new List<Func<Task>>(s_stoppers.Values);
        }

        if (stopActions.Count == 0)
        {
            return;
        }

        var tasks = new Task[stopActions.Count];
        for (var i = 0; i < stopActions.Count; i++)
        {
            tasks[i] = SafeInvoke(stopActions[i]);
        }

        try
        {
            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(timeoutMs)).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private static async Task SafeInvoke(Func<Task> stopper)
    {
        try
        {
            await stopper().ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private sealed class Registration : IDisposable
    {
        private readonly Guid _id;
        private int _disposed;

        public Registration(Guid id)
        {
            _id = id;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            lock (s_lock)
            {
                s_stoppers.Remove(_id);
            }
        }
    }
}
