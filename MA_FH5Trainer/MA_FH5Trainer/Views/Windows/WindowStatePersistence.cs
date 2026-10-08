using System.Windows;

namespace HorizonTuner.Views.Windows;

public static class WindowStatePersistence
{
    public static WindowState NormalizeForSave(WindowState state)
    {
        return state == WindowState.Minimized ? WindowState.Normal : state;
    }
}
