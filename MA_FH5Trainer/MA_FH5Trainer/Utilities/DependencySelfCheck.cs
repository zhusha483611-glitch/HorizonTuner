using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Reloaded.Memory.Sigscan;

namespace HorizonTuner.Utilities;

public static class DependencySelfCheck
{
    public static bool RunAndMaybeReport()
    {
        var issues = new StringBuilder();
        bool critical = false;

        var baseDir = AppContext.BaseDirectory;
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

        try
        {
            _ = typeof(Memory.Mem);
            _ = typeof(Scanner);
        }
        catch (Exception ex)
        {
            critical = true;
            issues.AppendLine("依赖加载失败：内存扫描组件不可用。");
            issues.AppendLine(ex.GetType().Name + ": " + ex.Message);
        }

        try
        {
            _ = typeof(MahApps.Metro.Controls.MetroWindow);
        }
        catch (Exception ex)
        {
            critical = true;
            issues.AppendLine("依赖加载失败：MahApps UI 组件不可用。");
            issues.AppendLine(ex.GetType().Name + ": " + ex.Message);
        }

        try
        {
            _ = typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject);
        }
        catch (Exception ex)
        {
            critical = true;
            issues.AppendLine("依赖加载失败：CommunityToolkit.Mvvm 组件不可用。");
            issues.AppendLine(ex.GetType().Name + ": " + ex.Message);
        }

        // 检测是否为单文件发布模式
        var isSingleFile = AppContext.GetData("APP_CONTEXT_BASE_DIRECTORY") != null ||
                          !File.Exists(Path.Combine(baseDir, "HorizonTuner.dll"));
        if (!isSingleFile)
        {
            var memoryDll = Path.Combine(baseDir, "Memory.dll");
            var sigscanDll = Path.Combine(baseDir, "Reloaded.Memory.Sigscan.dll");

            if (!File.Exists(memoryDll))
            {
                issues.AppendLine($"缺少文件：{memoryDll}");
            }
            if (!File.Exists(sigscanDll))
            {
                issues.AppendLine($"缺少文件：{sigscanDll}");
            }
        }

        if (issues.Length == 0)
        {
            return true;
        }

        var text =
            $"检测到工具依赖可能不完整或已损坏，部分功能（AoB 扫描/注入）可能无法工作。\n\n" +
            $"{issues}\n" +
            $"BaseDir: {baseDir}\n" +
            $"ToolVersion: {version}\n\n" +
            "建议：删除旧目录后从 Release 完整包重新解压；或检查杀软隔离记录并将工具目录加入白名单。";

        MessageBox.Show(text, "HorizonTuner - 依赖自检", MessageBoxButton.OK,
            critical ? MessageBoxImage.Error : MessageBoxImage.Warning);

        return !critical;
    }
}

