using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<NotificationSender>();

var app = builder.Build();

app.MapPost("/api/notifications/send", async (SendNotificationRequest request, NotificationSender sender) =>
{
    await sender.SendAsync(request.RecipientEmail, request.Subject, request.Body);
    return Results.Accepted();
});

app.MapGet("/api/notifications/health", () => Results.Ok(new { Status = "Healthy", Service = "NotificationService" }));

app.Run();

public record SendNotificationRequest(string RecipientEmail, string Subject, string Body);
