using HorizonTuner.Resources.Keybinds;
using HorizonTuner.Views.Windows;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private void InitializeHotkeys()
    {
        HotkeysManager.Register(_jumpHackHotkey);
        HotkeysManager.Register(_brakeHackHotkey);
        HotkeysManager.Register(_velocityHotkey);
        HotkeysManager.Register(_wheelspeedHotkey);

        if (MainWindow.Instance != null)
        {
            var viewModel = MainWindow.Instance.ViewModel;
            AddHotkeyIfMissing(viewModel.Hotkeys, _velocityHotkey);
            AddHotkeyIfMissing(viewModel.Hotkeys, _wheelspeedHotkey);
            AddHotkeyIfMissing(viewModel.Hotkeys, _jumpHackHotkey);
            AddHotkeyIfMissing(viewModel.Hotkeys, _brakeHackHotkey);
        }
    }

    private static void AddHotkeyIfMissing(System.Collections.ObjectModel.ObservableCollection<GlobalHotkey> hotkeys, GlobalHotkey hotkey)
    {
        if (hotkeys.Any(existing => string.Equals(existing.Name, hotkey.Name, StringComparison.Ordinal)))
        {
            return;
        }

        hotkeys.Add(hotkey);
    }
}
