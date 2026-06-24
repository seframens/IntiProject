using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProjectChecking.Wpf.Services;
using ProjectChecking.Wpf.Views.Pages;

namespace ProjectChecking.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BuildHeader();
        BuildMenu();
        ShowDefault();
    }

    private void BuildHeader()
    {
        var u = AppState.CurrentUser;
        if (u != null)
            UserText.Text = $"{u.FullName} ({u.Role})";
    }

    private void BuildMenu()
    {
        MenuPanel.Children.Clear();
        AddMenuItem("Личный кабинет", () => new AccountPage());

        if (AppState.IsAdmin)
        {
            AddMenuItem("Пользователи", () => new UsersPage());
            AddMenuItem("Роли", () => new RolesPage());
            AddMenuItem("Виды деятельности", () => new ActivitiesPage());
            AddMenuItem("Квалификации", () => new QualificationsPage());
            AddMenuItem("Руководители", () => new LeadersPage());
            AddMenuItem("Работники", () => new WorkersPage());
        }
        else if (AppState.IsManager)
        {
            AddMenuItem("Проекты", () => new ProjectsPage(ProjectsPage.Mode.Manager));
        }
        else if (AppState.IsLeader)
        {
            AddMenuItem("Мои проекты", () => new ProjectsPage(ProjectsPage.Mode.Leader));
        }
        else if (AppState.IsWorker)
        {
            AddMenuItem("Мои проекты", () => new ProjectsPage(ProjectsPage.Mode.Worker));
        }
    }

    private void AddMenuItem(string title, System.Func<UserControl> factory)
    {
        var btn = new Button
        {
            Content = title,
            Style = (System.Windows.Style)FindResource("SideMenuButton")
        };
        btn.Click += (_, _) => MainContent.Content = factory();
        MenuPanel.Children.Add(btn);
    }

    private void ShowDefault()
    {
        MainContent.Content = new AccountPage();
    }

    private void OnLogout(object sender, RoutedEventArgs e)
    {
        try
        {
            _ = AppState.Api.PostAsync("/api/auth/logout", null);
        }
        catch { }
        AppState.Clear();
        Close();
        App.ShowLogin();
    }
}
