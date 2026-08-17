using System.Windows;
using HorizonTuner.Views.Windows;

namespace HorizonTuner.Services.Dialogs;

public sealed class PresetManagerDialogService : IPresetManagerDialogService
{
    private readonly Window _owner;

    public PresetManagerDialogService(Window owner)
    {
        _owner = owner;
    }

    public void ShowInfo(string message, string title)
    {
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool ConfirmDelete(string presetName)
    {
        var result = MessageBox.Show(
            _owner,
            $"确定要删除预设 \"{presetName}\" 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    public bool TryGetText(string title, string message, string initialText, out string text)
    {
        var inputWindow = new InputWindow(title, message, initialText)
        {
            Owner = _owner
        };

        if (inputWindow.ShowDialog() == true)
        {
            text = inputWindow.InputText;
            return true;
        }

        text = string.Empty;
        return false;
    }
}

