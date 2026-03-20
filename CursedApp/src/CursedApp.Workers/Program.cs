using System;
using System.Threading;
using System.Threading.Tasks;
using CursedApp;
using CursedApp.Workers;

// THIRD entry point — the "background worker service"
// Runs as a Windows Service (sometimes), a console app (usually),
// or a scheduled task (on Jim's laptop for some reason)

Console.WriteLine("CursedApp Worker Service starting...");
Console.WriteLine($"Version: {Config.AppVersion}");

Config.Initialize();
DataAccess.ConnectionString = Config.DatabaseConnection;

var god = new GodClass();
var jobRunner = new JobRunner(god);
var emailSender = new EmailSender(god);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine("Workers running. Press Ctrl+C to stop.");

// Run all jobs in parallel — they all share the same GodClass instance
// and the same static state. Thread safety? That's a problem for Future Us.
try
{
    await Task.WhenAll(
        jobRunner.RunAsync(cts.Token),
        emailSender.RunAsync(cts.Token),
        // The "health check" loop — pings the API to make sure it's alive
        Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var response = await Helpers.SendHttp("http://localhost:5000/health");
                    GodClass.LogAudit($"Health check: API is alive ({response.Length} bytes)");
                }
                catch (Exception ex)
                {
                    GodClass.LogAudit($"Health check: API is DOWN — {ex.Message}");
                    // Send alert email — using the system that might also be down
                    god.QueueEmail(Config.AdminEmail, "ALERT: API is down", ex.Message);
                }
                await Task.Delay(60000, cts.Token); // Every minute
            }
        }, cts.Token)
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Workers shutting down...");
}
