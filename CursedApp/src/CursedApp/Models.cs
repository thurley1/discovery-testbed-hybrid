using System;
using System.Collections.Generic;

namespace CursedApp;

// ALL models in one file — "we'll separate them later" (2017)

public class Order
{
    public string Id { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    // TODO: Add shipping address (see JIRA-4521, opened 2019)
}

public class OrderItem
{
    public string Id { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class Customer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Tier { get; set; } = "Standard"; // Standard, Premium, VIP, Bob
    public DateTime CreatedAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public int TotalOrders { get; set; }
    public decimal LifetimeValue { get; set; }
}

public class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; } = "";
    public bool IsActive { get; set; } = true;
    // Weight is in grams unless Category is "Furniture" then it's kg
    // Nobody remembers why
    public double Weight { get; set; }
}

public class Invoice
{
    public string Id { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Sent, Paid, Overdue, Written Off, "Its Complicated"
    public DateTime IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime DueAt { get; set; }
    // PDF is stored as base64 in the database — migration to blob storage is "in progress" since 2020
    public string? PdfBase64 { get; set; }
}

public class Report
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string GeneratedBy { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public string Content { get; set; } = "";
    public string Format { get; set; } = "text"; // text, html, csv, "excel" (it's actually csv with .xlsx extension)
}

public class AuditEntry
{
    public string Id { get; set; } = "";
    public string Action { get; set; } = "";
    public string UserId { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime Timestamp { get; set; }
    // IP address stored as string — sometimes IPv4, sometimes IPv6, sometimes "localhost",
    // once it was "Bobs laptop"
    public string IpAddress { get; set; } = "";
}
