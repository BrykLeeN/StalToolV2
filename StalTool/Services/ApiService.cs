using System;
using System.Net.Http;
using Newtonsoft.Json; // <-- ПОДКЛЮЧАЕМ НАШ НАДЕЖНЫЙ ПАРСЕР

namespace StalTool.Services;

public class ApiService
{
    private const string BaseUrl = "https://staltool.duckdns.org"; 
    private static readonly HttpClient Http = new HttpClient();
    
    static ApiService() { }
    
    public class AuthRequest
    {
        [JsonProperty("Username")] public string Username { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("Email")] public string Email { get; set; }
        [JsonProperty("HWID")] public string HWID { get; set; }
    }

    public class LoginResponse
    {
        [JsonProperty("Token")] public string Token { get; set; }
        [JsonProperty("Username")] public string Username { get; set; }
        [JsonProperty("SubEnd")] public DateTime SubEnd { get; set; }
        [JsonProperty("Tier")] public string Tier { get; set; }
        
        public LoginResponse() { }
    }
    
    // 1. Метод Регистрации
    // public static async Task<string> RegisterAsync(string username, string email, string password)
    // {
    //     try
    //     {
    //         string myHwid = SatlTool.Services.HWIDEngine.GetHardwareId();
    //
    //         var request = new AuthRequest 
    //         { 
    //             Username = username,
    //             Email = email,
    //             Password = password, 
    //             HWID = myHwid 
    //         };
    //     
    //         // Ручная и безопасная сборка JSON
    //         string jsonBody = JsonConvert.SerializeObject(request);
    //         var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    //
    //         var response = await Http.PostAsync($"{BaseUrl}/api/auth/register", content);
    //
    //         if (response.IsSuccessStatusCode)
    //             return "OK";
    //     
    //         return await response.Content.ReadAsStringAsync(); 
    //     }
    //     catch (HttpRequestException)
    //     {
    //         return "Сервер недоступен. Проверьте подключение.";
    //     }
    //     catch (Exception ex)
    //     {
    //         return $"Неизвестная ошибка: {ex.Message}";
    //     }
    // }
    
    // 2. Метод Логина
    // public static async Task<(bool success, string token, DateTime subEnd, string tier, string error)> LoginAsync(string username, string password)
    // {
    //     try
    //     {
    //         string myHwid = SatlTool.Services.HWIDEngine.GetHardwareId();
    //
    //         var request = new AuthRequest 
    //         { 
    //             Username = username, 
    //             Password = password, 
    //             HWID = myHwid 
    //         };
    //         
    //         // Ручная и безопасная сборка JSON
    //         string jsonBody = JsonConvert.SerializeObject(request);
    //         var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    //
    //         var response = await Http.PostAsync($"{BaseUrl}/api/auth/login", content);
    //
    //         if (response.IsSuccessStatusCode)
    //         {
    //             // Читаем ответ как обычный текст и парсим через надежный Newtonsoft
    //             string jsonResponse = await response.Content.ReadAsStringAsync();
    //             var data = JsonConvert.DeserializeObject<LoginResponse>(jsonResponse);
    //             
    //             return (true, data?.Token ?? "", data?.SubEnd ?? DateTime.MinValue, data?.Tier ?? "Базовая", "");
    //         }
    //             
    //         string error = await response.Content.ReadAsStringAsync();
    //         return (false, "", DateTime.MinValue, "Базовая", error);
    //     }
    //     catch (HttpRequestException)
    //     {
    //         return (false, "", DateTime.MinValue, "Базовая", "Сервер недоступен. Проверьте подключение.");
    //     }
    //     catch (Exception ex)
    //     {
    //         return (false, "", DateTime.MinValue, "Базовая", $"Неизвестная ошибка: {ex.Message}");
    //     }
    // }
}