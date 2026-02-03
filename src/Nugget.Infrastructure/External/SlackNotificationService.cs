using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nugget.Core.Interfaces;
using SlackNet;
using SlackNet.WebApi;
using CoreUser = Nugget.Core.Entities.User;
using CoreTodo = Nugget.Core.Entities.Todo;

namespace Nugget.Infrastructure.External;

/// <summary>
/// Slack設定
/// </summary>
public class SlackOptions
{
    public const string SectionName = "Slack";

    /// <summary>
    /// Slack Bot Token
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// アプリケーションURL（通知メッセージ内のリンク用）
    /// </summary>
    public string AppUrl { get; set; } = "https://todo.company.com";
}

/// <summary>
/// Slack通知サービス実装
/// </summary>
public class SlackNotificationService : INotificationService
{
    private readonly ISlackApiClient _slackClient;
    private readonly SlackOptions _options;
    private readonly ILogger<SlackNotificationService> _logger;

    public SlackNotificationService(
        IOptions<SlackOptions> options,
        ILogger<SlackNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _slackClient = new SlackServiceBuilder()
            .UseApiToken(_options.BotToken)
            .GetApiClient();
    }

    public async Task SendNewTodoNotificationAsync(CoreTodo todo, IEnumerable<CoreUser> users, CancellationToken cancellationToken = default)
    {
        var message = BuildNewTodoMessage(todo);

        foreach (var user in users.Where(u => !string.IsNullOrEmpty(u.SlackUserId)))
        {
            try
            {
                await _slackClient.Chat.PostMessage(new Message
                {
                    Channel = user.SlackUserId!,
                    Text = message
                });

                _logger.LogInformation("新規ToDo通知を送信しました: UserId={UserId}, TodoId={TodoId}", user.Id, todo.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slack通知の送信に失敗しました: UserId={UserId}, TodoId={TodoId}", user.Id, todo.Id);
            }
        }
    }

    public async Task SendTodoUpdatedNotificationAsync(CoreTodo todo, IEnumerable<CoreUser> users, string changeDescription, CancellationToken cancellationToken = default)
    {
        var message = BuildTodoUpdatedMessage(todo, changeDescription);

        foreach (var user in users.Where(u => !string.IsNullOrEmpty(u.SlackUserId)))
        {
            try
            {
                await _slackClient.Chat.PostMessage(new Message
                {
                    Channel = user.SlackUserId!,
                    Text = message
                });

                _logger.LogInformation("ToDo更新通知を送信しました: UserId={UserId}, TodoId={TodoId}", user.Id, todo.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slack通知の送信に失敗しました: UserId={UserId}, TodoId={TodoId}", user.Id, todo.Id);
            }
        }
    }

    public async Task SendReminderNotificationAsync(CoreTodo todo, CoreUser user, int daysUntilDue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(user.SlackUserId))
        {
            _logger.LogWarning("SlackユーザーIDが設定されていないためリマインダーをスキップ: UserId={UserId}", user.Id);
            return;
        }

        var message = BuildReminderMessage(todo, daysUntilDue);

        try
        {
            await _slackClient.Chat.PostMessage(new Message
            {
                Channel = user.SlackUserId,
                Text = message
            });

            _logger.LogInformation("リマインダー通知を送信しました: UserId={UserId}, TodoId={TodoId}, DaysUntilDue={DaysUntilDue}",
                user.Id, todo.Id, daysUntilDue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "リマインダー通知の送信に失敗しました: UserId={UserId}, TodoId={TodoId}", user.Id, todo.Id);
        }
    }

    private string BuildNewTodoMessage(CoreTodo todo)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📋 *新しいToDoが追加されました*");
        sb.AppendLine();
        sb.AppendLine($"*タイトル:* {todo.Title}");
        sb.AppendLine($"*期限:* {todo.DueDate:yyyy年M月d日 HH:mm}");
        
        if (!string.IsNullOrEmpty(todo.Description))
        {
            sb.AppendLine($"*詳細:* {todo.Description}");
        }
        
        sb.AppendLine();
        sb.AppendLine($"→ <{_options.AppUrl}|アプリで確認>");
        
        return sb.ToString();
    }

    private string BuildTodoUpdatedMessage(CoreTodo todo, string changeDescription)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("🔄 *ToDoが更新されました*");
        sb.AppendLine();
        sb.AppendLine($"*タイトル:* {todo.Title}");
        sb.AppendLine($"*期限:* {todo.DueDate:yyyy年M月d日 HH:mm}");
        sb.AppendLine($"*変更内容:* {changeDescription}");
        sb.AppendLine();
        sb.AppendLine($"→ <{_options.AppUrl}|アプリで確認>");
        
        return sb.ToString();
    }

    private string BuildReminderMessage(CoreTodo todo, int daysUntilDue)
    {
        var urgencyEmoji = daysUntilDue switch
        {
            0 => "🚨",
            1 => "⚠️",
            _ => "⏰"
        };

        var daysText = daysUntilDue switch
        {
            0 => "本日が期限です",
            1 => "明日が期限です",
            _ => $"期限まであと{daysUntilDue}日です"
        };

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{urgencyEmoji} *リマインダー: {daysText}*");
        sb.AppendLine();
        sb.AppendLine($"*タイトル:* {todo.Title}");
        sb.AppendLine($"*期限:* {todo.DueDate:yyyy年M月d日 HH:mm}");
        sb.AppendLine();
        sb.AppendLine($"→ <{_options.AppUrl}|アプリで確認>");
        
        return sb.ToString();
    }
}
