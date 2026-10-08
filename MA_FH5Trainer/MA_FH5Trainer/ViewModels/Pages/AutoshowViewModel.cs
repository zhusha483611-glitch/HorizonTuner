using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HorizonTuner.Models;
using HorizonTuner.Views.Windows;
using static HorizonTuner.Resources.Cheats;

namespace HorizonTuner.ViewModels.Pages;

public partial class AutoshowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _uiElementsEnabled = true;

    [ObservableProperty]
    private bool _allCarsEnabled;

    [ObservableProperty]
    private bool _rareCarsEnabled;
    
    [ObservableProperty]
    private bool _freeCarsEnabled;
    
    private static Cheats.ForzaHorizon5.ISqlCheat SqlFh5 => GetClass<Cheats.ForzaHorizon5.Sql>();

    [RelayCommand]
    private async Task ExecuteSql(object parameter)
    {
        if (MainWindow.Instance == null)
        {
            return;
        }
        
        if (parameter is not string sParam || !MainWindow.Instance.ViewModel.Attached)
        {
            return;
        }
        
        UiElementsEnabled = false;
        await Query(sParam);
        UiElementsEnabled = true;
    }

    private static Task Query(string command)
    {
        return AutoshowSqlRunner.RunAsync(SqlFh5, command);
    }
}