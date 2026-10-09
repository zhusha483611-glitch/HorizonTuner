# HorizonTuner Test Coverage & Quality Report

Scope: `MA_FH5Trainer/HorizonTuner.Tests` (14 test files, ~70 test methods, ~100 assertions). CI runs `dotnet test` Release on windows-latest/.NET 8.

## 1. Coverage Map — What IS Tested

| Test file | Covers | ~Assertions | Evidence |
|---|---|---|---|
| `BrakeCurvesTests.cs` | `BrakeCurves` pure brake-boost math (level lookup, lerp, panic zone, diag string) | 13 | `BrakeCurvesTests.cs:15-126` |
| `VelocityCurvesTests.cs` | `VelocityCurves` linear + 3-stage boost (clamp, NaN, monotonic, target fractions) | 16 | `VelocityCurvesTests.cs:10-144` |
| `TriggerMathTests.cs` | `TriggerMath` percent→byte + trigger-u mapping (clamp, saturation, linear) | 9 | `TriggerMathTests.cs:11-61` |
| `ConvertersTests.cs` | 4 WPF converters (EnabledIfAnyOn, AndBoolean, Multiply, BoolParam) | 13 | `ConvertersTests.cs:12-146` |
| `HandlingAutoConfigMigrationTests.cs` | Config normalization (null list, missing Id, negative count, future time, bad curve shape) | 9 | `HandlingAutoConfigMigrationTests.cs:8-93` |
| `VelocityPresetEditingTests.cs` | Preset edit validation/apply/create/merge (stats immutability, shape preservation) | 16 | `VelocityPresetEditingTests.cs:7-128` |
| `VelocityPresetUsageTests.cs` | `MarkUsed` (Id assign, count increment, negative clamp) | 5 | `VelocityPresetUsageTests.cs:7-54` |
| `PresetHotnessTests.cs` | Hot-score ranking (recency, use-count) | 3 | `PresetHotnessTests.cs:8-69` |
| `ThrottledActionTests.cs` | Throttle first-call + cancel + exception swallow (generic + non-generic) | 5 | `ThrottledActionTests.cs:12-72` |
| `HotkeyInfrastructureTests.cs` | `AsyncSingleRunner` concurrency guard + `HotkeyRegistry` dedup | 5 | `HotkeyInfrastructureTests.cs:9-47` |
| `ResourceCleanupTests.cs` | `SeDebugPrivilegeHelper` handle cleanup (via `ISeDebugPrivilegeApi` fake) + `CheatLifecycle` order | 7 | `ResourceCleanupTests.cs:9-54` |
| `SqlExecutionTests.cs` | `AutoshowSqlRunner` scan→query ordering + `CleanupScope` LIFO | 2 | `SqlExecutionTests.cs:9-34` |
| `UiLifecycleAndConfigTests.cs` | `AppShutdownActions` ordering, `WindowStatePersistence`, `HotkeyStoragePaths` | 5 | `UiLifecycleAndConfigTests.cs:11-48` |
| `MemoryEdgeCaseTests.cs` | `Mem.ReadArrayMemory` zero-length + `Mem.AdvanceAddress` overflow clamp | 4 | `MemoryEdgeCaseTests.cs:8-24` |

## 2. Untested Critical Areas (the gaps)

- **Memory library core** (`Memory/Methods/AoB.cs`, `Read.cs`, `Write.cs`, `Resources/Detour.cs`, `Memory.cs`, `Utils.cs`): only two pure helpers tested; `WriteProcessMemory`/`ChangeProtection`/detour byte-diff/restore are untested. Inherent — needs a live process.
- **All `Cheats/*` classes** (`CarCheats`, `MiscCheats`, `Bypass` (CRC bypass), `CameraCheats`, `EnvironmentCheats`, `TuningCheats`, `UnlocksCheats`, `PhotomodeCheats`, `CustomizationCheats`, `Sql`): zero direct tests. These hold the highest-risk memory-write + offset-chain logic.
- **All ViewModels** (10 `*ViewModel.cs`): untested.
- **All Views/code-behind** (Handling.* partials, page code-behind): untested (UI).
- **`Services/Handling/Implementations/*`** facades (`DefaultCarCheatsFacade`, `DefaultMiscCheatsFacade`, `DefaultMemoryWriter`, `XInputGamepadReader`, `DefaultHandlingAutoConfigStore`): untested despite implementing mockable interfaces.
- **Config persistence** (`appconfigmanager.cs`, `handlingautoconfigmanager.cs` JSON round-trip), **`MemoryPool`** cache, **`VelocityPresetNaming`** dedup, **`GameVerPlat`** parsing, **`ThemeManager`**, **DI/host wiring**: untested.

## 3. Test Quality Assessment

