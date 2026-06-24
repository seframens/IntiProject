using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ProjectChecking.Wpf.Models;

namespace ProjectChecking.Wpf.Services;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly Func<string> _getBaseUrl;
    private readonly Func<string?> _getToken;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(Func<string> getBaseUrl, Func<string?> getToken)
    {
        _getBaseUrl = getBaseUrl;
        _getToken = getToken;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    private HttpRequestMessage Build(HttpMethod method, string path, HttpContent? content = null)
    {
        var url = _getBaseUrl().TrimEnd('/') + (path.StartsWith("/") ? path : "/" + path);
        var req = new HttpRequestMessage(method, url);
        var token = _getToken();
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (content != null) req.Content = content;
        return req;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage req)
    {
        using var resp = await _http.SendAsync(req).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            string msg = body;
            try
            {
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var problem = JsonSerializer.Deserialize<ProblemResponse>(body, JsonOptions);
                    if (problem != null && !string.IsNullOrWhiteSpace(problem.Message)) msg = problem.Message!;
                    else if (problem != null && !string.IsNullOrWhiteSpace(problem.Detail)) msg = problem.Detail!;
                    else if (problem != null && !string.IsNullOrWhiteSpace(problem.Title)) msg = problem.Title!;
                }
            }
            catch { }
            throw new ApiException((int)resp.StatusCode, msg);
        }
        if (string.IsNullOrWhiteSpace(body)) return default!;
        return JsonSerializer.Deserialize<T>(body, JsonOptions)!;
    }

    private async Task SendAsync(HttpRequestMessage req)
    {
        using var resp = await _http.SendAsync(req).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            string msg = body;
            try
            {
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var problem = JsonSerializer.Deserialize<ProblemResponse>(body, JsonOptions);
                    if (problem != null && !string.IsNullOrWhiteSpace(problem.Message)) msg = problem.Message!;
                    else if (problem != null && !string.IsNullOrWhiteSpace(problem.Detail)) msg = problem.Detail!;
                    else if (problem != null && !string.IsNullOrWhiteSpace(problem.Title)) msg = problem.Title!;
                }
            }
            catch { }
            throw new ApiException((int)resp.StatusCode, msg);
        }
    }

    public Task<T> GetAsync<T>(string path) => SendAsync<T>(Build(HttpMethod.Get, path));
    public Task<T> PostAsync<T>(string path, object? body) => SendAsync<T>(Build(HttpMethod.Post, path, body == null ? null : JsonContent.Create(body)));
    public Task PostAsync(string path, object? body) => SendAsync(Build(HttpMethod.Post, path, body == null ? null : JsonContent.Create(body)));
    public Task<T> PutAsync<T>(string path, object? body) => SendAsync<T>(Build(HttpMethod.Put, path, body == null ? null : JsonContent.Create(body)));
    public Task PutAsync(string path, object? body) => SendAsync(Build(HttpMethod.Put, path, body == null ? null : JsonContent.Create(body)));
    public Task DeleteAsync(string path) => SendAsync(Build(HttpMethod.Delete, path));

    public async Task<CsvImportResponse> UploadCsvAsync(string path, byte[] bytes, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", fileName);
        var req = Build(HttpMethod.Post, path, form);
        return await SendAsync<CsvImportResponse>(req).ConfigureAwait(false);
    }
}
