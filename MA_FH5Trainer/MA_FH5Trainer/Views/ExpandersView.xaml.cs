using System.Windows.Controls;
using HorizonTuner.Converters;
using HorizonTuner.Resources.Theme;
using HorizonTuner.ViewModels.Windows;
using HorizonTuner.Views.Windows;

namespace HorizonTuner.Views;

public partial class ExpandersView : Page
{
    public ExpandersView()
    {
        DataContext = this;
        ViewModel = MainWindow.Instance!.ViewModel;
        Theming = Theming.GetInstance();
        InitializeComponent();
    }
    
    public MainWindowViewModel ViewModel { get; }
    public Theming Theming { get; }
}