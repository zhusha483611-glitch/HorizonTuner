using System.Windows.Controls;
using HorizonTuner.ViewModels.Pages;
using HorizonTuner.Views.Windows;
using MahApps.Metro.Controls;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Autoshow : Page
{
    public Autoshow()
    {
        ViewModel = new AutoshowViewModel();
        DataContext = this;
        
        InitializeComponent();
    }
    
    public AutoshowViewModel ViewModel { get; }
}