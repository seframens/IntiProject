using System;
using System.Windows;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;

namespace ProjectChecking.Wpf.Views.Dialogs;

public partial class WorkerEditDialog : Window
{
    private readonly WorkerDto? _existing;

    public WorkerEditDialog(WorkerDto? existing)
    {
        InitializeComponent();
        _existing = existing;
        if (existing == null)
        {
            HeaderText.Text = "Новый работник";
            StatusBox.SelectedIndex = 0;
        }
        else
        {
            HeaderText.Text = "Редактирование работника";
            FullNameBox.Text = existing.FullName;
            StatusBox.SelectedIndex = existing.Status == "занят" ? 1 : 0;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        var fullName = FullNameBox.Text.Trim();
        if (string.IsNullOrEmpty(fullName))
        {
            ErrorText.Text = "Введите ФИО";
            return;
        }
        var status = (StatusBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "свободен";
        SaveButton.IsEnabled = false;
        try
        {
            var body = new WorkerUpsertRequest { FullName = fullName, Status = status };
            if (_existing == null)
                await AppState.Api.PostAsync<int>("/api/workers", body);
            else
                await AppState.Api.PutAsync($"/api/workers/{_existing.Id}", body);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }
}
