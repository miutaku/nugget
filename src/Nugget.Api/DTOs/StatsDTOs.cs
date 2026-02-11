namespace Nugget.Api.DTOs;

public record GlobalStatsResponse
{
    public int TotalUsers { get; init; }
    public int TotalGroups { get; init; }
    public int TotalTodos { get; init; }
    public int TotalAssignments { get; init; }
    public int CompletedAssignments { get; init; }
    public double CompletionRate { get; init; }
    public List<TargetTypeStats> TargetTypeBreakdown { get; init; } = [];
    public List<DailyActivityStats> RecentActivity { get; init; } = [];
}

public record PersonalStatsResponse
{
    public int TotalAssigned { get; init; }
    public int CompletedCount { get; init; }
    public int OverdueCount { get; init; }
    public double CompletionRate { get; init; }
    public List<DailyActivityStats> PersonalActivity { get; init; } = [];
}

public record TargetTypeStats(string TargetType, int Count);
public record DailyActivityStats(DateTime Date, int CreatedCount, int CompletedCount);
