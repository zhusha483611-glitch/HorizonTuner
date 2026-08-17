# HorizonTuner — Forza Horizon 5 修改器（简体中文）

> 基于 [MA_FH5Trainer](https://github.com/szaaamerik/MA_FH5Trainer) 的简体中文本地化版本。

HorizonTuner 是一款面向 Forza Horizon 5（`forzahorizon5.exe`）的开源内存修改器。界面为简体中文，采用 WPF + MVVM 架构，基于 MahApps.Metro 构建。项目完全开源（GPL-3.0），仅供个人非商业用途使用。

## 环境要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | 仅 Windows x64 |
| 运行时 | .NET 8 |
| 游戏平台 | Steam / UWP / OnlineFix |
| 权限 | 部分功能需要管理员权限 |

## 构建

```bash
dotnet restore MA_FH5Trainer/HorizonTuner.sln
dotnet build  MA_FH5Trainer/HorizonTuner.sln --configuration Debug
dotnet run --project MA_FH5Trainer/MA_FH5Trainer/HorizonTuner.csproj
```

> 注意：项目启用了 `TreatWarningsAsErrors=true`（警告即错误），提交前必须保证构建全绿。

### 发布（单文件自包含）

```bash
dotnet publish MA_FH5Trainer/MA_FH5Trainer/HorizonTuner.csproj \
  --configuration Release --runtime win-x64 --self-contained true --output ./publish \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## 文档

- `docs/` — 技术文档（多段式加速预设系统、速度 Boost 算法、深浅主题规范、预设管理系列）
- `.trae/documents/README.md` — 开发复盘文档主题索引

## 社区与支持

- Discord：<https://discord.gg/rHzev9brJ3>
- GitHub Issues：<https://github.com/szaaamerik/MA_FH5Trainer/issues/new/choose>
- 更新检查 API：<https://api.github.com/repos/szaaamerik/MA_FH5Trainer/releases/latest>

## 免责声明

- 本工具仅供个人非商业用途使用。
- 使用修改器存在被反作弊系统封禁的风险；作者不对因使用本工具导致的封禁、数据丢失或任何损害承担责任。

## 许可证

本应用基于 GPL-3.0 许可证开源，许可证副本见 [LICENSE](LICENSE)。贡献指引见 [CONTRIBUTING.md](CONTRIBUTING.md)。
