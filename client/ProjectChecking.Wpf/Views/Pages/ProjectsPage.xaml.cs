using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;
using ProjectChecking.Wpf.Views.Dialogs;

namespace ProjectChecking.Wpf.Views.Pages;

public partial class ProjectsPage : UserControl
{
    public enum Mode { Manager, Leader, Worker }

    private readonly Mode _mode;
    private int _page = 1;
    private const int PageSize = 20;
    private int _total = 0;
    private List<ProjectDto> _current = new();
    private List<LeaderDto> _leadersCache = new();

    public ProjectsPage(Mode mode)
    {
        InitializeComponent();
        _mode = mode;
        ApplyModeUi();
        Loaded += async (_, _) => { await LoadLeadersAsync(); await LoadAsync(); };
    }

    private void ApplyModeUi()
    {
        switch (_mode)
        {
            case Mode.Manager:
                HeaderText.Text = "Проекты";
                CreateBtn.Visibility = EditBtn.Visibility = DeleteBtn.Visibility = Visibility.Visible;
                CompleteBtn.Visibility = UncompleteBtn.Visibility = Visibility.Visible;
                StartBtn.Visibility = Visibility.Collapsed;
                CsvBtn.Visibility = Visibility.Visible;
                PdfBtn.Visibility = Visibility.Visible;
                LeaderFilterPanel.Visibility = Visibility.Visible;
                break;
            case Mode.Leader:
                HeaderText.Text = "Мои проекты (руководитель)";
                CreateBtn.Visibility = EditBtn.Visibility = DeleteBtn.Visibility = Visibility.Collapsed;
                StartBtn.Visibility = CompleteBtn.Visibility = UncompleteBtn.Visibility = Visibility.Visible;
                CsvBtn.Visibility = Visibility.Collapsed;
                PdfBtn.Visibility = Visibility.Visible;
                LeaderFilterPanel.Visibility = Visibility.Collapsed;
                break;
            case Mode.Worker:
                HeaderText.Text = "Мои проекты (работник)";
                CreateBtn.Visibility = EditBtn.Visibility = DeleteBtn.Visibility = Visibility.Collapsed;
                StartBtn.Visibility = CompleteBtn.Visibility = UncompleteBtn.Visibility = Visibility.Collapsed;
                CsvBtn.Visibility = Visibility.Collapsed;
                PdfBtn.Visibility = Visibility.Visible;
                LeaderFilterPanel.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private async Task LoadLeadersAsync()
    {
        // Руководитель видит только свои проекты, фильтр по другим руководителям не нужен.
        if (_mode == Mode.Leader)
        {
            return;
        }
        try
        {
            _leadersCache = await AppState.Api.GetAsync<List<LeaderDto>>("/api/leaders");
            var withEmpty = new List<LeaderDto> { new LeaderDto { Id = 0, FullName = "(все)" } };
            withEmpty.AddRange(_leadersCache);
            LeaderFilterBox.ItemsSource = withEmpty;
            LeaderFilterBox.SelectedIndex = 0;
        }
        catch { }
    }

    private string BuildQuery()
    {
        var sb = new StringBuilder();
        sb.Append($"?page={_page}&pageSize={PageSize}");
        var search = SearchBox.Text?.Trim();
        if (!string.IsNullOrEmpty(search)) sb.Append("&search=" + HttpUtility.UrlEncode(search));
        var statusItem = StatusBox.SelectedItem as ComboBoxItem;
        var status = statusItem?.Content?.ToString();
        if (!string.IsNullOrEmpty(status) && status != "(все)") sb.Append("&status=" + HttpUtility.UrlEncode(status));
        if (_mode != Mode.Leader && LeaderFilterBox.SelectedValue is int lid && lid > 0) sb.Append("&leaderId=" + lid);
        if (FromDatePicker.SelectedDate is DateTime f) sb.Append("&fromDate=" + f.ToString("yyyy-MM-dd"));
        if (ToDatePicker.SelectedDate is DateTime t) sb.Append("&toDate=" + t.ToString("yyyy-MM-dd"));
        // Руководитель видит и активные, и завершённые свои проекты (история),
        // чтобы можно было «Отменить завершение». У работника завершённые проекты
        // исчезают из списка.
        if (_mode == Mode.Leader) sb.Append("&scope=leader");
        else if (_mode == Mode.Worker) sb.Append("&scope=worker&excludeCompleted=true");
        return sb.ToString();
    }

    private async Task LoadAsync()
    {
        try
        {
            var resp = await AppState.Api.GetAsync<ProjectListResponse>("/api/projects" + BuildQuery());
            _current = resp.Items;
            _total = resp.TotalCount;
            Grid.ItemsSource = _current;
            var totalPages = (int)Math.Max(1, Math.Ceiling((double)_total / PageSize));
            if (_page > totalPages) { _page = totalPages; }
            PageInfo.Text = $"Стр. {_page} / {totalPages} (всего: {_total})";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void OnApplyFilters(object sender, RoutedEventArgs e)
    {
        _page = 1;
        await LoadAsync();
    }

    private async void OnResetFilters(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        StatusBox.SelectedIndex = 0;
        if (_mode != Mode.Leader && LeaderFilterBox.Items.Count > 0)
        {
            LeaderFilterBox.SelectedIndex = 0;
        }
        FromDatePicker.SelectedDate = null;
        ToDatePicker.SelectedDate = null;
        _page = 1;
        await LoadAsync();
    }

    private async void OnSearchKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { _page = 1; await LoadAsync(); }
    }

    private async void OnPrevPage(object sender, RoutedEventArgs e)
    {
        if (_page > 1) { _page--; await LoadAsync(); }
    }

    private async void OnNextPage(object sender, RoutedEventArgs e)
    {
        var totalPages = (int)Math.Max(1, Math.Ceiling((double)_total / PageSize));
        if (_page < totalPages) { _page++; await LoadAsync(); }
    }

    private async void OnCreate(object sender, RoutedEventArgs e)
    {
        var dlg = new ProjectEditDialog(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private async void OnEdit(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProjectDto p) return;
        if (_mode != Mode.Manager) return;
        var dlg = new ProjectEditDialog(p) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private async void OnGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_mode == Mode.Manager) OnEdit(sender, e);
        await Task.CompletedTask;
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProjectDto p) return;
        if (MessageBox.Show($"Удалить проект «{p.ActivityName} — {p.Customer}»?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            await AppState.Api.DeleteAsync($"/api/projects/{p.Id}");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnStart(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProjectDto p) return;
        try
        {
            await AppState.Api.PostAsync($"/api/projects/{p.Id}/start", null);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnComplete(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProjectDto p) return;
        try
        {
            await AppState.Api.PostAsync($"/api/projects/{p.Id}/complete", null);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnUncomplete(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProjectDto p) return;
        try
        {
            await AppState.Api.PostAsync($"/api/projects/{p.Id}/uncomplete", null);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnImportCsv(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|Все файлы|*.*",
            Title = "Импорт CSV"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var bytes = await File.ReadAllBytesAsync(dlg.FileName);
            var resp = await AppState.Api.UploadCsvAsync("/api/projects/import-csv", bytes, Path.GetFileName(dlg.FileName));
            var msg = $"Импортировано: {resp.Imported}\nПропущено: {resp.Skipped}";
            if (resp.Errors?.Count > 0) msg += "\n\n" + string.Join("\n", resp.Errors.Take(20));
            MessageBox.Show(msg, "Импорт CSV", MessageBoxButton.OK,
                resp.Skipped > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExportPdf(object sender, RoutedEventArgs e)
    {
        if (_current.Count == 0)
        {
            MessageBox.Show("Нет данных для экспорта", "Экспорт PDF");
            return;
        }
        var sfd = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"projects-{DateTime.Now:yyyyMMdd-HHmmss}.pdf",
            Title = "Сохранить PDF"
        };
        if (sfd.ShowDialog() != true) return;
        try
        {
            PdfExporter.ExportProjects(sfd.FileName, _current, BuildFilterDescription());
            MessageBox.Show($"PDF сохранён: {sfd.FileName}", "Экспорт PDF",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string BuildFilterDescription()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text)) parts.Add($"поиск: «{SearchBox.Text.Trim()}»");
        var statusItem = StatusBox.SelectedItem as ComboBoxItem;
        var status = statusItem?.Content?.ToString();
        if (!string.IsNullOrEmpty(status) && status != "(все)") parts.Add($"статус: {status}");
        if (LeaderFilterBox.SelectedItem is LeaderDto l && l.Id > 0) parts.Add($"руководитель: {l.FullName}");
        if (FromDatePicker.SelectedDate is DateTime f) parts.Add($"с {f:dd.MM.yyyy}");
        if (ToDatePicker.SelectedDate is DateTime t) parts.Add($"по {t:dd.MM.yyyy}");
        parts.Add($"стр. {_page} (по 20 на стр.)");
        return string.Join(", ", parts);
    }
}
