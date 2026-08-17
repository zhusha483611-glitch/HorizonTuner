using MA_FH5Trainer.Resources.Keybinds;
using MA_FH5Trainer.Views.Windows;

namespace MA_FH5Trainer.Views.SubPages.SelfVehicle;

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
            viewModel.Hotkeys.Add(_velocityHotkey);
            viewModel.Hotkeys.Add(_wheelspeedHotkey);
            viewModel.Hotkeys.Add(_jumpHackHotkey);
            viewModel.Hotkeys.Add(_brakeHackHotkey);
        }
    }
}
