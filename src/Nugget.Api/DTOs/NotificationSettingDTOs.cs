namespace Nugget.Api.DTOs;

/// <summary>
/// 通知設定リクエストDTO
/// </summary>
public record UpdateNotificationSettingRequest
{
    /// <summary>
    /// 期限前通知の日数設定（例: [3, 1, 0]）
    /// </summary>
    public int[] DaysBeforeDue { get; init; } = [];

    /// <summary>
    /// Slack通知を有効にするか
    /// </summary>
    public bool SlackNotificationEnabled { get; set; } = true;

    /// <summary>
    /// 通知する時刻（24時間形式、例: 9 = 09:00）
    /// </summary>
    public int NotificationHour { get; set; } = 9;
}

/// <summary>
/// 通知設定レスポンスDTO
/// </summary>
public record NotificationSettingResponse
{
    public int[] DaysBeforeDue { get; init; } = [];
    public bool SlackNotificationEnabled { get; init; }
    public int NotificationHour { get; init; }
}
