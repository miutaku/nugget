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

        // 最大リマインダー日数（デフォルト設定の最大値）
        const int maxDaysAhead = 7;

        // 期限が近いToDoを取得
        var todos = await dbContext.Todos
            .Include(t => t.Assignments)
                .ThenInclude(a => a.User)
                    .ThenInclude(u => u!.NotificationSetting)
            .Where(t => t.DueDate.Date <= now.Date.AddDays(maxDaysAhead))
            .Where(t => t.DueDate.Date >= now.Date)
            .Where(t => t.Assignments.Any(a => !a.IsCompleted))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("リマインダー対象のToDo数: {Count}", todos.Count);

        foreach (var todo in todos)
        {
            var daysUntilDue = (int)(todo.DueDate.Date - now.Date).TotalDays;

            // ToDoに設定されたリマインダー日数に該当するかチェック
            if (!todo.ReminderDays.Contains(daysUntilDue))
            {
                continue;
            }

            foreach (var assignment in todo.Assignments.Where(a => !a.IsCompleted))
            {
                var user = assignment.User;
                if (user == null || !user.IsActive)
                {
                    continue;
                }

                // ユーザーの通知設定をチェック
                var notificationSetting = user.NotificationSetting;
                if (notificationSetting != null)
                {
                    // 通知が無効の場合はスキップ
                    if (!notificationSetting.SlackNotificationEnabled)
                    {
                        continue;
                    }

                    // 通知時刻でない場合はスキップ
                    if (notificationSetting.NotificationHour != currentHour)
                    {
                        continue;
                    }

                    // ユーザー設定のリマインダー日数に該当しない場合はスキップ
                    if (!notificationSetting.DaysBeforeDue.Contains(daysUntilDue))
                    {
                        continue;
                    }
                }
                else
                {
                    // デフォルト: 9時に通知
                    if (currentHour != 9)
                    {
                        continue;
                    }
                }

                // 同じ日に既に通知済みの場合はスキップ
                if (assignment.LastNotifiedAt?.Date == now.Date)
                {
                    continue;
                }

                try
                {
                    await notificationService.SendReminderNotificationAsync(todo, user, daysUntilDue, cancellationToken);

                    // 通知日時を更新
                    assignment.LastNotifiedAt = now;
                    await dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "リマインダーを送信しました: TodoId={TodoId}, UserId={UserId}, DaysUntilDue={DaysUntilDue}",
                        todo.Id, user.Id, daysUntilDue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "リマインダー送信に失敗しました: TodoId={TodoId}, UserId={UserId}",
                        todo.Id, user.Id);
                }
            }
        }
    }
}
