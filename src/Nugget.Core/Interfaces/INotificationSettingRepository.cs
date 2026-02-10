using Nugget.Core.Entities;

namespace Nugget.Core.Interfaces;

/// <summary>
/// 通知設定リポジトリのインターフェース
/// </summary>
public interface INotificationSettingRepository
{
    /// <summary>
    /// ユーザーIDに紐づく通知設定を取得
    /// </summary>
    Task<NotificationSetting?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通知設定を更新または作成
    /// </summary>
    Task UpdateAsync(NotificationSetting setting, CancellationToken cancellationToken = default);
}
