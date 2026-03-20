using System;
using System.Threading;
using System.Threading.Tasks;
using CursedApp;

namespace CursedApp.Workers;

/// <summary>
/// Dedicated email sending loop. Also exists because the GodClass email
/// sending was "too slow" when called from the main loop.
/// This is literally just a wrapper around GodClass.SendPendingEmails()
/// running on a different schedule.
/// </summary>
public class EmailSender
{
    private readonly GodClass _god;
    private int _cycleCount = 0;

    public EmailSender(GodClass god)
    {
        _god = god;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _cycleCount++;

            try
            {
                _god.SendPendingEmails();

                // Also check for scheduled emails — stored in the DB with a SendAt timestamp
                var scheduled = DataAccess.ExecuteQuery(
                    "SELECT * FROM ScheduledEmails WHERE SendAt <= GETDATE() AND Status = 'Pending'");

                foreach (var email in scheduled)
                {
                    var to = email["Recipient"]?.ToString() ?? "";
                    var subject = email["Subject"]?.ToString() ?? "";
                    var body = email["Body"]?.ToString() ?? "";
                    var emailId = email["Id"]?.ToString() ?? "";

                    try
                    {
                        _god.SendEmailDirect(to, subject, body);
                        DataAccess.ExecuteNonQuery($"UPDATE ScheduledEmails SET Status = 'Sent', SentAt = GETDATE() WHERE Id = '{emailId}'");
                    }
                    catch (Exception ex)
                    {
                        DataAccess.ExecuteNonQuery($"UPDATE ScheduledEmails SET Status = 'Failed', Error = '{ex.Message.Replace("'", "''")}' WHERE Id = '{emailId}'");
                    }
                }

                // Digest email — send a summary to admins every 100 cycles (~16 minutes)
                if (_cycleCount % 100 == 0)
                {
                    var log = _god.GetAuditLog(50);
                    var digest = string.Join("\n", log);
                    _god.QueueEmail(Config.AdminEmail, $"System Digest ({DateTime.UtcNow:HH:mm})", digest);
                }
            }
            catch (Exception ex)
            {
                GodClass.LogAudit($"[EmailSender] Error in cycle {_cycleCount}: {ex.Message}");
            }

            await Task.Delay(10000, ct); // Every 10 seconds
        }
    }
}
