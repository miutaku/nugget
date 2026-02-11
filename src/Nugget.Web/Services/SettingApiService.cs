using System.Net.Http.Json;
using Nugget.Web.Models;

namespace Nugget.Web.Services;

/// <summary>
/// 設定用API クライアント
/// </summary>
public class SettingApiService
{
    private readonly HttpClient _httpClient;

    public SettingApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 通知設定を取得
    /// </summary>
    public async Task<NotificationSettingModel?> GetNotificationSettingsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<NotificationSettingModel>("api/notification-settings");
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 通知設定を更新
    /// </summary>
    public async Task<bool> UpdateNotificationSettingsAsync(NotificationSettingModel model)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/notification-settings", model);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
