using System.ComponentModel;
using System.Windows;
using HorizonTuner.Cheats.ForzaHorizon5;
using HorizonTuner.Services;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private void Handling_OnLoaded(object sender, RoutedEventArgs e)
    {
        _shutdownRegistration?.Dispose();
        _shutdownRegistration = ShutdownCoordinator.Register(StopAllAutomationAsync);

        var config = _configStore.Get();

        ViewModel.ThrottleTriggerThresholdPercent = ClampPercent(config.ThrottleTriggerThresholdPercent);
        ViewModel.BrakeTriggerThresholdPercent = ClampPercent(config.BrakeTriggerThresholdPercent);
        ViewModel.JumpButton = config.JumpButton;

        BrakeAssistNormalStrengthBox.Value = ClampPercent(config.BrakeAssistNormalStrengthPercent);
        BrakeAssistPanicThresholdBox.Value = ClampPercent(config.BrakeAssistPanicThresholdPercent);
        BrakeAssistPanicStrengthBox.Value = ClampPercent(config.BrakeAssistPanicStrengthPercent);
        VelLinearScale.Value = ClampPercent(config.VelocityLinearScalePercent);
        VelLinearGamma.Value = Math.Clamp(config.VelocityLinearGamma, 1.0, 4.0);

        // 加载速度强度和限制
        if (VelocityStrength != null)
        {
            VelocityStrength.Value = Math.Clamp(config.VelocityStrength, 0.0, 1.0);
        }
        if (VelocityLimit != null)
        {
            VelocityLimit.Value = Math.Clamp(config.VelocityLimitKmh, 0.0, 10000.0);
        }

        // 加载轮速设置
        if (WheelspeedValueBox != null)
        {
            WheelspeedValueBox.Value = Math.Clamp(config.WheelspeedValue, 1.0, 100.0);
        }
        if (WheelspeedLimit != null)
        {
            WheelspeedLimit.Value = Math.Clamp(config.WheelspeedLimitKmh, 1.0, 500.0);
        }

        // 加载加速和重力修改器
        if (AccelSlider != null)
        {
            AccelSlider.Value = Math.Clamp(config.AccelModifierPercent, 100.0, 1000.0);
        }
        if (GravValueBox != null)
        {
            GravValueBox.Value = Math.Clamp(config.GravityModifierPercent, -500.0, 500.0);
        }

        // 加载跳跃修改值
        if (JumpSlider != null)
        {
            JumpSlider.Value = Math.Clamp(config.JumpHackValue, 1.0, 15.0);
        }

        // 加载超级刹车值
        if (StopSlider != null)
        {
            StopSlider.Value = Math.Clamp(config.SuperBrakeValue, 1.0, 5.0);
        }

        // 加载技能分数倍率
        if (QuickSkillScoreBox != null)
        {
            QuickSkillScoreBox.Value = Math.Clamp(config.QuickSkillScoreMultiplier, 1.0, 15.0);
        }

        _suppressVelocityModeUiEvents = true;
        _velocityStage1Gamma = SanitizeDouble(config.VelocityStage1Gamma, 3.0);
        _velocityStage2Gamma = SanitizeDouble(config.VelocityStage2Gamma, 1.8);
        _velocityStage3Gamma = SanitizeDouble(config.VelocityStage3Gamma, 1.3);
        _velocityStage1Scale = SanitizeDouble(config.VelocityStage1Scale, 0.3);
        _velocityStage2Scale = SanitizeDouble(config.VelocityStage2Scale, 0.5);
        _velocityStage3Scale = SanitizeDouble(config.VelocityStage3Scale, 0.7);

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
        var velocityMaxKmh = VelocityMaxKmhControl;
        if (velocityMaxKmh != null)
        {
            velocityMaxKmh.Value = config.VelocityMode switch
            {
                HorizonTuner.Resources.Config.VelocityMode.S1Car => config.VelocityS1MaxKmh,
                HorizonTuner.Resources.Config.VelocityMode.S2Car => config.VelocityS2MaxKmh,
                HorizonTuner.Resources.Config.VelocityMode.ACar => config.VelocityAMaxKmh,
                HorizonTuner.Resources.Config.VelocityMode.Custom => config.VelocityCustomMaxKmh,
                _ => config.VelocityS2MaxKmh
            };
        }
        _suppressVelocityModeUiEvents = false;
        if (VelocityModeComboBox != null)
        {
            VelocityModeComboBox.SelectedIndex = (int)config.VelocityMode;
        }

        switch (config.VelocityMode)
        {
            case HorizonTuner.Resources.Config.VelocityMode.S1Car:
                _velocityStage1End = config.VelocityS1Stage1End;
                _velocityStage2End = config.VelocityS1Stage2End;
                _velocityStage1TargetFrac = config.VelocityS1Stage1TargetFrac;
                _velocityStage2TargetFrac = config.VelocityS1Stage2TargetFrac;
                break;
            case HorizonTuner.Resources.Config.VelocityMode.S2Car:
                _velocityStage1End = config.VelocityS2Stage1End;
                _velocityStage2End = config.VelocityS2Stage2End;
                _velocityStage1TargetFrac = config.VelocityS2Stage1TargetFrac;
                _velocityStage2TargetFrac = config.VelocityS2Stage2TargetFrac;
                break;
            case HorizonTuner.Resources.Config.VelocityMode.ACar:
                _velocityStage1End = config.VelocityAStage1End;
                _velocityStage2End = config.VelocityAStage2End;
                _velocityStage1TargetFrac = config.VelocityAStage1TargetFrac;
                _velocityStage2TargetFrac = config.VelocityAStage2TargetFrac;
                break;
            case HorizonTuner.Resources.Config.VelocityMode.Custom:
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

        UpdateCachedUiValues();

        var linearOn = config.VelocityLinearAutoOn;
        var multiStageOn = config.VelocityMultiStageAutoOn;
        var fixedOn = config.VelocityAutoOn && !linearOn && !multiStageOn;

        VelSwitch.Toggled -= VelocitySwitch_OnToggled;
        VelLinearSwitch.Toggled -= VelocityLinearSwitch_OnToggled;
        VelMultiStageSwitch.Toggled -= VelocityMultiStageSwitch_OnToggled;
        VelSwitch.IsOn = fixedOn;
        VelLinearSwitch.IsOn = linearOn;
        VelMultiStageSwitch.IsOn = multiStageOn;
        VelSwitch.Toggled += VelocitySwitch_OnToggled;
        VelLinearSwitch.Toggled += VelocityLinearSwitch_OnToggled;
        VelMultiStageSwitch.Toggled += VelocityMultiStageSwitch_OnToggled;

        if (config.VelocityAutoOn != fixedOn || config.VelocityLinearAutoOn != linearOn || config.VelocityMultiStageAutoOn != multiStageOn)
        {
            config.VelocityAutoOn = fixedOn;
            config.VelocityLinearAutoOn = linearOn;
            config.VelocityMultiStageAutoOn = multiStageOn;
            ScheduleHandlingConfigSave(config);
        }

        if (VelMultiStageSwitch.IsOn)
        {
            VelocityMultiStageSwitch_OnToggled(VelMultiStageSwitch, new RoutedEventArgs());
        }
        else if (VelLinearSwitch.IsOn)
        {
            VelocityLinearSwitch_OnToggled(VelLinearSwitch, new RoutedEventArgs());
        }
        else if (VelSwitch.IsOn)
        {
            VelocitySwitch_OnToggled(VelSwitch, new RoutedEventArgs());
        }

        var chaseSwitch = (global::MahApps.Metro.Controls.ToggleSwitch?)FindName("VelChaseLimitSwitch");
        if (chaseSwitch != null)
        {
            chaseSwitch.Toggled -= VelocityChaseLimitSwitch_OnToggled;
            chaseSwitch.IsOn = config.VelocityChaseLimitOn;
            chaseSwitch.Toggled += VelocityChaseLimitSwitch_OnToggled;
        }

        var advDiagSwitch = (global::MahApps.Metro.Controls.ToggleSwitch?)FindName("VelAdvancedDiagnosticsSwitch");
        if (advDiagSwitch != null)
        {
            advDiagSwitch.Toggled -= VelocityAdvancedDiagnosticsSwitch_OnToggled;
            advDiagSwitch.IsOn = config.ShowAdvancedVelocityDiagnostics;
            advDiagSwitch.Toggled += VelocityAdvancedDiagnosticsSwitch_OnToggled;
        }

        WheelSwitch.IsOn = config.WheelspeedAutoOn;
        JumpHackToggle.IsOn = config.JumpAutoOn;
        StopToggle.IsOn = config.BrakeAutoOn;
        BrakeAssistToggle.IsOn = config.BrakeAssistAutoOn;

        StartGamepadStatusLoop();
    }

    private async void Handling_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _shutdownRegistration?.Dispose();
        _shutdownRegistration = null;

        FlushHandlingConfigSave();

        await StopAllAutomationAsync().ConfigureAwait(false);

        ViewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
        _handlingConfigSaveTimer.Stop();
        _handlingConfigSaveTimer.Tick -= HandlingConfigSaveTimer_OnTick;

        Loaded -= Handling_OnLoaded;
        Unloaded -= Handling_OnUnloaded;
    }

    private void HandlingConfigSaveTimer_OnTick(object? sender, EventArgs e)
    {
        _handlingConfigSaveTimer.Stop();
        FlushHandlingConfigSave();
    }

    private async Task StopAllAutomationAsync()
    {
        _jumpButtonPrevDown = false;
        _throttlePrevDown = false;
        _throttleLinearPrevDown = false;
        _throttleMultiStagePrevDown = false;
        _brakePrevDown = false;
        _brakeAssistPrevDown = false;

        await CancelLoopAsync(ref _velocityAutoCts, ref _velocityAutoTask, 500).ConfigureAwait(false);
        await CancelLoopAsync(ref _velocityLinearAutoCts, ref _velocityLinearAutoTask, 500).ConfigureAwait(false);
        await CancelLoopAsync(ref _velocityMultiStageAutoCts, ref _velocityMultiStageAutoTask, 500).ConfigureAwait(false);
        await CancelLoopAsync(ref _wheelspeedAutoCts, ref _wheelspeedAutoTask, 500).ConfigureAwait(false);
        await CancelLoopAsync(ref _jumpAutoCts, ref _jumpAutoTask, 500).ConfigureAwait(false);
        await CancelLoopAsync(ref _brakeAutoCts, ref _brakeAutoTask, 500).ConfigureAwait(false);
        await CancelLoopAsync(ref _brakeAssistAutoCts, ref _brakeAssistAutoTask, 500).ConfigureAwait(false);
        await CancelLoopAsync(ref _gamepadStatusCts, ref _gamepadStatusTask, 500).ConfigureAwait(false);

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address > UIntPtr.Zero)
        {
            _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)0, false);
            _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackEnabled, (byte)0, false);
            _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackBoost, 1f, false);
            _memoryWriter.Write(address + CarCheatsOffsets.WheelspeedEnabled, (byte)0, false);
            _memoryWriter.Write(address + CarCheatsOffsets.JumpHackEnabled, (byte)0, false);
        }
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ViewModel.ThrottleTriggerThresholdPercent) &&
            e.PropertyName != nameof(ViewModel.BrakeTriggerThresholdPercent) &&
            e.PropertyName != nameof(ViewModel.JumpButton))
        {
            return;
        }

        var config = _configStore.Get();
        config.ThrottleTriggerThresholdPercent = ClampPercent(ViewModel.ThrottleTriggerThresholdPercent);
        config.BrakeTriggerThresholdPercent = ClampPercent(ViewModel.BrakeTriggerThresholdPercent);
        config.JumpButton = ViewModel.JumpButton;
        ScheduleHandlingConfigSave(config);
    }
}
