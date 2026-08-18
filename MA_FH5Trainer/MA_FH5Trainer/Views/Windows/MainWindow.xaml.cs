using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using HorizonTuner.Resources.Config;
using HorizonTuner.Resources.Keybinds;
using HorizonTuner.Resources.Theme;
using HorizonTuner.Services;
using HorizonTuner.ViewModels.Windows;

namespace HorizonTuner.Views.Windows;

public partial class MainWindow
{
    private readonly DispatcherTimer _windowConfigSaveTimer = new();

    public MainWindow()
    {
        Instance = this;
        ViewModel = new MainWindowViewModel();
        DataContext = this;
        Loaded += (_, _) =>
        {
            ApplyWindowStateFromConfig();
            ViewModel.HotkeysEnabled = HotkeysManager.SetupSystemHook();
            set.IsEnabled = ViewModel.HotkeysEnabled;
        };

        ViewModel.MakeExpandersView();
        InitializeComponent();
        InitializeWindowStateSync();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        HotkeysManager.ShutdownSystemHook();
        base.OnClosed(e);
    }

    public static MainWindow? Instance { get; private set; } = null;
    public MainWindowViewModel ViewModel { get; }
    public Theming Theming => Theming.GetInstance();

    private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (WindowState != WindowState.Normal)
        {
            return;
        }

        var isLeftButton = e.ChangedButton == MouseButton.Left;
        if (!isLeftButton)
        {
            return;
        }

        Point position = e.GetPosition(this);
        bool isWithinTopArea = position.Y < 50;
        if (!isWithinTopArea)
        {
            return;
        }

        DragMove();
    }

    private void WindowStateAction_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        switch (button.Tag)
        {
            case "1":
            {
                SystemCommands.MinimizeWindow(this);
                break;
            }
            case "3":
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Maximized;
                }
                break;
            }
            case "2":
            {
                SystemCommands.CloseWindow(this);
                break;
            }
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        AppShutdownState.BeginShutdown();
        SaveWindowStateToConfig();
        ViewModel.Close();
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var dataContext = button?.DataContext;
        if (dataContext is not GlobalHotkey hotkey)
        {
            MessageBox.Show("No hotkey selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (HotKeyBox.HotKey == null)
        {
            MessageBox.Show("No hotkey selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        if (HotkeysManager.CheckExists(HotKeyBox.HotKey.Key, HotKeyBox.HotKey.ModifierKeys))
        {
            MessageBox.Show("Hotkey already exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        hotkey.Key = HotKeyBox.HotKey.Key;
        hotkey.Modifier = HotKeyBox.HotKey.ModifierKeys;
        hotkey.Hotkey = HotKeyBox.HotKey;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        HotkeysManager.SaveAll();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox box)
        {
            return;
        }

        GlobalHotkey? hotkey = ((GlobalHotkey?)box.SelectedItem);
        if (hotkey != null && HotKeyBox != null)
        {
            HotKeyBox.HotKey = hotkey.Hotkey;
        }
    }

    private void ApplyWindowStateFromConfig()
    {
        var config = AppConfigManager.Get();

        WindowState state = WindowState.Normal;
        Enum.TryParse<WindowState>(config.WindowState, out state);

        if (config.WindowWidth >= MinWidth)
        {
            Width = config.WindowWidth;
        }

        if (config.WindowHeight >= MinHeight)
        {
            Height = config.WindowHeight;
        }

        if (config.WindowLeft is double left && config.WindowTop is double top)
        {
            var width = config.WindowWidth >= MinWidth ? config.WindowWidth : Width;
            var height = config.WindowHeight >= MinHeight ? config.WindowHeight : Height;
            var (clampedLeft, clampedTop) = ClampToVirtualScreen(left, top, width, height);
            Left = clampedLeft;
            Top = clampedTop;
        }

        WindowState = state;
    }

    private void SaveWindowStateToConfig()
    {
        var config = AppConfigManager.Get();

        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;

        var width = bounds.Width;
        var height = bounds.Height;

        if (width >= MinWidth)
        {
            config.WindowWidth = width;
        }

        if (height >= MinHeight)
        {
            config.WindowHeight = height;
        }

        config.WindowState = WindowStatePersistence.NormalizeForSave(WindowState).ToString();
        config.WindowLeft = bounds.Left;
        config.WindowTop = bounds.Top;

        AppConfigManager.Save(config);
    }

    private void InitializeWindowStateSync()
    {
        _windowConfigSaveTimer.Interval = TimeSpan.FromMilliseconds(400);
        _windowConfigSaveTimer.Tick += (_, _) =>
        {
            _windowConfigSaveTimer.Stop();
            SaveWindowStateToConfig();
        };

        LocationChanged += (_, _) => ScheduleWindowStateSave();
        SizeChanged += (_, _) => ScheduleWindowStateSave();
        StateChanged += (_, _) => ScheduleWindowStateSave();
    }

    private void ScheduleWindowStateSave()
    {
        if (!IsLoaded)
        {
            return;
        }

        _windowConfigSaveTimer.Stop();
        _windowConfigSaveTimer.Start();
    }

    private static (double left, double top) ClampToVirtualScreen(double left, double top, double width, double height)
    {
        var vsLeft = SystemParameters.VirtualScreenLeft;
        var vsTop = SystemParameters.VirtualScreenTop;
        var vsRight = vsLeft + SystemParameters.VirtualScreenWidth;
        var vsBottom = vsTop + SystemParameters.VirtualScreenHeight;

        var visibleWidth = double.IsFinite(width) ? Math.Max(100d, width) : 100d;
        var visibleHeight = double.IsFinite(height) ? Math.Max(100d, height) : 100d;

        var maxLeft = vsRight - visibleWidth;
        var maxTop = vsBottom - visibleHeight;

        if (maxLeft < vsLeft)
        {
            maxLeft = vsLeft;
        }

        if (maxTop < vsTop)
        {
            maxTop = vsTop;
        }

        return (Math.Clamp(left, vsLeft, maxLeft), Math.Clamp(top, vsTop, maxTop));
    }
}
