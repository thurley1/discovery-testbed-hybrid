using HybridApp.Data;
using HybridApp.Domain;
using Microsoft.EntityFrameworkCore;
using NotificationService;
using ReportService;

var builder = WebApplication.CreateBuilder(args);

// Single shared database context — used by WebApi, NotificationService, and ReportService
// This is a strong monolith signal: all "services" share one database via one DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Domain repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Register services from other projects in the same DI container
// Strong monolith signal — true microservices would be separate processes
builder.Services.AddScoped<NotificationSender>();
builder.Services.AddScoped<ReportGenerator>();
builder.Services.AddScoped<ReportScheduler>();
builder.Services.AddSingleton<SharedEventBus>();

var app = builder.Build();

app.MapGroup("/api/orders").MapOrderEndpoints();
app.MapGroup("/api/customers").MapCustomerEndpoints();

// Proxy endpoints that delegate to "service" classes — looks like a monolith gateway
app.MapPost("/api/notifications/send", async (SendNotificationRequest request, NotificationSender sender) =>
{
    await sender.SendAsync(request.RecipientEmail, request.Subject, request.Body);
    return Results.Accepted();
});

app.MapPost("/api/reports/generate", async (GenerateReportRequest request, ReportGenerator generator) =>
{
    var report = await generator.GenerateAsync(request.ReportType, request.StartDate, request.EndDate);
    return Results.Ok(report);
});

app.Run();

public record SendNotificationRequest(string RecipientEmail, string Subject, string Body);
public record GenerateReportRequest(string ReportType, DateTime StartDate, DateTime EndDate);
