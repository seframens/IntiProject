using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;

namespace ProjectChecking.Wpf.Views.Dialogs;

public class QualSelectItem : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set { _isChecked = value; OnPropertyChanged(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class LeaderEditDialog : Window
{
    private readonly LeaderDto? _existing;
    private ObservableCollection<QualSelectItem> _items = new();

    public LeaderEditDialog(LeaderDto? existing)
    {
        InitializeComponent();
        _existing = existing;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            var quals = await AppState.Api.GetAsync<List<NamedDictionaryDto>>("/api/qualifications");
            _items = new ObservableCollection<QualSelectItem>();
            foreach (var q in quals)
            {
                var checkedNow = _existing?.QualificationIds?.Contains(q.Id) == true;
                _items.Add(new QualSelectItem { Id = q.Id, Name = q.Name, IsChecked = checkedNow });
            }
            QualList.ItemsSource = _items;
            if (_existing == null)
            {
                HeaderText.Text = "Новый руководитель";
            }
            else
            {
                HeaderText.Text = "Редактирование руководителя";
                FullNameBox.Text = _existing.FullName;
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
        var fullName = FullNameBox.Text.Trim();
        if (string.IsNullOrEmpty(fullName))
        {
            ErrorText.Text = "Введите ФИО";
            return;
        }
        var selected = _items.Where(x => x.IsChecked).Select(x => x.Id).ToList();
        SaveButton.IsEnabled = false;
        try
        {
            var body = new LeaderUpsertRequest { FullName = fullName, QualificationIds = selected };
            if (_existing == null)
                await AppState.Api.PostAsync<int>("/api/leaders", body);
            else
                await AppState.Api.PutAsync($"/api/leaders/{_existing.Id}", body);
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
