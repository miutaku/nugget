using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nugget.Api.DTOs;
using Nugget.Core.Entities;
using Nugget.Core.Interfaces;
using System.Security.Claims;

namespace Nugget.Api.Controllers;

[ApiController]
[Route("api/notification-settings")]
[Authorize]
[EnableRateLimiting("StrictPolicy")]
public class NotificationSettingsController : ControllerBase
{
    private readonly INotificationSettingRepository _repository;
    private readonly ILogger<NotificationSettingsController> _logger;

    public NotificationSettingsController(
        INotificationSettingRepository repository,
        ILogger<NotificationSettingsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 現在のユーザーの通知設定を取得
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var settings = await _repository.GetByUserIdAsync(userId, cancellationToken);
        
        if (settings == null)
        {
            // デフォルト設定を返す
            return Ok(new NotificationSettingResponse
            {
                DaysBeforeDue = new[] { 7, 3, 1, 0 },
                SlackNotificationEnabled = true,
                NotificationHour = 9
            });
        }

        return Ok(new NotificationSettingResponse
        {
            DaysBeforeDue = settings.DaysBeforeDue,
            SlackNotificationEnabled = settings.SlackNotificationEnabled,
            NotificationHour = settings.NotificationHour
        });
    }

    /// <summary>
    /// 現在のユーザーの通知設定を更新
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateNotificationSettingRequest request, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var settings = new NotificationSetting
        {
            UserId = userId,
            DaysBeforeDue = request.DaysBeforeDue,
            SlackNotificationEnabled = request.SlackNotificationEnabled,
            NotificationHour = request.NotificationHour
        };

        await _repository.UpdateAsync(settings, cancellationToken);
        
        _logger.LogInformation("ユーザー {UserId} の通知設定を更新しました", userId);

        return NoContent();
    }
}
