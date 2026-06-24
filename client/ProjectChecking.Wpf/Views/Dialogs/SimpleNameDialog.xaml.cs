using System;
using System.Windows;
using ProjectChecking.Wpf.Services;

namespace ProjectChecking.Wpf.Views.Dialogs;

public partial class SimpleNameDialog : Window
{
    private readonly string _basePath;
    private readonly int? _id;

    public SimpleNameDialog(string header, string basePath, string title, int? id = null, string? initialName = null)
    {
        InitializeComponent();
        HeaderText.Text = header;
        Title = title;
        _basePath = basePath;
        _id = id;
        if (initialName != null) NameBox.Text = initialName;
        Loaded += (_, _) => NameBox.Focus();
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
        SaveButton.IsEnabled = false;
        try
        {
            var body = new { Name = name };
            if (_id == null)
                await AppState.Api.PostAsync<object>(_basePath, body);
            else
                await AppState.Api.PutAsync($"{_basePath}/{_id}", body);
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
