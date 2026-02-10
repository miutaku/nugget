using System.Net.Http.Json;
using Nugget.Web.Models;

namespace Nugget.Web.Services;

/// <summary>
/// 統計用API クライアント
/// </summary>
public class StatsApiService
{
    private readonly HttpClient _httpClient;

    public StatsApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 全体の統計情報を取得（管理者のみ）
    /// </summary>
    public async Task<GlobalStatsModel?> GetGlobalStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<GlobalStatsModel>("api/stats/global");
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 自分の統計情報を取得
    /// </summary>
    public async Task<PersonalStatsModel?> GetPersonalStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PersonalStatsModel>("api/stats/personal");
        }
        catch (Exception)
        {
            return null;
        }
    }
}
