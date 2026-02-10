namespace Nugget.Web.Models;

/// <summary>
/// ToDo作成リクエスト
/// </summary>
public class CreateTodoModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);
    public TimeSpan DueTime { get; set; } = new TimeSpan(17, 0, 0);
    public string TargetType { get; set; } = "All";
    public string? TargetGroupName { get; set; }
    public Guid? TargetGroupId { get; set; }
    public string? TargetAttributeKey { get; set; }
    public string? TargetAttributeValue { get; set; }
    public bool NotifyImmediately { get; set; } = true;
    public List<int> ReminderDays { get; set; } = [3, 1, 0];
    public List<Guid>? TargetUserIds { get; set; }

    /// <summary>
    /// 期限日時を取得
    /// </summary>
    public DateTime GetDueDateTime() => DueDate.Date.Add(DueTime);
}

/// <summary>
/// ToDo作成結果
/// </summary>
public class CreateTodoResult
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class GroupDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

public class CreatedTodoProgressResponse
{
    public Guid TodoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int TotalAssigned { get; set; }
    public int CompletedCount { get; set; }
    public double CompletionRate { get; set; }
    public List<TodoAssignmentProgressDto> Assignments { get; set; } = new();
}

public class TodoAssignmentProgressDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Division { get; set; }
}
public class SystemInfo
{
    public string OrganizationName { get; set; } = string.Empty;
}
