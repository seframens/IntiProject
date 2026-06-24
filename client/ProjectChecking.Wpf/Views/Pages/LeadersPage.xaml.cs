using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;
using ProjectChecking.Wpf.Views.Dialogs;

namespace ProjectChecking.Wpf.Views.Pages;

public partial class LeadersPage : UserControl
{
    public LeadersPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            var items = await AppState.Api.GetAsync<List<LeaderDto>>("/api/leaders");
            Grid.ItemsSource = items;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void OnAdd(object sender, RoutedEventArgs e)
    {
        var dlg = new LeaderEditDialog(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private async void OnEdit(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not LeaderDto item) return;
        var dlg = new LeaderEditDialog(item) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private void OnGridDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => OnEdit(sender, e);

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not LeaderDto item) return;
        if (MessageBox.Show($"Удалить руководителя «{item.FullName}»?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            await AppState.Api.DeleteAsync($"/api/leaders/{item.Id}");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
