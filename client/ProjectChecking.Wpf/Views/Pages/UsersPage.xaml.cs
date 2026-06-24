using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;
using ProjectChecking.Wpf.Views.Dialogs;

namespace ProjectChecking.Wpf.Views.Pages;

public partial class UsersPage : UserControl
{
    public UsersPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            var items = await AppState.Api.GetAsync<List<UserDto>>("/api/users");
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
        var dlg = new UserEditDialog(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private async void OnEdit(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not UserDto user) return;
        var dlg = new UserEditDialog(user) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private async void OnGridDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OnEdit(sender, e);
        await System.Threading.Tasks.Task.CompletedTask;
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not UserDto user) return;
        if (user.Id == AppState.CurrentUser?.Id)
        {
            MessageBox.Show("Нельзя удалить самого себя", "Ошибка");
            return;
        }
        if (MessageBox.Show($"Удалить пользователя «{user.Login}»?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            await AppState.Api.DeleteAsync($"/api/users/{user.Id}");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
