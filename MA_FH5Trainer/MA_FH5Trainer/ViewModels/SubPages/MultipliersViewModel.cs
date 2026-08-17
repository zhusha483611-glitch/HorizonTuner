using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HorizonTuner.Views.Windows;

namespace HorizonTuner.ViewModels.SubPages;

public partial class MultipliersViewModel : ObservableObject
{
    [ObservableProperty]
    private bool m_driftComponentsEnabled = true;

    [ObservableProperty]
    private bool m_speedComponentsEnabled = true;

    [ObservableProperty]
    private bool m_signComponentsEnabled = true;

    [ObservableProperty]
    private bool m_trapComponentsEnabled = true;

}