namespace HybridApp.Contracts;

/// <summary>
/// Contract for the notification service.
/// Used for inter-service communication — a multi-service signal.
/// </summary>
public interface INotificationService
{
    Task SendNotificationAsync(string recipientEmail, string subject, string body, CancellationToken ct = default);
    Task SendBatchNotificationAsync(IEnumerable<string> recipientEmails, string subject, string body, CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
