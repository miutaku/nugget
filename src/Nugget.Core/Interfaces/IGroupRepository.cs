using Nugget.Core.Entities;

namespace Nugget.Core.Interfaces;

/// <summary>
/// グループリポジトリインターフェース
/// </summary>
public interface IGroupRepository
{
    /// <summary>
    /// IDでグループを取得（メンバー詳細を含む）
    /// </summary>
    Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// SCIM External IDでグループを取得
    /// </summary>
    Task<Group?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全グループを取得
    /// </summary>
    Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// グループを追加
    /// </summary>
    Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default);

    /// <summary>
    /// グループを更新
    /// </summary>
    Task UpdateAsync(Group group, CancellationToken cancellationToken = default);

    /// <summary>
    /// グループメンバーを更新（完全置換）
    /// </summary>
    Task UpdateMembersAsync(Group group, IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// グループを削除
    /// </summary>
    Task DeleteAsync(Group group, CancellationToken cancellationToken = default);
}
