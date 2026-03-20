using System;

namespace CursedApp;

/// <summary>
/// Application configuration. All values hardcoded because "environment variables
/// are a DevOps concern and we don't have DevOps." Credentials rotate annually
/// (they don't).
/// </summary>
public static class Config
{
    // Database — same server for dev, staging, and prod
    // (different databases though... usually)
    public static string DatabaseConnection { get; set; } =
        "Server=prod-db-server.internal;Database=CursedApp;User Id=sa;Password=CursedApp2019!;TrustServerCertificate=true;";

    // SMTP — the intern set this up and left
    public static string SmtpHost => "smtp.company-email.com";
    public static int SmtpPort => 587;
    public static string SmtpUser => "noreply@cursedapp.com";
    public static string SmtpPassword => "EmailP@ss2021!";
    public static string FromEmail => "noreply@cursedapp.com";

    // External APIs
    public static string WarehouseApiUrl => "http://warehouse-internal:8080";
    public static string SmsApiUrl => "https://sms-provider-3.example.com/v2"; // v1 and v2 were different providers
    public static string SmsApiKey => "sk_live_sms_DEADBEEF1234567890";
    public static string PaymentGatewayUrl => "https://payments.example.com";
    public static string PaymentGatewayKey => "pk_live_PAY_CAFEBABE0987654321";

    // File storage
    public static string UploadPath => @"C:\CursedApp\Uploads"; // Yes, an absolute Windows path
    public static string TempPath => @"C:\CursedApp\Temp";
    public static string ReportOutputPath => @"\\fileserver\reports\cursedapp"; // UNC path, because why not

    // Feature flags — boolean fields, no proper feature flag system
    public static bool EnableNewCheckout { get; set; } = false; // "New" since 2020
    public static bool EnableBetaDashboard { get; set; } = true;
    public static bool EnableSmsNotifications { get; set; } = false; // Disabled after the "incident"
    public static bool MaintenanceMode { get; set; } = false;

    // Admin
    public static string AdminEmail => "admin@cursedapp.com";
    public static string SuperAdminPassword => "Admin123!"; // For the admin panel backdoor

    // Misc
    public static int MaxUploadSizeMb => 25;
    public static int SessionTimeoutHours => 24;
    public static int CacheDefaultMinutes => 5;
    public static string AppVersion => "3.7.1-rc2-hotfix-final-FINAL";
    public static string Environment => "Production"; // This is always "Production", even in dev

    // JWT secret — rotated once in 2019 after a security audit
    public static string JwtSecret => "super-secret-jwt-key-do-not-share-with-anyone-especially-not-in-source-control";

    public static void Initialize()
    {
        // "Override from environment" — but nobody sets these env vars
        var connStr = System.Environment.GetEnvironmentVariable("CURSEDAPP_DB");
        if (!string.IsNullOrEmpty(connStr))
            DatabaseConnection = connStr;

        var maintenance = System.Environment.GetEnvironmentVariable("CURSEDAPP_MAINTENANCE");
        if (maintenance == "1" || maintenance == "true" || maintenance == "yes" || maintenance == "on")
            MaintenanceMode = true;
    }
}
