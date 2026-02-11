namespace Nugget.Core.Entities;

/// <summary>
/// ToDo割り当てエンティティ（各ユーザーへのToDo割り当て）
/// </summary>
public class TodoAssignment
{
    public Guid Id { get; set; }

    /// <summary>
    /// ToDo ID
    /// </summary>
    public Guid TodoId { get; set; }

    /// <summary>
    /// ユーザーID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 完了フラグ
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 完了日時
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 最後に通知した日
    /// </summary>
    public DateTime? LastNotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Todo Todo { get; set; } = null!;
    public User User { get; set; } = null!;
}
