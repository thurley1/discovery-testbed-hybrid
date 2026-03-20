using ReportService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ReportGenerator>();
builder.Services.AddSingleton<ReportScheduler>();

var app = builder.Build();

app.MapPost("/api/reports/generate", async (GenerateReportRequest request, ReportGenerator generator) =>
{
    var report = await generator.GenerateAsync(request.ReportType, request.StartDate, request.EndDate);
    return Results.Ok(report);
});

app.MapPost("/api/reports/schedule", (ScheduleReportRequest request, ReportScheduler scheduler) =>
{
    scheduler.Schedule(request.ReportType, request.CronExpression);
    return Results.Accepted();
});

app.MapGet("/api/reports/health", () => Results.Ok(new { Status = "Healthy", Service = "ReportService" }));

app.Run();

public record GenerateReportRequest(string ReportType, DateTime StartDate, DateTime EndDate);
public record ScheduleReportRequest(string ReportType, string CronExpression);
