using System.Windows.Controls;
using HorizonTuner.Resources.Theme;
using HorizonTuner.ViewModels.Pages;
using HorizonTuner.Views.Windows;
using MahApps.Metro.Controls;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Garage : Page
{
    public Garage()
    {
        ViewModel = new AutoshowViewModel();
        DataContext = this;
        InitializeComponent();
    }
    
    public AutoshowViewModel ViewModel { get; }
}