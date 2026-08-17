## 目的
- 解释 Handling（多段式加速等）为什么需要 CRC Patch、指针（RacePtr/LocalPlayer）、AoB 签名扫描与 Detour。
- 说明各模块职责边界与关键失败点，便于后续维护与排障。

## 总体链路（从 UI 到游戏内生效）
- UI 开关触发注入：`VelocityMultiStageSwitch_OnToggled`。[Handling.Velocity.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Velocity.cs)
- 注入核心：`CarCheats.CheatLocalPlayer()` 建立本地玩家 detour，返回 `LocalPlayerHookDetourAddress`（一个可写的“代码洞/中转内存”基址）。[CarCheats.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L44-L130)
- 循环写入：detour 建立后，Handling 的自动循环按频率把控制值写入 detour 内存偏移（例如 VelBoost/VelLimit/VelEnabled）。[Handling.Automation.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs)

## CRC Patch（为什么要打补丁、在哪里做）
### 作用
- FH5 内部会对某些关键代码路径做一致性/校验；训练器通过覆盖关键指令使其“放行”，避免后续 detour 或内存改写被拦截导致注入失败或立刻失效。

### 实现位置
- CRC Patch 由 `Bypass` 负责：定位 `CallAddress` 后把目标地址的 3 字节写成 `48 39 FF`，并提供“是否已打补丁”和“补写”能力。
  - 扫描与写补丁：[Bypass.DisableCrcChecks](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/Bypass.cs#L59-L97)
  - 检查补丁是否仍在（可用于诊断）：[Bypass.IsCrcPatchApplied](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/Bypass.cs#L12-L27)
  - 补写（在地址已知但被覆盖时）：[Bypass.TryReapplyCrcPatch](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/Bypass.cs#L29-L57)

### 与本地玩家 detour 的关系
- `CarCheats.CheatLocalPlayer()` 在构建 detour 前会确保 CRC Patch 已就绪；如果 `CallAddress` 没拿到或补丁没成功，会直接返回失败，导致 detour 不会建立。[CarCheats.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L66-L86)

## 指针（RacePtr / LocalPlayer 指针的意义）
### RacePtr 是什么
- `CheatLocalPlayer()` 需要一个“比赛/车辆上下文”的根指针（代码里命名为 `_racePtr`），用于从游戏对象链路里计算出本地玩家结构地址，并把该地址写到 detour 内存区的 `LocalPlayer` 槽位，供后续循环使用。
- 获取方式：`CheatRacePtr()` 通过 AoB 扫描命中某条指令，然后读取相对偏移，计算真实指针地址。[CarCheats.CheatRacePtr](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L132-L146)

### LocalPlayer“地址”是什么
- 这里的“本地玩家地址”并不是 UI 直接从游戏读出来的地址，而是：
  - detour 分配得到的 `LocalPlayerHookDetourAddress`（中转内存基址）
  - `LocalPlayer` 相关内容存放在 `LocalPlayerHookDetourAddress + CarCheatsOffsets.LocalPlayer` 这一偏移槽位中
- `CarCheatsOffsets` 定义了 detour 内存布局（VelEnabled/VelBoost/VelLimit/LocalPlayer 等）。[CarCheatsOffsets](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L7-L24)

## AoB 签名（Signature）与扫描策略
### AoB 签名的目的
- 游戏版本/平台不同导致绝对地址不可依赖，通过匹配一段特征字节序列（AoB）来定位“目标指令附近”的稳定锚点。

### 签名来源与覆盖机制
- 默认签名写在各 Cheat 方法里（例如 `CheatLocalPlayer` 的 `defaultSig`）。[CarCheats.CheatLocalPlayer](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L49-L65)
- 同时支持外部覆盖：`CheatsUtilities.GetSignature(feature, defaultSig)` 会尝试读取 `%LocalAppData%\\MA_FH5Trainer\\signatures.json`，按 Feature/Platform/GameVersion 查找更优签名，找不到就回退默认值。[CheatsUtilities.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CheatsUtilities.cs#L132-L165)

### 扫描范围与容错
- `SmartAobScan` 优先使用主模块内存范围作为扫描区间（更快），若读取主模块信息失败则回退到全进程扫描。[CheatsUtilities.SmartAobScan](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CheatsUtilities.cs#L20-L67)
- 在 `CheatLocalPlayer` 中，扫描失败会按顺序尝试：主模块范围 → 本地模块（同目录 dll）→ 全进程。[CarCheats.CheatLocalPlayer](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L49-L65)

### signatures.json 结构建议
- 文件位置：`%LocalAppData%\\MA_FH5Trainer\\signatures.json`（不存在则使用内置默认签名）。
- 读取策略（简化描述）：优先按 `Feature -> Platform -> GameVersion` 精确匹配，其次允许 `*` 通配回退。
- 典型结构示例（示意，不保证字段完整）：  

```json
{
  "CheatLocalPlayer": {
    "Steam": {
      "v685.421": "F3 0F ? ? ? 49 8B ? 49 8B ? 0F 28"
    },
    "*": {
      "*": "F3 0F ? ? ? 49 8B ? 49 8B ? 0F 28"
    }
  },
  "*": {
    "*": {
      "*": "..."
    }
  }
}
```

## Detour（什么是 detour，为什么要它）
### detour 的作用
- detour 本质上是“在目标指令处跳转到我们分配的代码洞执行自定义汇编，然后再跳回去”。这样可以在游戏逻辑执行的关键时机把我们需要的数值写回或消费掉。

### detour 的创建
- detour 分配与写入由 `Memory.Mem.CreateDetour` 完成：通过 `VirtualAllocEx` 在目标附近找一块可执行内存，写入洞内代码，并在原地址打 JMP。[Memory.cs](../../MA_FH5Trainer/Memory/Memory.cs#L250-L303)

### LocalPlayer detour 的特殊性
- `LocalPlayerHookDetourAddress` 是后续所有 Handling 写入的“根”，它既代表代码洞的起点，也代表一段固定布局的数据区（按 `CarCheatsOffsets` 存放 Vel/Brake/Wheelspeed/Jump 等开关与参数）。[CarCheatsOffsets](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L7-L24)
- Handling 的多段式循环实际写入的是：`LocalPlayerHookDetourAddress + CarCheatsOffsets.Vel*`。[Handling.Automation.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs)

## 附加（Attached）与句柄有效性
### 附加机制
- 主窗口每秒轮询游戏进程并调用 `OpenProcess` 获取句柄，驱动 UI 的 Attached 状态。[MainWindowViewModel.cs](../../MA_FH5Trainer/MA_FH5Trainer/ViewModels/Windows/MainWindowViewModel.cs#L320-L431)
- `OpenProcess` 在 Memory 项目中实现：设置 `MProc.ProcessId/Process/Handle` 并申请权限。[Memory.cs](../../MA_FH5Trainer/Memory/Memory.cs#L138-L189)

### 为什么“看起来已附加但注入失败”
- 注入需要三个条件同时成立：句柄有效 + CRC Patch 就绪 + AoB 能命中目标指令并成功分配/写入 detour。
- 任一条件不成立都会导致 `CheatLocalPlayer()` 返回 false 或 detour 地址仍为 0，最终表现为“Detour 未建立/本地玩家地址为 0”。[CarCheats.CheatLocalPlayer](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L44-L130)

## 常见失败类型与排查顺序
### 1) CRC Patch 未就绪
- 典型表现：`CheatLocalPlayer()` 早期返回 false（bypass 地址没找到或 patch 没成功）。[CarCheats.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L66-L86)
- 排查：检查 `Bypass.IsCrcPatchApplied()` 是否为 true。[Bypass.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/Bypass.cs#L12-L27)

### 2) 签名未命中（AoB 扫描返回 0）
- 典型表现：`_localPlayerHookAddress == 0` 或 RacePtr 扫描失败并弹出 ShowError。[CarCheats.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L49-L65)
- 排查：确认游戏版本/平台匹配；必要时通过 `signatures.json` 覆盖签名（feature/platform/version 三维）。

### 3) 场景未就绪（还没进入可驾驶状态）
- 典型表现：RacePtr 读不到（`_racePtr == 0`），或者 detour 建立了但消费逻辑没有触发/写入看似无效。
- 排查：确保已进入驾驶场景再开启；避免在加载界面/过场阶段立即注入。

### 4) detour 分配或写入失败
- 典型表现：`CreateDetour` 返回 0，或后续写入 detour 偏移频繁失败。
- 排查：确认句柄权限（必要时管理员运行），以及进程未退出、Handle 未失效。

## 与 Handling 相关的“复位/重试”策略（当前实现）
- 多段式开关开启时，若 `CheatLocalPlayer()` 返回失败，会尝试：
  - 补一次 CRC Patch（`DisableCrcChecks`）
  - 重置 CarCheats 的地址/指针缓存（`Reset`）
  - 再重试一次 `CheatLocalPlayer`（成功则继续写入并启动循环，失败才提示）
- 目的：降低“偶发状态未就绪/句柄抖动”造成的启用失败概率，把“需要重启游戏”压到兜底场景。

## 相关文件清单（便于维护）
- 附加状态机与句柄管理：[MainWindowViewModel.cs](../../MA_FH5Trainer/MA_FH5Trainer/ViewModels/Windows/MainWindowViewModel.cs)
- CRC Patch：[Bypass.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/Bypass.cs)
- 本地玩家 detour 与 RacePtr 指针：[CarCheats.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs)
- 签名选择与 AoB 扫描容错：[CheatsUtilities.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CheatsUtilities.cs)
- detour 分配与跳转写入：[Memory.cs](../../MA_FH5Trainer/Memory/Memory.cs)
- 多段式加速开关与注入入口：[Handling.Velocity.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Velocity.cs)
- 多段式循环写入与 detour 维持：[Handling.Automation.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs)
