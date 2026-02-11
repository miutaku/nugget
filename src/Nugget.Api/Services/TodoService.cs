using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nugget.Api.DTOs;
using Nugget.Core.Entities;
using Nugget.Core.Enums;
using Nugget.Core.Interfaces;
using Nugget.Infrastructure.Data;

namespace Nugget.Api.Services;

/// <summary>
/// ToDo管理サービス
/// </summary>
public class TodoService
{
    private readonly NuggetDbContext _context;
    private readonly ITodoRepository _todoRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly INotificationService _notificationService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TodoService> _logger;

    private static readonly TimeSpan UserTodoCacheDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TargetUsersCacheDuration = TimeSpan.FromMinutes(5);

    public TodoService(
        NuggetDbContext context,
        ITodoRepository todoRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        INotificationService notificationService,
        IMemoryCache cache,
        ILogger<TodoService> logger)
    {
        _context = context;
        _todoRepository = todoRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _notificationService = notificationService;
        _cache = cache;
        _logger = logger;
    }

    private static string UserTodoCacheKey(Guid userId) => $"user-todos:{userId}";
    private static string TodoTargetsCacheKey(Guid todoId) => $"todo-targets:{todoId}";

    /// <summary>
    /// 自分のToDo一覧を取得
    /// </summary>
    public async Task<IReadOnlyList<MyTodoAssignmentResponse>> GetMyTodosAsync(
        Guid userId,
        bool? isCompleted = null,
        string? searchTerm = null,
        string sortBy = "dueDate",
        CancellationToken cancellationToken = default)
    {
        var query = _context.TodoAssignments
            .Include(a => a.Todo)
                .ThenInclude(t => t.CreatedBy)
            .Where(a => a.UserId == userId);

        if (isCompleted.HasValue)
        {
            query = query.Where(a => a.IsCompleted == isCompleted.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(a => 
                a.Todo.Title.ToLower().Contains(term) || 
                (a.Todo.Description != null && a.Todo.Description.ToLower().Contains(term)));
        }

        query = sortBy.ToLower() switch
        {
            "createdat" => query.OrderByDescending(a => a.Todo.CreatedAt),
            "duedate" or _ => query.OrderBy(a => a.Todo.DueDate)
        };

        var assignments = await query.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        return assignments.Select(a => new MyTodoAssignmentResponse
        {
            AssignmentId = a.Id,
            TodoId = a.TodoId,
            Title = a.Todo.Title,
            Description = a.Todo.Description,
            DueDate = a.Todo.DueDate,
            IsCompleted = a.IsCompleted,
            CompletedAt = a.CompletedAt,
            DaysUntilDue = (int)(a.Todo.DueDate.Date - now.Date).TotalDays,
            CreatedBy = new CreatedByResponse
            {
                Id = a.Todo.CreatedBy.Id,
                Name = a.Todo.CreatedBy.Name,
                Email = a.Todo.CreatedBy.Email
            }
        }).ToList();
    }

    /// <summary>
    /// ToDoを作成（管理者用）
    /// </summary>
    public async Task<Todo> CreateTodoAsync(
        CreateTodoRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            CreatedById = createdById,
            TargetType = request.TargetType,
            TargetGroupName = request.TargetGroupName,
            TargetGroupId = request.TargetGroupId,
            TargetAttributeKey = request.TargetAttributeKey,
            TargetAttributeValue = request.TargetAttributeValue,
            NotifyImmediately = request.NotifyImmediately,
            ReminderDays = request.ReminderDays ?? [3, 1, 0]
        };

        // 対象ユーザーを取得
        var targetUsers = await GetTargetUsersAsync(
            request.TargetType, request.TargetGroupName, request.TargetGroupId,
            request.TargetUserIds, request.TargetAttributeKey, request.TargetAttributeValue,
            cancellationToken);

        // ToDo割り当てを作成
        foreach (var user in targetUsers)
        {
            todo.Assignments.Add(new TodoAssignment
            {
                Id = Guid.NewGuid(),
                TodoId = todo.Id,
                UserId = user.Id
            });
        }

        await _todoRepository.AddAsync(todo, cancellationToken);

        // 対象ユーザーのキャッシュを無効化
        foreach (var user in targetUsers)
        {
            _cache.Remove(UserTodoCacheKey(user.Id));
        }

        _logger.LogInformation("ToDoを作成しました: TodoId={TodoId}, Title={Title}, TargetUsers={TargetUserCount}",
            todo.Id, todo.Title, targetUsers.Count);

        // 即時通知
        if (request.NotifyImmediately)
        {
            await _notificationService.SendNewTodoNotificationAsync(todo, targetUsers, cancellationToken);
        }

        return todo;
        return todo;
    }

