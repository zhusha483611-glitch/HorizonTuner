using System.Windows;
using System.Windows.Controls;
using HorizonTuner.Cheats;
using HorizonTuner.Cheats.ForzaHorizon5;
using HorizonTuner.Resources.Config;
using HorizonTuner.Services;
using HorizonTuner.Services.Handling.Curves;
using HorizonTuner.Views.Windows;
using MahApps.Metro.Controls;
using static HorizonTuner.Resources.Cheats;
using static HorizonTuner.Resources.Memory;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private static bool IsGameAttached()
    {
        var handle = GetInstance().MProc.Handle;
        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            return false;
        }

        if (GetInstance().MProc.ProcessId <= 0)
        {
            return false;
        }

        try
        {
            return !GetInstance().MProc.Process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryFixupInjectionPrerequisitesAsync()
    {
        try
        {
            await GetClass<Bypass>().DisableCrcChecks().ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            GetClass<CarCheats>().Reset();
        }
        catch
        {
        }

        try
        {
            return GetClass<Bypass>().IsCrcPatchApplied();
        }
        catch
        {
            return false;
        }
    }

    private async void VelocitySwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        if (toggleSwitch.IsOn && VelLinearSwitch.IsOn)
        {
            VelLinearSwitch.IsOn = false;
        }
        if (toggleSwitch.IsOn && VelMultiStageSwitch.IsOn)
        {
            VelMultiStageSwitch.IsOn = false;
        }

        ViewModel.AreUiElementsEnabled = false;
        try
        {
            if (_carCheats.LocalPlayerHookDetourAddress == 0)
            {
                await _carCheats.CheatLocalPlayer();
            }
        }
        catch
        {
            toggleSwitch.Toggled -= VelocitySwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += VelocitySwitch_OnToggled;
            return;
        }
        finally
        {
            ViewModel.AreUiElementsEnabled = true;
        }

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            toggleSwitch.Toggled -= VelocitySwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += VelocitySwitch_OnToggled;
            return;
        }

        UpdateCachedVelocityValues();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.VelBoost, ToBoostPerApply(_velocityBoostValue), false);
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.VelLimit, KmhToMph(_velocityLimitValue), false);
        _velocityHotkey.CanExecute = toggleSwitch.IsOn;

        var config = _configStore.Get();
        config.VelocityAutoOn = toggleSwitch.IsOn;
        if (toggleSwitch.IsOn)
        {
            config.VelocityLinearAutoOn = false;
        }
        _configStore.Save(config);

        if (toggleSwitch.IsOn)
        {
            StartVelocityAutoLoop();
        }
        else
        {
            StopVelocityAuto();
        }
    }

    private async void VelocityLinearSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        if (toggleSwitch.IsOn && VelSwitch.IsOn)
        {
            VelSwitch.IsOn = false;
        }
        if (toggleSwitch.IsOn && VelMultiStageSwitch.IsOn)
        {
            VelMultiStageSwitch.IsOn = false;
        }

        ViewModel.AreUiElementsEnabled = false;
        try
        {
            if (_carCheats.LocalPlayerHookDetourAddress == 0)
            {
                await _carCheats.CheatLocalPlayer();
            }
        }
        catch
        {
            toggleSwitch.Toggled -= VelocityLinearSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += VelocityLinearSwitch_OnToggled;
            return;
        }
        finally
        {
            ViewModel.AreUiElementsEnabled = true;
        }

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            toggleSwitch.Toggled -= VelocityLinearSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += VelocityLinearSwitch_OnToggled;
            return;
        }

        UpdateCachedVelocityValues();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.VelBoost, 1f, false);
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.VelLimit, KmhToMph(_velocityLimitValue), false);
        _velocityHotkey.CanExecute = false;

        var config = _configStore.Get();
        config.VelocityLinearAutoOn = toggleSwitch.IsOn;
        if (toggleSwitch.IsOn)
        {
            config.VelocityAutoOn = false;
        }
        _configStore.Save(config);

        if (toggleSwitch.IsOn)
        {
            StartVelocityLinearAutoLoop();
        }
        else
        {
            StopVelocityLinearAuto();
        }
    }

    private async void VelocityMultiStageSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        if (!toggleSwitch.IsOn)
        {
            var configOff = _configStore.Get();
            configOff.VelocityMultiStageAutoOn = false;
            _configStore.Save(configOff);
            StopVelocityMultiStageAuto();
            return;
        }

        if (toggleSwitch.IsOn && !_gamepadReader.IsAvailable)
        {
            toggleSwitch.Toggled -= VelocityMultiStageSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += VelocityMultiStageSwitch_OnToggled;
            MessageBox.Show("XInput 不可用，无法读取 RT 输入，多段式加速不会生效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (toggleSwitch.IsOn && !_gamepadReader.TryGetState(out _))
        {
            toggleSwitch.Toggled -= VelocityMultiStageSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += VelocityMultiStageSwitch_OnToggled;
            MessageBox.Show("未检测到 XInput 手柄，无法读取 RT 输入，多段式加速不会生效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (toggleSwitch.IsOn && VelSwitch.IsOn)
        {
            VelSwitch.IsOn = false;
        }
        if (toggleSwitch.IsOn && VelLinearSwitch.IsOn)
        {
            VelLinearSwitch.IsOn = false;
        }

        ViewModel.AreUiElementsEnabled = false;
        var injected = true;
        try
        {
            if (_carCheats.LocalPlayerHookDetourAddress == 0)
            {
                injected = await _carCheats.CheatLocalPlayer();
            }
        }
        catch
        {
            toggleSwitch.Toggled -= VelocityMultiStageSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += VelocityMultiStageSwitch_OnToggled;
            MessageBox.Show("多段式加速注入失败（读取模块信息失败或游戏未处于可注入状态）。请确保已附加到游戏并进入驾驶场景后重试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        finally
        {
            ViewModel.AreUiElementsEnabled = true;
        }

        if (!injected || _carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            toggleSwitch.Toggled -= VelocityMultiStageSwitch_OnToggled;
            toggleSwitch.IsOn = false;
            toggleSwitch.Toggled += VelocityMultiStageSwitch_OnToggled;

            if (!IsGameAttached())
            {
                MessageBox.Show("当前未附加到游戏（句柄无效或未能打开进程）。请等待右上角显示 Attached=On 后再试，必要时以管理员运行。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var crcOk = await TryFixupInjectionPrerequisitesAsync().ConfigureAwait(true);
            bool injectedRetry;
            try
            {
                injectedRetry = await _carCheats.CheatLocalPlayer();
            }
            catch
            {
                injectedRetry = false;
            }

            if (injectedRetry && _carCheats.LocalPlayerHookDetourAddress > UIntPtr.Zero)
            {
                toggleSwitch.Toggled -= VelocityMultiStageSwitch_OnToggled;
                toggleSwitch.IsOn = true;
                toggleSwitch.Toggled += VelocityMultiStageSwitch_OnToggled;
            }
            else
            {
                MessageBox.Show(
                    crcOk
                        ? "多段式加速注入失败（Detour 未建立）。请确认已进入驾驶场景后重试；若反复失败，可能是游戏当前状态/版本不匹配导致签名未命中。"
                        : "多段式加速注入失败（CRC Patch 未就绪）。请等待附加稳定或以管理员运行；仍失败再尝试重启游戏。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
        }

        UpdateCachedVelocityValues();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.VelBoost, 1f, false);
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.VelLimit, KmhToMph(_velocityLimitValue), false);
        _velocityHotkey.CanExecute = false;

        var config = _configStore.Get();
        config.VelocityMultiStageAutoOn = toggleSwitch.IsOn;
        if (toggleSwitch.IsOn)
        {
            config.VelocityAutoOn = false;
            config.VelocityLinearAutoOn = false;
        }
        _configStore.Save(config);

        if (toggleSwitch.IsOn)
        {
            StartVelocityMultiStageAutoLoop();
        }
        else
        {
            StopVelocityMultiStageAuto();
        }
    }

    private void VelocityChaseLimitSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        var config = _configStore.Get();
        config.VelocityChaseLimitOn = toggleSwitch.IsOn;
        _configStore.Save(config);
    }

    private async void VelocitySelfCheckButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        ViewModel.AreUiElementsEnabled = false;
        try
        {
            if (!IsGameAttached())
            {
                MessageBox.Show("当前未附加到游戏（句柄无效或未能打开进程）。请等待主界面显示 Attached=On 后再试。", "速度注入自检/修复",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var crcOk = await TryFixupInjectionPrerequisitesAsync().ConfigureAwait(true);

            bool detourOk;
            try
            {
                detourOk = _carCheats.LocalPlayerHookDetourAddress > UIntPtr.Zero || await _carCheats.CheatLocalPlayer().ConfigureAwait(true);
            }
            catch
            {
                detourOk = false;
            }

            var address = _carCheats.LocalPlayerHookDetourAddress;
            var hookActive = _carCheats.IsLocalPlayerHookActive();

            var text = $"Detour={(detourOk ? "OK" : "FAIL")}\nDetourAddress={(address > UIntPtr.Zero ? $"0x{address.ToUInt64():X}" : "N/A")}\nHookActive={(hookActive ? "OK" : "FAIL")}\nCrcPatchOk={(crcOk ? "OK" : "FAIL")}";
            var ok = detourOk && hookActive && crcOk;
            MessageBox.Show(text, "速度注入自检/修复", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        finally
        {
            ViewModel.AreUiElementsEnabled = true;
        }
    }

    private void VelocityStrength_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        // 保存到配置
        var config = _configStore.Get();
        config.VelocityStrength = Math.Clamp(e.NewValue ?? 0.10, 0.0, 1.0);
        ScheduleHandlingConfigSave(config);
        _velocityBoostValue = (float)config.VelocityStrength + 1f;

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero) return;
        UpdateCachedVelocityValues();
        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (VelLinearSwitch.IsOn && _gamepadReader.TryGetState(out var state))
        {
            var thresholdByte = GetThrottleThresholdByte();
            var u = CalculateTriggerU(state.Gamepad.bRightTrigger, thresholdByte);
            _memoryWriter.Write(address + CarCheatsOffsets.VelBoost, ToBoostPerApply(CalculateVelocityLinearBoost(u)), false);
            return;
        }

        if (VelLinearSwitch.IsOn)
        {
            _memoryWriter.Write(address + CarCheatsOffsets.VelBoost, 1f, false);
            return;
        }

        _memoryWriter.Write(address + CarCheatsOffsets.VelBoost, ToBoostPerApply(_velocityBoostValue), false);
    }

    private void VelocityLimit_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        // 保存到配置
        var config = _configStore.Get();
        config.VelocityLimitKmh = Math.Clamp(e.NewValue ?? 300.0, 0.0, 10000.0);
        ScheduleHandlingConfigSave(config);
        _velocityLimitValue = (float)config.VelocityLimitKmh;

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero) return;
        UpdateCachedVelocityValues();
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.VelLimit, KmhToMph(_velocityLimitValue), false);
    }

    private void VelLinearScale_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        var config = _configStore.Get();
        config.VelocityLinearScalePercent = ClampPercent(e.NewValue ?? 40);
        ScheduleHandlingConfigSave(config);
        _velocityLinearScalePercent = config.VelocityLinearScalePercent;
    }

    private void VelLinearGamma_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        var config = _configStore.Get();
        config.VelocityLinearGamma = Math.Clamp(e.NewValue ?? 2.2, 1.0, 4.0);
        ScheduleHandlingConfigSave(config);
        _velocityLinearGamma = config.VelocityLinearGamma;
    }

    private VelocityMode GetVelocityModeSelection()
    {
        if (!Dispatcher.CheckAccess())
        {
            return _configStore.Get().VelocityMode;
        }

        if (VelocityModeComboBox != null && VelocityModeComboBox.SelectedIndex >= 0)
        {
            return (VelocityMode)VelocityModeComboBox.SelectedIndex;
        }

        return _configStore.Get().VelocityMode;
    }

    private void VelocityMaxKmh_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        if (_suppressVelocityModeUiEvents)
        {
            return;
        }

        var config = _configStore.Get();
        var mode = GetVelocityModeSelection();
        var value = Math.Clamp(e.NewValue ?? 320d, 50d, 600d);

        switch (mode)
        {
            case VelocityMode.S1Car:
                config.VelocityS1MaxKmh = value;
                break;
            case VelocityMode.S2Car:
                config.VelocityS2MaxKmh = value;
                break;
            case VelocityMode.ACar:
                config.VelocityAMaxKmh = value;
                break;
            case VelocityMode.Custom:
                config.VelocityCustomMaxKmh = value;
                break;
        }

        config.VelocityMode = mode;
        ScheduleHandlingConfigSave(config);
    }

    private void VelStage1Gamma_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        if (_suppressVelocityModeUiEvents)
        {
            return;
        }

        var config = _configStore.Get();
        var mode = GetVelocityModeSelection();
        var value = SanitizeDouble(e.NewValue ?? 3.0, 3.0);

        config.VelocityStage1Gamma = value;
        switch (mode)
        {
            case VelocityMode.S1Car:
                config.VelocityS1Stage1Gamma = value;
                break;
            case VelocityMode.S2Car:
                config.VelocityS2Stage1Gamma = value;
                break;
            case VelocityMode.ACar:
                config.VelocityAStage1Gamma = value;
                break;
            case VelocityMode.Custom:
                break;
        }

        config.VelocityMode = mode;
        ScheduleHandlingConfigSave(config);
        _velocityStage1Gamma = value;
    }

    private void VelStage2Gamma_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        if (_suppressVelocityModeUiEvents)
        {
            return;
        }

        var config = _configStore.Get();
        var mode = GetVelocityModeSelection();
        var value = SanitizeDouble(e.NewValue ?? 1.8, 1.8);

        config.VelocityStage2Gamma = value;
        switch (mode)
        {
            case VelocityMode.S1Car:
                config.VelocityS1Stage2Gamma = value;
                break;
            case VelocityMode.S2Car:
                config.VelocityS2Stage2Gamma = value;
                break;
            case VelocityMode.ACar:
                config.VelocityAStage2Gamma = value;
                break;
            case VelocityMode.Custom:
                break;
        }

        config.VelocityMode = mode;
        ScheduleHandlingConfigSave(config);
        _velocityStage2Gamma = value;
    }

    private void VelStage3Gamma_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        if (_suppressVelocityModeUiEvents)
        {
            return;
        }

        var config = _configStore.Get();
        var mode = GetVelocityModeSelection();
        var value = SanitizeDouble(e.NewValue ?? 1.3, 1.3);

        config.VelocityStage3Gamma = value;
        switch (mode)
        {
            case VelocityMode.S1Car:
                config.VelocityS1Stage3Gamma = value;
                break;
            case VelocityMode.S2Car:
                config.VelocityS2Stage3Gamma = value;
                break;
            case VelocityMode.ACar:
                config.VelocityAStage3Gamma = value;
                break;
            case VelocityMode.Custom:
                break;
        }

        config.VelocityMode = mode;
        ScheduleHandlingConfigSave(config);
        _velocityStage3Gamma = value;
    }

    private void VelStage1Scale_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        if (_suppressVelocityModeUiEvents)
        {
            return;
        }

        var config = _configStore.Get();
        var mode = GetVelocityModeSelection();
        var value = SanitizeDouble((e.NewValue ?? 30) / 100d, 0.3);

        config.VelocityStage1Scale = value;
        switch (mode)
        {
            case VelocityMode.S1Car:
                config.VelocityS1Stage1Scale = value;
                break;
            case VelocityMode.S2Car:
                config.VelocityS2Stage1Scale = value;
                break;
            case VelocityMode.ACar:
                config.VelocityAStage1Scale = value;
                break;
            case VelocityMode.Custom:
                break;
        }

        config.VelocityMode = mode;
        ScheduleHandlingConfigSave(config);
        _velocityStage1Scale = value;
    }

    private void VelStage2Scale_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        if (_suppressVelocityModeUiEvents)
        {
            return;
        }

        var config = _configStore.Get();
        var mode = GetVelocityModeSelection();
        var value = SanitizeDouble((e.NewValue ?? 50) / 100d, 0.5);

        config.VelocityStage2Scale = value;
        switch (mode)
        {
            case VelocityMode.S1Car:
                config.VelocityS1Stage2Scale = value;
                break;
            case VelocityMode.S2Car:
                config.VelocityS2Stage2Scale = value;
                break;
            case VelocityMode.ACar:
                config.VelocityAStage2Scale = value;
                break;
            case VelocityMode.Custom:
                break;
        }

        config.VelocityMode = mode;
        ScheduleHandlingConfigSave(config);
        _velocityStage2Scale = value;
    }

    private void VelStage3Scale_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        if (_suppressVelocityModeUiEvents)
        {
            return;
        }

        var config = _configStore.Get();
        var mode = GetVelocityModeSelection();
        var value = SanitizeDouble((e.NewValue ?? 70) / 100d, 0.7);

        config.VelocityStage3Scale = value;
        switch (mode)
        {
            case VelocityMode.S1Car:
                config.VelocityS1Stage3Scale = value;
                break;
            case VelocityMode.S2Car:
                config.VelocityS2Stage3Scale = value;
                break;
            case VelocityMode.ACar:
                config.VelocityAStage3Scale = value;
                break;
            case VelocityMode.Custom:
                break;
        }

        config.VelocityMode = mode;
        ScheduleHandlingConfigSave(config);
        _velocityStage3Scale = value;
    }

    private void VelocityModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressVelocityModeUiEvents)
        {
            return;
        }

        if (VelocityModeComboBox == null || VelocityModeComboBox.SelectedIndex < 0)
        {
            return;
        }

        var mode = (VelocityMode)VelocityModeComboBox.SelectedIndex;
        ApplyVelocityPreset(mode);

        _suppressVelocityModeUiEvents = true;
        try
        {
            var velocityMaxKmh = VelocityMaxKmhControl;
            if (velocityMaxKmh != null)
            {
                var config = _configStore.Get();
                velocityMaxKmh.Value = mode switch
                {
                    VelocityMode.S1Car => config.VelocityS1MaxKmh,
                    VelocityMode.S2Car => config.VelocityS2MaxKmh,
                    VelocityMode.ACar => config.VelocityAMaxKmh,
                    VelocityMode.Custom => config.VelocityCustomMaxKmh,
                    _ => config.VelocityS2MaxKmh
                };
            }
            if (VelStage1Gamma != null)
            {
                VelStage1Gamma.Value = _velocityStage1Gamma;
            }
            if (VelStage2Gamma != null)
            {
                VelStage2Gamma.Value = _velocityStage2Gamma;
            }
            if (VelStage3Gamma != null)
            {
                VelStage3Gamma.Value = _velocityStage3Gamma;
            }
            if (VelStage1Scale != null)
            {
                VelStage1Scale.Value = _velocityStage1Scale * 100;
            }
            if (VelStage2Scale != null)
            {
                VelStage2Scale.Value = _velocityStage2Scale * 100;
            }
            if (VelStage3Scale != null)
            {
                VelStage3Scale.Value = _velocityStage3Scale * 100;
            }
        }
        finally
        {
            _suppressVelocityModeUiEvents = false;
        }
    }

    private float CalculateVelocityLinearBoost(double u)
    {
        return VelocityCurves.CalculateLinearBoost(_velocityBoostValue, _velocityLinearScalePercent, _velocityLinearGamma, u);
    }

    private float CalculateVelocityMultiStageBoost(double u)
    {
        var f1 = Math.Clamp(_velocityStage1TargetFrac * (0.7 + 0.6 * Math.Clamp(_velocityStage1Scale, 0d, 1d)), 0.10, 0.95);
        var f2Base = _velocityStage2TargetFrac * (0.7 + 0.6 * Math.Clamp(_velocityStage2Scale, 0d, 1d));
        var f2 = Math.Clamp(f2Base, f1 + 0.05, 0.99);

        return VelocityCurves.CalculateMultiStageBoostWithTargets(
            _velocityBoostValue,
            _velocityStage1Gamma,
            _velocityStage2Gamma,
            _velocityStage3Gamma,
            _velocityStage1End,
            _velocityStage2End,
            f1,
            f2,
            u);
    }

    private void ApplyVelocityPreset(VelocityMode mode)
    {
        var config = _configStore.Get();
        switch (mode)
        {
            case VelocityMode.S1Car:
                _velocityStage1Gamma = config.VelocityS1Stage1Gamma;
                _velocityStage2Gamma = config.VelocityS1Stage2Gamma;
                _velocityStage3Gamma = config.VelocityS1Stage3Gamma;
                _velocityStage1Scale = config.VelocityS1Stage1Scale;
                _velocityStage2Scale = config.VelocityS1Stage2Scale;
                _velocityStage3Scale = config.VelocityS1Stage3Scale;
                _velocityStage1End = config.VelocityS1Stage1End;
                _velocityStage2End = config.VelocityS1Stage2End;
                _velocityStage1TargetFrac = config.VelocityS1Stage1TargetFrac;
                _velocityStage2TargetFrac = config.VelocityS1Stage2TargetFrac;
                break;

            case VelocityMode.S2Car:
                _velocityStage1Gamma = config.VelocityS2Stage1Gamma;
                _velocityStage2Gamma = config.VelocityS2Stage2Gamma;
                _velocityStage3Gamma = config.VelocityS2Stage3Gamma;
                _velocityStage1Scale = config.VelocityS2Stage1Scale;
                _velocityStage2Scale = config.VelocityS2Stage2Scale;
                _velocityStage3Scale = config.VelocityS2Stage3Scale;
                _velocityStage1End = config.VelocityS2Stage1End;
                _velocityStage2End = config.VelocityS2Stage2End;
                _velocityStage1TargetFrac = config.VelocityS2Stage1TargetFrac;
                _velocityStage2TargetFrac = config.VelocityS2Stage2TargetFrac;
                break;

            case VelocityMode.ACar:
                _velocityStage1Gamma = config.VelocityAStage1Gamma;
                _velocityStage2Gamma = config.VelocityAStage2Gamma;
                _velocityStage3Gamma = config.VelocityAStage3Gamma;
                _velocityStage1Scale = config.VelocityAStage1Scale;
                _velocityStage2Scale = config.VelocityAStage2Scale;
                _velocityStage3Scale = config.VelocityAStage3Scale;
                _velocityStage1End = config.VelocityAStage1End;
                _velocityStage2End = config.VelocityAStage2End;
                _velocityStage1TargetFrac = config.VelocityAStage1TargetFrac;
                _velocityStage2TargetFrac = config.VelocityAStage2TargetFrac;
                break;

            case VelocityMode.Custom:
                _velocityStage1Gamma = config.VelocityStage1Gamma;
                _velocityStage2Gamma = config.VelocityStage2Gamma;
                _velocityStage3Gamma = config.VelocityStage3Gamma;
                _velocityStage1Scale = config.VelocityStage1Scale;
                _velocityStage2Scale = config.VelocityStage2Scale;
                _velocityStage3Scale = config.VelocityStage3Scale;
                _velocityStage1End = config.VelocityStage1End;
                _velocityStage2End = config.VelocityStage2End;
                _velocityStage1TargetFrac = config.VelocityStage1TargetFrac;
                _velocityStage2TargetFrac = config.VelocityStage2TargetFrac;
                break;
        }

        config.VelocityMode = mode;
        config.VelocityStage1Gamma = _velocityStage1Gamma;
        config.VelocityStage2Gamma = _velocityStage2Gamma;
        config.VelocityStage3Gamma = _velocityStage3Gamma;
        config.VelocityStage1Scale = _velocityStage1Scale;
        config.VelocityStage2Scale = _velocityStage2Scale;
        config.VelocityStage3Scale = _velocityStage3Scale;
        config.VelocityStage1End = _velocityStage1End;
        config.VelocityStage2End = _velocityStage2End;
        config.VelocityStage1TargetFrac = _velocityStage1TargetFrac;
        config.VelocityStage2TargetFrac = _velocityStage2TargetFrac;
        _configStore.Save(config);
    }

    private void SavePresetButton_OnClick(object sender, RoutedEventArgs e)
    {
        var config = _configStore.Get();

        if (config.CustomVelocityPresets.Count >= MaxCustomVelocityPresets)
        {
            MessageBox.Show(
                $"自定义预设数量已达到上限（{MaxCustomVelocityPresets}）。\n请先删除不需要的预设。",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var name = Models.VelocityPresetNaming.MakeUniqueName(
            config.CustomVelocityPresets,
            $"预设 {config.CustomVelocityPresets.Count + 1}");

        var preset = new Models.VelocityPreset
        {
            Name = name,
            Stage1Gamma = _velocityStage1Gamma,
            Stage2Gamma = _velocityStage2Gamma,
            Stage3Gamma = _velocityStage3Gamma,
            Stage1Scale = _velocityStage1Scale,
            Stage2Scale = _velocityStage2Scale,
            Stage3Scale = _velocityStage3Scale,
            CreatedTime = DateTime.Now
        };

        config.CustomVelocityPresets.Add(preset);
        _configStore.Save(config);

        MessageBox.Show($"预设 \"{preset.Name}\" 已保存", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ManagePresetsButton_OnClick(object sender, RoutedEventArgs e)
    {
        PresetManagerWindow window = new(ApplyCustomPreset)
        {
            Owner = Window.GetWindow(this)
        };
        window.ShowDialog();
    }

    private void ApplyCustomPreset(Models.VelocityPreset preset)
    {
        if (preset == null) return;

        _velocityStage1Gamma = preset.Stage1Gamma;
        _velocityStage2Gamma = preset.Stage2Gamma;
        _velocityStage3Gamma = preset.Stage3Gamma;
        _velocityStage1Scale = preset.Stage1Scale;
        _velocityStage2Scale = preset.Stage2Scale;
        _velocityStage3Scale = preset.Stage3Scale;

        _suppressVelocityModeUiEvents = true;
        try
        {
            if (VelStage1Gamma != null) VelStage1Gamma.Value = _velocityStage1Gamma;
            if (VelStage2Gamma != null) VelStage2Gamma.Value = _velocityStage2Gamma;
            if (VelStage3Gamma != null) VelStage3Gamma.Value = _velocityStage3Gamma;
            if (VelStage1Scale != null) VelStage1Scale.Value = _velocityStage1Scale * 100;
            if (VelStage2Scale != null) VelStage2Scale.Value = _velocityStage2Scale * 100;
            if (VelStage3Scale != null) VelStage3Scale.Value = _velocityStage3Scale * 100;
            if (VelocityModeComboBox != null)
            {
                VelocityModeComboBox.SelectedIndex = (int)VelocityMode.Custom;
            }
        }
        finally
        {
            _suppressVelocityModeUiEvents = false;
        }

        var config = _configStore.Get();
        config.VelocityMode = VelocityMode.Custom;
        config.VelocityStage1Gamma = _velocityStage1Gamma;
        config.VelocityStage2Gamma = _velocityStage2Gamma;
        config.VelocityStage3Gamma = _velocityStage3Gamma;
        config.VelocityStage1Scale = _velocityStage1Scale;
        config.VelocityStage2Scale = _velocityStage2Scale;
        config.VelocityStage3Scale = _velocityStage3Scale;
        Models.VelocityPresetUsage.MarkUsed(preset, DateTime.UtcNow);
        _configStore.Save(config);
    }

    private static double CalculateTriggerU(byte trigger, byte threshold)
    {
        return TriggerMath.CalculateTriggerU(trigger, threshold);
    }

    private float CalculateVelocityBoost(double u)
    {
        var strength = Math.Clamp(_velocityBoostValue - 1f, 0f, 0.2f);
        var boost = 1d + strength * u;
        return (float)Math.Clamp(boost, 1d, 2d);
    }
}
