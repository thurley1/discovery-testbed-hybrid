namespace NotificationService;

public class EmailTemplate
{
    public string Subject { get; }
    public string Body { get; }
    public string TemplateName { get; }
    public DateTime CreatedAt { get; }

    public EmailTemplate(string subject, string body)
    {
        Subject = subject;
        Body = body;
        TemplateName = "default";
        CreatedAt = DateTime.UtcNow;
    }

    public string Render(string recipientEmail)
    {
        return $"To: {recipientEmail}\nSubject: {Subject}\n\n{Body}";
    }

    public string RenderHtml(string recipientEmail)
    {
        return $"<html><body><p>To: {recipientEmail}</p><h1>{Subject}</h1><p>{Body}</p></body></html>";
    }
}
