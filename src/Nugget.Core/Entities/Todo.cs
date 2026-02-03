using Nugget.Core.Enums;

namespace Nugget.Core.Entities;

/// <summary>
/// ToDoエンティティ
/// </summary>
public class Todo
{
    public Guid Id { get; set; }

    /// <summary>
    /// タイトル
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 詳細説明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 期限日時
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// 作成者ID
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// 対象タイプ
    /// </summary>
    public TargetType TargetType { get; set; }

    /// <summary>
    /// 対象グループ名（TargetType = Group の場合）
    /// </summary>
    public string? TargetGroupName { get; set; }

    /// <summary>
    /// 即時通知するか
    /// </summary>
    public bool NotifyImmediately { get; set; } = true;

    /// <summary>
    /// 期限前通知の日数設定（例: [3, 1, 0] = 3日前、1日前、当日）
    /// </summary>
    public int[] ReminderDays { get; set; } = [3, 1, 0];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<TodoAssignment> Assignments { get; set; } = new List<TodoAssignment>();
}
