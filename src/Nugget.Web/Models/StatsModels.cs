namespace Nugget.Web.Models;

public class GlobalStatsModel
{
    public int TotalUsers { get; set; }
    public int TotalGroups { get; set; }
    public int TotalTodos { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public double CompletionRate { get; set; }
    public List<TargetTypeStats> TargetTypeBreakdown { get; set; } = new();
    public List<DailyActivityStats> RecentActivity { get; set; } = new();
}

public class PersonalStatsModel
{
    public int TotalAssigned { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
    public double CompletionRate { get; set; }
    public List<DailyActivityStats> PersonalActivity { get; set; } = new();
}

public class TargetTypeStats
{
    public string TargetType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyActivityStats
{
    public DateTime Date { get; set; }
    public int CreatedCount { get; set; }
    public int CompletedCount { get; set; }
}
