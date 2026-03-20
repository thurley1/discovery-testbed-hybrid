using HybridApp.Data;
using HybridApp.Domain;
using Microsoft.EntityFrameworkCore;

namespace ReportService;

/// <summary>
/// Generates reports. Directly queries the shared AppDbContext for data —
/// a strong monolith coupling signal (services sharing a database).
/// </summary>
public class ReportGenerator
{
    private readonly AppDbContext _dbContext;

    public string OutputFormat { get; set; } = "PDF";
    public bool IncludeCharts { get; set; } = true;
    public int MaxRowCount { get; set; } = 10000;

    public ReportGenerator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportResult> GenerateAsync(string reportType, DateTime startDate, DateTime endDate)
    {
        // Query shared database directly — same DB as WebApi and NotificationService
        var orderCount = await _dbContext.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .CountAsync();

        var customerCount = await _dbContext.Customers.CountAsync();

        return new ReportResult
        {
            ReportId = Guid.NewGuid(),
            ReportType = reportType,
            GeneratedAt = DateTime.UtcNow,
            Format = OutputFormat,
            RowCount = orderCount
        };
    }

    public async Task<byte[]> ExportAsync(Guid reportId)
    {
        await Task.Delay(50); // Simulate export
        return Array.Empty<byte>();
    }
}

public class ReportResult
{
    public Guid ReportId { get; init; }
    public string ReportType { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public string Format { get; init; } = string.Empty;
    public int RowCount { get; init; }
}
