using System.Text;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace MA_FH5Trainer.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private async void ModifierToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;
        if (_carCheats.AccelDetourAddress == 0)
        {
            await _carCheats.CheatAccel();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_carCheats.AccelDetourAddress <= 0)
        {
            toggleSwitch.Toggled -= ModifierToggleSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += ModifierToggleSwitch_OnToggled;
            return;
        }

        _memoryWriter.Write(_carCheats.AccelDetourAddress + 0x58, toggleSwitch.IsOn ? (byte)1 : (byte)0);
        _memoryWriter.Write(_carCheats.AccelDetourAddress + 0x59, Convert.ToSingle(AccelSlider.Value) / 100);
        ViewModel.IsAccelEnabled = toggleSwitch.IsOn;
    }

    private void ModifierValueBox_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ViewModel.AccelValue = Convert.ToDouble(e.NewValue);

        // 保存到配置
        var config = _configStore.Get();
        config.AccelModifierPercent = Math.Clamp(e.NewValue, 100.0, 1000.0);
        ScheduleHandlingConfigSave(config);

        if (_carCheats.AccelDetourAddress <= UIntPtr.Zero)
        {
            return;
        }

        _memoryWriter.Write(_carCheats.AccelDetourAddress + 0x59, Convert.ToSingle(e.NewValue) / 100);
    }

    private async void NoWaterDragSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        toggleSwitch.IsEnabled = false;
        if (_carCheats.NoWaterDragDetourAddress == 0)
        {
            await _carCheats.CheatNoWaterDrag();
        }
        toggleSwitch.IsEnabled = true;

        if (_carCheats.NoWaterDragDetourAddress == 0)
        {
            toggleSwitch.Toggled -= NoWaterDragSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += NoWaterDragSwitch_OnToggled;
            return;
        }
        _memoryWriter.Write(_carCheats.NoWaterDragDetourAddress + 0x17, toggleSwitch.IsOn ? (byte)1 : (byte)0);
    }

    private async void NoClipSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;
        if (_carCheats.NoClipDetourAddress == 0)
        {
            await _carCheats.CheatNoClip();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_carCheats.NoClipDetourAddress == 0)
        {
            toggleSwitch.Toggled -= NoClipSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += NoClipSwitch_OnToggled;
            return;
        }
        _memoryWriter.Write(_carCheats.NoClipDetourAddress + 0x31, toggleSwitch.IsOn ? (byte)1 : (byte)0);
    }

    private async void GravToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;
        if (_carCheats.GravityDetourAddress == 0)
        {
            await _carCheats.CheatGravity();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_carCheats.GravityDetourAddress <= 0)
        {
            toggleSwitch.Toggled -= GravToggleSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled -= GravToggleSwitch_OnToggled;
            return;
        }

        _memoryWriter.Write(_carCheats.GravityDetourAddress + 0x59, toggleSwitch.IsOn ? (byte)1 : (byte)0);
        _memoryWriter.Write(_carCheats.GravityDetourAddress + 0x5A, Convert.ToSingle(GravValueBox.Value) / 100);
        ViewModel.IsGravityEnabled = toggleSwitch.IsOn;
    }

    private void GravValueBox_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ViewModel.GravityValue = Convert.ToDouble(e.NewValue);

        // 保存到配置
        var config = _configStore.Get();
        config.GravityModifierPercent = Math.Clamp(e.NewValue, -500.0, 500.0);
        ScheduleHandlingConfigSave(config);

        if (_carCheats.GravityDetourAddress <= UIntPtr.Zero)
        {
            return;
        }

        _memoryWriter.Write(_carCheats.GravityDetourAddress + 0x5A, Convert.ToSingle(e.NewValue) / 100);
    }

    private async void QuickNameSpooferSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;
        if (_miscCheats.NameDetourAddress == 0)
        {
            await _miscCheats.CheatName();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_miscCheats.NameDetourAddress == 0)
        {
            toggleSwitch.Toggled -= QuickNameSpooferSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += QuickNameSpooferSwitch_OnToggled;
            return;
        }

        _memoryWriter.Write(_miscCheats.NameDetourAddress + 0x55, toggleSwitch.IsOn ? (byte)1 : (byte)0);
        var name = QuickNameBox.Text ?? string.Empty;
        var bytes = Encoding.Unicode.GetBytes(name);
        var buffer = new byte[34];
        Array.Copy(bytes, buffer, Math.Min(bytes.Length, 32));
        _memoryWriter.WriteArray(_miscCheats.NameDetourAddress + 0x56, buffer);
    }

    private void QuickNameBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_miscCheats.NameDetourAddress == 0)
        {
            return;
        }

        var name = QuickNameBox.Text ?? string.Empty;
        var bytes = Encoding.Unicode.GetBytes(name);
        var buffer = new byte[34];
        Array.Copy(bytes, buffer, Math.Min(bytes.Length, 32));
        _memoryWriter.WriteArray(_miscCheats.NameDetourAddress + 0x56, buffer);
    }

    private async void QuickUnbreakableSkillScoreSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;
        if (_miscCheats.UnbreakableSkillScoreDetourAddress == 0)
        {
            await _miscCheats.CheatUnbreakableSkillScore();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_miscCheats.UnbreakableSkillScoreDetourAddress == 0)
        {
            toggleSwitch.Toggled -= QuickUnbreakableSkillScoreSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += QuickUnbreakableSkillScoreSwitch_OnToggled;
            return;
        }

        _memoryWriter.Write(_miscCheats.UnbreakableSkillScoreDetourAddress + 0x1A, toggleSwitch.IsOn ? (byte)1 : (byte)0);
    }

    private async void QuickSkillScoreToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;
        if (_miscCheats.SkillScoreMultiplierDetourAddress == 0)
        {
            await _miscCheats.CheatSkillScoreMultiplier();
        }
        ViewModel.AreUiElementsEnabled = true;

        if (_miscCheats.SkillScoreMultiplierDetourAddress == 0)
        {
            toggleSwitch.Toggled -= QuickSkillScoreToggle_Toggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += QuickSkillScoreToggle_Toggled;
            return;
        }

        var value = (int)QuickSkillScoreBox.Value.GetValueOrDefault();
        _memoryWriter.Write(_miscCheats.SkillScoreMultiplierDetourAddress + 0x1D, value);
        _memoryWriter.Write(_miscCheats.SkillScoreMultiplierDetourAddress + 0x1C, toggleSwitch.IsOn ? (byte)1 : (byte)0);
    }

    private void QuickSkillScoreBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        // 保存到配置
        var config = _configStore.Get();
        config.QuickSkillScoreMultiplier = Math.Clamp(e.NewValue ?? 1.0, 1.0, 15.0);
        ScheduleHandlingConfigSave(config);

        if (_miscCheats.SkillScoreMultiplierDetourAddress == 0)
        {
            return;
        }

        var value = (int)QuickSkillScoreBox.Value.GetValueOrDefault();
        _memoryWriter.Write(_miscCheats.SkillScoreMultiplierDetourAddress + 0x1D, value);
    }
}