    /// <summary>
    /// ToDoを取得（権限チェック用）
    /// </summary>
    public async Task<Todo?> GetTodoByIdAsync(Guid todoId, CancellationToken cancellationToken = default)
    {
        return await _todoRepository.GetByIdAsync(todoId, cancellationToken);
    }

    /// <summary>
    /// ToDoを更新
    /// </summary>
    public async Task<Todo?> UpdateTodoAsync(
        Guid todoId,
        UpdateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var todo = await _todoRepository.GetByIdAsync(todoId, cancellationToken);
        if (todo == null)
        {
            return null;
        }

        var changes = new List<string>();

        if (!string.IsNullOrEmpty(request.Title) && request.Title != todo.Title)
        {
            todo.Title = request.Title;
            changes.Add("タイトル");
        }

        if (request.Description != null && request.Description != todo.Description)
        {
            todo.Description = request.Description;
            changes.Add("詳細");
        }

        if (request.DueDate.HasValue && request.DueDate.Value != todo.DueDate)
        {
            var oldDueDate = todo.DueDate;
            todo.DueDate = request.DueDate.Value;
            changes.Add($"期限 ({oldDueDate:yyyy/MM/dd} → {todo.DueDate:yyyy/MM/dd})");
        }

        if (request.NotifyImmediately.HasValue)
        {
            todo.NotifyImmediately = request.NotifyImmediately.Value;
        }

        if (request.ReminderDays != null)
        {
            todo.ReminderDays = request.ReminderDays;
        }

        await _todoRepository.UpdateAsync(todo, cancellationToken);

        _logger.LogInformation("ToDoを更新しました: TodoId={TodoId}, Changes={Changes}", todoId, string.Join(", ", changes));

        // 更新通知
        if (changes.Count > 0)
        {
            var targetUsers = todo.Assignments
                .Where(a => !a.IsCompleted)
                .Select(a => a.User)
                .ToList();

            await _notificationService.SendTodoUpdatedNotificationAsync(
                todo,
                targetUsers,
                string.Join(", ", changes),
                cancellationToken);
        }

        return todo;
    }

