using System.Windows;
using System.Windows.Controls;
using MA_FH5Trainer.Cheats.ForzaHorizon5;
using MA_FH5Trainer.Resources.Input;
using MahApps.Metro.Controls;

namespace MA_FH5Trainer.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private async void WheelspeedSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;

        if (_carCheats.LocalPlayerHookDetourAddress == 0)
        {
            await _carCheats.CheatLocalPlayer();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            toggleSwitch.Toggled -= WheelspeedSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += WheelspeedSwitch_OnToggled;
            return;
        }

        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.WheelspeedMode, (byte)WheelspeedModeBox.SelectedIndex);
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.WheelspeedBoost, Convert.ToSingle(WheelspeedValueBox.Value));
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.WheelspeedLimit, Convert.ToSingle(WheelspeedLimit.Value));
        _wheelspeedHotkey.CanExecute = toggleSwitch.IsOn;

        var config = _configStore.Get();
        config.WheelspeedAutoOn = toggleSwitch.IsOn;
        _configStore.Save(config);

        if (toggleSwitch.IsOn)
        {
            StartWheelspeedAutoLoop();
        }
        else
        {
            StopWheelspeedAuto();
        }
    }

    private void WheelspeedNum_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        // 保存到配置
        var config = _configStore.Get();
        config.WheelspeedValue = Math.Clamp(e.NewValue ?? 10.0, 1.0, 100.0);
        ScheduleHandlingConfigSave(config);

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero) return;
        UpdateCachedWheelspeedValues();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.WheelspeedBoost, _wheelspeedBoostValue, false);
    }

    private void WheelspeedModeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero) return;
        UpdateCachedWheelspeedValues();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.WheelspeedMode, _wheelspeedModeValue, false);
    }

    private void WheelspeedLimit_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        // 保存到配置
        var config = _configStore.Get();
        config.WheelspeedLimitKmh = Math.Clamp(e.NewValue ?? 300.0, 1.0, 500.0);
        ScheduleHandlingConfigSave(config);

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            return;
        }

        UpdateCachedWheelspeedValues();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.WheelspeedLimit, _wheelspeedLimitValue, false);
    }

    private void JumpSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is Slider slider)
        {
            slider.Value = (Math.Round(e.NewValue, 2));
        }

        // 保存到配置
        var config = _configStore.Get();
        config.JumpHackValue = Math.Clamp(e.NewValue, 1.0, 15.0);
        ScheduleHandlingConfigSave(config);

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            return;
        }

        UpdateCachedJumpValue();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.JumpHackBoost, _jumpBoostValue, false);
    }

    private async void JumpSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;

        if (_carCheats.LocalPlayerHookDetourAddress == 0)
        {
            await _carCheats.CheatLocalPlayer();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            toggleSwitch.Toggled -= JumpSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += JumpSwitch_OnToggled;
            return;
        }
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.JumpHackBoost, Convert.ToSingle(JumpSlider.Value));
        _jumpHackHotkey.CanExecute = toggleSwitch.IsOn;

        var config = _configStore.Get();
        config.JumpAutoOn = toggleSwitch.IsOn;
        _configStore.Save(config);

        if (toggleSwitch.IsOn)
        {
            StartJumpAutoLoop();
        }
        else
        {
            StopJumpAuto();
        }
    }

    private XInputButtons GetJumpButton()
    {
        return ViewModel.JumpButton?.Trim().ToUpperInvariant() switch
        {
            "B" => XInputButtons.B,
            "X" => XInputButtons.X,
            "Y" => XInputButtons.Y,
            _ => XInputButtons.A
        };
    }
}
