namespace HorizonTuner.Cheats;

public sealed class CleanupScope : IDisposable
{
    private readonly Stack<Action> _actions = new();

    public void Push(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Push(action);
    }

    public void Dispose()
    {
        while (_actions.Count > 0)
        {
            _actions.Pop()();
        }
    }
}
