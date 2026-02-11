using Microsoft.EntityFrameworkCore;
using Nugget.Core.Entities;
using Nugget.Core.Interfaces;
using Nugget.Infrastructure.Data;

namespace Nugget.Infrastructure.Repositories;

/// <summary>
/// グループリポジトリ実装
/// </summary>
public class GroupRepository : IGroupRepository
{
    private readonly NuggetDbContext _context;

    public GroupRepository(NuggetDbContext context)
    {
        _context = context;
    }

    public async Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .Include(g => g.UserGroups)
            .ThenInclude(ug => ug.User)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Group?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .Include(g => g.UserGroups)
            .ThenInclude(ug => ug.User)
            .FirstOrDefaultAsync(g => g.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .Include(g => g.UserGroups)
            .ThenInclude(ug => ug.User)
            .OrderBy(g => g.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync(cancellationToken);
        return group;
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMembersAsync(Group group, IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        // 既存のメンバーシップを削除
        await _context.UserGroups
            .Where(ug => ug.GroupId == group.Id)
            .ExecuteDeleteAsync(cancellationToken);

        // 新しいメンバーシップを追加
        foreach (var userId in userIds)
        {
            _context.UserGroups.Add(new UserGroup { GroupId = group.Id, UserId = userId });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Group group, CancellationToken cancellationToken = default)
    {
        _context.Groups.Remove(group);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
