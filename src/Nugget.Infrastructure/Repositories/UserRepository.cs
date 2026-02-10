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

    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.NotificationSetting)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
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

    public async Task<IReadOnlyList<User>> SearchUsersAsync(string query, CancellationToken cancellationToken = default)
    {
        var q = _context.Users.Where(u => u.IsActive);
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLower();
            q = q.Where(u => 
                u.Name.ToLower().Contains(lowerQuery) || 
                u.Email.ToLower().Contains(lowerQuery) ||
                (u.Department != null && u.Department.ToLower().Contains(lowerQuery)) ||
                (u.Division != null && u.Division.ToLower().Contains(lowerQuery)) ||
                (u.JobTitle != null && u.JobTitle.ToLower().Contains(lowerQuery))
            );
        }

        return await q.OrderBy(u => u.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetUsersByAttributeAsync(string attributeKey, string attributeValue, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.IsActive);
        var lowerValue = attributeValue.ToLower();

        query = attributeKey.ToLowerInvariant() switch
        {
            "department" => query.Where(u => u.Department != null && u.Department.ToLower().Contains(lowerValue)),
            "division" => query.Where(u => u.Division != null && u.Division.ToLower().Contains(lowerValue)),
            "jobtitle" => query.Where(u => u.JobTitle != null && u.JobTitle.ToLower().Contains(lowerValue)),
            "employeenumber" => query.Where(u => u.EmployeeNumber != null && u.EmployeeNumber.ToLower().Contains(lowerValue)),
            "costcenter" => query.Where(u => u.CostCenter != null && u.CostCenter.ToLower().Contains(lowerValue)),
            "organization" => query.Where(u => u.Organization != null && u.Organization.ToLower().Contains(lowerValue)),
            _ => query.Where(u => false) // Unknown attribute returns no results
        };

        return await query.OrderBy(u => u.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctAttributeValuesAsync(string attributeKey, CancellationToken cancellationToken = default)
    {
        var activeUsers = _context.Users.Where(u => u.IsActive);

        IQueryable<string?> values = attributeKey.ToLowerInvariant() switch
        {
            "department" => activeUsers.Select(u => u.Department),
            "division" => activeUsers.Select(u => u.Division),
            "jobtitle" => activeUsers.Select(u => u.JobTitle),
            "employeenumber" => activeUsers.Select(u => u.EmployeeNumber),
            "costcenter" => activeUsers.Select(u => u.CostCenter),
            "organization" => activeUsers.Select(u => u.Organization),
            _ => Enumerable.Empty<string?>().AsQueryable()
        };

        return await values
            .Where(v => v != null)
            .Distinct()
            .OrderBy(v => v)
            .Select(v => v!)
            .ToListAsync(cancellationToken);
    }
}
