using Nugget.Core.Enums;

namespace Nugget.Api.DTOs;

/// <summary>
/// ToDo作成リクエストDTO
/// </summary>
public record CreateTodoRequest
{
    /// <summary>
    /// タイトル
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 詳細説明
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 期限日時
    /// </summary>
    public required DateTime DueDate { get; init; }

    /// <summary>
    /// 対象タイプ
    /// </summary>
    public TargetType TargetType { get; init; } = TargetType.All;

    /// <summary>
    /// 対象グループ名（TargetType = Group の場合）
    /// </summary>
    public string? TargetGroupName { get; init; }

    /// <summary>
    /// 対象グループID
    /// </summary>
    public Guid? TargetGroupId { get; init; }

    /// <summary>
    /// 対象ユーザーID（TargetType = Individual の場合）
    /// </summary>
    public List<Guid>? TargetUserIds { get; init; }

    /// <summary>
    /// 属性キー（TargetType = Attribute の場合。例: "Department"）
    /// </summary>
    public string? TargetAttributeKey { get; init; }

    /// <summary>
    /// 属性値（TargetType = Attribute の場合。例: "営業部"）
    /// </summary>
    public string? TargetAttributeValue { get; init; }

    /// <summary>
    /// 即時通知するか
    /// </summary>
    public bool NotifyImmediately { get; init; } = true;

    /// <summary>
    /// 期限前通知の日数設定
    /// </summary>
    public int[]? ReminderDays { get; init; }
}

/// <summary>
/// ToDo更新リクエストDTO
/// </summary>
public record UpdateTodoRequest
{
    /// <summary>
    /// タイトル
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// 詳細説明
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 期限日時
    /// </summary>
    public DateTime? DueDate { get; init; }

    /// <summary>
    /// 即時通知するか
    /// </summary>
    public bool? NotifyImmediately { get; init; }

    /// <summary>
    /// 期限前通知の日数設定
    /// </summary>
    public int[]? ReminderDays { get; init; }
}

/// <summary>
/// ToDoレスポンスDTO
/// </summary>
public record TodoResponse
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime DueDate { get; init; }
    public TargetType TargetType { get; init; }
    public string? TargetGroupName { get; init; }
    public Guid? TargetGroupId { get; init; }
    public bool NotifyImmediately { get; init; }
    public int[] ReminderDays { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public required CreatedByResponse CreatedBy { get; init; }
}

/// <summary>
/// 作成者情報DTO
/// </summary>
public record CreatedByResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
}

/// <summary>
/// 自分のToDo割り当て一覧レスポンスDTO
/// </summary>
public record MyTodoAssignmentResponse
{
    public Guid AssignmentId { get; init; }
    public Guid TodoId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime DueDate { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int DaysUntilDue { get; init; }
    public required CreatedByResponse CreatedBy { get; init; }
}

/// <summary>
/// 作成したToDoの進捗レスポンスDTO
/// </summary>
public record CreatedTodoProgressResponse
{
    public Guid TodoId { get; init; }
    public required string Title { get; init; }
    public DateTime DueDate { get; init; }
    public int TotalAssigned { get; init; }
    public int CompletedCount { get; init; }
    public double CompletionRate => TotalAssigned > 0 ? (double)CompletedCount / TotalAssigned * 100 : 0;
    public required List<TodoAssignmentProgressDto> Assignments { get; init; }
}

/// <summary>
/// 個別のToDo割り当て進捗DTO
/// </summary>
public record TodoAssignmentProgressDto
{
    public Guid UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime? CompletedAt { get; init; }
}
