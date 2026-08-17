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
    
    private static Cheats.ForzaHorizon5.Sql SqlFh5 => GetClass<Cheats.ForzaHorizon5.Sql>();

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

    private static async Task Query(string command)
    {
        if (!SqlFh5.WereScansSuccessful)
        {
            await SqlFh5.SqlExecAobScan();
        }

        if (SqlFh5.WereScansSuccessful)
        {
            await Task.Run(() => SqlFh5.Query(command));
        }
    }
}