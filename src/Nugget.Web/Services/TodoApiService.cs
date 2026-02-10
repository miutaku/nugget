using System.Net.Http.Json;
using Nugget.Web.Models;

namespace Nugget.Web.Services;

/// <summary>
/// ToDo API クライアント
/// </summary>
public class TodoApiService
{
    private readonly HttpClient _httpClient;

    public TodoApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 自分のToDo一覧を取得
    /// </summary>
    public async Task<List<MyTodoAssignment>> GetMyTodosAsync(bool? isCompleted = null, string? searchTerm = null, string sortBy = "dueDate")
    {
        var url = $"api/todos/my?sortBy={sortBy}";
        if (isCompleted.HasValue)
        {
            url += $"&isCompleted={isCompleted.Value.ToString().ToLower()}";
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        }

        var response = await _httpClient.GetFromJsonAsync<List<MyTodoAssignment>>(url);
        return response ?? [];
    }

    /// <summary>
    /// ToDoを完了にする
    /// </summary>
    public async Task<bool> CompleteTodoAsync(Guid todoId)
    {
        var response = await _httpClient.PatchAsync($"api/todos/{todoId}/complete", null);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// ToDoの完了を取り消す
    /// </summary>
    public async Task<bool> UncompleteTodoAsync(Guid todoId)
    {
        var response = await _httpClient.PatchAsync($"api/todos/{todoId}/uncomplete", null);
        return response.IsSuccessStatusCode;
    }
}
