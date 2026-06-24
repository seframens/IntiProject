using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;

namespace ProjectChecking.Wpf.Views.Dialogs;

public partial class ActivityEditDialog : Window
{
    private readonly ActivityDto? _existing;

    public ActivityEditDialog(ActivityDto? existing)
    {
        InitializeComponent();
        _existing = existing;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            var quals = new List<NamedDictionaryDto> { new NamedDictionaryDto { Id = 0, Name = "(не задана)" } };
            quals.AddRange(await AppState.Api.GetAsync<List<NamedDictionaryDto>>("/api/qualifications"));
            QualificationBox.ItemsSource = quals;
            if (_existing == null)
            {
                HeaderText.Text = "Новый вид деятельности";
                QualificationBox.SelectedValue = 0;
            }
            else
            {
                HeaderText.Text = "Редактирование вида деятельности";
                NameBox.Text = _existing.Name;
                QualificationBox.SelectedValue = _existing.QualificationId ?? 0;
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
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
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorText.Text = "Введите название";
            return;
        }
        int? qualId = null;
        if (QualificationBox.SelectedValue is int qid && qid > 0) qualId = qid;
        SaveButton.IsEnabled = false;
        try
        {
            var body = new { Name = name, QualificationId = qualId };
            if (_existing == null)
                await AppState.Api.PostAsync<ActivityDto>("/api/activities", body);
            else
                await AppState.Api.PutAsync($"/api/activities/{_existing.Id}", body);
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
