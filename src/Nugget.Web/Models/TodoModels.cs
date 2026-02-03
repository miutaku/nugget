namespace Nugget.Web.Models;

/// <summary>
/// 自分のToDo割り当て
/// </summary>
public class MyTodoAssignment
{
    public Guid AssignmentId { get; set; }
    public Guid TodoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int DaysUntilDue { get; set; }
    public CreatedBy CreatedBy { get; set; } = new();

    /// <summary>
    /// 期限の状態を取得
    /// </summary>
    public DueStatus Status
    {
        get
        {
            if (DaysUntilDue < 0) return DueStatus.Overdue;
            if (DaysUntilDue == 0) return DueStatus.DueToday;
            if (DaysUntilDue <= 3) return DueStatus.DueSoon;
            return DueStatus.Normal;
        }
    }
}

/// <summary>
/// 作成者情報
/// </summary>
public class CreatedBy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// 期限状態
/// </summary>
public enum DueStatus
{
    Normal,
    DueSoon,
    DueToday,
    Overdue
}

/// <summary>
/// フィルター種別
/// </summary>
public enum TodoFilterType
{
    All,
    Incomplete,
    Completed,
    Overdue
}

/// <summary>
/// ソート種別
/// </summary>
public enum TodoSortBy
{
    DueDate,
    CreatedAt
}
