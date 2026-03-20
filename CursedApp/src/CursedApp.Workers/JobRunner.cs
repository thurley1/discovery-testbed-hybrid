using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CursedApp;

namespace CursedApp.Workers;

/// <summary>
/// Processes all background jobs in one infinite loop.
/// Orders, reports, cleanup, invoicing, notifications — all sequential,
/// all in the same thread. If one job hangs, everything stops.
/// "We should use Hangfire" — every retrospective since 2019.
/// </summary>
public class JobRunner
{
    private readonly GodClass _god;
    private int _iterationCount = 0;

    public JobRunner(GodClass god)
    {
        _god = god;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _iterationCount++;
            GodClass.LogAudit($"[JobRunner] Iteration {_iterationCount} starting");

            try
            {
                // Process pending orders
                _god.ProcessOrders();

                // Generate daily reports (runs every iteration — 30 seconds — "just in case")
                if (_iterationCount % 120 == 0) // ~every hour
                {
                    var report = _god.GenerateReport("sales", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
                    GodClass.LogAudit($"[JobRunner] Daily sales report: {report.Length} chars");
                }

                // Invoice generation — find orders without invoices
                GenerateInvoices();

                // Cleanup old data
                if (_iterationCount % 360 == 0) // ~every 3 hours
                {
                    CleanupOldData();
                }

                // Customer tier recalculation
                if (_iterationCount % 720 == 0) // ~every 6 hours
                {
                    RecalculateCustomerTiers();
                }

                // Low stock alerts
                CheckLowStock();

                // Overdue invoice reminders
                SendOverdueReminders();
            }
            catch (Exception ex)
            {
                GodClass.LogAudit($"[JobRunner] ERROR in iteration {_iterationCount}: {ex.Message}");
                // Continue — partial work is better than no work
            }

            await Task.Delay(30000, ct); // 30 seconds between iterations
        }
    }

    private void GenerateInvoices()
    {
        var uninvoicedOrders = DataAccess.ExecuteQuery(
            "SELECT o.* FROM Orders o LEFT JOIN Invoices i ON o.Id = i.OrderId WHERE i.Id IS NULL AND o.Status = 'Processing'");

        foreach (var order in uninvoicedOrders)
        {
            var orderId = order["Id"]?.ToString() ?? "";
            var total = Convert.ToDecimal(order["Total"] ?? 0);
            var customerId = order["CustomerId"]?.ToString() ?? "";

            // Tax calculation — inline, because it doesn't belong anywhere specific
            var customerRows = DataAccess.ExecuteQuery($"SELECT * FROM Customers WHERE Id = '{customerId}'");
            var state = "CA"; // Default to California because most customers are there (citation needed)
            var tax = Helpers.CalculateTax(total, state);

            var invoiceId = Helpers.GenerateId();
            DataAccess.ExecuteNonQuery(
                $"INSERT INTO Invoices (Id, OrderId, CustomerId, Subtotal, Tax, Total, Status, IssuedAt, DueAt) VALUES ('{invoiceId}', '{orderId}', '{customerId}', {total}, {tax}, {total + tax}, 'Sent', GETDATE(), DATEADD(day, 30, GETDATE()))");

            _god.QueueEmail(customerId, $"Invoice {invoiceId}", $"Your invoice for ${total + tax:F2} is due in 30 days.");
        }
    }

    private void CleanupOldData()
    {
        // Delete audit entries older than 90 days
        DataAccess.ExecuteNonQuery("DELETE FROM AuditEntries WHERE Timestamp < DATEADD(day, -90, GETDATE())");

        // Delete expired sessions
        _god.CleanupExpiredSessions();

        // Delete temp files older than 7 days
        try
        {
            if (System.IO.Directory.Exists(Config.TempPath))
            {
                foreach (var file in System.IO.Directory.GetFiles(Config.TempPath))
                {
                    if (System.IO.File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-7))
                        System.IO.File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            GodClass.LogAudit($"[JobRunner] Temp cleanup failed: {ex.Message}");
        }

        GodClass.LogAudit("[JobRunner] Cleanup complete");
    }

    private void RecalculateCustomerTiers()
    {
        // Update customer tiers based on lifetime value
        // Business rules were written on a napkin and photographed — the photo was lost
        DataAccess.ExecuteNonQuery(@"
            UPDATE Customers SET Tier = CASE
                WHEN LifetimeValue > 50000 THEN 'VIP'
                WHEN LifetimeValue > 10000 THEN 'Premium'
                ELSE 'Standard'
            END");

        GodClass.LogAudit("[JobRunner] Customer tiers recalculated");
    }

    private void CheckLowStock()
    {
        var lowStock = DataAccess.ExecuteQuery("SELECT * FROM Products WHERE StockQuantity < 10 AND IsActive = 1");
        foreach (var product in lowStock)
        {
            var name = product["Name"]?.ToString() ?? "Unknown";
            var qty = product["StockQuantity"]?.ToString() ?? "?";
            _god.QueueEmail(Config.AdminEmail, $"Low Stock: {name}", $"{name} has only {qty} units remaining.");
        }
    }

    private void SendOverdueReminders()
    {
        var overdue = DataAccess.ExecuteQuery(
            "SELECT * FROM Invoices WHERE Status = 'Sent' AND DueAt < GETDATE()");

        foreach (var invoice in overdue)
        {
            var customerId = invoice["CustomerId"]?.ToString() ?? "";
            var invoiceId = invoice["Id"]?.ToString() ?? "";
            var total = invoice["Total"]?.ToString() ?? "0";

            _god.QueueEmail(customerId, $"OVERDUE: Invoice {invoiceId}",
                $"Your invoice for ${total} is overdue. Please pay immediately.");

            // Mark as overdue
            DataAccess.ExecuteNonQuery($"UPDATE Invoices SET Status = 'Overdue' WHERE Id = '{invoiceId}'");
        }
    }
}
