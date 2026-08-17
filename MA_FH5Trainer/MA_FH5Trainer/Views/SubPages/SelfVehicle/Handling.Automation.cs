using System.Windows;
using HorizonTuner.Cheats;
using HorizonTuner.Cheats.ForzaHorizon5;
using HorizonTuner.Resources.Config;
using HorizonTuner.Services;
using static HorizonTuner.Resources.Cheats;
using static HorizonTuner.Resources.Memory;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private const int LocalPlayerVelocityXOffset = 0x20;
    private const int LocalPlayerVelocityYOffset = 0x24;
    private const int LocalPlayerVelocityZOffset = 0x28;

    private static void CancelLoop(ref CancellationTokenSource? cts, ref Task? task)
    {
        try
        {
            cts?.Cancel();
        }
        catch
        {
        }

        cts?.Dispose();
        cts = null;
        task = null;
    }

    private static Task CancelLoopAsync(ref CancellationTokenSource? cts, ref Task? task, int timeoutMs)
    {
        Task? pending = task;
        try
        {
            cts?.Cancel();
        }
        catch
        {
        }

        cts?.Dispose();
        cts = null;
        task = null;
        return pending == null ? Task.CompletedTask : WaitForTaskOrTimeout(pending, timeoutMs);
    }

    private static async Task WaitForTaskOrTimeout(Task pending, int timeoutMs)
    {
        try
        {
            await Task.WhenAny(pending, Task.Delay(timeoutMs)).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private void StartVelocityAutoLoop()
    {
        StopVelocityAuto();
        _velocityAutoCts = new CancellationTokenSource();
        _velocityAutoTask = Task.Run(() => RunVelocityAutoAsync(_velocityAutoCts.Token));
    }

    private void StopVelocityAuto()
    {
        CancelLoop(ref _velocityAutoCts, ref _velocityAutoTask);
        _throttlePrevDown = false;

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address > UIntPtr.Zero)
        {
            _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)0, false);
        }
    }

    private async Task RunVelocityAutoAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
            var lastParamWriteUtc = DateTime.MinValue;
            var lastBoost = float.NaN;
            var lastLimit = float.NaN;
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _velocityLoopTicks);
                try
                {
                    await EnsureLocalPlayerDetourAsync(token).ConfigureAwait(false);

                    var address = _carCheats.LocalPlayerHookDetourAddress;
                    if (address > UIntPtr.Zero && _gamepadReader.TryGetState(out var state))
                    {
                        var pressed = state.Gamepad.bRightTrigger >= GetThrottleThresholdByte();

                        if (pressed)
                        {
                            var boost = ToBoostPerApply(_velocityBoostValue);
                            var limit = KmhToMph(_velocityLimitValue);
                            var now = DateTime.UtcNow;
                            if (!_throttlePrevDown ||
                                now - lastParamWriteUtc >= TimeSpan.FromMilliseconds(100) ||
                                !float.IsFinite(lastBoost) ||
                                !float.IsFinite(lastLimit) ||
                                Math.Abs(boost - lastBoost) >= 0.0005f ||
                                Math.Abs(limit - lastLimit) >= 0.05f)
                            {
                                lastParamWriteUtc = now;
                                lastBoost = boost;
                                lastLimit = limit;
                                _memoryWriter.Write(address + CarCheatsOffsets.VelBoost, boost, false);
                                _memoryWriter.Write(address + CarCheatsOffsets.VelLimit, limit, false);
                            }

                            _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)1, false);
                            Interlocked.Increment(ref _velocityWriteAttempts);
                        }
                        else if (!pressed && _throttlePrevDown)
                        {
                            _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)0, false);
                        }

                        _throttlePrevDown = pressed;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RunVelocityAutoAsync loop error: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需处理
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunVelocityAutoAsync fatal error: {ex.Message}");
        }
    }

    private void StartVelocityLinearAutoLoop()
    {
        StopVelocityLinearAuto();
        _velocityLinearAutoCts = new CancellationTokenSource();
        _velocityLinearAutoTask = Task.Run(() => RunVelocityLinearAutoAsync(_velocityLinearAutoCts.Token));
    }

    private void StopVelocityLinearAuto()
    {
        CancelLoop(ref _velocityLinearAutoCts, ref _velocityLinearAutoTask);
        _throttleLinearPrevDown = false;

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address > UIntPtr.Zero)
        {
            _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)0, false);
        }
    }

    private void StartVelocityMultiStageAutoLoop()
    {
        StopVelocityMultiStageAuto();
        _velocityMultiStageAutoCts = new CancellationTokenSource();
        _velocityMultiStageAutoTask = Task.Run(() => RunVelocityMultiStageAutoAsync(_velocityMultiStageAutoCts.Token));
    }

    private void StopVelocityMultiStageAuto()
    {
        CancelLoop(ref _velocityMultiStageAutoCts, ref _velocityMultiStageAutoTask);
        _throttleMultiStagePrevDown = false;
        _velocityMultiStageSpeedUEma = double.NaN;
        _velocityMultiStageLastSpeedU = double.NaN;

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address > UIntPtr.Zero)
        {
            _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)0, false);
        }
    }

    private async Task RunVelocityMultiStageAutoAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
            var lastParamWriteUtc = DateTime.MinValue;
            var lastU = -1d;

            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _velocityMultiStageLoopTicks);
                try
                {
                    await EnsureLocalPlayerDetourAsync(token).ConfigureAwait(false);

                    var address = _carCheats.LocalPlayerHookDetourAddress;
                    if (address <= UIntPtr.Zero)
                    {
                        Interlocked.Increment(ref _velocityMultiStageStateFail);
                        continue;
                    }

                    if (!_gamepadReader.TryGetState(out var state))
                    {
                        Interlocked.Increment(ref _velocityMultiStageStateFail);
                        continue;
                    }

                    Interlocked.Increment(ref _velocityMultiStageStateOk);

                    var thresholdByte = GetThrottleThresholdByte();
                    var rt = state.Gamepad.bRightTrigger;
                    var pressed = rt >= thresholdByte;

                    if (pressed)
                    {
                        Interlocked.Increment(ref _velocityMultiStagePressedTicks);

                        var uThrottle = CalculateTriggerU(rt, thresholdByte);
                        var uSpeed = CalculateVelocitySpeedU(address);
                        var speedBoostPerSec = CalculateVelocityMultiStageBoost(uSpeed);
                        var baseBoostPerSec = 1f + (speedBoostPerSec - 1f) * (float)Math.Clamp(uThrottle, 0d, 1d);
                        var maxBoostPerSec = 1f + (_velocityBoostValue - 1f) * (float)Math.Clamp(uThrottle, 0d, 1d);
                        var boostPerSec = ApplyVelocityChaseLimitBoost(address, baseBoostPerSec, maxBoostPerSec);
                        var boost = ToBoostPerApply(boostPerSec);
                        var now = DateTime.UtcNow;
                        var nearBoundary = Math.Abs(uSpeed - _velocityStage1End) <= 0.03d ||
                                           Math.Abs(uSpeed - _velocityStage2End) <= 0.03d;
                        var uThreshold = nearBoundary ? 0.005d : 0.01d;
                        var minIntervalMs = nearBoundary ? 50 : 100;

                        if (Math.Abs(uSpeed - lastU) >= uThreshold ||
                            now - lastParamWriteUtc >= TimeSpan.FromMilliseconds(minIntervalMs))
                        {
                            lastU = uSpeed;
                            lastParamWriteUtc = now;
                            var ok1 = _memoryWriter.Write(address + CarCheatsOffsets.VelBoost, boost, false);
                            var ok2 = _memoryWriter.Write(address + CarCheatsOffsets.VelLimit, KmhToMph(_velocityLimitValue), false);
                            if (!ok1 || !ok2)
                            {
                                Interlocked.Increment(ref _velocityMultiStageWriteFails);
                            }
                        }

                        var ok3 = _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)1, false);
                        if (!ok3)
                        {
                            Interlocked.Increment(ref _velocityMultiStageWriteFails);
                        }
                        Interlocked.Increment(ref _velocityMultiStageWriteAttempts);
                    }
                    else if (!pressed && _throttleMultiStagePrevDown)
                    {
                        _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)0, false);
                    }

                    _throttleMultiStagePrevDown = pressed;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _velocityMultiStageExceptions);
                    _velocityMultiStageLastExceptionUtc = DateTime.UtcNow;
                    _velocityMultiStageLastExceptionType = ex.GetType().Name;
                    _velocityMultiStageLastExceptionMessage = ex.Message;
                    System.Diagnostics.Debug.WriteLine($"RunVelocityMultiStageAutoAsync loop error: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private double CalculateVelocitySpeedU(UIntPtr detourAddress)
    {
        if (!TryReadSpeedKmh(detourAddress, out var kmh))
        {
            var lastU = _velocityMultiStageLastSpeedU;
            if (double.IsFinite(lastU))
            {
                return Math.Clamp(lastU, 0d, 1d);
            }

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

        var uRaw = Math.Clamp(kmh / Math.Max(1d, maxKmh), 0d, 1d);
        var ema = _velocityMultiStageSpeedUEma;
        if (!double.IsFinite(ema))
        {
            ema = uRaw;
        }
        else
        {
            const double alpha = 0.15d;
            ema = ema + (uRaw - ema) * alpha;

            var prev = _velocityMultiStageLastSpeedU;
            if (double.IsFinite(prev))
            {
                const double maxStep = 0.05d;
                ema = Math.Clamp(ema, prev - maxStep, prev + maxStep);
            }
        }

        ema = Math.Clamp(ema, 0d, 1d);
        _velocityMultiStageSpeedUEma = ema;
        _velocityMultiStageLastSpeedU = ema;
        return ema;
    }

    private static bool TryReadSpeedKmh(UIntPtr detourAddress, out double kmh)
    {
        kmh = 0d;
        if (detourAddress <= UIntPtr.Zero)
        {
            return false;
        }

        var localPlayerPtr = GetInstance().ReadMemory<UIntPtr>(unchecked((nuint)(detourAddress + CarCheatsOffsets.LocalPlayer)));
        if (localPlayerPtr <= UIntPtr.Zero)
        {
            return false;
        }

        var vx = GetInstance().ReadMemory<float>(unchecked((nuint)(localPlayerPtr + LocalPlayerVelocityXOffset)));
        var vy = GetInstance().ReadMemory<float>(unchecked((nuint)(localPlayerPtr + LocalPlayerVelocityYOffset)));
        var vz = GetInstance().ReadMemory<float>(unchecked((nuint)(localPlayerPtr + LocalPlayerVelocityZOffset)));

        if (!float.IsFinite(vx) || !float.IsFinite(vy) || !float.IsFinite(vz))
        {
            return false;
        }

        var speedMetersPerSecond = Math.Sqrt((double)vx * vx + (double)vz * vz);
        kmh = speedMetersPerSecond * 3.6d;
        return double.IsFinite(kmh);
    }

    private float ApplyVelocityChaseLimitBoost(UIntPtr detourAddress, float baseBoostPerSec, float maxBoostPerSec)
    {
        var config = _configStore.Get();
        if (!config.VelocityChaseLimitOn)
        {
            return baseBoostPerSec;
        }

        if (!TryReadSpeedKmh(detourAddress, out var speedKmh))
        {
            return baseBoostPerSec;
        }

        var limitKmh = Math.Max(1f, _velocityLimitValue);
        var error = (limitKmh - speedKmh) / limitKmh;
        if (!double.IsFinite(error) || error <= 0d)
        {
            return baseBoostPerSec;
        }

        var kp = double.IsFinite(config.VelocityChaseKp) ? Math.Clamp(config.VelocityChaseKp, 0.1, 10.0) : 1.2;
        var max = float.IsFinite(maxBoostPerSec) ? Math.Clamp(maxBoostPerSec, 1f, 2f) : 1f;
        var maxDelta = max - 1f;
        if (maxDelta <= 0f)
        {
            return baseBoostPerSec;
        }

        var baseDelta = float.IsFinite(baseBoostPerSec) ? Math.Clamp(baseBoostPerSec - 1f, 0f, maxDelta) : 0f;
        var chaseDelta = Math.Clamp((float)(kp * error), 0f, maxDelta);
        var minDelta = double.IsFinite(config.VelocityChaseMinDelta) ? Math.Clamp(config.VelocityChaseMinDelta, 0.0, 1.0) : 0.01;
        var minDeltaF = Math.Min((float)minDelta, maxDelta);
        chaseDelta = Math.Clamp(chaseDelta, minDeltaF, maxDelta);

        return 1f + Math.Max(baseDelta, chaseDelta);
    }

    private async Task RunVelocityLinearAutoAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
            var lastParamWriteUtc = DateTime.MinValue;
            var lastU = -1d;
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                try
                {
                    await EnsureLocalPlayerDetourAsync(token).ConfigureAwait(false);

                    var address = _carCheats.LocalPlayerHookDetourAddress;
                    if (address > UIntPtr.Zero && _gamepadReader.TryGetState(out var state))
                    {
                        var thresholdByte = GetThrottleThresholdByte();
                        var rt = state.Gamepad.bRightTrigger;
                        var pressed = rt >= thresholdByte;

                        if (pressed)
                        {
                            var u = CalculateTriggerU(rt, thresholdByte);
                            var baseBoostPerSec = CalculateVelocityLinearBoost(u);
                            var maxBoostPerSec = 1f + (_velocityBoostValue - 1f) * (float)Math.Clamp(u, 0d, 1d);
                            var boostPerSec = ApplyVelocityChaseLimitBoost(address, baseBoostPerSec, maxBoostPerSec);
                            var boost = ToBoostPerApply(boostPerSec);
                            var now = DateTime.UtcNow;
                            if (Math.Abs(u - lastU) >= 0.01d || now - lastParamWriteUtc >= TimeSpan.FromMilliseconds(100))
                            {
                                lastU = u;
                                lastParamWriteUtc = now;
                                _memoryWriter.Write(address + CarCheatsOffsets.VelBoost, boost, false);
                                _memoryWriter.Write(address + CarCheatsOffsets.VelLimit, KmhToMph(_velocityLimitValue), false);
                            }

                            _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)1, false);
                        }
                        else if (!pressed && _throttleLinearPrevDown)
                        {
                            _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)0, false);
                        }

                        _throttleLinearPrevDown = pressed;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RunVelocityLinearAutoAsync loop error: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需处理
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunVelocityLinearAutoAsync fatal error: {ex.Message}");
        }
    }

    private void StartWheelspeedAutoLoop()
    {
        StopWheelspeedAuto();
        _wheelspeedAutoCts = new CancellationTokenSource();
        _wheelspeedAutoTask = Task.Run(() => RunWheelspeedAutoAsync(_wheelspeedAutoCts.Token));
    }

    private void StopWheelspeedAuto()
    {
        CancelLoop(ref _wheelspeedAutoCts, ref _wheelspeedAutoTask);

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address > UIntPtr.Zero)
        {
            _memoryWriter.Write(address + CarCheatsOffsets.WheelspeedEnabled, (byte)0, false);
        }
    }

    private async Task RunWheelspeedAutoAsync(CancellationToken token)
    {
        try
        {
            var prev = false;
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _wheelspeedLoopTicks);
                try
                {
                    await EnsureLocalPlayerDetourAsync(token).ConfigureAwait(false);

                    var address = _carCheats.LocalPlayerHookDetourAddress;
                    if (address > UIntPtr.Zero && _gamepadReader.TryGetState(out var state))
                    {
                        var pressed = state.Gamepad.bRightTrigger >= GetThrottleThresholdByte();

                        if (pressed)
                        {
                            _memoryWriter.Write(address + CarCheatsOffsets.WheelspeedMode, _wheelspeedModeValue, false);
                            _memoryWriter.Write(address + CarCheatsOffsets.WheelspeedBoost, _wheelspeedBoostValue, false);
                            _memoryWriter.Write(address + CarCheatsOffsets.WheelspeedLimit, _wheelspeedLimitValue, false);
                            _memoryWriter.Write(address + CarCheatsOffsets.WheelspeedEnabled, (byte)1, false);
                            Interlocked.Increment(ref _wheelspeedWriteAttempts);
                        }
                        else if (!pressed && prev)
                        {
                            _memoryWriter.Write(address + CarCheatsOffsets.WheelspeedEnabled, (byte)0, false);
                        }

                        prev = pressed;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RunWheelspeedAutoAsync loop error: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需处理
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunWheelspeedAutoAsync fatal error: {ex.Message}");
        }
    }

    private void StartJumpAutoLoop()
    {
        StopJumpAuto();
        _jumpAutoCts = new CancellationTokenSource();
        _jumpAutoTask = Task.Run(() => RunJumpAutoAsync(_jumpAutoCts.Token));
    }

    private void StopJumpAuto()
    {
        CancelLoop(ref _jumpAutoCts, ref _jumpAutoTask);
        _jumpButtonPrevDown = false;
    }

    private async Task RunJumpAutoAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _jumpLoopTicks);
                await EnsureLocalPlayerDetourAsync(token).ConfigureAwait(false);

                var address = _carCheats.LocalPlayerHookDetourAddress;
                if (address > UIntPtr.Zero && _gamepadReader.TryGetState(out var state))
                {
                    var down = _gamepadReader.IsButtonDown(state, GetJumpButton());
                    if (down && !_jumpButtonPrevDown)
                    {
                        _memoryWriter.Write(address + CarCheatsOffsets.JumpHackEnabled, (byte)1, false);
                    }

                    _jumpButtonPrevDown = down;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需处理
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunJumpAutoAsync fatal error: {ex.Message}");
        }
    }

    private void StartBrakeAutoLoop()
    {
        StopBrakeAuto();
        _brakeAutoCts = new CancellationTokenSource();
        _brakeAutoTask = Task.Run(() => RunBrakeAutoAsync(_brakeAutoCts.Token));
    }

    private void StopBrakeAuto()
    {
        CancelLoop(ref _brakeAutoCts, ref _brakeAutoTask);
        _brakePrevDown = false;

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address > UIntPtr.Zero)
        {
            _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackEnabled, (byte)0, false);
            _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackBoost, 1f, false);
        }
    }

    private async Task RunBrakeAutoAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _brakeLoopTicks);
                await EnsureLocalPlayerDetourAsync(token).ConfigureAwait(false);

                var address = _carCheats.LocalPlayerHookDetourAddress;
                if (address > UIntPtr.Zero && _gamepadReader.TryGetState(out var state))
                {
                    var lt = state.Gamepad.bLeftTrigger;
                    var pressed = lt >= GetBrakeThresholdByte();

                    if (pressed)
                    {
                        var t = lt / 255d;
                        var boost = CalculateSuperBrakeBoost(t);
                        _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackBoost, (float)boost, false);
                        _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackEnabled, (byte)1, false);
                    }
                    else if (!pressed && _brakePrevDown)
                    {
                        _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackEnabled, (byte)0, false);
                        _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackBoost, 1f, false);
                    }

                    _brakePrevDown = pressed;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需处理
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunBrakeAutoAsync fatal error: {ex.Message}");
        }
    }

    private void StartBrakeAssistAutoLoop()
    {
        StopBrakeAssistAuto();
        _brakeAssistAutoCts = new CancellationTokenSource();
        _brakeAssistAutoTask = Task.Run(() => RunBrakeAssistAutoAsync(_brakeAssistAutoCts.Token));
    }

    private void StopBrakeAssistAuto()
    {
        CancelLoop(ref _brakeAssistAutoCts, ref _brakeAssistAutoTask);
        _brakeAssistPrevDown = false;

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address > UIntPtr.Zero)
        {
            _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackEnabled, (byte)0, false);
            _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackBoost, 1f, false);
        }
    }

    private async Task RunBrakeAssistAutoAsync(CancellationToken token)
    {
        try
        {
            var lastUiUpdateUtc = DateTime.MinValue;
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _brakeAssistLoopTicks);
                await EnsureLocalPlayerDetourAsync(token).ConfigureAwait(false);

                var address = _carCheats.LocalPlayerHookDetourAddress;
                if (address > UIntPtr.Zero && _gamepadReader.TryGetState(out var state))
                {
                    var lt = state.Gamepad.bLeftTrigger;
                    var pressed = lt >= GetBrakeThresholdByte();

                    if (pressed)
                    {
                        var t = lt / 255d;
                        var (boost, diag) = CalculateBrakeAssist(t);

                        var ok1 = _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackBoost, (float)boost, false);
                        var ok2 = _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackEnabled, (byte)1, false);
                        if (!ok1 || !ok2)
                        {
                            Interlocked.Increment(ref _brakeAssistWriteFails);
                        }

                        var now = DateTime.UtcNow;
                        if (now - lastUiUpdateUtc >= TimeSpan.FromMilliseconds(200))
                        {
                            lastUiUpdateUtc = now;
                            var hookActive = _carCheats.IsLocalPlayerHookActive();
                            var crcOk = GetClass<Bypass>().IsCrcPatchApplied();
                            var extra =
                                $"\nHookActive: {hookActive}\nCrcPatchOk: {crcOk}\nWriteFails: {Interlocked.Read(ref _brakeAssistWriteFails)}\nHookReapply: {Interlocked.Read(ref _localPlayerHookReapplyCount)}\nDetourRebuild: {Interlocked.Read(ref _localPlayerDetourRebuildCount)}";
                            _ = Application.Current.Dispatcher.BeginInvoke(() => ViewModel.BrakeAssistDiagnosticsText = diag + extra);
                        }
                    }
                    else if (!pressed && _brakeAssistPrevDown)
                    {
                        _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackEnabled, (byte)0, false);
                        _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackBoost, 1f, false);
                        _ = Application.Current.Dispatcher.BeginInvoke(() => ViewModel.BrakeAssistDiagnosticsText = string.Empty);
                    }

                    _brakeAssistPrevDown = pressed;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需处理
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunBrakeAssistAutoAsync fatal error: {ex.Message}");
        }
    }

    private async Task EnsureLocalPlayerDetourAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        if (!IsGameAttached())
        {
            return;
        }

        if (_carCheats.LocalPlayerHookDetourAddress > UIntPtr.Zero)
        {
            if (_carCheats.IsLocalPlayerHookActive())
            {
                return;
            }

            Interlocked.Increment(ref _localPlayerHookReapplyCount);
            _carCheats.TryReapplyLocalPlayerHook();
            if (_carCheats.IsLocalPlayerHookActive())
            {
                return;
            }
        }

        var now = DateTime.UtcNow;
        if (now - _lastLocalPlayerDetourAttemptUtc < TimeSpan.FromSeconds(1))
        {
            return;
        }

        _lastLocalPlayerDetourAttemptUtc = now;

        try
        {
            await _carCheats.CheatLocalPlayer().ConfigureAwait(false);
        }
        catch
        {
        }

        if (_carCheats.LocalPlayerHookDetourAddress <= UIntPtr.Zero)
        {
            return;
        }

        Interlocked.Increment(ref _localPlayerDetourRebuildCount);
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.BrakeHackEnabled, (byte)0, false);
        _memoryWriter.Write(_carCheats.LocalPlayerHookDetourAddress + CarCheatsOffsets.BrakeHackBoost, 1f, false);
    }
}
