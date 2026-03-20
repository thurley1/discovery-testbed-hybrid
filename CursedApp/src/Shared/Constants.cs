namespace CursedApp.Shared;

/// <summary>
/// All the magic strings and numbers in one place.
/// "At least they're not scattered across the codebase" — true, but they're still magic.
/// </summary>
public static class Constants
{
    // Order statuses — also defined as strings in Models.cs, DataAccess queries, and 3 stored procs
    public const string OrderPending = "Pending";
    public const string OrderProcessing = "Processing";
    public const string OrderShipped = "Shipped";
    public const string OrderDelivered = "Delivered";
    public const string OrderCancelled = "Cancelled";
    public const string OrderRefunded = "Refunded";
    public const string OrderOnHold = "On Hold"; // Added during The Incident of 2022

    // Invoice statuses
    public const string InvoiceDraft = "Draft";
    public const string InvoiceSent = "Sent";
    public const string InvoicePaid = "Paid";
    public const string InvoiceOverdue = "Overdue";
    public const string InvoiceWrittenOff = "Written Off";

    // Customer tiers
    public const string TierStandard = "Standard";
    public const string TierPremium = "Premium";
    public const string TierVip = "VIP";
    public const string TierBob = "Bob"; // Bob from accounting has his own tier

    // Roles
    public const string RoleUser = "User";
    public const string RoleAdmin = "Admin";
    public const string RoleSuperAdmin = "SuperAdmin";
    public const string RoleReadOnly = "ReadOnly";
    public const string RoleBob = "Bob"; // Again with Bob

    // Magic numbers
    public const int MaxRetries = 3;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 1000;
    public const int SessionTimeoutMinutes = 1440; // 24 hours — same as Config.SessionTimeoutHours * 60
    public const decimal FreeShippingThreshold = 75.00m;
    public const decimal VipDiscountPercent = 20.0m;
    public const decimal PremiumDiscountPercent = 10.0m;
    public const int LowStockThreshold = 10;
    public const int CriticalStockThreshold = 3;
    public const int InvoiceDueDays = 30;
    public const int OverdueGracePeriodDays = 7;

    // Date formats — used inconsistently across the codebase
    public const string DateFormatShort = "MM/dd/yy";
    public const string DateFormatLong = "MMMM dd, yyyy";
    public const string DateFormatIso = "yyyy-MM-dd";
    public const string DateFormatDatabase = "yyyy-MM-dd HH:mm:ss";
    public const string DateFormatBob = "dd-MMM-yyyy"; // Bob's preferred format

    // Error messages
    public const string ErrorNotFound = "Resource not found";
    public const string ErrorUnauthorized = "You are not authorized to perform this action";
    public const string ErrorServerError = "An unexpected error occurred. Please try again later.";
    public const string ErrorBob = "Ask Bob"; // When no one knows what the error means

    // Table names — because someone might want to rename them (they won't)
    public const string TableOrders = "Orders";
    public const string TableOrderItems = "OrderItems";
    public const string TableCustomers = "Customers";
    public const string TableProducts = "Products";
    public const string TableInvoices = "Invoices";
    public const string TableInventory = "Inventory";
    public const string TableUsers = "Users";
    public const string TableAuditEntries = "AuditEntries";
    public const string TableFileUploads = "FileUploads";
    public const string TableWebhooks = "Webhooks";
    public const string TableScheduledEmails = "ScheduledEmails";
}
