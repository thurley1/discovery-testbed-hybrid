using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CursedApp;

namespace CursedApp.Api.Controllers;

/// <summary>
/// One controller to rule them all. Orders, customers, products, reports,
/// auth, files, admin — everything lives here because "we'll split it up
/// when we do the rewrite" (planned for Q2 2021, now Q4 2026).
/// </summary>
[ApiController]
[Route("api")]
public class EverythingController : ControllerBase
{
    private readonly GodClass _god;

    public EverythingController(GodClass god)
    {
        _god = god;
    }

    #region Orders

    [HttpGet("orders/{id}")]
    public IActionResult GetOrder(string id)
    {
        var order = _god.GetOrder(id);
        if (order == null) return NotFound(new { error = "Order not found", id });
        return Ok(order);
    }

    [HttpPost("orders/process")]
    public IActionResult ProcessOrders()
    {
        // Trigger order processing manually — also runs on a timer in Program.cs
        _god.ProcessOrders();
        return Ok(new { message = "Orders processed (probably)" });
    }

    [HttpGet("orders")]
    public IActionResult ListOrders([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int size = 50)
    {
        var where = string.IsNullOrEmpty(status) ? "" : $" WHERE Status = '{status}'";
        var sql = $"SELECT * FROM Orders{where} ORDER BY CreatedAt DESC OFFSET {(page - 1) * size} ROWS FETCH NEXT {size} ROWS ONLY";
        var results = DataAccess.ExecuteQuery(sql);
        return Ok(results);
    }

    #endregion

    #region Customers

    [HttpGet("customers/{id}")]
    public IActionResult GetCustomer(string id)
    {
        var rows = DataAccess.ExecuteQuery($"SELECT * FROM Customers WHERE Id = '{id}'");
        if (rows.Count == 0) return NotFound();
        return Ok(rows[0]);
    }

    [HttpGet("customers")]
    public IActionResult ListCustomers([FromQuery] string? tier)
    {
        var sql = string.IsNullOrEmpty(tier)
            ? "SELECT * FROM Customers ORDER BY Name"
            : $"SELECT * FROM Customers WHERE Tier = '{tier}' ORDER BY Name";
        return Ok(DataAccess.ExecuteQuery(sql));
    }

    [HttpPost("customers")]
    public IActionResult CreateCustomer([FromBody] Dictionary<string, string> data)
    {
        var id = Helpers.GenerateId();
        DataAccess.ExecuteNonQuery(
            $"INSERT INTO Customers (Id, Name, Email, Phone, Tier, CreatedAt) VALUES ('{id}', '{data.GetValueOrDefault("name", "")}', '{data.GetValueOrDefault("email", "")}', '{data.GetValueOrDefault("phone", "")}', 'Standard', GETDATE())");
        return Created($"/api/customers/{id}", new { id });
    }

    #endregion

    #region Products

    [HttpGet("products/{id}")]
    public IActionResult GetProduct(string id)
    {
        var cached = _god.GetFromCache<Dictionary<string, object?>>($"product:{id}");
        if (cached != null) return Ok(cached);

        var rows = DataAccess.ExecuteQuery($"SELECT * FROM Products WHERE Id = '{id}'");
        if (rows.Count == 0) return NotFound();

        _god.SetCache($"product:{id}", rows[0], TimeSpan.FromMinutes(10));
        return Ok(rows[0]);
    }

    [HttpGet("products")]
    public IActionResult ListProducts([FromQuery] string? category, [FromQuery] bool activeOnly = true)
    {
        var conditions = new List<string>();
        if (activeOnly) conditions.Add("IsActive = 1");
        if (!string.IsNullOrEmpty(category)) conditions.Add($"Category = '{category}'");

        var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
        return Ok(DataAccess.ExecuteQuery($"SELECT * FROM Products{where} ORDER BY Name"));
    }

    #endregion

    #region Reports

    [HttpGet("reports/{type}")]
    public IActionResult GenerateReport(string type, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;
        var report = _god.GenerateReport(type, fromDate, toDate);
        return Ok(new { type, from = fromDate, to = toDate, content = report });
    }

    #endregion

    #region Auth — yes, in the same controller

    [HttpPost("auth/login")]
    public IActionResult Login([FromBody] Dictionary<string, string> credentials)
    {
        var email = credentials.GetValueOrDefault("email", "");
        var password = credentials.GetValueOrDefault("password", "");

        // Check against hardcoded admin credentials first
        if (email == "admin@cursedapp.com" && password == Config.SuperAdminPassword)
        {
            var session = _god.CreateSession("admin", "SuperAdmin");
            return Ok(new { sessionId = session, role = "SuperAdmin" });
        }

        // Check database
        var hashedPassword = Helpers.Hash(password);
        var rows = DataAccess.ExecuteQuery(
            $"SELECT Id, Role FROM Users WHERE Email = '{email}' AND PasswordHash = '{hashedPassword}'");

        if (rows.Count == 0)
            return Unauthorized(new { error = "Invalid credentials" });

        var userId = rows[0]["Id"]?.ToString() ?? "";
        var role = rows[0]["Role"]?.ToString() ?? "User";
        var sessionId = _god.CreateSession(userId, role);

        return Ok(new { sessionId, role });
    }

    [HttpPost("auth/logout")]
    public IActionResult Logout([FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        // Sessions expire on their own — logout is just for show
        return Ok(new { message = "Logged out" });
    }

    [HttpGet("auth/me")]
    public IActionResult GetCurrentUser([FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return Unauthorized();
        var session = _god.GetSession(sessionId);
        if (session == null) return Unauthorized();
        return Ok(session);
    }

    #endregion

    #region Files

    [HttpPost("files/upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file");
        if (file.Length > Config.MaxUploadSizeMb * 1024 * 1024) return BadRequest("File too large");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileName = _god.UploadFile(ms.ToArray(), file.FileName);

        return Ok(new { fileName, size = file.Length });
    }

    [HttpGet("files/{fileName}")]
    public IActionResult DownloadFile(string fileName)
    {
        var content = _god.DownloadFile(fileName);
        if (content == null) return NotFound();
        return File(content, "application/octet-stream", fileName);
    }

    #endregion

    #region Admin

    [HttpGet("admin/audit")]
    public IActionResult GetAuditLog([FromQuery] int count = 100)
    {
        // No auth check — anyone can see the audit log (it's a feature, not a bug)
        return Ok(_god.GetAuditLog(count));
    }

    [HttpPost("admin/cache/clear")]
    public IActionResult ClearCache()
    {
        // Nuclear option
        _god.InvalidateCache("*"); // This doesn't actually work with our cache implementation
        return Ok(new { message = "Cache cleared (sort of)" });
    }

    [HttpGet("admin/stats")]
    public IActionResult GetStats()
    {
        var cached = _god.GetFromCache<Dictionary<string, object?>>("dashboard:stats");
        return Ok(cached ?? new Dictionary<string, object?> { ["message"] = "Stats not cached yet" });
    }

    #endregion

    #region Webhooks

    [HttpPost("webhooks/trigger")]
    public async Task<IActionResult> TriggerWebhook([FromBody] Dictionary<string, object> data)
    {
        var eventType = data.GetValueOrDefault("event")?.ToString() ?? "unknown";
        await _god.SendWebhookAsync(eventType, data);
        return Ok(new { message = $"Webhook triggered: {eventType}" });
    }

    #endregion
}
