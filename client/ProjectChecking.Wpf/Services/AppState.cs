using System;
using ProjectChecking.Wpf.Models;

namespace ProjectChecking.Wpf.Services;

public static class AppState
{
    public static string ApiBaseUrl { get; private set; } = "http://localhost:8080";
    public static ApiClient Api { get; private set; } = null!;
    public static string? Token { get; set; }
    public static CurrentUserDto? CurrentUser { get; set; }

    public static void Initialize()
    {
        var fromEnv = Environment.GetEnvironmentVariable("PROJECT_CHECKING_API");
        if (!string.IsNullOrWhiteSpace(fromEnv)) ApiBaseUrl = fromEnv.TrimEnd('/');
        Api = new ApiClient(() => ApiBaseUrl, () => Token);
    }

    public static void SetBaseUrl(string url)
    {
        ApiBaseUrl = url.TrimEnd('/');
    }

    public static void Clear()
    {
        Token = null;
        CurrentUser = null;
    }

    public static bool IsAdmin => CurrentUser?.Role == "администратор";
    public static bool IsManager => CurrentUser?.Role == "менеджер";
    public static bool IsLeader => CurrentUser?.Role == "руководитель проекта";
    public static bool IsWorker => CurrentUser?.Role == "работник";
}
