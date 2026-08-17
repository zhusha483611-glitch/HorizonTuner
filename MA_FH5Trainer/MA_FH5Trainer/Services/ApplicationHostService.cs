using System.Windows;
using HorizonTuner.Resources.Theme;
using HorizonTuner.Views.Windows;
using MahApps.Metro.Controls;
using Microsoft.Extensions.Hosting;

namespace HorizonTuner.Services;

public class ApplicationHostService(IServiceProvider serviceProvider) : IHostedService
{
    private MetroWindow? _navigationWindow;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HandleActivationAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task HandleActivationAsync()
    {
        await Task.CompletedTask;

        if (!Application.Current.Windows.OfType<MainWindow>().Any())
        {
            InitTheme();
            _navigationWindow = (serviceProvider.GetService(typeof(MetroWindow)) as MetroWindow)!;
            _navigationWindow.Show();
        }

        await Task.CompletedTask;
    }

    private static void InitTheme()
    {
        AppThemeManager.Initialize();
    }
}
