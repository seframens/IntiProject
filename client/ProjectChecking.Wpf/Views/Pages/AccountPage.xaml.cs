using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ProjectChecking.Wpf.Views.Pages;

public partial class AccountPage : UserControl
{
    public AccountPage()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        var u = AppState.CurrentUser;
        if (u == null) return;
        LoginBox.Text = u.Login;
        RoleBox.Text = u.Role;
        FullNameBox.Text = u.FullName;
        PhoneBox.Text = u.Phone ?? string.Empty;
        BirthDatePicker.SelectedDate = u.BirthDate;
        RegistrationBox.Text = u.RegistrationDate == default
            ? string.Empty
            : u.RegistrationDate.ToLocalTime().ToString("dd.MM.yyyy");
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        SaveStatus.Text = string.Empty;

        var phone = PhoneBox.Text.Trim();
        if (!string.IsNullOrEmpty(phone))
        {
            var phoneRegex = new Regex(@"^(?:\+7|8)\d{10}$");
            if (!phoneRegex.IsMatch(phone))
            {
                SaveStatus.Foreground = System.Windows.Media.Brushes.Red;
                SaveStatus.Text = "Номер телефона должен начинаться с +7 или 8 и содержать 11 цифр";
                return;
            }
        }

        try
        {
            var req = new UpdateMeRequest
            {
                FullName = FullNameBox.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
                BirthDate = BirthDatePicker.SelectedDate
            };
            await AppState.Api.PutAsync("/api/account/me", req);
            var me = await AppState.Api.GetAsync<CurrentUserDto>("/api/account/me");
            AppState.CurrentUser = me;
            SaveStatus.Foreground = System.Windows.Media.Brushes.Green;
            SaveStatus.Text = "Сохранено";
        }
        catch (Exception ex)
        {
            SaveStatus.Foreground = System.Windows.Media.Brushes.Red;
            SaveStatus.Text = ex.Message;
        }
    }

    private async void OnChangePassword(object sender, RoutedEventArgs e)
    {
        PasswordStatus.Text = string.Empty;
        var newP = NewPasswordBox.Password;
        var confirmP = ConfirmPasswordBox.Password;
        if (string.IsNullOrEmpty(newP) || string.IsNullOrEmpty(confirmP))
        {
            PasswordStatus.Foreground = System.Windows.Media.Brushes.Red;
            PasswordStatus.Text = "Заполните оба поля";
            return;
        }
        if (newP != confirmP)
        {
            PasswordStatus.Foreground = System.Windows.Media.Brushes.Red;
            PasswordStatus.Text = "Пароли не совпадают";
            return;
        }

        var passwordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};:'"",.<>?/\\|`~])[A-Za-z\d!@#$%^&*()_+\-=\[\]{};:'"",.<>?/\\|`~]{8,}$");
        if (!passwordRegex.IsMatch(newP))
        {
            PasswordStatus.Foreground = System.Windows.Media.Brushes.Red;
            PasswordStatus.Text = "Пароль должен содержать минимум 8 символов, 1 заглавную букву и 1 спецсимвол";
            return;
        }

        try
        {
            var req = new UpdateMeRequest
            {
                FullName = FullNameBox.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
                BirthDate = BirthDatePicker.SelectedDate,
                NewPassword = newP,
                ConfirmPassword = confirmP
            };
            await AppState.Api.PutAsync("/api/account/me", req);
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            PasswordStatus.Foreground = System.Windows.Media.Brushes.Green;
            PasswordStatus.Text = "Пароль изменён";
        }
        catch (Exception ex)
        {
            PasswordStatus.Foreground = System.Windows.Media.Brushes.Red;
            PasswordStatus.Text = ex.Message;
        }
    }
}
