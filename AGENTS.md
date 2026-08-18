# HorizonTuner 项目指南

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
  - `Services/` — DI 服务（`ApplicationHostService`、`Dialogs/`）
  - `ViewModels/`（Windows、Pages、SubPages/SelfVehicle/*）、`Views/SubPages/SelfVehicle/Handling.xaml.cs` 拆为多个 partial
  - `Models/`、`Controls/`、`Converters/`、`Resources/`（Config / Theme / Translations / Keybinds / Input）
- `MA_FH5Trainer/Memory/` — 内存库：`Methods/`（AoB/Read/Write）、`Types/`、`Utils.cs`
- `docs/` — 活跃技术文档（`.md`）；`archive/docs/` — 已过时的过程性/修复类文档（不再主动引用）
- `.trae/documents/` — 开发复盘文档（57 个，**按主题链分组，先看 `README.md` 索引**；已过时提案归档于 `archive/trae/`）
- `archive/src/` — 已归档的无引用源码类（死代码，可恢复）
- `.github/workflows/ci.yml` — CI（Windows + .NET 8，restore + build + test）；`LICENSE`（GPL-3.0）、`.editorconfig`、`.gitattributes` 为规范文件

## 构建 / 测试 / 运行
```bash
dotnet restore MA_FH5Trainer/HorizonTuner.sln
dotnet build  MA_FH5Trainer/HorizonTuner.sln --configuration Debug
dotnet test MA_FH5Trainer/HorizonTuner.sln --configuration Release（HorizonTuner.Tests：转换器/配置迁移/预设/节流/Handling 曲线；CI 自动运行）
dotnet run --project MA_FH5Trainer/MA_FH5Trainer/HorizonTuner.csproj
# 单文件自包含发布：
dotnet publish MA_FH5Trainer/MA_FH5Trainer/HorizonTuner.csproj \
  --configuration Release --runtime win-x64 --self-contained true --output ./publish \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
- 强名称签名：`MA_FH5Trainer.snk`、`Memory.snk`。`TreatWarningsAsErrors=true`（**警告即错误**，提交前必须 `dotnet build` 全绿）。

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
- 改动后务必 `dotnet build` 通过；涉及被测试逻辑（转换器/配置迁移/预设/节流/Handling 曲线）的改动需 `dotnet test` 通过。

## 开发工作流
1. **诊断**：先读相关技术文档/复盘（见下节），用 `Handling.Diagnostics.cs` 高级诊断输出定位，勿盲改。
2. **修改**：遵循"精准修改"——只触碰问题相关代码，匹配现有风格，不顺手重构。
3. **验证**：`dotnet build` 全绿（警告即错误）+ `dotnet test` 通过；涉及数值/曲线改动需实机验证；UI 交互仍以手工回归清单（`docs/预设管理参数编辑回归检查清单.md`）覆盖。
4. **文档同步**：修复/改动落地后，新问题写复盘到 `.trae/documents/` 并更新 `README.md` 索引；核心技术方案更新 `docs/` 对应技术文档。
5. **提交**：小步提交，英文消息（`fix:`/`feat:`/`chore:`/`docs:` 前缀）。

## 文档与归档工作流
- **文档分层**：
  - `docs/` — 活跃技术文档（系统设计、算法、规范、用户指南），**只放仍有效的内容**
  - `.trae/documents/` — 开发复盘（按主题链组织），新复盘先查 `README.md` 索引避免重复
  - `archive/docs|trae/` — 过时/已落地提案（不再主动引用，但保留可查）
  - `archive/src/` — 死代码（无引用源码类），恢复时 `git mv` 回原目录
- **归档纪律**：
  - 未落地的"方案/计划"类复盘 → 归档 `archive/trae/`
  - 已被复盘覆盖的修复类 `docs/` 文档 → 归档 `archive/docs/`
  - 确认无引用的源码类 → 归档 `archive/src/`（先全局 grep 类名确认零引用）
  - 归档/新增文档后**必须**同步 `.trae/documents/README.md` 索引计数
- **链接纪律**：文档内引用源码一律用**相对路径**（如 `../../MA_FH5Trainer/MA_FH5Trainer/...`），**禁止 `file:///` 绝对路径**（跨机器失效）；提交前可用 `grep -rn "file://" --include="*.md" .` 检查。

## 敏感改动前先读
- 多段式 / Boost / 刹车 / 预设：`.trae/documents/` 同名文档（如“分析并修复多段式加速无反应”“优化预设管理界面（…测试_文档）”“将Boost换算改为log1p_expm1数值稳定版本”）；`docs/` 技术文档（多段式加速预设系统技术文档、速度Boost算法、深浅主题规范、预设管理系列）。
- `MA_FH5Trainer/openspec/` 已移除；当前不再维护 OpenSpec 工作流。涉及功能/架构的长期方案请改为更新 `docs/` 与 `.trae/documents/`，Bug 恢复、拼写、格式、非破坏性依赖更新继续直接修改。

## 外部资源
- GitHub：https://github.com/szaaamerik/MA_FH5Trainer
- Discord：https://discord.gg/rHzev9brJ3
- 更新检查 API：`https://api.github.com/repos/szaaamerik/MA_FH5Trainer/releases/latest`
