using System.Net.Http.Json;
using Nugget.Web.Models;

namespace Nugget.Web.Services;

/// <summary>
/// 管理者用API クライアント
/// </summary>
public class AdminApiService
{
    private readonly HttpClient _httpClient;

    public AdminApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// ToDoを作成
    /// </summary>
    public async Task<CreateTodoResult?> CreateTodoAsync(CreateTodoModel model)
    {
        var request = new
        {
            title = model.Title,
            description = model.Description,
            dueDate = model.GetDueDateTime(),
            targetType = model.TargetType,
            targetGroupName = model.TargetGroupName,
            targetGroupId = model.TargetGroupId,
            notifyImmediately = model.NotifyImmediately,
            reminderDays = model.ReminderDays.ToArray()
        };

        var response = await _httpClient.PostAsJsonAsync("api/todos", request);
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CreateTodoResult>();
        }

        return null;
    }

    /// <summary>
    /// グループ一覧を取得
    /// </summary>
    public async Task<List<GroupDto>> GetGroupsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<GroupDto>>("api/groups") ?? new List<GroupDto>();
    }
}
