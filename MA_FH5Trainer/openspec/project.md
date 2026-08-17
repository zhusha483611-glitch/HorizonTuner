# Project Context

## Purpose
MA_FH5Trainer_CN 是 Forza Horizon 5 的开源游戏修改器（作弊工具），支持简体中文。该工具允许玩家修改游戏中的各种参数，包括车辆性能、解锁内容、调校设置等，支持 Steam 和 UWP 平台。

**项目目标：**
- 提供稳定、安全的游戏修改功能
- 支持多游戏版本和平台
- 提供友好的用户界面
- 持续更新以适配游戏新版本

## Tech Stack

### 核心技术
- **.NET 8.0** - 主框架（目标框架：net8.0-windows）
- **WPF** - 桌面应用 UI 框架
- **C# 12** - 编程语言（启用可空引用类型和隐式 using）

### UI 框架
- **MahApps.Metro 2.4.11** - 现代 UI 组件库
- **CommunityToolkit.Mvvm 8.4.0** - MVVM 工具包
- **Microsoft.Extensions.Hosting 8.0.1** - 依赖注入和宿主服务

### 内存操作
- **Reloaded.Memory.Sigscan 3.1.9** - 内存签名扫描
- **P/Invoke** - Windows API 调用
- **System.Management 8.0.0** - 系统管理

### 开发工具
- **GitHub Actions** - CI/CD 自动化
- **OpenSpec** - 规范驱动的开发流程

## Project Conventions

### Code Style

#### 命名约定
- **类名**：PascalCase（如 `CarCheats`、`MainWindowViewModel`）
- **方法名**：PascalCase（如 `Cleanup`、`Reset`、`InitializeViewModel`）
- **属性名**：PascalCase（如 `AttachedText`、`VersionBrush`）
- **私有字段**：camelCase 或 _camelCase（如 `m_timer`、`_instance`）
- **常量**：PascalCase 或 UPPER_SNAKE_CASE（如 `MutexName`、`GitHubRepoUrl`）
- **接口名**：以 `I` 开头的 PascalCase（如 `ICheatsBase`、`IRevertBase`）
- **命名空间**：PascalCase，与项目结构匹配（如 `MA_FH5Trainer.Cheats`）

#### 文件组织
- 每个类一个文件，文件名与类名匹配
- 使用命名空间组织代码，与目录结构对应
- 资源文件（XAML）放在 `Resources/` 目录
- 视图模型放在 `ViewModels/` 目录
- 视图放在 `Views/` 目录

#### 代码注释
- 使用 XML 文档注释（`///`）记录公共 API
- 关键逻辑添加行内注释，说明"为什么"而非"是什么"
- 避免过度注释，代码应自解释

#### 编译选项
- 启用可空引用类型（`Nullable enable`）
- 启用隐式 using（`ImplicitUsings enable`）
- 允许不安全代码块（`AllowUnsafeBlocks true`）
- 检查溢出/下溢（`CheckForOverflowUnderflow true`）
- 将警告视为错误（`TreatWarningsAsErrors true`）

### Architecture Patterns

#### MVVM 模式
- **Model**：数据模型，位于 `Models/` 目录
- **View**：XAML 视图，位于 `Views/` 目录
- **ViewModel**：视图模型，位于 `ViewModels/` 目录
- 使用 `CommunityToolkit.Mvvm` 简化 MVVM 实现
- 通过 `[ObservableProperty]` 自动生成属性
- 通过 `[RelayCommand]` 自动生成命令

#### 依赖注入
- 使用 `Microsoft.Extensions.DependencyInjection`
- 在 `App.xaml.cs` 中配置服务
- 通过 `App.GetRequiredService<T>()` 获取服务实例
- 使用 `AddHostedService` 注册后台服务

#### 单例模式
- `Memory.GetInstance()` - 内存管理单例
- `GameVerPlat.GetInstance()` - 游戏版本/平台信息单例
- `Cheats.GetClass<T>()` - 作弊实例缓存（泛型单例）

#### 作弊模块设计
- 实现 `ICheatsBase` 接口（必须实现 `Cleanup()` 和 `Reset()` 方法）
- 可选实现 `IRevertBase` 接口（用于恢复原始值）
- 使用 `Cheats.GetClass<T>()` 获取实例
- 作弊实例缓存在 `g_CachedInstances` 字典中

#### 内存操作模式
- 使用 AoB（Array of Bytes）扫描定位内存地址
- 扫描结果缓存在 `MemoryPool` 中
- 支持多游戏版本和平台（Steam/UWP）
- 通过 `Memory` 类提供统一的内存读写接口

### Testing Strategy

测试项目已移除（曾为 `MA_FH5Trainer.Tests`，xUnit，覆盖转换器/配置迁移/预设/节流动作），待重建。重建原则：
- 使用 xUnit 作为测试框架
- 测试项目命名 `*.Tests.csproj`
- 单元测试覆盖核心业务逻辑（配置迁移、预设规范化、节流、转换器）
- 集成测试验证内存操作正确性（需 Windows 环境）

