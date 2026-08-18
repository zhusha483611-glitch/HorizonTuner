using System.Windows.Input;

namespace HorizonTuner.Resources.Keybinds;

public sealed class HotkeyRegistry
{
    private readonly List<GlobalHotkey> _hotkeys = [];
    private readonly object _lock = new();

    public bool Register(GlobalHotkey hotkey)
    {
        ArgumentNullException.ThrowIfNull(hotkey);

        lock (_lock)
        {
            if (_hotkeys.Any(existing => string.Equals(existing.Name, hotkey.Name, StringComparison.Ordinal)))
            {
                return false;
            }

            _hotkeys.Add(hotkey);
            return true;
        }
    }

    public bool CheckExists(Key key, ModifierKeys modifierKeys)
    {
        lock (_lock)
        {
            return _hotkeys.Any(globalHotkey => globalHotkey.Key == key && globalHotkey.Modifier == modifierKeys);
        }
    }

    public IReadOnlyList<GlobalHotkey> Snapshot()
    {
        lock (_lock)
        {
            return _hotkeys.ToArray();
        }
    }
}
