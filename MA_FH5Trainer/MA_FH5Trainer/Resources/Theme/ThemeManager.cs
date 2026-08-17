using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace MA_FH5Trainer.Resources.Theme;

public enum AppTheme
{
    Light,
    Dark
}

public static class AppThemeManager
{
    private const string ConfigFileName = "theme-config.json";
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MA_FH5Trainer",
        ConfigFileName
    );

    private static AppTheme _currentTheme = AppTheme.Light;
    private static readonly object _lock = new();

    // 缓存主题字典，避免重复创建
    private static readonly Dictionary<AppTheme, ResourceDictionary> _themeCache = new();
    private static readonly Dictionary<AppTheme, ResourceDictionary> _mahAppsThemeCache = new();

    public static AppTheme CurrentTheme
    {
        get
        {
            lock (_lock)
            {
                return _currentTheme;
            }
        }
    }

    public static event EventHandler<AppTheme>? ThemeChanged;

    static AppThemeManager()
    {
        LoadThemePreference();
        PreloadThemes();
    }

    /// <summary>
    /// 预加载主题资源，减少切换时的延迟
    /// </summary>
    private static void PreloadThemes()
    {
        try
        {
            foreach (AppTheme theme in Enum.GetValues(typeof(AppTheme)))
            {
                var themeResourceUri = GetThemeUri(theme);
                var mahAppsThemeUri = GetMahAppsThemeUri(theme);

                if (!_themeCache.ContainsKey(theme))
                {
                    _themeCache[theme] = new ResourceDictionary { Source = themeResourceUri };
                }

                if (!_mahAppsThemeCache.ContainsKey(theme))
                {
                    _mahAppsThemeCache[theme] = new ResourceDictionary { Source = mahAppsThemeUri };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to preload themes: {ex.Message}");
        }
    }

    private static Uri GetThemeUri(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Light => new Uri("/Resources/Theme/LightTheme.xaml", UriKind.Relative),
            AppTheme.Dark => new Uri("/Resources/Theme/DarkTheme.xaml", UriKind.Relative),
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null)
        };
    }

    private static Uri GetMahAppsThemeUri(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Light => new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml", UriKind.Absolute),
            AppTheme.Dark => new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Blue.xaml", UriKind.Absolute),
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null)
        };
    }

    public static void Initialize()
    {
        lock (_lock)
        {
            ApplyTheme(_currentTheme);
            ThemeChanged?.Invoke(null, _currentTheme);
        }
    }

    public static void SetTheme(AppTheme theme)
    {
        lock (_lock)
        {
            if (_currentTheme == theme)
            {
                return;
            }

            _currentTheme = theme;
            ApplyTheme(theme);
            SaveThemePreference(theme);
            ThemeChanged?.Invoke(null, theme);
        }
    }

    public static void ToggleTheme()
    {
        lock (_lock)
        {
            var newTheme = _currentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            SetTheme(newTheme);
        }
    }

    private static void ApplyTheme(AppTheme theme)
    {
        try
        {
            var app = Application.Current;
            if (app?.Resources?.MergedDictionaries == null)
            {
                return;
            }

            // 确保在UI线程上执行主题切换
            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.Invoke(() => ApplyTheme(theme));
                return;
            }

            var mergedDictionaries = app.Resources.MergedDictionaries;

            // 使用缓存的字典，避免重复加载
            var themeResource = _themeCache.TryGetValue(theme, out var cachedTheme)
                ? cachedTheme
                : new ResourceDictionary { Source = GetThemeUri(theme) };

            var mahAppsTheme = _mahAppsThemeCache.TryGetValue(theme, out var cachedMahApps)
                ? cachedMahApps
                : new ResourceDictionary { Source = GetMahAppsThemeUri(theme) };

            // 移除旧主题字典
            RemoveThemeDictionaries(mergedDictionaries);

            // 添加新字典（使用安全克隆避免引用问题）
            mergedDictionaries.Add(SafeCloneResourceDictionary(mahAppsTheme));
            mergedDictionaries.Add(SafeCloneResourceDictionary(themeResource));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply theme: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 安全克隆资源字典，跳过无法冻结的资源如Storyboard
    /// </summary>
    private static ResourceDictionary SafeCloneResourceDictionary(ResourceDictionary source)
    {
        var clone = new ResourceDictionary();
        foreach (var key in source.Keys)
        {
            try
            {
                var value = source[key];
                // 跳过Storyboard等无法冻结的动画资源
                if (value is Storyboard)
                {
                    continue;
                }
                clone[key] = value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clone resource key '{key}': {ex.Message}");
            }
        }
        return clone;
    }

    private static void RemoveThemeDictionaries(IList<ResourceDictionary> mergedDictionaries)
    {
        for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
        {
            var dict = mergedDictionaries[i];
            if (dict.Source != null && IsThemeDictionary(dict.Source.ToString()))
            {
                mergedDictionaries.RemoveAt(i);
            }
        }
    }

    private static bool IsThemeDictionary(string source)
    {
        return source.Contains("LightTheme.xaml") ||
               source.Contains("DarkTheme.xaml") ||
               source.Contains("LightThemeResources.xaml") ||
               source.Contains("DarkThemeResources.xaml") ||
               source.Contains("MahApps.Metro;component/Styles/Themes/");
    }

    private static void SaveThemePreference(AppTheme theme)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var config = new ThemeConfig { Theme = theme.ToString() };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save theme preference: {ex.Message}");
        }
    }

    private static void LoadThemePreference()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                _currentTheme = AppTheme.Light;
                return;
            }

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<ThemeConfig>(json);

            if (config != null && Enum.TryParse<AppTheme>(config.Theme, out var theme))
            {
                _currentTheme = theme;
            }
            else
            {
                _currentTheme = AppTheme.Light;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load theme preference: {ex.Message}");
            _currentTheme = AppTheme.Light;
        }
    }

    /// <summary>
    /// 清理主题缓存，释放内存
    /// </summary>
    public static void ClearCache()
    {
        lock (_lock)
        {
            _themeCache.Clear();
            _mahAppsThemeCache.Clear();
        }
    }

    private class ThemeConfig
    {
        public string Theme { get; set; } = AppTheme.Light.ToString();
    }
}