    /// <summary>
    /// ToDoを削除
    /// </summary>
    public async Task<bool> DeleteTodoAsync(Guid todoId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var todo = await _todoRepository.GetByIdAsync(todoId, cancellationToken);
            if (todo == null)
            {
                return false;
            }

            // 権限チェック (作成者または管理者)
            if (!isAdmin && todo.CreatedById != currentUserId)
            {
                throw new UnauthorizedAccessException("このToDoを削除する権限がありません。");
            }

            // 通知対象ユーザーの取得 (未完了の割り当てを持つユーザー)
            var targetUsers = todo.Assignments
                .Where(a => !a.IsCompleted)
                .Select(a => a.User)
                .ToList();

            // 削除通知の送信
            await _notificationService.SendTodoDeletedNotificationAsync(todo, targetUsers, cancellationToken);

            // 削除実行
            await _todoRepository.DeleteAsync(todo, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("ToDoを削除しました: TodoId={TodoId}, DeletedBy={UserId}", todoId, currentUserId);
        
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "ToDo削除中にエラーが発生しました: TodoId={TodoId}", todoId);
            throw;
        }
    }

    /// <summary>
    /// ToDoを完了にする
    /// </summary>
    public async Task<bool> CompleteTodoAsync(
        Guid todoId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _context.TodoAssignments
            .FirstOrDefaultAsync(a => a.TodoId == todoId && a.UserId == userId, cancellationToken);

        if (assignment == null)
        {
            return false;
        }

        assignment.IsCompleted = true;
        assignment.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // キャッシュ無効化
        _cache.Remove(UserTodoCacheKey(userId));

        _logger.LogInformation("ToDoを完了しました: TodoId={TodoId}, UserId={UserId}", todoId, userId);

        return true;
    }

    /// <summary>
    /// ToDoの完了を取り消す
    /// </summary>
    public async Task<bool> UncompleteTodoAsync(
        Guid todoId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _context.TodoAssignments
            .FirstOrDefaultAsync(a => a.TodoId == todoId && a.UserId == userId, cancellationToken);

        if (assignment == null)
        {
            return false;
        }

        assignment.IsCompleted = false;
        assignment.CompletedAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        // キャッシュ無効化
        _cache.Remove(UserTodoCacheKey(userId));

        _logger.LogInformation("ToDoの完了を取り消しました: TodoId={TodoId}, UserId={UserId}", todoId, userId);

        return true;
    }

    /// <summary>
    /// 作成したToDoの進捗一覧を取得
    /// </summary>
    public async Task<IReadOnlyList<CreatedTodoProgressResponse>> GetCreatedTodosProgressAsync(
        Guid creatorId,
        CancellationToken cancellationToken = default)
    {
        var todos = await _context.Todos
            .Include(t => t.Assignments)
                .ThenInclude(a => a.User)
            .Where(t => t.CreatedById == creatorId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return todos.Select(t => new CreatedTodoProgressResponse
        {
            TodoId = t.Id,
            Title = t.Title,
            DueDate = t.DueDate,
            TotalAssigned = t.Assignments.Count,
            CompletedCount = t.Assignments.Count(a => a.IsCompleted),
            Assignments = t.Assignments.Select(a => new TodoAssignmentProgressDto
            {
                UserId = a.UserId,
                UserName = a.User.Name,
                UserEmail = a.User.Email,
                IsCompleted = a.IsCompleted,
                CompletedAt = a.CompletedAt
            }).OrderBy(a => a.IsCompleted).ThenBy(a => a.UserName).ToList()
        }).ToList();
    }

    private async Task<IReadOnlyList<User>> GetTargetUsersAsync(
        TargetType targetType,
        string? targetGroupName,
        Guid? targetGroupId,
        List<Guid>? targetUserIds,
        string? targetAttributeKey,
        string? targetAttributeValue,
        CancellationToken cancellationToken)
    {
        return targetType switch
        {
            TargetType.All => await _userRepository.GetAllActiveUsersAsync(cancellationToken),
            TargetType.Individual when targetUserIds != null => await _context.Users
                .Where(u => targetUserIds.Contains(u.Id) && u.IsActive)
                .ToListAsync(cancellationToken),
            TargetType.Group when targetGroupId.HasValue => await GetGroupMembersAsync(targetGroupId.Value, cancellationToken),
            TargetType.Attribute when !string.IsNullOrEmpty(targetAttributeKey) && !string.IsNullOrEmpty(targetAttributeValue)
                => await _userRepository.GetUsersByAttributeAsync(targetAttributeKey, targetAttributeValue, cancellationToken),
            _ => []
        };
    }

    private async Task<IReadOnlyList<User>> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group == null)
        {
            return [];
        }
        
        return group.UserGroups
            .Select(ug => ug.User)
            .Where(u => u.IsActive)
            .ToList();
    }
}
