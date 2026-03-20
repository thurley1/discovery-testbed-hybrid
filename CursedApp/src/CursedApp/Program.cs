using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CursedApp;

// NOTE: This is the "main" entry point, but CursedApp.Api and CursedApp.Workers
// also have their own Program.cs entry points. Nobody knows which one to run.

public class Program
{
    // Microservice registry — because someone read a blog post once
    private static readonly Dictionary<string, Type> _microservices = new()
    {
        ["order-service"] = typeof(GodClass),
        ["email-service"] = typeof(GodClass),
        ["report-service"] = typeof(GodClass),
        ["auth-service"] = typeof(GodClass),
        ["cache-service"] = typeof(GodClass),
    };

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting CursedApp Platform v3.7.1-rc2-hotfix-final-FINAL");
        Console.WriteLine($"Registered {_microservices.Count} microservices (all pointing to GodClass)");

        // Initialize the god class — it does everything
        var god = new GodClass();

        // Someone thought this was dependency injection
        Config.Initialize();
        DataAccess.ConnectionString = Config.DatabaseConnection;

        // Start all "microservices" (they're all GodClass)
        foreach (var (name, type) in _microservices)
        {
            Console.WriteLine($"  [BOOT] Starting microservice: {name} ({type.Name})");
        }

        // Run the main loop — this was supposed to be temporary in 2016
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                god.ProcessOrders();
                god.SendPendingEmails();
                god.RefreshCache();
                god.CleanupExpiredSessions();
                await Task.Delay(5000, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Shutting down gracefully (hopefully)");
        }
    }
}
