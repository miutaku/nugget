using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nugget.Api.DTOs;
using Nugget.Api.Services;
using Nugget.Core.Enums;
using Nugget.Core.Interfaces;
using System.Security.Claims;

namespace Nugget.Api.Controllers;

/// <summary>
/// ToDo API コントローラー
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly TodoService _todoService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<TodosController> _logger;

    public TodosController(TodoService todoService, IUserRepository userRepository, ILogger<TodosController> logger)
    {
        _todoService = todoService;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// 自分のToDo一覧を取得
    /// </summary>
    /// <param name="isCompleted">完了フィルタ（true: 完了のみ, false: 未完了のみ, null: すべて）</param>
    /// <param name="sortBy">ソート順（dueDate: 期限順, createdAt: 作成日順）</param>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<MyTodoAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTodos(
        [FromQuery] bool? isCompleted = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "dueDate",
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var todos = await _todoService.GetMyTodosAsync(userId, isCompleted, searchTerm, sortBy, cancellationToken);
        return Ok(todos);
    }

    /// <summary>
    /// 自分の作成したToDoの進捗一覧を取得
    /// </summary>
    [HttpGet("created")]
    [ProducesResponseType(typeof(IReadOnlyList<CreatedTodoProgressResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreatedTodos(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var progress = await _todoService.GetCreatedTodosProgressAsync(userId, cancellationToken);
        return Ok(progress);
    }

    /// <summary>
    /// ToDoを作成
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTodo(
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("タイトルは必須です");
        }

        if (request.DueDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest("期限は今日以降の日付を設定してください");
        }

        var userId = GetCurrentUserId();
        var todo = await _todoService.CreateTodoAsync(request, userId, cancellationToken);

        var response = new TodoResponse
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            DueDate = todo.DueDate,
            TargetType = todo.TargetType,
            TargetGroupName = todo.TargetGroupName,
            NotifyImmediately = todo.NotifyImmediately,
            ReminderDays = todo.ReminderDays,
            CreatedAt = todo.CreatedAt,
            UpdatedAt = todo.UpdatedAt,
            CreatedBy = new CreatedByResponse
            {
                Id = userId,
                Name = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown",
                Email = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown"
            }
        };

        return CreatedAtAction(nameof(GetMyTodos), response);
    }

    /// <summary>
    /// ToDoを更新（管理者のみ）
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTodo(
        Guid id,
        [FromBody] UpdateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        // 権限チェック
        var userId = GetCurrentUserId();
        var existingTodo = await _todoService.GetTodoByIdAsync(id, cancellationToken);
        
        if (existingTodo == null)
        {
            return NotFound();
        }

        // 管理者か、作成者本人以外は禁止
        if (!User.IsInRole("Admin") && existingTodo.CreatedById != userId)
        {
             return Forbid();
        }

        var todo = await _todoService.UpdateTodoAsync(id, request, cancellationToken);

        if (todo == null)
        {
            return NotFound();
        }

        var response = new TodoResponse
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            DueDate = todo.DueDate,
            TargetType = todo.TargetType,
            TargetGroupName = todo.TargetGroupName,
            NotifyImmediately = todo.NotifyImmediately,
            ReminderDays = todo.ReminderDays,
            CreatedAt = todo.CreatedAt,
            UpdatedAt = todo.UpdatedAt,
            CreatedBy = new CreatedByResponse
            {
                Id = todo.CreatedBy.Id,
                Name = todo.CreatedBy.Name,
                Email = todo.CreatedBy.Email
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// ToDoを完了にする
    /// </summary>
    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteTodo(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _todoService.CompleteTodoAsync(id, userId, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// ToDoの完了を取り消す
    /// </summary>
    [HttpPatch("{id:guid}/uncomplete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UncompleteTodo(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _todoService.UncompleteTodoAsync(id, userId, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// 指定された属性キーのユニーク値一覧を取得
    /// </summary>
    [HttpGet("attribute-values")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttributeValues(
        [FromQuery] string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("属性キーは必須です");
        }

        var values = await _userRepository.GetDistinctAttributeValuesAsync(key, cancellationToken);
        return Ok(values);
    }

    /// <summary>
    /// 現在のログインユーザーのIDを取得します。
    /// SAML認証やSCIM統合などにより、複数の NameIdentifier クレームが存在する可能性があるため、
    /// ClaimsTransformation で正規化された GUID 形式の値を優先的に取得します。
    /// </summary>
    private Guid GetCurrentUserId()
    {
        // ClaimsTransformation によって追加された Guid 形式の NameIdentifier を優先的に探す
        var userIdClaim = User.FindAll(ClaimTypes.NameIdentifier)
                              .Select(c => c.Value)
                              .FirstOrDefault(v => Guid.TryParse(v, out _));

        if (string.IsNullOrEmpty(userIdClaim))
        {
            // なければ最初に見つかったものを取得
            userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found in claims or is in invalid format");
        }
        return userId;
    }
}
