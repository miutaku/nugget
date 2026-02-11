using Microsoft.EntityFrameworkCore;
using Nugget.Core.Entities;
using Nugget.Core.Interfaces;
using Nugget.Infrastructure.Data;

namespace Nugget.Infrastructure.Repositories;

public class NotificationSettingRepository : INotificationSettingRepository
{
    private readonly NuggetDbContext _context;

    public NotificationSettingRepository(NuggetDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationSetting?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task UpdateAsync(NotificationSetting setting, CancellationToken cancellationToken = default)
    {
        var existing = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == setting.UserId, cancellationToken);

        if (existing == null)
        {
            _context.NotificationSettings.Add(setting);
        }
        else
        {
            existing.DaysBeforeDue = setting.DaysBeforeDue;
            existing.SlackNotificationEnabled = setting.SlackNotificationEnabled;
            existing.NotificationHour = setting.NotificationHour;
            _context.NotificationSettings.Update(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
