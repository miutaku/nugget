using Microsoft.EntityFrameworkCore;
using Nugget.Core.Entities;
using Nugget.Core.Interfaces;
using Nugget.Infrastructure.Data;

namespace Nugget.Infrastructure.Repositories;

/// <summary>
/// ユーザーリポジトリ実装
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly NuggetDbContext _context;

    public UserRepository(NuggetDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.NotificationSetting)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.NotificationSetting)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetBySamlNameIdAsync(string samlNameId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.NotificationSetting)
            .FirstOrDefaultAsync(u => u.SamlNameId == samlNameId, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .Include(u => u.NotificationSetting)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
