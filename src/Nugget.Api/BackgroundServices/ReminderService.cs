using Microsoft.EntityFrameworkCore;
using Nugget.Core.Interfaces;
using Nugget.Infrastructure.Data;

namespace Nugget.Api.BackgroundServices;

/// <summary>
/// 期限リマインダー通知バックグラウンドサービス
/// </summary>
public class ReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public ReminderService(
        IServiceProvider serviceProvider,
        ILogger<ReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("リマインダーサービスを開始しました");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "リマインダー処理中にエラーが発生しました");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NuggetDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var currentHour = now.Hour;

        // 通知対象のユーザーを取得（現在の時刻が通知設定時刻と一致するユーザー）
        var usersToNotify = await dbContext.Users
            .Include(u => u.NotificationSetting)
            .Where(u => u.IsActive && u.SlackUserId != null)
            .Where(u => (u.NotificationSetting != null && u.NotificationSetting.NotificationHour == currentHour) 
                     || (u.NotificationSetting == null && currentHour == 9)) // デフォルトは9時
            .ToListAsync(cancellationToken);

        _logger.LogInformation("現在の時刻 ({Hour}時) に通知対象のユーザー数: {Count}", currentHour, usersToNotify.Count);

        foreach (var user in usersToNotify)
        {
            // ユーザーの通知設定をチェック
            var settings = user.NotificationSetting;
            if (settings != null && !settings.SlackNotificationEnabled)
            {
                continue;
            }

            // ユーザーのリマインダー対象日数（設定がない場合はデフォルト[7, 3, 1, 0]）
            var userReminderDays = settings?.DaysBeforeDue ?? [7, 3, 1, 0];

            // ユーザーに割り当てられた未完了のToDoを取得（今日まだ通知されていないもの）
            var assignments = await dbContext.TodoAssignments
                .Include(a => a.Todo)
                .Where(a => a.UserId == user.Id && !a.IsCompleted)
                .Where(a => a.LastNotifiedAt == null || a.LastNotifiedAt.Value.Date < now.Date)
                .ToListAsync(cancellationToken);

            var itemsToNotify = new List<(Nugget.Core.Entities.Todo Todo, int DaysUntilDue)>();

            foreach (var assignment in assignments)
            {
                var daysUntilDue = (int)(assignment.Todo.DueDate.Date - now.Date).TotalDays;

                // 期限内の場合のみ通知（期限切れは別の仕組みか、ここで含めるか検討が必要だが、一旦0日以上とする）
                if (daysUntilDue >= 0)
                {
                    // ユーザー設定の日数、またはToDo設定の日数に含まれるかチェック
                    if (userReminderDays.Contains(daysUntilDue) || assignment.Todo.ReminderDays.Contains(daysUntilDue))
                    {
                        itemsToNotify.Add((assignment.Todo, daysUntilDue));
                    }
                }
            }

            if (itemsToNotify.Any())
            {
                try
                {
                    await notificationService.SendDailyDigestNotificationAsync(user, itemsToNotify, cancellationToken);

                    // 通知済み日時を更新
                    foreach (var assignment in assignments)
                    {
                        // 通知対象に含まれたToDoの割り当てのみ更新
                        if (itemsToNotify.Any(i => i.Todo.Id == assignment.TodoId))
                        {
                            assignment.LastNotifiedAt = now;
                        }
                    }
                    
                    await dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "ダイジェストリマインダーを送信しました: UserId={UserId}, TodoCount={Count}",
                        user.Id, itemsToNotify.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ダイジェストリマインダーの送信に失敗しました: UserId={UserId}", user.Id);
                }
            }
        }
    }
}
