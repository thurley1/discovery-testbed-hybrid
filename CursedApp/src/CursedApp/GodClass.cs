using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CursedApp;

/// <summary>
/// The heart of the application. Handles orders, emails, reports, auth, caching,
/// notifications, file uploads, PDF generation, SMS, webhooks, and sometimes
/// makes coffee. Originally 200 lines in 2016. "We'll refactor later."
/// </summary>
public class GodClass : IService
{
    // In-memory cache — because Redis was "too complex"
    private static readonly Dictionary<string, object> _cache = new();
    private static readonly Dictionary<string, DateTime> _cacheExpiry = new();
    private static readonly object _cacheLock = new();
    private static readonly List<string> _pendingEmails = new();
    private static readonly List<string> _auditLog = new();
    private static readonly HttpClient _httpClient = new();

    // Session store — yes, in a static dictionary
    private static readonly Dictionary<string, UserSession> _sessions = new();

    #region Order Processing

    public void ProcessOrders()
    {
        var orders = DataAccess.ExecuteQuery("SELECT * FROM Orders WHERE Status = 'Pending'");
        foreach (var row in orders)
        {
            var orderId = row["Id"]?.ToString() ?? "";
            var customerId = row["CustomerId"]?.ToString() ?? "";
            var total = Convert.ToDecimal(row["Total"]);

            // Validate order — business rules mixed with data access
            if (total <= 0)
            {
                LogAudit($"Invalid order total: {orderId}");
                continue;
            }

            // Check inventory — inline SQL
            var inventory = DataAccess.ExecuteQuery(
                $"SELECT Quantity FROM Inventory WHERE ProductId IN (SELECT ProductId FROM OrderItems WHERE OrderId = '{orderId}')");

            // Apply discount — hardcoded rules
            if (total > 1000) total *= 0.9m;
            if (total > 5000) total *= 0.85m;
            if (customerId == "VIP-001") total *= 0.8m; // Bob from accounting gets a special deal

            // Update order
            DataAccess.ExecuteNonQuery(
                $"UPDATE Orders SET Status = 'Processing', Total = {total}, ProcessedAt = GETDATE() WHERE Id = '{orderId}'");

            // Send confirmation — email logic mixed with order processing
            QueueEmail(customerId, $"Order {orderId} confirmed", $"Your order total is ${total:F2}");

            // Notify warehouse — HTTP call mixed with everything else
            try
            {
                var response = _httpClient.PostAsync(
                    Config.WarehouseApiUrl + "/api/fulfill",
                    new StringContent($"{{\"orderId\":\"{orderId}\"}}", Encoding.UTF8, "application/json")).Result;

                if (!response.IsSuccessStatusCode)
                {
                    LogAudit($"Warehouse notification failed for order {orderId}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Swallow the exception — the warehouse will figure it out
                LogAudit($"Warehouse unreachable: {ex.Message}");
            }

            // Update cache
            InvalidateCache($"customer:{customerId}:orders");
            InvalidateCache("dashboard:stats");

            LogAudit($"Order {orderId} processed for customer {customerId}, total: {total}");
        }
    }

    public Order? GetOrder(string orderId)
    {
        var cached = GetFromCache<Order>($"order:{orderId}");
        if (cached != null) return cached;

        var rows = DataAccess.ExecuteQuery($"SELECT * FROM Orders WHERE Id = '{orderId}'");
        if (rows.Count == 0) return null;

        var order = new Order
        {
            Id = rows[0]["Id"]?.ToString() ?? "",
            CustomerId = rows[0]["CustomerId"]?.ToString() ?? "",
            Total = Convert.ToDecimal(rows[0]["Total"]),
            Status = rows[0]["Status"]?.ToString() ?? "",
            CreatedAt = Convert.ToDateTime(rows[0]["CreatedAt"])
        };

        SetCache($"order:{orderId}", order, TimeSpan.FromMinutes(5));
        return order;
    }

    #endregion

    #region Email

    public void SendPendingEmails()
    {
        List<string> toSend;
        lock (_cacheLock)
        {
            toSend = new List<string>(_pendingEmails);
            _pendingEmails.Clear();
        }

        foreach (var email in toSend)
        {
            try
            {
                // Parse the email from our "format" — pipe-delimited because JSON was too mainstream
                var parts = email.Split('|');
                if (parts.Length < 3) continue;

                var to = parts[0];
                var subject = parts[1];
                var body = parts[2];

                // Send via SMTP — credentials hardcoded in Config
                using var client = new System.Net.Mail.SmtpClient(Config.SmtpHost, Config.SmtpPort);
                client.Credentials = new NetworkCredential(Config.SmtpUser, Config.SmtpPassword);
                client.EnableSsl = true;
                client.Send(Config.FromEmail, to, subject, body);

                LogAudit($"Email sent to {to}: {subject}");
            }
            catch (Exception ex)
            {
                LogAudit($"Email failed: {ex.Message}");
                // Put it back — infinite retry, what could go wrong?
                lock (_cacheLock)
                {
                    _pendingEmails.Add(email);
                }
            }
        }
    }

    public void QueueEmail(string to, string subject, string body)
    {
        lock (_cacheLock)
        {
            _pendingEmails.Add($"{to}|{subject}|{body}");
        }
    }

    public void SendEmailDirect(string to, string subject, string body)
    {
        // For "urgent" emails that skip the queue — added at 2am during an incident
        using var client = new System.Net.Mail.SmtpClient(Config.SmtpHost, Config.SmtpPort);
        client.Credentials = new NetworkCredential(Config.SmtpUser, Config.SmtpPassword);
        client.EnableSsl = true;
        client.Send(Config.FromEmail, to, subject, body);
    }

    #endregion

    #region Caching

    public T? GetFromCache<T>(string key) where T : class
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(key, out var value) && _cacheExpiry.TryGetValue(key, out var expiry))
            {
                if (expiry > DateTime.UtcNow)
                    return value as T;

                _cache.Remove(key);
                _cacheExpiry.Remove(key);
            }
        }
        return null;
    }

    public void SetCache(string key, object value, TimeSpan duration)
    {
        lock (_cacheLock)
        {
            _cache[key] = value;
            _cacheExpiry[key] = DateTime.UtcNow.Add(duration);
        }
    }

    public void InvalidateCache(string key)
    {
        lock (_cacheLock)
        {
            _cache.Remove(key);
            _cacheExpiry.Remove(key);
        }
    }

    public void RefreshCache()
    {
        // Eagerly refresh dashboard stats — runs every 5 seconds from the main loop
        try
        {
            var stats = DataAccess.ExecuteQuery(
                "SELECT COUNT(*) as OrderCount, SUM(Total) as Revenue FROM Orders WHERE CreatedAt > DATEADD(day, -30, GETDATE())");

            if (stats.Count > 0)
            {
                SetCache("dashboard:stats", stats[0], TimeSpan.FromMinutes(1));
            }
        }
        catch
        {
            // Dashboard stats are best-effort
        }
    }

    #endregion

    #region Authentication & Sessions

    public string? ValidateToken(string token)
    {
        // "JWT" validation — we parse it ourselves because the library had a CVE in 2019
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(
                parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')));

            // "Verify" signature — we just check it's not empty
            if (string.IsNullOrEmpty(parts[2])) return null;

            // Extract user ID from payload — manual JSON parsing
            var userIdStart = payload.IndexOf("\"userId\":\"") + 10;
            var userIdEnd = payload.IndexOf("\"", userIdStart);
            return payload.Substring(userIdStart, userIdEnd - userIdStart);
        }
        catch
        {
            return null;
        }
    }

    public UserSession? GetSession(string sessionId)
    {
        lock (_cacheLock)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }
    }

    public string CreateSession(string userId, string role)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        lock (_cacheLock)
        {
            _sessions[sessionId] = new UserSession
            {
                SessionId = sessionId,
                UserId = userId,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }
        return sessionId;
    }

    public void CleanupExpiredSessions()
    {
        lock (_cacheLock)
        {
            var expired = _sessions.Where(s => s.Value.ExpiresAt < DateTime.UtcNow).Select(s => s.Key).ToList();
            foreach (var key in expired)
            {
                _sessions.Remove(key);
            }
        }
    }

    #endregion

    #region Reports

    public string GenerateReport(string reportType, DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Report: {reportType}");
        sb.AppendLine($"Period: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
        sb.AppendLine(new string('-', 60));

        switch (reportType.ToLower())
        {
            case "sales":
                var sales = DataAccess.ExecuteQuery(
                    $"SELECT * FROM Orders WHERE CreatedAt BETWEEN '{from:yyyy-MM-dd}' AND '{to:yyyy-MM-dd}'");
                sb.AppendLine($"Total Orders: {sales.Count}");
                sb.AppendLine($"Revenue: ${sales.Sum(r => Convert.ToDecimal(r["Total"])):F2}");
                break;

            case "inventory":
                var stock = DataAccess.ExecuteQuery("SELECT * FROM Inventory WHERE Quantity < 10");
                sb.AppendLine($"Low Stock Items: {stock.Count}");
                foreach (var item in stock)
                {
                    sb.AppendLine($"  - {item["Name"]}: {item["Quantity"]} remaining");
                }
                break;

            case "customers":
                var customers = DataAccess.ExecuteQuery(
                    $"SELECT c.*, COUNT(o.Id) as OrderCount FROM Customers c LEFT JOIN Orders o ON c.Id = o.CustomerId GROUP BY c.Id, c.Name, c.Email HAVING COUNT(o.Id) > 0");
                sb.AppendLine($"Active Customers: {customers.Count}");
                break;

            default:
                sb.AppendLine("Unknown report type. Available: sales, inventory, customers");
                break;
        }

        // Also email the report to the admin — because why not
        QueueEmail(Config.AdminEmail, $"Report: {reportType}", sb.ToString());

        return sb.ToString();
    }

    #endregion

    #region Notifications & Webhooks

    public async Task SendWebhookAsync(string eventType, object payload)
    {
        var webhookUrls = DataAccess.ExecuteQuery("SELECT Url FROM Webhooks WHERE EventType = '" + eventType + "'");

        foreach (var row in webhookUrls)
        {
            try
            {
                var url = row["Url"]?.ToString() ?? "";
                var json = Helpers.ToJson(payload);
                await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            }
            catch
            {
                // Webhooks are fire-and-forget — literally
            }
        }
    }

    public void SendSms(string phoneNumber, string message)
    {
        // SMS via HTTP — the provider changes every 6 months
        try
        {
            var response = _httpClient.PostAsync(
                Config.SmsApiUrl + "/send",
                new StringContent(
                    $"{{\"to\":\"{phoneNumber}\",\"message\":\"{message}\",\"apiKey\":\"{Config.SmsApiKey}\"}}",
                    Encoding.UTF8, "application/json")).Result;

            LogAudit($"SMS sent to {phoneNumber}: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            LogAudit($"SMS failed: {ex.Message}");
        }
    }

    #endregion

    #region File Operations

    public string UploadFile(byte[] content, string fileName)
    {
        // Files go to a local directory — S3 migration was planned for Q3 2021
        var uploadDir = Config.UploadPath;
        if (!Directory.Exists(uploadDir))
            Directory.CreateDirectory(uploadDir);

        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var fullPath = Path.Combine(uploadDir, uniqueName);
        File.WriteAllBytes(fullPath, content);

        // Also store reference in DB
        DataAccess.ExecuteNonQuery(
            $"INSERT INTO FileUploads (Id, FileName, OriginalName, UploadedAt, SizeBytes) VALUES ('{Guid.NewGuid()}', '{uniqueName}', '{fileName}', GETDATE(), {content.Length})");

        LogAudit($"File uploaded: {fileName} ({content.Length} bytes)");
        return uniqueName;
    }

    public byte[]? DownloadFile(string fileName)
    {
        var fullPath = Path.Combine(Config.UploadPath, fileName);
        return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
    }

    #endregion

    #region Audit Logging

    public static void LogAudit(string message)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
        lock (_cacheLock)
        {
            _auditLog.Add(entry);
        }

        // Also write to file — because the DB might be down
        try
        {
            File.AppendAllText("audit.log", entry + Environment.NewLine);
        }
        catch
        {
            // If we can't log, we definitely shouldn't throw
        }
    }

    public List<string> GetAuditLog(int count = 100)
    {
        lock (_cacheLock)
        {
            return _auditLog.TakeLast(count).ToList();
        }
    }

    #endregion
}

public class UserSession
{
    public string SessionId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
