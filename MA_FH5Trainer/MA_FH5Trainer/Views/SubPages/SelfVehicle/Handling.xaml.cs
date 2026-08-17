using MA_FH5Trainer.Services.Handling.Implementations;
using MA_FH5Trainer.ViewModels.SubPages.SelfVehicle;
using MA_FH5Trainer.Resources.Keybinds;
using System.Windows.Input;

namespace MA_FH5Trainer.Views.SubPages.SelfVehicle;

public partial class Handling
{
    public Handling()
    {
        ViewModel = new HandlingViewModel();
        DataContext = this;

        _memoryWriter = new DefaultMemoryWriter();
        _gamepadReader = new XInputGamepadReader();
        _configStore = new DefaultHandlingAutoConfigStore();
        _carCheats = new DefaultCarCheatsFacade();
        _miscCheats = new DefaultMiscCheatsFacade();

        _jumpHackHotkey = new GlobalHotkey("Jump Hack", ModifierKeys.None, Key.None, JumpHackCallback, 1000);
        _brakeHackHotkey = new GlobalHotkey("Super Brake", ModifierKeys.None, Key.None, BrakeHackCallback, 1);
        _velocityHotkey = new GlobalHotkey("Velocity", ModifierKeys.None, Key.Q, VelocityCallback, 1);
        _wheelspeedHotkey = new GlobalHotkey("Wheelspeed", ModifierKeys.None, Key.None, WheelspeedCallback, 1);
        
        InitializeHandlingConfigSave();

        InitializeComponent();
        Loaded += Handling_OnLoaded;
        Unloaded += Handling_OnUnloaded;
        ViewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        InitializeHotkeys();
    }

    public HandlingViewModel ViewModel { get; }
}
