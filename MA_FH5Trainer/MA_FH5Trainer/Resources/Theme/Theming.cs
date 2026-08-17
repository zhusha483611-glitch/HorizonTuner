using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace HorizonTuner.Resources.Theme;

/// <summary>
/// Provides typed access to theme resources for ViewModel binding.
/// Acts as a bridge between XAML resources and ViewModels.
/// </summary>
public sealed class Theming : INotifyPropertyChanged
{
    private static readonly object s_lock = new();
    private static Theming? _instance;

    public static Theming GetInstance()
    {
        lock (s_lock)
        {
            return _instance ??= new Theming();
        }
    }

    private Brush _lighterColour = Brushes.Transparent;
    public Brush LighterColour
    {
        get => _lighterColour;
        private set => SetField(ref _lighterColour, value);
    }

    private Brush _lightColour = Brushes.Transparent;
    public Brush LightColour
    {
        get => _lightColour;
        private set => SetField(ref _lightColour, value);
    }

    private Brush _mainColour = Brushes.Transparent;
    public Brush MainColour
    {
        get => _mainColour;
        private set => SetField(ref _mainColour, value);
    }

    private Brush _darkishColour = Brushes.Transparent;
    public Brush DarkishColour
    {
        get => _darkishColour;
        private set => SetField(ref _darkishColour, value);
    }

    private Brush _darkColour = Brushes.Transparent;
    public Brush DarkColour
    {
        get => _darkColour;
        private set => SetField(ref _darkColour, value);
    }

    private Brush _darkerColour = Brushes.Transparent;
    public Brush DarkerColour
    {
        get => _darkerColour;
        private set => SetField(ref _darkerColour, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    private Theming()
    {
        // Initial load
        UpdateColors();
        
        // Listen for theme changes to update brushes
        AppThemeManager.ThemeChanged += (s, e) => 
        {
            // Ensure we run on UI thread if needed, though usually ThemeChanged happens on UI thread
            if (Application.Current.Dispatcher.CheckAccess())
            {
                UpdateColors();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(UpdateColors);
            }
        };
    }

    private void UpdateColors()
    {
        // Fetch brushes from the current resources using the new semantic keys
        LighterColour = GetBrushResource("HighlightBackgroundBrush");
        LightColour = GetBrushResource("ControlBackgroundBrush");
        MainColour = GetBrushResource("WindowBackgroundBrush");
        DarkishColour = GetBrushResource("SideBarBackgroundBrush");
        DarkColour = GetBrushResource("TitleBarBackgroundBrush");
        DarkerColour = GetBrushResource("InputBackgroundBrush");
    }

    private Brush GetBrushResource(string key)
    {
        try
        {
            if (Application.Current?.TryFindResource(key) is Brush brush)
            {
                return brush;
            }
        }
        catch (Exception)
        {
            // Fallback or log if needed
        }
        
        return Brushes.Transparent; // Fallback
    }
}