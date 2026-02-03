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
    public bool NotifyImmediately { get; set; } = true;
    public List<int> ReminderDays { get; set; } = [3, 1, 0];

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
