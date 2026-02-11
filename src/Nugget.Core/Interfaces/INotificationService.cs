using Nugget.Core.Entities;

namespace Nugget.Core.Interfaces;

/// <summary>
/// 通知サービスインターフェース
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 新規ToDo通知を送信
    /// </summary>
    Task SendNewTodoNotificationAsync(Todo todo, IEnumerable<User> users, CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDo更新通知を送信
    /// </summary>
    Task SendTodoUpdatedNotificationAsync(Todo todo, IEnumerable<User> users, string changeDescription, CancellationToken cancellationToken = default);

    /// <summary>
    /// リマインダー通知を送信
    /// </summary>
    Task SendReminderNotificationAsync(Todo todo, User user, int daysUntilDue, CancellationToken cancellationToken = default);

    /// <summary>
    /// デイリーダイジェスト通知を送信
    /// </summary>
    Task SendDailyDigestNotificationAsync(User user, IEnumerable<(Todo Todo, int DaysUntilDue)> todos, CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDo削除通知を送信
    /// </summary>
    Task SendTodoDeletedNotificationAsync(Todo todo, IEnumerable<User> users, CancellationToken cancellationToken = default);
}
