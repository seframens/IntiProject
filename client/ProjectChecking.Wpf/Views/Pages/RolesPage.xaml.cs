using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;

namespace ProjectChecking.Wpf.Views.Pages;

public partial class RolesPage : UserControl
{
    public RolesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            var items = await AppState.Api.GetAsync<List<RoleDto>>("/api/roles");
            Grid.ItemsSource = items;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();
}
