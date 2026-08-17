using System.Windows;
using MahApps.Metro.Controls;

namespace MA_FH5Trainer.Views.Windows;

/// <summary>
/// 输入对话框窗口
/// </summary>
public partial class InputWindow : MetroWindow
{
    public string InputText { get; private set; } = string.Empty;

    public InputWindow(string title, string prompt, string defaultText = "")
    {
        InitializeComponent();
        Title = title;
        PromptLabel.Content = prompt;
        InputTextBox.Text = defaultText;
        InputTextBox.Focus();
    }

    private void OKButton_OnClick(object sender, RoutedEventArgs e)
    {
        InputText = InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void InputTextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            OKButton_OnClick(sender, e);
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            CancelButton_OnClick(sender, e);
        }
    }
}
