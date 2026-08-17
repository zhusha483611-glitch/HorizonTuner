using System.Windows;
using HorizonTuner.Cheats;
using HorizonTuner.Cheats.ForzaHorizon5;
using HorizonTuner.Resources.Config;
using HorizonTuner.Resources.Input;
using static HorizonTuner.Resources.Cheats;
using static HorizonTuner.Resources.Memory;
using MahApps.Metro.Controls;
using HorizonTuner.Utilities;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private const string LocalPlayerSignature = "F3 0F ? ? ? 49 8B ? 49 8B ? 0F 28";
    private const string CrcSignature = "4C 3B ? 0F 95 ? 0F 94";

    private DateTime _velApplyWindowStartUtc = DateTime.UtcNow;
    private int _velApplyWindowStartCounter;
    private ThrottledAction<string>? _gamepadStatusThrottledUpdate;
    private ThrottledAction<string>? _velocityDiagnosticsThrottledUpdate;

    private void InitializeThrottledUpdates()
    {
        _gamepadStatusThrottledUpdate = new ThrottledAction<string>(
            text => ViewModel.GamepadStatusText = text,
            TimeSpan.FromMilliseconds(500));

        _velocityDiagnosticsThrottledUpdate = new ThrottledAction<string>(
            text => ViewModel.VelocityDiagnosticsText = text,
            TimeSpan.FromMilliseconds(500));
    }

    private void CancelThrottledUpdates()
    {
        _gamepadStatusThrottledUpdate?.Cancel();
        _velocityDiagnosticsThrottledUpdate?.Cancel();
    }

    private void StartGamepadStatusLoop()
    {
        StopGamepadStatusLoop();
        InitializeThrottledUpdates();
        _gamepadStatusCts = new CancellationTokenSource();
        _gamepadStatusTask = RunGamepadStatusAsync(_gamepadStatusCts.Token);
    }

    private void StopGamepadStatusLoop()
    {
        CancelLoop(ref _gamepadStatusCts, ref _gamepadStatusTask);
        CancelThrottledUpdates();
    }

    private async Task RunGamepadStatusAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var window = now - _autoWriteStatsWindowUtc;
                if (window >= TimeSpan.FromSeconds(1))
                {
                    ResetPerformanceCounters();
                    _autoWriteStatsWindowUtc = now;
                }

                var (text, snapshot) = await BuildGamepadStatusSnapshotAsync();
                var velocityDiag = BuildVelocityDiagnosticsText(snapshot);

                _gamepadStatusThrottledUpdate?.Invoke(text);
                _velocityDiagnosticsThrottledUpdate?.Invoke(velocityDiag);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gamepad status loop error: {ex.Message}");
            }

            try
            {
                await Task.Delay(200, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void ResetPerformanceCounters()
    {
        Interlocked.Exchange(ref _velocityWriteAttempts, 0);
        Interlocked.Exchange(ref _wheelspeedWriteAttempts, 0);
        Interlocked.Exchange(ref _velocityLoopTicks, 0);
        Interlocked.Exchange(ref _velocityMultiStageWriteAttempts, 0);
        Interlocked.Exchange(ref _velocityMultiStageWriteFails, 0);
        Interlocked.Exchange(ref _velocityMultiStageLoopTicks, 0);
        Interlocked.Exchange(ref _velocityMultiStageStateOk, 0);
        Interlocked.Exchange(ref _velocityMultiStageStateFail, 0);
        Interlocked.Exchange(ref _velocityMultiStagePressedTicks, 0);
        Interlocked.Exchange(ref _velocityMultiStageExceptions, 0);
        Interlocked.Exchange(ref _wheelspeedLoopTicks, 0);
        Interlocked.Exchange(ref _jumpLoopTicks, 0);
        Interlocked.Exchange(ref _brakeLoopTicks, 0);
        Interlocked.Exchange(ref _brakeAssistLoopTicks, 0);
    }

    private async Task<(string text, VelocityUiSnapshot snapshot)> BuildGamepadStatusSnapshotAsync()
    {
        var isAvailable = _gamepadReader.IsAvailable;
        XInputState state = default;
        var hasState = isAvailable && _gamepadReader.TryGetState(out state);
        var cfg = _configStore.Get();

        var text = isAvailable
            ? hasState
                ? cfg.ShowAdvancedVelocityDiagnostics
                    ? $"手柄已连接 (index={_gamepadReader.CurrentUserIndex})  RT={state.Gamepad.bRightTrigger}  LT={state.Gamepad.bLeftTrigger}"
                    : $"手柄已连接 (index={_gamepadReader.CurrentUserIndex})"
                : "手柄未检测到"
            : "XInput 不可用";

        var snapshot = await Application.Current.Dispatcher.InvokeAsync(() => new VelocityUiSnapshot(
            VelSwitch.IsOn,
            VelLinearSwitch.IsOn,
            VelMultiStageSwitch.IsOn,
            GetThrottleThresholdByte(),
            hasState,
            state,
            text
        ));

        return (text, snapshot);
    }

    private void VelocityAdvancedDiagnosticsSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        var config = _configStore.Get();
        config.ShowAdvancedVelocityDiagnostics = toggleSwitch.IsOn;
        _configStore.Save(config);
    }

    private readonly record struct VelocityUiSnapshot(
        bool FixedOn,
        bool LinearOn,
        bool MultiOn,
        byte ThresholdByte,
        bool HasState,
        XInputState State,
        string GamepadStatusText);

    private string BuildVelocityDiagnosticsText(VelocityUiSnapshot snapshot)
    {
        var address = _carCheats.LocalPlayerHookDetourAddress;
        var addressOk = address > UIntPtr.Zero;

        var (baseBoostPerSec, maxBoostPerSec) = CalculateBoostValues(snapshot);
        var calcBoost = snapshot.HasState && IsThrottlePressed(snapshot)
            ? ApplyVelocityChaseLimitBoost(address, baseBoostPerSec, maxBoostPerSec)
            : 1f;

        var mode = snapshot.MultiOn ? "Multi" : snapshot.LinearOn ? "Linear" : snapshot.FixedOn ? "Fixed" : "Off";
        var hookActive = addressOk && _carCheats.IsLocalPlayerHookActive();
        var crcOk = GetClass<Bypass>().IsCrcPatchApplied();

        var memData = ReadMemoryDiagnosticsData(address, addressOk);
        UpdateApplyRateEstimate(memData.ApplyCounter);

        var speedKmh = 0d;
        var speedOk = addressOk && TryReadSpeedKmh(address, out speedKmh);
        var config = _configStore.Get();

        return config.ShowAdvancedVelocityDiagnostics
            ? BuildAdvancedDiagnosticsText(snapshot, mode, hookActive, crcOk, memData, calcBoost, speedOk, speedKmh, config)
            : BuildSimpleDiagnosticsText(snapshot, mode, hookActive, crcOk, config);
    }

    private (float baseBoost, float maxBoost) CalculateBoostValues(VelocityUiSnapshot snapshot)
    {
        if (!IsThrottlePressed(snapshot))
        {
            return (1f, 1f);
        }

        var uThrottle = CalculateThrottleInput(snapshot);
        var uSpeed = CalculateSpeedRatio();

        if (snapshot.MultiOn)
        {
            var speedBoost = CalculateVelocityMultiStageBoost(uSpeed);
            var baseBoost = 1f + (speedBoost - 1f) * (float)Math.Clamp(uThrottle, 0d, 1d);
            var maxBoost = 1f + (_velocityBoostValue - 1f) * (float)Math.Clamp(uThrottle, 0d, 1d);
            return (baseBoost, maxBoost);
        }
        else if (snapshot.LinearOn)
        {
            var baseBoost = CalculateVelocityLinearBoost(uThrottle);
            var maxBoost = 1f + (_velocityBoostValue - 1f) * (float)Math.Clamp(uThrottle, 0d, 1d);
            return (baseBoost, maxBoost);
        }
        else if (snapshot.FixedOn)
        {
            return (_velocityBoostValue, _velocityBoostValue);
        }

        return (1f, 1f);
    }

    private bool IsThrottlePressed(VelocityUiSnapshot snapshot)
    {
        if (!snapshot.HasState) return false;
        var rt = snapshot.State.Gamepad.bRightTrigger;
        return rt >= snapshot.ThresholdByte;
    }

    private double CalculateThrottleInput(VelocityUiSnapshot snapshot)
    {
        if (!snapshot.HasState) return 0d;
        var rt = snapshot.State.Gamepad.bRightTrigger;
        return CalculateTriggerU(rt, snapshot.ThresholdByte);
    }

    private double CalculateSpeedRatio()
    {
        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (!TryReadSpeedKmh(address, out var speedKmh))
        {
            return 0d;
        }

        var config = _configStore.Get();
        var mode = config.VelocityMode;
        var maxKmh = mode switch
        {
            VelocityMode.ACar => config.VelocityAMaxKmh,
            VelocityMode.S1Car => config.VelocityS1MaxKmh,
            VelocityMode.S2Car => config.VelocityS2MaxKmh,
            VelocityMode.Custom => config.VelocityCustomMaxKmh,
            _ => config.VelocityS2MaxKmh
        };

        if (!double.IsFinite(maxKmh))
        {
            maxKmh = 320d;
        }

        maxKmh = Math.Clamp(maxKmh, 50d, 600d);
        return Math.Clamp(speedKmh / Math.Max(1d, maxKmh), 0d, 1d);
    }

    private MemoryDiagnosticsData ReadMemoryDiagnosticsData(UIntPtr address, bool addressOk)
    {
        var data = new MemoryDiagnosticsData();

        if (!addressOk)
        {
            return data;
        }

        try
        {
            data.Enabled = GetInstance().ReadMemory<byte>(unchecked((nuint)(address + CarCheatsOffsets.VelEnabled)));
            data.Boost = GetInstance().ReadMemory<float>(unchecked((nuint)(address + CarCheatsOffsets.VelBoost)));
            data.Limit = GetInstance().ReadMemory<float>(unchecked((nuint)(address + CarCheatsOffsets.VelLimit)));
            data.ApplyCounter = GetInstance().ReadMemory<int>(unchecked((nuint)(address + CarCheatsOffsets.VelApplyCounter)));
            data.LocalPlayer = GetInstance().ReadMemory<UIntPtr>(unchecked((nuint)(address + CarCheatsOffsets.LocalPlayer)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Read memory diagnostics failed: {ex.Message}");
        }

        return data;
    }

    private void UpdateApplyRateEstimate(int applyCounter)
    {
        var now = DateTime.UtcNow;
        if (_carCheats.LocalPlayerHookDetourAddress > UIntPtr.Zero)
        {
            UpdateDetourApplyHzEstimate(applyCounter, now);
        }

        var window = now - _velApplyWindowStartUtc;
        if (window >= TimeSpan.FromSeconds(1) || window < TimeSpan.Zero)
        {
            _velApplyWindowStartUtc = now;
            _velApplyWindowStartCounter = applyCounter;
        }
    }

    private string BuildSimpleDiagnosticsText(VelocityUiSnapshot snapshot, string mode, bool hookActive, bool crcOk, HandlingAutoConfig config)
    {
        var writes = Interlocked.Read(ref _velocityWriteAttempts);
        var multiWrites = Interlocked.Read(ref _velocityMultiStageWriteAttempts);
        var ticks = Interlocked.Read(ref _velocityLoopTicks);
        var multiTicks = Interlocked.Read(ref _velocityMultiStageLoopTicks);
        var multiStateFail = Interlocked.Read(ref _velocityMultiStageStateFail);
        var multiExceptions = Interlocked.Read(ref _velocityMultiStageExceptions);
        var multiWriteFails = Interlocked.Read(ref _velocityMultiStageWriteFails);

        var loopOk = mode == "Multi" ? multiTicks > 0 : ticks > 0;
        var writeOk = mode == "Multi" ? multiWrites > 0 : writes > 0;
        var stateText = "Ok";
        if (mode == "Multi")
        {
            if (!hookActive)
            {
                stateText = "HookOff";
            }
            else if (multiExceptions > 0)
            {
                stateText = $"Ex:{_velocityMultiStageLastExceptionType ?? "Unknown"}";
            }
            else if (multiStateFail > 0)
            {
                stateText = "StateFail";
            }
            else if (multiWriteFails > 0)
            {
                stateText = "WriteFail";
            }
        }
        var aob = BuildAobSignatureSummary();

        return $"速度模式={mode}  HookActive={(hookActive ? "Y" : "N")}  CrcOk={(crcOk ? "Y" : "N")}  {aob}  " +
               $"pressed={(IsThrottlePressed(snapshot) ? "Y" : "N")}  loop={(loopOk ? "Y" : "N")}  write={(writeOk ? "Y" : "N")}  " +
               $"chase={(config.VelocityChaseLimitOn ? "Y" : "N")}  state={stateText}  ({snapshot.GamepadStatusText})";
    }

    private static string BuildAobSignatureSummary()
    {
        static string Fmt(string sig)
        {
            if (!CheatsUtilities.TryGetLastAobDiagnostics(sig, out var diag) || diag == null)
            {
                return "N/A";
            }

            var readsTotal = diag.ReadsOk + diag.ReadsFailed + diag.ReadsPartial;
            var failRate = readsTotal > 0 ? (double)diag.ReadsFailed / readsTotal : 0d;
            return $"m={diag.Matches} fail={failRate:P0} ms={diag.ElapsedMs:0}";
        }

        var lp = Fmt(LocalPlayerSignature);
        var crc = Fmt(CrcSignature);
        return $"Sig(LP:{lp} CRC:{crc})";
    }

    private string BuildAdvancedDiagnosticsText(
        VelocityUiSnapshot snapshot,
        string mode,
        bool hookActive,
        bool crcOk,
        MemoryDiagnosticsData memData,
        float calcBoost,
        bool speedOk,
        double speedKmh,
        HandlingAutoConfig config)
    {
        var address = _carCheats.LocalPlayerHookDetourAddress;
        var addrText = address > UIntPtr.Zero ? $"0x{address.ToUInt64():X}" : "N/A";

        var (baseBoostPerSec, maxBoostPerSec) = CalculateBoostValues(snapshot);
        var applyHz = GetDetourApplyHzEstimate();
        var calcBoostTick = ToBoostPerApply(calcBoost);

        var speedText = speedOk ? $"{speedKmh:0.0}kmh" : "N/A";
        var limitKmh = _velocityLimitValue;
        var limitMph = limitKmh * 0.621371f;
        var memVelLimitKmh = memData.Limit / 0.621371f;
        var localPlayerText = memData.LocalPlayer > UIntPtr.Zero ? $"0x{memData.LocalPlayer.ToUInt64():X}" : "0x0";
        var error = speedOk && limitKmh > 0f ? Math.Clamp((limitKmh - (float)speedKmh) / limitKmh, -1d, 1d) : 0d;

        var window = DateTime.UtcNow - _velApplyWindowStartUtc;
        var applyPerSec = window.TotalSeconds > 0d
            ? (memData.ApplyCounter - _velApplyWindowStartCounter) / window.TotalSeconds
            : 0d;

        var writes = Interlocked.Read(ref _velocityWriteAttempts);
        var multiWrites = Interlocked.Read(ref _velocityMultiStageWriteAttempts);
        var multiFails = Interlocked.Read(ref _velocityMultiStageWriteFails);
        var ticks = Interlocked.Read(ref _velocityLoopTicks);
        var multiTicks = Interlocked.Read(ref _velocityMultiStageLoopTicks);
        var multiStateOk = Interlocked.Read(ref _velocityMultiStageStateOk);
        var multiStateFail = Interlocked.Read(ref _velocityMultiStageStateFail);
        var multiPressedTicks = Interlocked.Read(ref _velocityMultiStagePressedTicks);
        var multiExceptions = Interlocked.Read(ref _velocityMultiStageExceptions);
        var lastEx = multiExceptions > 0
            ? $"  lastEx={_velocityMultiStageLastExceptionType ?? "Unknown"}@{_velocityMultiStageLastExceptionUtc:HH:mm:ss}"
            : "";

        var maxKmh = config.VelocityMode switch
        {
            VelocityMode.ACar => config.VelocityAMaxKmh,
            VelocityMode.S1Car => config.VelocityS1MaxKmh,
            VelocityMode.S2Car => config.VelocityS2MaxKmh,
            VelocityMode.Custom => config.VelocityCustomMaxKmh,
            _ => config.VelocityS2MaxKmh
        };

        var uThrottle = CalculateThrottleInput(snapshot);
        var uSpeed = CalculateSpeedRatio();

        // 多段式加速参数诊断
        var multiStageParams = snapshot.MultiOn
            ? $"  s1g={_velocityStage1Gamma:0.00} s2g={_velocityStage2Gamma:0.00} s3g={_velocityStage3Gamma:0.00} s1s={_velocityStage1Scale:0.00} s2s={_velocityStage2Scale:0.00} s3s={_velocityStage3Scale:0.00}"
            : "";
        var aob = BuildAobSignatureSummary();

        return $"速度模式={mode}  Detour={addrText}  HookActive={(hookActive ? "Y" : "N")}  CrcOk={(crcOk ? "Y" : "N")}  " +
               $"{aob}  " +
               $"阈值={snapshot.ThresholdByte}  pressed={(IsThrottlePressed(snapshot) ? "Y" : "N")}  uThrottle={uThrottle:0.000}  " +
               $"speed={speedText}  maxKmh={maxKmh:0}  uSpeed={uSpeed:0.000}  " +
               $"chase={(config.VelocityChaseLimitOn ? "Y" : "N")}  err={error:0.000}  baseBoost={baseBoostPerSec:0.000}  calcBoost={calcBoost:0.000}  boostTick={calcBoostTick:0.000000}  applyHz={applyHz:0.0}  " +
               $"limit={limitKmh:0.0}kmh({limitMph:0.0}mph)  " +
               $"mem(en={memData.Enabled},boost={memData.Boost:0.000},limit={memData.Limit:0.0}mph~{memVelLimitKmh:0.0}kmh,applied/s={applyPerSec:0.0})  localPlayer={localPlayerText}  " +
               $"write/s={writes}  multiWrite/s={multiWrites}  multiFail/s={multiFails}  tick/s={ticks}  multiTick/s={multiTicks}  " +
               $"multiStateOk/s={multiStateOk}  multiStateFail/s={multiStateFail}  multiPressedTick/s={multiPressedTicks}  multiEx/s={multiExceptions}{lastEx}{multiStageParams}  ({snapshot.GamepadStatusText})";
    }

    private struct MemoryDiagnosticsData
    {
        public byte Enabled;
        public float Boost;
        public float Limit;
        public int ApplyCounter;
        public UIntPtr LocalPlayer;
    }
}
