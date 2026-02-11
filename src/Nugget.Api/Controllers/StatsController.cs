using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nugget.Api.DTOs;
using Nugget.Infrastructure.Data;
using System.Security.Claims;

namespace Nugget.Api.Controllers;

/// <summary>
/// 統計情報 API コントローラー
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly NuggetDbContext _context;
    private readonly ILogger<StatsController> _logger;

    public StatsController(NuggetDbContext context, ILogger<StatsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 全体の統計情報を取得（管理者のみ）
    /// </summary>
    [HttpGet("global")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GlobalStatsResponse>> GetGlobalStats(CancellationToken cancellationToken)
    {
        var totalUsers = await _context.Users.CountAsync(u => u.IsActive, cancellationToken);
        var totalGroups = await _context.Groups.CountAsync(cancellationToken);
        var totalTodos = await _context.Todos.CountAsync(cancellationToken);
        
        var assignmentStats = await _context.TodoAssignments
            .GroupBy(a => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Completed = g.Count(a => a.IsCompleted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalAssignments = assignmentStats?.Total ?? 0;
        var completedAssignments = assignmentStats?.Completed ?? 0;
        var completionRate = totalAssignments > 0 ? (double)completedAssignments / totalAssignments * 100 : 0;

        var typeBreakdown = await _context.Todos
            .GroupBy(t => t.TargetType)
            .Select(g => new TargetTypeStats(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken);

        // 直近7日間のアクティビティ
        var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);
        var recentActivity = await _context.TodoAssignments
            .Include(a => a.Todo)
            .Where(a => a.Todo.CreatedAt >= sevenDaysAgo || (a.CompletedAt != null && a.CompletedAt >= sevenDaysAgo))
            .ToListAsync(cancellationToken);

        var activityList = Enumerable.Range(0, 7)
            .Select(i => sevenDaysAgo.AddDays(i))
            .Select(date => new DailyActivityStats(
                date,
                recentActivity.Count(a => a.Todo.CreatedAt.Date == date),
                recentActivity.Count(a => a.CompletedAt?.Date == date)
            ))
            .ToList();

        return new GlobalStatsResponse
        {
            TotalUsers = totalUsers,
            TotalGroups = totalGroups,
            TotalTodos = totalTodos,
            TotalAssignments = totalAssignments,
            CompletedAssignments = completedAssignments,
            CompletionRate = Math.Round(completionRate, 1),
            TargetTypeBreakdown = typeBreakdown,
            RecentActivity = activityList
        };
    }

    /// <summary>
    /// 自分の統計情報を取得
    /// </summary>
    [HttpGet("personal")]
    public async Task<ActionResult<PersonalStatsResponse>> GetPersonalStats(CancellationToken cancellationToken)
    {
        // ClaimsTransformation によって追加された Guid 形式の NameIdentifier を優先的に探す
        var userIdStr = User.FindAll(ClaimTypes.NameIdentifier)
                            .Select(c => c.Value)
                            .FirstOrDefault(v => Guid.TryParse(v, out _));

        if (string.IsNullOrEmpty(userIdStr))
        {
            userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var assignments = await _context.TodoAssignments
            .Include(a => a.Todo)
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        var totalAssigned = assignments.Count;
        var completedCount = assignments.Count(a => a.IsCompleted);
        var now = DateTime.UtcNow.Date;
        var overdueCount = assignments.Count(a => !a.IsCompleted && a.Todo.DueDate.Date < now);
        
        var completionRate = totalAssigned > 0 ? (double)completedCount / totalAssigned * 100 : 0;

        var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);
        var activityList = Enumerable.Range(0, 7)
            .Select(i => sevenDaysAgo.AddDays(i))
            .Select(date => new DailyActivityStats(
                date,
                assignments.Count(a => a.Todo.CreatedAt.Date == date),
                assignments.Count(a => a.CompletedAt?.Date == date)
            ))
            .ToList();

        return new PersonalStatsResponse
        {
            TotalAssigned = totalAssigned,
            CompletedCount = completedCount,
            OverdueCount = overdueCount,
            CompletionRate = Math.Round(completionRate, 1),
            PersonalActivity = activityList
        };
    }
}
