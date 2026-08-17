using System.Windows.Threading;

namespace MA_FH5Trainer.Utilities;

public class ThrottledAction
{
    private readonly Action _action;
    private readonly TimeSpan _interval;
    private readonly DispatcherTimer _timer;
    private bool _isPending;

    public ThrottledAction(Action action, TimeSpan interval)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _interval = interval;
        _timer = new DispatcherTimer(interval, DispatcherPriority.Background, OnTimerTick, Dispatcher.CurrentDispatcher);
        _timer.Stop();
    }

    public void Invoke()
    {
        if (_timer.IsEnabled)
        {
            _isPending = true;
            return;
        }

        ExecuteAction();
        _timer.Start();
    }

    public void Cancel()
    {
        _timer.Stop();
        _isPending = false;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Stop();

        if (_isPending)
        {
            _isPending = false;
            ExecuteAction();
            _timer.Start();
        }
    }

    private void ExecuteAction()
    {
        try
        {
            _action();
        }
        catch
        {
            // 节流操作中的异常不应影响主流程
        }
    }
}

public class ThrottledAction<T>
{
    private readonly Action<T> _action;
    private readonly TimeSpan _interval;
    private readonly DispatcherTimer _timer;
    private T? _pendingValue;
    private bool _hasPendingValue;

    public ThrottledAction(Action<T> action, TimeSpan interval)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _interval = interval;
        _timer = new DispatcherTimer(interval, DispatcherPriority.Background, OnTimerTick, Dispatcher.CurrentDispatcher);
        _timer.Stop();
    }

    public void Invoke(T value)
    {
        if (_timer.IsEnabled)
        {
            _pendingValue = value;
            _hasPendingValue = true;
            return;
        }

        ExecuteAction(value);
        _timer.Start();
    }

    public void Cancel()
    {
        _timer.Stop();
        _hasPendingValue = false;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Stop();

        if (_hasPendingValue)
        {
            _hasPendingValue = false;
            ExecuteAction(_pendingValue!);
            _timer.Start();
        }
    }

    private void ExecuteAction(T value)
    {
        try
        {
            _action(value);
        }
        catch
        {
            // 节流操作中的异常不应影响主流程
        }
    }
}
