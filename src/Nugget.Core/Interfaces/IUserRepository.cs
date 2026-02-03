using Nugget.Core.Entities;

namespace Nugget.Core.Interfaces;

/// <summary>
/// ユーザーリポジトリインターフェース
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// IDでユーザーを取得
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// メールアドレスでユーザーを取得
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// SAML NameIDでユーザーを取得
    /// </summary>
    Task<User?> GetBySamlNameIdAsync(string samlNameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全アクティブユーザーを取得
    /// </summary>
    Task<IReadOnlyList<User>> GetAllActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザーを追加
    /// </summary>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザーを更新
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
