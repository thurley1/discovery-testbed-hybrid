using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CursedApp;

namespace CursedApp.Api.Middleware;

/// <summary>
/// The middleware that does everything. Auth, logging, caching, request transformation,
/// rate limiting, CORS (again), error handling, and metrics — all in one Invoke().
/// "Single Responsibility? This IS its single responsibility: handling requests."
/// — Actual quote from the original author's PR description
/// </summary>
public class DoEverythingMiddleware
{
    private readonly RequestDelegate _next;
    private static int _requestCount = 0;
    private static readonly Dictionary<string, int> _rateLimitTracker = new();
    private static readonly Dictionary<string, (byte[] Body, DateTime CachedAt)> _responseCache = new();

    public DoEverythingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        _requestCount++;
        var requestId = $"REQ-{_requestCount:D8}";

        // === PHASE 1: Logging (pre-request) ===
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        GodClass.LogAudit($"[{requestId}] {method} {path} from {clientIp}");

        // === PHASE 2: Maintenance mode check ===
        if (Config.MaintenanceMode && !path.StartsWith("/health"))
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("{\"error\":\"Service unavailable — maintenance in progress\"}");
            return;
        }

        // === PHASE 3: Rate limiting (in-memory, per-IP, resets on restart) ===
        var rateLimitKey = $"{clientIp}:{DateTime.UtcNow:yyyyMMddHHmm}";
        lock (_rateLimitTracker)
        {
            _rateLimitTracker.TryGetValue(rateLimitKey, out var count);
            if (count > 100) // 100 requests per minute per IP
            {
                context.Response.StatusCode = 429;
                context.Response.WriteAsync("{\"error\":\"Rate limited\"}").Wait(); // .Wait() in async — fight me
                return;
            }
            _rateLimitTracker[rateLimitKey] = count + 1;
        }

        // === PHASE 4: Authentication ===
        var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (!path.StartsWith("/api/auth") && !path.StartsWith("/health") && !path.StartsWith("/legacy"))
        {
            string? userId = null;

            if (!string.IsNullOrEmpty(sessionId))
            {
                var god = context.RequestServices.GetRequiredService<GodClass>();
                var session = god.GetSession(sessionId);
                if (session != null) userId = session.UserId;
            }
            else if (!string.IsNullOrEmpty(token))
            {
                var god = context.RequestServices.GetRequiredService<GodClass>();
                userId = god.ValidateToken(token);
            }

            // Auth is optional for GET requests (because the mobile app doesn't send tokens sometimes)
            if (userId == null && method != "GET")
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
                return;
            }

            if (userId != null)
            {
                context.Items["UserId"] = userId;
            }
        }

        // === PHASE 5: Response caching (GET only, in-memory) ===
        var cacheKey = $"{method}:{path}";
        if (method == "GET")
        {
            lock (_responseCache)
            {
                if (_responseCache.TryGetValue(cacheKey, out var cached) &&
                    cached.CachedAt > DateTime.UtcNow.AddSeconds(-30))
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    context.Response.Headers["X-Cache"] = "HIT";
                    await context.Response.Body.WriteAsync(cached.Body);
                    return;
                }
            }
        }

        // === PHASE 6: Request body transformation ===
        // Convert snake_case to camelCase because the mobile app sends snake_case
        // but the controllers expect camelCase (sometimes PascalCase, nobody knows)
        if (context.Request.ContentType?.Contains("application/json") == true)
        {
            context.Request.EnableBuffering();
        }

        // === PHASE 7: Execute the actual request ===
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // === PHASE 8a: Global exception handling ===
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var errorJson = $"{{\"error\":\"{ex.Message.Replace("\"", "\\\"")}\",\"requestId\":\"{requestId}\"}}";
            await context.Response.WriteAsync(errorJson);
            GodClass.LogAudit($"[{requestId}] ERROR: {ex.Message}");
        }

        // === PHASE 8b: Cache the response ===
        if (method == "GET" && context.Response.StatusCode == 200)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var body = responseBody.ToArray();
            lock (_responseCache)
            {
                _responseCache[cacheKey] = (body, DateTime.UtcNow);
            }
        }

        // === PHASE 9: Copy response to original stream ===
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);

        // === PHASE 10: Logging (post-request) ===
        stopwatch.Stop();
        GodClass.LogAudit($"[{requestId}] {context.Response.StatusCode} in {stopwatch.ElapsedMilliseconds}ms");

        // === PHASE 11: Metrics (to nowhere) ===
        // TODO: Send to Prometheus/Grafana/DataDog/something (ticket INFRA-892, opened 2021)

        // === PHASE 12: Cleanup rate limit tracker (every 1000 requests) ===
        if (_requestCount % 1000 == 0)
        {
            lock (_rateLimitTracker)
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-5).ToString("yyyyMMddHHmm");
                var staleKeys = _rateLimitTracker.Keys.Where(k => k.Split(':').Last().CompareTo(cutoff) < 0).ToList();
                foreach (var key in staleKeys)
                    _rateLimitTracker.Remove(key);
            }
        }
    }
}
