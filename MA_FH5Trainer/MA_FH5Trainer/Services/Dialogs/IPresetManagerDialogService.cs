namespace MA_FH5Trainer.Services.Dialogs;

public interface IPresetManagerDialogService
{
    void ShowInfo(string message, string title);
    void ShowError(string message, string title);
    bool ConfirmDelete(string presetName);
    bool TryGetText(string title, string message, string initialText, out string text);
}