**Strong.** Tests are meaningful, not tautological — they assert invariants (monotonicity `VelocityCurvesTests.cs:104-112`, range `:138-144`, `≤1` `BrakeCurvesTests.cs:120-126`) and boundaries (clamp `:47-51`, NaN `VelocityCurvesTests.cs:87-93`, overflow `MemoryEdgeCaseTests.cs:17-24`, zero-length `:8-15`). Good edge-case coverage on pure math.

**Abstraction/mocking is used where needed:** `FakePrivilegeApi`/`ISeDebugPrivilegeApi` (`ResourceCleanupTests.cs:56-92`), `FakeSqlCheat`/`ISqlCheat` (`SqlExecutionTests.cs:36-53`), `FakeCheat`/`ICheatsBase` (`:94-100`). This proves the codebase already has seams for the risky P/Invoke paths.

**Deterministic & fast:** no live game, no real memory, no sleeps. One weakness: `ThrottledActionTests.cs:5-9` admits it cannot verify throttle *timing* (WPF Dispatcher timer won't fire in test host) — tests only confirm first-call-immediate + cancel, so the actual throttle interval behavior is unverified. `PresetHotness` uses `DateTime.Now` but only via relative comparisons, so OK.

## 4. Critical-Risk Code Coverage

The genuinely dangerous code — `WriteProcessMemory` (`Write.cs:20,51`), `Detour.cs` byte patch/restore, `AoB.cs` scan loop, `Bypass.cs` CRC bypass — is **not unit-tested**, which is largely inherent (requires an attached `forzahorizon5.exe`). However, the architecture is correct: pure calc is separated into `Services/Handling/Curves/` (static, no state — see `VelocityCurves.cs`) and **well tested**, while side-effectful work sits behind interfaces (`IMemoryWriter`, `ICarCheatsFacade`, `ISeDebugPrivilegeApi`) that are mockable but currently unused by tests. The facades themselves are untested.

## 5. CI Integration

`ci.yml` is adequate for build+test+trx artifact upload with `cancel-in-progress` and PR gating. `TreatWarningsAsErrors=true` acts as a lint gate. **Missing gates:** (a) no coverage report — `coverlet.collector` is referenced but CI never passes `--collect:"XPlat Code Coverage"`; (b) no `dotnet format --verify-no-changes` style/lint step; (c) no coverage threshold/fail-under.

## 6. Concrete Improvement Recommendations

1. **CI coverage gate:** add `--collect:"XPlat Code Coverage" --results-directory ./coverage` to the test step, upload `coverage.cobertura.xml`, optionally `reportgenerator` + fail-under ~40%. Cheap, high signal.
2. **CI format gate:** add `dotnet format MA_FH5Trainer/HorizonTuner.sln --verify-no-changes --no-restore` step.
3. **Test the facades:** add tests for `DefaultCarCheatsFacade`/`DefaultMiscCheatsFacade`/`DefaultMemoryWriter` against their interfaces with fake cheats — verifies wiring of the Handling bridge (currently zero coverage on the layer between pure curves and real cheats).
4. **Test `VelocityPresetNaming`** (dedup logic) — pure Model, listed in source, no test. Add cases: unique name passthrough, collision → " (1)", repeated collision → " (2)".
5. **Test config round-trip:** serialize/deserialize `HandlingAutoConfig`/`AppConfig` to a temp dir via `System.Text.Json` — catches schema drift the migration tests don't cover.
6. **Test `MemoryPool`** cache get/set/evict — no live game needed.
7. **Refactor `ThrottledAction`** to inject an `ITimer`/`IDispatcher` abstraction so the throttle *interval* (not just first call) is testable; current tests are happy-path only.
8. **Extract pure logic from `Detour.cs`** — the original-vs-patched byte comparison/restore (`Detour.cs:33,56,78`) can be a pure `byte[]` function testable without memory.
9. **Extract pure logic from `Cheats/*`** — offset-chain resolution and value clamping in `CarCheats`/`TuningCheats` can be static helpers and tested; leaves only the raw `WriteMemory` call untested (acceptable).
10. **Property-based tests** for curves: random `u∈[0,1]` → assert result ∈[1, base] and monotonic; generalizes the hand-picked monotonicity tests.
11. **`TriggerMath` edge cases:** threshold=0 and trigger=0=threshold boundary.
12. **`GameVerPlat`** version/platform string parsing if pure.

**Bottom line:** Test quality is high and well-targeted at the pure-calc surface, with good boundary/invariant discipline and existing interface seams for risky code. The big gap is the mockable-but-untested facade/implementation layer and config persistence; the truly untestable memory-write core is correctly isolated. Adding a coverage gate + facade/persistence/naming tests would move coverage from "curves-only" to "all testable logic" with modest effort.
