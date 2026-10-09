# Handling Subsystem Analysis Report

## Strengths

**Layering & separation:** The Abstractions→Curves→Implementations split is architecturally sound. The five interfaces (`ICarCheatsFacade`, `IGamepadReader`, `IHandlingAutoConfigStore`, `IMemoryWriter`, `IMiscCheatsFacade`) are cohesive and minimal — each isolates one external dependency, adhering to ISP. The Curves layer (`BrakeCurves`, `TriggerMath`, `VelocityCurves`) is pure static math with zero I/O, fully unit-testable. Implementations are thin `sealed` adapters.

**Numerical robustness:** `VelocityCurves.CalculateMultiStageBoostWithTargets` (`VelocityCurves.cs:28-78`) sanitizes all NaN/Inf, enforces `end2 ≥ end1+0.05` and `f2 ≥ f1+0.05`, monotonic breakpoints `b0<b1<b2<b3`, and is C0-continuous at stage boundaries. `ToBoostPerApply` (`UiCache.cs:53-60`) correctly computes `(1+δ)^(1/hz)` via `expm1(log1p(δ)/hz)`, decoupling boost rate from tick rate. Manual `Log1p`/`Expm1` Taylor series (5-term, `|x|<1e-4`) are sound (error ≈1e-24).

**Lifecycle robustness:** `EnsureLocalPlayerDetourAsync` (`Automation.cs:735-787`) handles detach/reattach every tick — reapply hook if inactive, rebuild detour if missing (throttled 1/sec). `AppShutdownState.IsShuttingDown` checked in callbacks and loops. `ShutdownCoordinator` registration/disposal clean. Config save debounced (400ms `DispatcherTimer`).

**Defensive EMA:** `CalculateVelocitySpeedU` (`Automation.cs:318-338`) uses EMA α=0.15 with ±0.05 step clamp, preventing single-tick spikes.

## Weaknesses

**1. DI bypass:** `Handling.xaml.cs:15-19` hand-constructs all 5 implementations instead of resolving from the DI container. Abstractions aid test substitution but aren't container-wired.

**2. Read-path asymmetry — no `IMemoryReader`:** All reads bypass the abstraction via `GetInstance().ReadMemory<T>(...)` directly — `Automation.cs:350-358` (velocity vector), `Diagnostics.cs:267-271`. Writes go through `IMemoryWriter`; reads do not. Real layering leak.

**3. Static cheat grabs in UI:** `Diagnostics.cs:708,169` call `GetClass<Bypass>()` directly; `Velocity.cs:44,52,60` call `GetClass<Bypass>()`/`GetClass<CarCheats>()` directly, bypassing `ICarCheatsFacade`.

**4. CONCRETE BUG — `PhysicsMisc.cs:118-120`:**
```csharp
toggleSwitch.Toggled -= GravToggleSwitch_OnToggled;
toggleSwitch.IsOn = false;
toggleSwitch.Toggled -= GravToggleSwitch_OnToggled;  // BUG: should be +=
```
On failed gravity injection, the handler is unsubscribed twice and never re-added — the toggle permanently loses its handler. Compare `Brake.cs:43-45` which does `-=`/`IsOn=false`/`+=`.

**5. Race in `EnsureLocalPlayerDetourAsync`:** Called from 7 loops concurrently (`Automation.cs:92,202,418,502,566,619,683`). `_lastLocalPlayerDetourAttemptUtc` (`State.cs:53`) is unsynchronized, and `_carCheats.CheatLocalPlayer()` (detour rebuild) can be invoked concurrently. The 1/sec throttle reduces but does not eliminate the race. Most significant correctness risk.

**6. `async void` without uniform try/catch:** `WheelspeedSwitch_OnToggled` (`WheelspeedJump.cs:11`) and `JumpSwitch_OnToggled` (`:109`) lack try/catch around awaited `CheatLocalPlayer()`, unlike `Velocity.cs:260-280`. An exception leaves `AreUiElementsEnabled=false` stuck.

**7. Shutdown timeout risk:** `StopAllAutomationAsync` (`Lifecycle.cs:252-259`) awaits 8 `CancelLoopAsync(...,500)` sequentially → worst case 4s, but `ShutdownCoordinator.StopAllAsync` defaults to 3000ms (`ShutdownCoordinator.cs:23`).

