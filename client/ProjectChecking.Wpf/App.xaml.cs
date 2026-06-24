using System.Windows;
using ProjectChecking.Wpf.Services;
using ProjectChecking.Wpf.Views;

namespace ProjectChecking.Wpf;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        AppState.Initialize();
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ShowLogin();
    }

    public static void ShowLogin()
    {
        var login = new LoginWindow();
        var ok = login.ShowDialog();
        if (ok != true)
        {
            Current.Shutdown();
            return;
        }

        var main = new MainWindow();
        Current.MainWindow = main;
        main.Show();
    }
}
