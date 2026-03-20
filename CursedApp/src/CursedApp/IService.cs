using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CursedApp;

/// <summary>
/// The One Interface To Rule Them All.
/// "We'll split this into smaller interfaces when we refactor" — Sprint 47 retro notes (2018)
/// ISP violation score: unmeasurable.
/// </summary>
public interface IService
{
    // Order processing
    void ProcessOrders();
    Order? GetOrder(string orderId);

    // Email
    void SendPendingEmails();
    void QueueEmail(string to, string subject, string body);
    void SendEmailDirect(string to, string subject, string body);

    // Caching
    T? GetFromCache<T>(string key) where T : class;
    void SetCache(string key, object value, TimeSpan duration);
    void InvalidateCache(string key);
    void RefreshCache();

    // Authentication
    string? ValidateToken(string token);
    UserSession? GetSession(string sessionId);
    string CreateSession(string userId, string role);
    void CleanupExpiredSessions();

    // Reports
    string GenerateReport(string reportType, DateTime from, DateTime to);

    // Notifications
    Task SendWebhookAsync(string eventType, object payload);
    void SendSms(string phoneNumber, string message);

    // Files
    string UploadFile(byte[] content, string fileName);
    byte[]? DownloadFile(string fileName);

    // Audit
    List<string> GetAuditLog(int count = 100);
}
