using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;

namespace ProjectChecking.Wpf.Views.Dialogs;

public class WorkerSelectItem : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = "свободен";
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

public partial class ProjectEditDialog : Window
{
    private readonly ProjectDto? _existing;
    private List<ActivityDto> _activities = new();
    private List<LeaderDto> _leaders = new();
    private List<WorkerDto> _workers = new();
    private ObservableCollection<WorkerSelectItem> _workerItems = new();

    public ProjectEditDialog(ProjectDto? existing)
    {
        InitializeComponent();
        _existing = existing;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _activities = await AppState.Api.GetAsync<List<ActivityDto>>("/api/activities");
            _leaders = await AppState.Api.GetAsync<List<LeaderDto>>("/api/leaders");
            _workers = await AppState.Api.GetAsync<List<WorkerDto>>("/api/workers");

            ActivityBox.ItemsSource = _activities;

            HashSet<int> existingWorkerIds = new();
            ProjectDetailDto? detail = null;
            if (_existing != null)
            {
                try
                {
                    detail = await AppState.Api.GetAsync<ProjectDetailDto>($"/api/projects/{_existing.Id}");
                    existingWorkerIds = detail.Workers.Select(w => w.Id).ToHashSet();
                }
                catch
                {
                }
            }

            _workerItems = new ObservableCollection<WorkerSelectItem>();
            foreach (var w in _workers)
            {
                var isCheckedNow = existingWorkerIds.Contains(w.Id);
                _workerItems.Add(new WorkerSelectItem
                {
                    Id = w.Id,
                    FullName = w.FullName,
                    Status = w.Status,
                    IsChecked = isCheckedNow
                });
            }
            WorkersList.ItemsSource = _workerItems;

            if (_existing == null)
            {
                HeaderText.Text = "Новый проект";
                if (_activities.Count > 0) ActivityBox.SelectedValue = _activities[0].Id;
                BuildLeaderListForActivity();
                LeaderBox.SelectedValue = 0;
            }
            else
            {
                HeaderText.Text = "Редактирование проекта";
                ActivityBox.SelectedValue = _existing.ActivityId;
                CustomerBox.Text = _existing.Customer;
                DescriptionBox.Text = detail?.Description ?? _existing.Description ?? string.Empty;
                BuildLeaderListForActivity();
                LeaderBox.SelectedValue = _existing.LeaderId ?? 0;
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private void OnActivityChanged(object sender, SelectionChangedEventArgs e)
    {
        BuildLeaderListForActivity();
    }

    private void BuildLeaderListForActivity()
    {
        if (ActivityBox.SelectedItem is not ActivityDto a)
        {
            LeaderBox.ItemsSource = new List<LeaderDto> { new LeaderDto { Id = 0, FullName = "(не назначен)" } };
            LeaderBox.SelectedValue = 0;
            LeaderHint.Text = string.Empty;
            return;
        }
        var allowed = new List<LeaderDto> { new LeaderDto { Id = 0, FullName = "(не назначен)" } };
        if (a.QualificationId == null)
        {
            allowed.AddRange(_leaders);
            LeaderHint.Text = "Вид деятельности не привязан к квалификации — доступны все руководители";
        }
        else
        {
            var matching = _leaders.Where(l => l.QualificationIds.Contains(a.QualificationId.Value)).ToList();
            allowed.AddRange(matching);
            LeaderHint.Text = matching.Count == 0
                ? $"Нет руководителей с требуемой квалификацией: {a.QualificationName}"
                : $"Требуется квалификация: {a.QualificationName}";
        }
        LeaderBox.ItemsSource = allowed;
        LeaderBox.SelectedValue = 0;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        if (ActivityBox.SelectedValue is not int activityId)
        {
            ErrorText.Text = "Выберите вид деятельности";
            return;
        }
        var customer = CustomerBox.Text.Trim();
        if (string.IsNullOrEmpty(customer))
        {
            ErrorText.Text = "Заполните заказчика";
            return;
        }
        int? leaderId = null;
        if (LeaderBox.SelectedValue is int lid && lid > 0) leaderId = lid;
        var workerIds = _workerItems.Where(w => w.IsChecked).Select(w => w.Id).ToList();
        SaveButton.IsEnabled = false;
        try
        {
            if (_existing == null)
            {
                var req = new ProjectCreateRequest
                {
                    ActivityId = activityId,
                    Customer = customer,
                    Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim(),
                    LeaderId = leaderId,
                    WorkerIds = workerIds
                };
                await AppState.Api.PostAsync<int>("/api/projects", req);
            }
            else
            {
                var req = new ProjectUpdateRequest
                {
                    ActivityId = activityId,
                    Customer = customer,
                    Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim(),
                    LeaderId = leaderId,
                    WorkerIds = workerIds
                };
                await AppState.Api.PutAsync($"/api/projects/{_existing.Id}", req);
            }
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
