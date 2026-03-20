namespace NotificationService;

public class NotificationSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly bool _useSsl;

    public NotificationSender()
    {
        _smtpHost = "localhost";
        _smtpPort = 587;
        _useSsl = true;
    }

    public async Task SendAsync(string recipientEmail, string subject, string body)
    {
        var template = new EmailTemplate(subject, body);
        var rendered = template.Render(recipientEmail);

        // In a real implementation, this would use an SMTP client
        await Task.Delay(10); // Simulate send
        Console.WriteLine($"Notification sent to {recipientEmail}: {rendered}");
    }

    public async Task SendBatchAsync(IEnumerable<string> recipientEmails, string subject, string body)
    {
        foreach (var email in recipientEmails)
        {
            await SendAsync(email, subject, body);
        }
    }
}
