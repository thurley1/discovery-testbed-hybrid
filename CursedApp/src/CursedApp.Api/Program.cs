using CursedApp;
using CursedApp.Api.Middleware;

// SECOND entry point — this one runs the web API
// Nobody is sure if this or the main Program.cs should be the "real" one
// In production, both run on different ports via IIS (don't ask)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register the god class as a singleton — because DI is "just a service locator anyway"
builder.Services.AddSingleton<GodClass>();
builder.Services.AddSingleton<IService>(sp => sp.GetRequiredService<GodClass>());

// CORS — allow everything, we'll lock it down later (we won't)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize config
Config.Initialize();

// The do-everything middleware — must be first (and last, conceptually)
app.UseMiddleware<DoEverythingMiddleware>();

app.UseCors();
app.MapControllers();

// Also map some minimal API endpoints because someone read about them
app.MapGet("/health", () => new
{
    Status = "ok",
    Version = Config.AppVersion,
    Environment = Config.Environment, // Always "Production"
    Uptime = "unknown" // We don't track this
});

app.MapGet("/api/version", () => Config.AppVersion);

Console.WriteLine($"CursedApp API starting on port 5000 (or maybe 5001, check IIS)");
app.Run();
