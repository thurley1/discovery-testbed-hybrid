namespace ReportService;

public class ReportScheduler
{
    private readonly Dictionary<string, ScheduledReport> _schedules = new();

    public string DefaultTimezone { get; set; } = "UTC";
    public int MaxConcurrentReports { get; set; } = 3;
    public bool EnableRetryOnFailure { get; set; } = true;

    public void Schedule(string reportType, string cronExpression)
    {
        var scheduled = new ScheduledReport
        {
            ReportType = reportType,
            CronExpression = cronExpression,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _schedules[reportType] = scheduled;
    }

    public void Unschedule(string reportType)
    {
        _schedules.Remove(reportType);
    }

    public IReadOnlyList<ScheduledReport> GetActiveSchedules()
    {
        return _schedules.Values.Where(s => s.IsActive).ToList();
    }
}

public class ScheduledReport
{
    public string ReportType { get; init; } = string.Empty;
    public string CronExpression { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; set; }
}
