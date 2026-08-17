using System.Windows.Input;
using System.Windows.Threading;
using HorizonTuner.Cheats.ForzaHorizon5;
using HorizonTuner.Resources.Config;
using HorizonTuner.Resources.Keybinds;
using HorizonTuner.Services;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private const int MaxCustomVelocityPresets = 20;

    private readonly GlobalHotkey _jumpHackHotkey;
    private readonly GlobalHotkey _brakeHackHotkey;
    private readonly GlobalHotkey _velocityHotkey;
    private readonly GlobalHotkey _wheelspeedHotkey;

    private IDisposable? _shutdownRegistration;

    private readonly DispatcherTimer _handlingConfigSaveTimer = new();
    private HandlingAutoConfig? _pendingHandlingConfigSave;

    private CancellationTokenSource? _velocityAutoCts;
    private Task? _velocityAutoTask;
    private CancellationTokenSource? _velocityLinearAutoCts;
    private Task? _velocityLinearAutoTask;
    private CancellationTokenSource? _velocityMultiStageAutoCts;
    private Task? _velocityMultiStageAutoTask;
    private CancellationTokenSource? _wheelspeedAutoCts;
    private Task? _wheelspeedAutoTask;
    private CancellationTokenSource? _jumpAutoCts;
    private Task? _jumpAutoTask;
    private CancellationTokenSource? _brakeAutoCts;
    private Task? _brakeAutoTask;
    private CancellationTokenSource? _brakeAssistAutoCts;
    private Task? _brakeAssistAutoTask;
    private CancellationTokenSource? _gamepadStatusCts;
    private Task? _gamepadStatusTask;

    private bool _jumpButtonPrevDown;
    private bool _throttlePrevDown;
    private bool _throttleLinearPrevDown;
    private bool _throttleMultiStagePrevDown;
    private bool _brakePrevDown;
    private bool _brakeAssistPrevDown;

    private bool _suppressVelocityModeUiEvents;

    private global::MahApps.Metro.Controls.NumericUpDown? VelocityMaxKmhControl =>
        (global::MahApps.Metro.Controls.NumericUpDown?)FindName("VelocityMaxKmh");

    private DateTime _lastLocalPlayerDetourAttemptUtc = DateTime.MinValue;

    private long _velocityWriteAttempts;
    private long _wheelspeedWriteAttempts;
    private long _velocityLoopTicks;
    private long _velocityMultiStageWriteAttempts;
    private long _velocityMultiStageWriteFails;
    private long _velocityMultiStageLoopTicks;
    private long _velocityMultiStageStateOk;
    private long _velocityMultiStageStateFail;
    private long _velocityMultiStagePressedTicks;
    private long _velocityMultiStageExceptions;
    private DateTime _velocityMultiStageLastExceptionUtc;
    private string? _velocityMultiStageLastExceptionType;
    private string? _velocityMultiStageLastExceptionMessage;
    private double _velocityMultiStageLastSpeedU = double.NaN;
    private double _velocityMultiStageSpeedUEma = double.NaN;
    private long _wheelspeedLoopTicks;
    private long _jumpLoopTicks;
    private long _brakeLoopTicks;
    private long _brakeAssistLoopTicks;
    private long _brakeAssistWriteFails;
    private long _localPlayerHookReapplyCount;
    private long _localPlayerDetourRebuildCount;
    private DateTime _autoWriteStatsWindowUtc = DateTime.UtcNow;

    private double _detourApplyHzEma = 60d;
    private int _detourApplyHzLastCounter;
    private DateTime _detourApplyHzLastUtc = DateTime.UtcNow;

    private float _velocityBoostValue;
    private float _velocityLimitValue;
    private byte _wheelspeedModeValue;
    private float _wheelspeedBoostValue;
    private float _wheelspeedLimitValue;
    private float _jumpBoostValue;
    private int _superBrakeStrengthLevel;
    private double _brakeAssistNormalStrengthPercent;
    private double _brakeAssistPanicThresholdPercent;
    private double _brakeAssistPanicStrengthPercent;
    private double _velocityLinearScalePercent;
    private double _velocityLinearGamma;
    private double _velocityStage1Gamma;
    private double _velocityStage2Gamma;
    private double _velocityStage3Gamma;
    private double _velocityStage1Scale;
    private double _velocityStage2Scale;
    private double _velocityStage3Scale;
    private double _velocityStage1End = 0.30;
    private double _velocityStage2End = 0.70;
    private double _velocityStage1TargetFrac = 0.75;
    private double _velocityStage2TargetFrac = 0.95;

    private void InitializeHandlingConfigSave()
    {
        _handlingConfigSaveTimer.Interval = TimeSpan.FromMilliseconds(400);
        _handlingConfigSaveTimer.Tick += HandlingConfigSaveTimer_OnTick;
    }

    private void ScheduleHandlingConfigSave(HandlingAutoConfig config)
    {
        _pendingHandlingConfigSave = config;
        _handlingConfigSaveTimer.Stop();
        _handlingConfigSaveTimer.Start();
    }

    private void FlushHandlingConfigSave()
    {
        var pending = _pendingHandlingConfigSave;
        if (pending == null)
        {
            return;
        }

        _pendingHandlingConfigSave = null;
        _handlingConfigSaveTimer.Stop();
        _configStore.Save(pending);
    }

    private void JumpHackCallback()
    {
        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address <= UIntPtr.Zero)
        {
            return;
        }

        _memoryWriter.Write(address + CarCheatsOffsets.JumpHackEnabled, (byte)1);
    }

    private void BrakeHackCallback()
    {
        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address <= UIntPtr.Zero)
        {
            return;
        }

        _memoryWriter.Write(address + CarCheatsOffsets.BrakeHackEnabled, (byte)1);
    }

    private void VelocityCallback()
    {
        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address <= UIntPtr.Zero)
        {
            return;
        }

        _memoryWriter.Write(address + CarCheatsOffsets.VelEnabled, (byte)1);
    }

    private void WheelspeedCallback()
    {
        if (AppShutdownState.IsShuttingDown)
        {
            return;
        }

        var address = _carCheats.LocalPlayerHookDetourAddress;
        if (address <= UIntPtr.Zero)
        {
            return;
        }

        _memoryWriter.Write(address + CarCheatsOffsets.WheelspeedEnabled, (byte)1);
    }
}
