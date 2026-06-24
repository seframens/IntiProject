using System;
using System.Windows;
using ProjectChecking.Wpf.Models;
using ProjectChecking.Wpf.Services;

namespace ProjectChecking.Wpf.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        ApiBox.Text = AppState.ApiBaseUrl;
        LoginBox.Focus();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void OnLogin(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        var login = LoginBox.Text.Trim();
        var password = PasswordBox.Password;
        var apiUrl = ApiBox.Text.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            ErrorText.Text = "Введите логин и пароль";
            return;
        }
        if (string.IsNullOrEmpty(apiUrl))
        {
            ErrorText.Text = "Введите адрес сервера";
            return;
        }

        LoginButton.IsEnabled = false;
        AppState.SetBaseUrl(apiUrl);
        try
        {
            var resp = await AppState.Api.PostAsync<LoginResponse>("/api/auth/login", new LoginRequest
            {
                Login = login,
                Password = password
            });
            AppState.Token = resp.Token;
            var me = await AppState.Api.GetAsync<CurrentUserDto>("/api/account/me");
            AppState.CurrentUser = me;
            DialogResult = true;
            Close();
        }
        catch (ApiException ex)
        {
            ErrorText.Text = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Не удалось подключиться: " + ex.Message;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
