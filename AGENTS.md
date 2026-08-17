# MA_FH5Trainer_CN 项目指南

> 给未来 ZCode 代理的速查指南。详细背景见 `docs/` 与 `.trae/documents/`（按主题命名）。

## 项目概况
- Forza Horizon 5（`forzahorizon5.exe`）开源修改器，简体中文，**GPL-3.0**，仅供个人非商业用途。
- 平台：Steam / UWP / OnlineFix。**仅 Windows x64，必须 .NET 8**。
- 技术：WPF + MVVM；MahApps.Metro 2.4.11、CommunityToolkit.Mvvm 8.4.0、Microsoft.Extensions.Hosting 8.0.1、Reloaded.Memory.Sigscan 3.1.9（AoB 扫描）、P/Invoke、XInput。
- 内存操作：AoB 扫描 + 指针链/偏移，结果缓存于 `MemoryPool`；支持多游戏版本/平台。

## 目录结构（要点）
- `MA_FH5Trainer/MA_FH5Trainer/` — 主程序
  - `Cheats/` — 作弊实现；`ICheatsBase`（必须 `Cleanup`/`Reset`）、`IRevertBase`（可选，恢复原值）
  - `Services/Handling/` — Handling 模块化：`Abstractions/`（接口）`Curves/`（纯计算）`Implementations/`（落地）
  - `Services/` — DI 服务（`ApplicationHostService`、`WindowsProviderService`、`Dialogs/`）
  - `ViewModels/`（Windows、Pages、SubPages/SelfVehicle/*）、`Views/SubPages/SelfVehicle/Handling.xaml.cs` 拆为多个 partial
  - `Models/`、`Controls/`、`Converters/`、`Resources/`（Config / Theme / Translations / Keybinds / Input）
- `MA_FH5Trainer/Memory/` — 内存库：`Methods/`（AoB/Read/Write）、`Types/`、`Utils.cs`
- `MA_FH5Trainer/MA_FH5Trainer.Tests/` — xUnit 测试项目
- `MA_FH5Trainer/openspec/` — OpenSpec 规范与变更
- `docs/` — 活跃技术文档（`.md`）；`archive/docs/` — 已过时的过程性/修复类文档（不再主动引用）
- `.trae/documents/` — 开发复盘文档（约 50 个，按主题命名）

## 构建 / 测试 / 运行
```bash
dotnet restore MA_FH5Trainer/MA_FH5Trainer.sln
dotnet build  MA_FH5Trainer/MA_FH5Trainer.sln --configuration Debug
dotnet test   MA_FH5Trainer/MA_FH5Trainer.sln            # xUnit：转换器/配置迁移/预设/节流动作
dotnet run --project MA_FH5Trainer/MA_FH5Trainer/MA_FH5Trainer.csproj
# 单文件自包含发布：
dotnet publish MA_FH5Trainer/MA_FH5Trainer/MA_FH5Trainer.csproj \
  --configuration Release --runtime win-x64 --self-contained true --output ./publish \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
- 强名称签名：`MA_FH5Trainer.snk`、`Memory.snk`。`TreatWarningsAsErrors=true`（**警告即错误**，提交前必须 `dotnet build` + `dotnet test` 全绿）。

## 架构与边界
- MVVM：`CommunityToolkit.Mvvm` 的 `[ObservableProperty]`/`[RelayCommand]` 自动生成；XAML 用 `DynamicResource` 绑定。
- DI：`Microsoft.Extensions.DependencyInjection`，在 `App.xaml.cs` 注册；取实例 `App.GetRequiredService<T>()`；后台服务 `AddHostedService`。
- 单例：`Memory.GetInstance()`、`GameVerPlat.GetInstance()`、`Cheats.GetClass<T>()`（缓存于 `g_CachedInstances`）。
- 作弊模块：实现 `ICheatsBase`，取实例用 `Cheats.GetClass<T>()`；要恢复原值实现 `IRevertBase`。
- Handling 分层：接口（Abstractions）→ 纯计算（Curves）→ 实现（Implementations）；扩展走对应目录，**不要改 UI 代码**。
- 内存读写统一走 `Memory` 类；AoB 扫描结果进 `MemoryPool` 缓存。

## 编码规范
- 命名：类/方法/属性 PascalCase；私有字段 `camelCase` 或 `_camelCase`；常量 `UPPER_SNAKE` 或 PascalCase；接口 `I`+PascalCase；命名空间与目录一致；**一个类一个文件**。
- 编译器开关：`Nullable enable`、`ImplicitUsings enable`、`AllowUnsafeBlocks true`、`CheckForOverflowUnderflow true`、`TreatWarningsAsErrors true`、`SignAssembly true`。
- 公共 API 写 XML 文档注释；关键逻辑写“为什么”的行内注释，勿过度注释。
- 大页面（如 Handling）用多个 partial 文件拆分职责（`.Automation`/`.Brake`/`.Velocity`/`.State`…）。

## UI / 主题 / 多语言
- 基于 MahApps.Metro；自定义样式放 `Resources/Theme/`（`ThemeConstants.xaml`、`DesignTokens.xaml`、Dark/Light 资源）。
- **优先用语义资源键**（`PrimaryBrush`、`BackgroundBrush`、`ForegroundBrush`…）通过 `DynamicResource` 绑定，**禁止硬编码色值**。
- 翻译文件 `Resources/Translations/*.xaml`，`DynamicResource` 绑定；新增语言复制现有结构。
- 全局快捷键：`Resources/Keybinds/GlobalHotkey.cs` + `HotkeysManager.cs`。

## 配置与持久化
- `System.Text.Json`；配置目录 `%LocalAppData%\MA_FH5Trainer\`。
- `AppConfig`：窗口大小/状态等。`HandlingAutoConfig`：Handling 自动触发（**扳机阈值默认 12%**）+ 多段式加速预设（S1/S2/A 三模式，各三段 Gamma 曲线）。
- 多段式预设模型 `VelocityPreset`；命名去重 `VelocityPresetNaming`。

## 内存操作注意
- 所有读写前检查 `Memory.Attached`；用 `try-catch` 兜底；退出调用 `Cleanup()` 清理。
- 避免频繁 AoB 扫描（结果缓存 `MemoryPool`）；注意指针链/偏移、64 位地址与类型转换。
- 轮速注入频率 **62.5 Hz**（非 UI 线程），注意 CPU 占用。

## 已知约束 / 坑
- 仅 Windows x64、必须 .NET 8、发布须单文件；部分功能需管理员权限；`Mutex` 保证单实例。
- 反作弊封禁风险，项目不担责。
- 多段式“无效果”常见原因：开关未开、扳机阈值不匹配；诊断见 `Handling.Diagnostics.cs`。自动触发基于 RT 加速 / LT 刹车，阈值用于避免自动加减速。
- 改动后务必 `dotnet build` + `dotnet test` 通过。

## 敏感改动前先读
- 多段式 / Boost / 刹车 / 预设：`.trae/documents/` 同名文档（如“分析并修复多段式加速无反应”“优化预设管理界面（…测试_文档）”“将Boost换算改为log1p_expm1数值稳定版本”）；`docs/` 技术文档（多段式加速预设系统技术文档、速度Boost算法、深浅主题规范、预设管理系列）。
- OpenSpec 变更流程见 `MA_FH5Trainer/openspec/AGENTS.md`：新建变更提案 → `tasks.md` 实现 → `openspec validate <id> --strict --no-interactive` → 归档 `openspec archive <id> --yes`。**跳过提案**：Bug 恢复、拼写、格式、非破坏性依赖更新。

## 外部资源
- GitHub：https://github.com/szaaamerik/MA_FH5Trainer
- Discord：https://discord.gg/rHzev9brJ3
- 更新检查 API：`https://api.github.com/repos/szaaamerik/MA_FH5Trainer/releases/latest`
