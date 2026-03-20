using HybridApp.Data;
using HybridApp.Domain;

namespace NotificationService;

/// <summary>
/// Sends notifications. Directly accesses the shared AppDbContext to look up
/// customer email preferences — a strong monolith coupling signal.
/// </summary>
public class NotificationSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly bool _useSsl;
    private readonly AppDbContext _dbContext;

    public NotificationSender(AppDbContext dbContext)
    {
        _smtpHost = "localhost";
        _smtpPort = 587;
        _useSsl = true;
        _dbContext = dbContext;
    }

    public async Task SendAsync(string recipientEmail, string subject, string body)
    {
        // Look up customer preferences from the shared database
        var customer = _dbContext.Customers
            .FirstOrDefault(c => c.Email == recipientEmail);

        var template = new EmailTemplate(subject, body);
        var rendered = template.Render(recipientEmail);

        // In a real implementation, this would use an SMTP client
        await Task.Delay(10); // Simulate send
        Console.WriteLine($"Notification sent to {recipientEmail}: {rendered}");
    }

    public async Task SendOrderConfirmationAsync(Guid orderId)
    {
        // Direct database access to load order details — shared DB coupling
        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order is null) return;

        var customer = await _dbContext.Customers.FindAsync(order.CustomerId);
        if (customer is null) return;

        await SendAsync(customer.Email, $"Order {orderId} Confirmed", $"Total: {order.TotalAmount:C}");
    }

    public async Task SendBatchAsync(IEnumerable<string> recipientEmails, string subject, string body)
    {
        foreach (var email in recipientEmails)
        {
            await SendAsync(email, subject, body);
        }
    }
}