### Git Workflow

#### 分支策略
- `main` / `master` - 主分支，稳定版本
- `feature/功能名称` - 功能开发分支
- `fix/问题描述` - Bug 修复分支
- `release/版本号` - 发布准备分支

#### 提交规范
- 使用清晰的提交消息，描述变更内容
- 提交前确保代码编译通过
- 避免提交敏感信息（如 API 密钥）
- 提交消息格式建议：`类型: 简短描述`（如 `feat: 添加新的车辆作弊功能`）

#### 发布流程
- 使用 Git 标签标记版本（格式：`vX.Y.Z`）
- GitHub Actions 自动构建和发布
- 发布包包含单文件可执行程序

## Domain Context

### Forza Horizon 5 游戏信息
- **进程名称**：`forzahorizon5.exe`
- **支持平台**：Steam、UWP（Microsoft Store）、OnlineFix
- **版本检测**：
  - Steam/OnlineFix：通过文件版本检测
  - UWP：通过 appxmanifest.xml 检测

### 作弊功能分类
1. **车辆作弊**（`CarCheats.cs`）：速度、刹车、重力、传送、跳跃等
2. **解锁作弊**（`UnlocksCheats.cs`）：金币、经验值、技能点、抽奖、赛季点数等
3. **调校作弊**（`TuningCheats.cs`）：空气动力、悬挂、轮胎、转向等参数编辑
4. **环境作弊**（`EnvironmentCheats.cs`）：天气、时间等
5. **相机作弊**（`CameraCheats.cs`）：相机控制
6. **定制作弊**（`CustomizationCheats.cs`）：车辆外观定制
7. **照片模式作弊**（`PhotomodeCheats.cs`）：照片模式增强
8. **其他作弊**（`MiscCheats.cs`）：杂项功能

### 内存扫描技术
- **AoB 扫描**：通过字节特征码定位内存地址
- **指针链**：处理动态内存地址
- **偏移量**：相对于基址的偏移
- **多版本支持**：同一功能支持不同游戏版本

### 游戏版本和平台类型
```csharp
public enum GameType : ushort
{
    None = 0,
    Fh5  // Forza Horizon 5
}
```

平台类型：Steam、UWP、OnlineFix、Unknown

## Important Constraints

### 技术约束
- **平台限制**：仅支持 Windows x64
- **.NET 版本**：必须使用 .NET 8.0
- **目标平台**：`win-x64`
- **单文件发布**：发布时必须打包为单文件
- **程序集签名**：使用强名称签名（`MA_FH5Trainer.snk`、`Memory.snk`）

### 安全约束
- **管理员权限**：部分功能可能需要管理员权限
- **反作弊风险**：使用修改器可能导致游戏封禁
- **责任声明**：项目不承担因使用修改器导致的任何后果
- **互斥锁**：使用 Mutex 确保只有一个实例运行

### 性能约束
- **内存扫描**：避免频繁扫描，使用缓存
- **UI 响应**：耗时操作应在后台线程执行
- **资源清理**：应用退出时必须清理所有资源
- **定时器**：使用 `System.Timers.Timer` 进行周期性检查

### 法律约束
- **开源许可**：GPL-3.0 许可证
- **非商业用途**：仅供个人非商业用途使用
- **知识产权**：不得用于侵犯游戏知识产权

## External Dependencies

### NuGet 包
- `CommunityToolkit.Mvvm` (8.4.0) - MVVM 框架
- `MahApps.Metro` (2.4.11) - UI 组件库
- `Microsoft.Extensions.Hosting` (8.0.1) - 依赖注入和宿主服务
- `System.Management` (8.0.0) - 系统管理
- `Reloaded.Memory.Sigscan` (3.1.9) - 内存签名扫描

### 外部服务
- **GitHub API**：用于检查更新
  - URL: `https://api.github.com/repos/szaaamerik/MA_FH5Trainer/releases/latest`
  - 用于获取最新版本信息
- **Discord**：社区支持服务器
  - URL: `https://discord.gg/rHzev9brJ3`

### Windows API
通过 P/Invoke 调用的 Windows API，主要用于：
- 进程管理（`OpenProcess`、`CloseHandle`）
- 内存操作（`ReadProcessMemory`、`WriteProcessMemory`）
- 系统钩子（全局快捷键）

### 项目内依赖
- `Memory` 项目：内存操作库，主项目依赖此项目
- 签名文件：`MA_FH5Trainer.snk`、`Memory.snk`（强名称签名）

### 配置文件
- `app.manifest` - 应用程序清单
- `AssemblyInfo.cs` - 程序集信息
- 资源文件：XAML 资源、翻译文件、主题文件
