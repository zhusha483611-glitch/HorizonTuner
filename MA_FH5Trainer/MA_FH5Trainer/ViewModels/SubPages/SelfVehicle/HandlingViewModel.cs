using CommunityToolkit.Mvvm.ComponentModel;
using MA_FH5Trainer.Models;

namespace MA_FH5Trainer.ViewModels.SubPages.SelfVehicle;

public partial class HandlingViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _areUiElementsEnabled = true;

    [ObservableProperty]
    private double _accelValue;
    
    [ObservableProperty]
    private double _gravityValue;

    [ObservableProperty]
    private bool _isAccelEnabled;
    
    [ObservableProperty]
    private bool _isGravityEnabled;

    [ObservableProperty]
    private string _gamepadStatusText = string.Empty;

    [ObservableProperty]
    private double _throttleTriggerThresholdPercent = 12;

    [ObservableProperty]
    private double _brakeTriggerThresholdPercent = 12;

    [ObservableProperty]
    private string _jumpButton = "A";

    [ObservableProperty]
    private string _brakeAssistDiagnosticsText = string.Empty;

    [ObservableProperty]
    private string _velocityDiagnosticsText = string.Empty;
}
