namespace HybridApp.Contracts;

/// <summary>
/// Contract for the report service.
/// Used for inter-service communication — a multi-service signal.
/// </summary>
public interface IReportService
{
    Task<ReportRequest> GenerateReportAsync(string reportType, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task ScheduleReportAsync(string reportType, string cronExpression, CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}

public record ReportRequest(Guid ReportId, string ReportType, DateTime GeneratedAt, string Format, int RowCount);
