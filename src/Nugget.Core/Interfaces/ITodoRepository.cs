using Nugget.Core.Entities;

namespace Nugget.Core.Interfaces;

/// <summary>
/// ToDoリポジトリインターフェース
/// </summary>
public interface ITodoRepository
{
    /// <summary>
    /// IDでToDoを取得
    /// </summary>
    Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全ToDoを取得
    /// </summary>
    Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを追加
    /// </summary>
    Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを更新
    /// </summary>
    Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを削除
    /// </summary>
    Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 期限間近のToDoを取得（リマインダー用）
    /// </summary>
    Task<IReadOnlyList<Todo>> GetTodosWithUpcomingDueDateAsync(int daysAhead, CancellationToken cancellationToken = default);
}
