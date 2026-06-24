using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;

namespace ProjectChecking.Wpf.Views.Dialogs;

public partial class UserEditDialog : Window
{
    private readonly UserDto? _existing;

    public UserEditDialog(UserDto? existing)
    {
        InitializeComponent();
        _existing = existing;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            var roles = await AppState.Api.GetAsync<List<RoleDto>>("/api/roles");
            RoleBox.ItemsSource = roles;
            if (_existing == null)
            {
                HeaderText.Text = "Новый пользователь";
                RoleBox.SelectedValue = roles.FirstOrDefault()?.Id;
            }
            else
            {
                HeaderText.Text = "Редактирование пользователя";
                LoginBox.Text = _existing.Login;
                FullNameBox.Text = _existing.FullName;
                PhoneBox.Text = _existing.Phone ?? string.Empty;
                BirthDatePicker.SelectedDate = _existing.BirthDate;
                RoleBox.SelectedValue = _existing.RoleId;
                PasswordLabel.Text = "Новый пароль (необязательно)";

                if (AppState.CurrentUser != null && _existing.Id == AppState.CurrentUser.Id)
                {
                    RoleBox.IsEnabled = false;
                    SelfRoleHint.Visibility = System.Windows.Visibility.Visible;
                }
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
        var login = LoginBox.Text.Trim();
        var fullName = FullNameBox.Text.Trim();
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(fullName))
        {
            ErrorText.Text = "Заполните логин и ФИО";
            return;
        }
        if (RoleBox.SelectedValue is not int roleId)
        {
            ErrorText.Text = "Выберите роль";
            return;
        }
        var phone = string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim();
        var birthDate = BirthDatePicker.SelectedDate;
        SaveButton.IsEnabled = false;
        try
        {
            if (_existing == null)
            {
                var password = PasswordBox.Password;
                if (string.IsNullOrEmpty(password))
                {
                    ErrorText.Text = "Введите пароль";
                    return;
                }
                await AppState.Api.PostAsync<int>("/api/users", new UserCreateRequest
                {
                    Login = login,
                    Password = password,
                    FullName = fullName,
                    RoleId = roleId,
                    Phone = phone,
                    BirthDate = birthDate
                });
            }
            else
            {
                var pwd = PasswordBox.Password;
                await AppState.Api.PutAsync($"/api/users/{_existing.Id}", new UserUpdateRequest
                {
                    Login = login,
                    FullName = fullName,
                    RoleId = roleId,
                    Phone = phone,
                    BirthDate = birthDate,
                    Password = string.IsNullOrEmpty(pwd) ? null : pwd
                });
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
