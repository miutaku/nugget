namespace Nugget.Core.Entities;

/// <summary>
/// ユーザー通知設定エンティティ
/// </summary>
public class NotificationSetting
{
    public Guid Id { get; set; }

    /// <summary>
    /// ユーザーID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 期限前通知の日数（例: [3, 1, 0] = 3日前、1日前、当日）
    /// デフォルト: 3日前、1日前、当日
    /// </summary>
    public int[] DaysBeforeDue { get; set; } = [3, 1, 0];

    /// <summary>
    /// Slack通知を有効にするか
    /// </summary>
    public bool SlackNotificationEnabled { get; set; } = true;

    /// <summary>
    /// 通知する時刻（24時間形式、例: 9 = 09:00）
    /// </summary>
    public int NotificationHour { get; set; } = 9;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
