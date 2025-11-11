using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace IKuaiDeviceMonitor.Services;

public class IKuaiClient : IDisposable
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private string? _cookie;
    private bool _disposed;

    public IKuaiClient(string host, int port, bool https = false)
    {
        _baseUrl = $"{(https ? "https" : "http")}://{host}:{port}";
        var handler = new HttpClientHandler();
        if (https)
            // 跳过证书验证
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        Debug.WriteLine($"IKuaiClient initialized - Base URL: {_baseUrl}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            Debug.WriteLine($"Login attempt - URL: {_baseUrl}/Action/login");
            Debug.WriteLine($"Username: {username}, Password length: {password?.Length ?? 0}");

            var passwd = MD5Hash(password);
            var pass = Convert.ToBase64String(Encoding.UTF8.GetBytes("salt_11" + password));

            Debug.WriteLine($"MD5 Hash: {passwd}");
            Debug.WriteLine($"Base64 Pass: {pass}");

            var loginInfo = new { passwd, pass, remember_password = "", username };
            var jsonPayload = JsonConvert.SerializeObject(loginInfo);
            Debug.WriteLine($"Login payload: {jsonPayload}");

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/Action/login", content);
            var responseText = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Response status: {response.StatusCode}");
            Debug.WriteLine($"Response body: {responseText}");

            var result = JsonConvert.DeserializeObject<dynamic>(responseText);

            if (result?.Result == 10000)
            {
                _cookie = response.Headers.TryGetValues("Set-Cookie", out var cookies)
                    ? cookies.FirstOrDefault()
                    : null;
                Debug.WriteLine(
                    $"Login successful, cookie: {_cookie?.Substring(0, Math.Min(50, _cookie?.Length ?? 0))}");
                return _cookie != null;
            }

            Debug.WriteLine($"Login failed, result code: {result?.Result}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Login HTTP error: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Login unexpected error: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<T?> ExecAsync<T>(string funcName, string action, object param)
    {
        if (_cookie == null) throw new InvalidOperationException("Not logged in");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/Action/call");
            request.Headers.Add("Cookie", _cookie);
            request.Content = new StringContent(
                JsonConvert.SerializeObject(new { func_name = funcName, action, param }),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseText);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"API call failed: {ex.Message}");
            throw;
        }
    }

    private static string MD5Hash(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}