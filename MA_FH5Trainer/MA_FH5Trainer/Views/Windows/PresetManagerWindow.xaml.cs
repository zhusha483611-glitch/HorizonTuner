using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MA_FH5Trainer.Services.Dialogs;
using MA_FH5Trainer.ViewModels.Windows;

namespace MA_FH5Trainer.Views.Windows;

/// <summary>
/// 预设管理窗口
/// </summary>
public partial class PresetManagerWindow : MetroWindow
{
    private readonly PresetManagerViewModel _viewModel;

    public PresetManagerWindow(Action<Models.VelocityPreset> applyPresetCallback)
    {
        InitializeComponent();
        _viewModel = new PresetManagerViewModel(applyPresetCallback, new PresetManagerDialogService(this), Close);
        DataContext = _viewModel;
        PreviewKeyDown += PresetManagerWindow_OnPreviewKeyDown;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void PresetsListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.ApplyAndCloseCommand.CanExecute(null))
        {
            _viewModel.ApplyAndCloseCommand.Execute(null);
        }
    }

    private void PresetsCardListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.ApplyAndCloseCommand.CanExecute(null))
        {
            _viewModel.ApplyAndCloseCommand.Execute(null);
        }
    }

    private void PresetManagerWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (FindName("SearchTextBox") is TextBox textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
            e.Handled = true;
        }
    }
}
