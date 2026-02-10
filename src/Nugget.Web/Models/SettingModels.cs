namespace Nugget.Web.Models;

/// <summary>
/// 通知設定モデル
/// </summary>
public class NotificationSettingModel
{
    /// <summary>
    /// 期限前通知の日数設定（例: [7, 3, 1, 0]）
    /// </summary>
    public List<int> DaysBeforeDue { get; set; } = new();

    /// <summary>
    /// Slack通知を有効にするか
    /// </summary>
    public bool SlackNotificationEnabled { get; set; } = true;

    /// <summary>
    /// 通知する時刻（24時間形式、例: 9 = 09:00）
    /// </summary>
    public int NotificationHour { get; set; } = 9;
}