**8. Validation/clamp mismatch:** `VelocityPresetEditing.GammaMin=0.1, GammaMax=10` (`VelocityPresetEditing.cs:5-6`) but runtime clamps are `g1∈[0.6,4.0], g2∈[0.8,4.0], g3∈[1.0,6.0]` (`VelocityCurves.cs:44-46`). Preset validates OK but is silently clamped at runtime.

**9. Dead/parallel code:** `VelocityCurves.CalculateMultiStageBoost` (`:80-119`, scale-based) is bypassed by UI, which calls `CalculateMultiStageBoostWithTargets` directly via `Velocity.cs:765-781` with a different f1/f2 formula.

**10. Magic numbers:** `0.621371f` duplicated (`UiCache.cs:117`, `Diagnostics.cs:375-376`). Raw hex offsets `0x58,0x59,0x17,0x31,0x5A,0x55,0x56,0x1A,0x1D,0x1C` in `PhysicsMisc.cs` not in `CarCheatsOffsets`. Timer intervals 33/16 ms inline.

**11. Duplication:** 6 `VelStage*Gamma/Scale_OnValueChanged` handlers (`Velocity.cs:506-696`) near-identical. `ApplyVelocityPreset` (`:783-853`) has 4 identical 10-field copy arms. `CalculateVelocitySpeedU` and `CalculateSpeedRatio` duplicate maxKmh mode-switch.

## Notable Design Decisions

1. **Per-apply boost via log1p/expm1** — decouples loop rate from boost rate; excellent.
2. **Throttled param writes** (Δu ≥ threshold or time elapsed) — reduces write bandwidth while staying responsive.
3. **Chase-limit PI control** (`ApplyVelocityChaseLimitBoost`, `Automation.cs:370-405`) — closes loop to target speed.
4. **62.5 Hz applies to wheelspeed** (`Automation.cs:496`, 16ms) and brake-assist (`:679`, 16ms); velocity loops are ~30 Hz (33ms). The 62.5 Hz figure is the detour apply rate, not loop tick rate.
5. **Strategy pattern missed:** Fixed/Linear/MultiStage are three strategies but implemented as duplicated `Run*AutoAsync` loops sharing ~80% structure.

## Lines per partial
Velocity 964, Automation 788, Diagnostics 435, Lifecycle 287, PhysicsMisc 257, State 195, UiCache 187, Brake 150, WheelspeedJump 158, xaml.cs 36, Hotkeys 34, Dependencies 12. **~3500 total.**

## Concrete Improvement Recommendations

1. **Fix `PhysicsMisc.cs:120`**: change second `-=` to `+=`. High priority — permanently breaks gravity toggle after failed injection.
2. **Add `IMemoryReader`** interface; route all `GetInstance().ReadMemory<T>` through it (`Automation.cs:350-358`, `Diagnostics.cs:267-271`).
3. **Serialize `EnsureLocalPlayerDetourAsync`** with `SemaphoreSlim(1,1)` around rebuild path — eliminates multi-loop race on `CheatLocalPlayer()`.
4. **Parallelize `StopAllAutomationAsync`** (`Lifecycle.cs:252-259`): `await Task.WhenAll(...)` the 8 cancels; eliminates 4s>3s timeout risk.
5. **Extract `IVelocityStrategy`** for Fixed/Linear/MultiStage; collapse 3 duplicated loops into template + strategy (~300 lines saved).
6. **Add try/catch to `WheelspeedSwitch_OnToggled`/`JumpSwitch_OnToggled`** (`WheelspeedJump.cs:11,109`) with `finally` restoring `AreUiElementsEnabled`.
7. **Reconcile gamma bounds**: make `VelocityPresetEditing.GammaMin/Max` match `VelocityCurves.cs:44-46` runtime clamps.
8. **Move raw hex offsets** in `PhysicsMisc.cs` into `CarCheatsOffsets`; extract `KmhToMphFactor` const.
9. **Verify `VelocityCurves.CalculateMultiStageBoost`** (`:80-119`) — if only tests use it, remove or document; UI uses different f1/f2.
10. **Wire Handling dependencies through DI** (`Handling.xaml.cs:15-19`) per AGENTS.md.
