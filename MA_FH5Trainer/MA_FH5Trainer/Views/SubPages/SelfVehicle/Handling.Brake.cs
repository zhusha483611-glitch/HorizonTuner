using System.Windows;
using HorizonTuner.Cheats.ForzaHorizon5;
using HorizonTuner.Services.Handling.Curves;
using MahApps.Metro.Controls;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private void StopSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // 保存到配置
        var config = _configStore.Get();
        config.SuperBrakeValue = Math.Clamp(e.NewValue, 1.0, 5.0);
        ScheduleHandlingConfigSave(config);

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero) return;
        UpdateCachedSuperBrakeStrength();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.BrakeHackBoost, (float)CalculateSuperBrakeBoost(1d));
    }

    private async void StopSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        if (toggleSwitch.IsOn && BrakeAssistToggle.IsOn)
        {
            BrakeAssistToggle.IsOn = false;
        }

        ViewModel.AreUiElementsEnabled = false;
        if (_carCheats.LocalPlayerHookDetourAddress == 0)
        {
            await _carCheats.CheatLocalPlayer();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            toggleSwitch.Toggled -= StopSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += StopSwitch_OnToggled;
            return;
        }

        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.BrakeHackBoost, (float)CalculateSuperBrakeBoost(1d));
        _brakeHackHotkey.CanExecute = toggleSwitch.IsOn;

        var config = _configStore.Get();
        config.BrakeAutoOn = toggleSwitch.IsOn;
        _configStore.Save(config);

        if (toggleSwitch.IsOn)
        {
            StartBrakeAutoLoop();
        }
        else
        {
            StopBrakeAuto();
        }
    }

    private async void BrakeAssistSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        if (toggleSwitch.IsOn && StopToggle.IsOn)
        {
            StopToggle.IsOn = false;
        }

        if (toggleSwitch.IsOn)
        {
            ViewModel.AreUiElementsEnabled = false;
            if (_carCheats.LocalPlayerHookDetourAddress == 0)
            {
                await _carCheats.CheatLocalPlayer();
            }
            ViewModel.AreUiElementsEnabled = true;

            if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
            {
                toggleSwitch.Toggled -= BrakeAssistSwitch_OnToggled;
                toggleSwitch.IsOn = false;
                toggleSwitch.Toggled += BrakeAssistSwitch_OnToggled;
                return;
            }

            StartBrakeAssistAutoLoop();
        }
        else
        {
            StopBrakeAssistAuto();
            ViewModel.BrakeAssistDiagnosticsText = string.Empty;
        }

        var config = _configStore.Get();
        config.BrakeAssistAutoOn = toggleSwitch.IsOn;
        _configStore.Save(config);
    }

    private void BrakeAssistNormalStrength_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        var config = _configStore.Get();
        config.BrakeAssistNormalStrengthPercent = ClampPercent(e.NewValue ?? 0);
        ScheduleHandlingConfigSave(config);
        _brakeAssistNormalStrengthPercent = config.BrakeAssistNormalStrengthPercent;
    }

    private void BrakeAssistPanicThreshold_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        var config = _configStore.Get();
        config.BrakeAssistPanicThresholdPercent = ClampPercent(e.NewValue ?? 0);
        ScheduleHandlingConfigSave(config);
        _brakeAssistPanicThresholdPercent = config.BrakeAssistPanicThresholdPercent;
    }

    private void BrakeAssistPanicStrength_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        var config = _configStore.Get();
        config.BrakeAssistPanicStrengthPercent = ClampPercent(e.NewValue ?? 0);
        ScheduleHandlingConfigSave(config);
        _brakeAssistPanicStrengthPercent = config.BrakeAssistPanicStrengthPercent;
    }

    private double CalculateSuperBrakeBoost(double t)
    {
        return BrakeCurves.CalculateSuperBrakeBoost(_superBrakeStrengthLevel, t);
    }

    private double GetSuperBrakeMinBoost()
    {
        return BrakeCurves.GetSuperBrakeMinBoost(_superBrakeStrengthLevel);
    }

    private (double Boost, string Diagnostics) CalculateBrakeAssist(double t)
    {
        return BrakeCurves.CalculateBrakeAssist(
            _brakeAssistNormalStrengthPercent,
            _brakeAssistPanicThresholdPercent,
            _brakeAssistPanicStrengthPercent,
            t);
    }
}
