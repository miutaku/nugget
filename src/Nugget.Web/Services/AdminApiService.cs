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
            targetAttributeKey = model.TargetAttributeKey,
            targetAttributeValue = model.TargetAttributeValue,
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
    /// 作成したToDoの進捗一覧を取得
    /// </summary>
    public async Task<List<CreatedTodoProgressResponse>> GetCreatedTodosProgressAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<CreatedTodoProgressResponse>>("api/todos/created") ?? new List<CreatedTodoProgressResponse>();
    }

    /// <summary>
    /// グループ一覧を取得
    /// </summary>
    public async Task<List<GroupDto>> GetGroupsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<GroupDto>>("api/groups") ?? new List<GroupDto>();
    }

    public async Task<List<string>> GetAttributeValuesAsync(string attributeKey)
    {
        return await _httpClient.GetFromJsonAsync<List<string>>($"api/todos/attribute-values?key={attributeKey}") ?? new List<string>();
    }

    /// <summary>
    /// 属性キーと値でユーザーを検索
    /// </summary>
    public async Task<List<UserResponse>> GetUsersByAttributeAsync(string attributeKey, string attributeValue)
    {
        return await _httpClient.GetFromJsonAsync<List<UserResponse>>($"api/users?attributeKey={Uri.EscapeDataString(attributeKey)}&attributeValue={Uri.EscapeDataString(attributeValue)}") ?? new List<UserResponse>();
    }

    /// <summary>
    /// ユーザーを検索（汎用）
    /// </summary>
    public async Task<List<UserResponse>> SearchUsersAsync(string query)
    {
        return await _httpClient.GetFromJsonAsync<List<UserResponse>>($"api/users?q={Uri.EscapeDataString(query)}") ?? new List<UserResponse>();
    }

    /// <summary>
    /// グループメンバーを取得
    /// </summary>
    public async Task<List<UserResponse>> GetGroupUsersAsync(Guid groupId)
    {
        return await _httpClient.GetFromJsonAsync<List<UserResponse>>($"api/groups/{groupId}/users") ?? new List<UserResponse>();
    }

    /// <summary>
    /// システム情報を取得
    /// </summary>
    public async Task<SystemInfo?> GetSystemInfoAsync()
    {
        return await _httpClient.GetFromJsonAsync<SystemInfo>("api/system/info");
    }
}
