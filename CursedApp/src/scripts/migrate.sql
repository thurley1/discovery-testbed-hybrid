-- CursedApp Database Migration
-- Run this before deployment. Or after. Or both. It's idempotent (mostly).
-- Last updated: 2023-11-08

-- Orders
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
CREATE TABLE Orders (
    Id NVARCHAR(50) PRIMARY KEY,
    CustomerId NVARCHAR(50) NOT NULL,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    ProcessedAt DATETIME2 NULL,
    ShippedAt DATETIME2 NULL
    -- TODO: Add ShippingAddress (JIRA-4521, opened 2019)
);

-- OrderItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
CREATE TABLE OrderItems (
    Id NVARCHAR(50) PRIMARY KEY,
    OrderId NVARCHAR(50) NOT NULL,
    ProductId NVARCHAR(50) NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0
    -- No foreign key because "it slows down inserts"
);

-- Customers
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
CREATE TABLE Customers (
    Id NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NULL,
    Tier NVARCHAR(20) NOT NULL DEFAULT 'Standard',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    LastOrderAt DATETIME2 NULL,
    TotalOrders INT NOT NULL DEFAULT 0,
    LifetimeValue DECIMAL(18,2) NOT NULL DEFAULT 0
);

-- Products
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
CREATE TABLE Products (
    Id NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Sku NVARCHAR(50) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    Category NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Weight FLOAT NULL -- grams, unless Category = 'Furniture', then kg
);

-- Inventory (yes, separate from Products.StockQuantity — they get out of sync regularly)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Inventory')
CREATE TABLE Inventory (
    ProductId NVARCHAR(50) PRIMARY KEY,
    Quantity INT NOT NULL DEFAULT 0,
    Name NVARCHAR(200) NULL, -- Duplicated from Products because of "performance"
    LastUpdated DATETIME2 NULL
);

-- Invoices
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
CREATE TABLE Invoices (
    Id NVARCHAR(50) PRIMARY KEY,
    OrderId NVARCHAR(50) NOT NULL,
    CustomerId NVARCHAR(50) NOT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    Tax DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Draft',
    IssuedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    PaidAt DATETIME2 NULL,
    DueAt DATETIME2 NOT NULL,
    PdfBase64 NVARCHAR(MAX) NULL -- Yes, PDFs as base64 in the database
);

-- Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
CREATE TABLE Users (
    Id NVARCHAR(50) PRIMARY KEY,
    Email NVARCHAR(200) NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL, -- MD5, not bcrypt
    Role NVARCHAR(20) NOT NULL DEFAULT 'User',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- AuditEntries
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditEntries')
CREATE TABLE AuditEntries (
    Id NVARCHAR(50) PRIMARY KEY DEFAULT NEWID(),
    Action NVARCHAR(100) NOT NULL,
    UserId NVARCHAR(50) NULL,
    EntityType NVARCHAR(100) NULL,
    EntityId NVARCHAR(50) NULL,
    Details NVARCHAR(MAX) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
    IpAddress NVARCHAR(100) NULL
);

-- FileUploads
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FileUploads')
CREATE TABLE FileUploads (
    Id NVARCHAR(50) PRIMARY KEY,
    FileName NVARCHAR(500) NOT NULL,
    OriginalName NVARCHAR(500) NOT NULL,
    UploadedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    SizeBytes BIGINT NOT NULL DEFAULT 0
);

-- Webhooks
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Webhooks')
CREATE TABLE Webhooks (
    Id INT IDENTITY(1,1) PRIMARY KEY, -- Different ID type than everything else, naturally
    EventType NVARCHAR(100) NOT NULL,
    Url NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- ScheduledEmails
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScheduledEmails')
CREATE TABLE ScheduledEmails (
    Id NVARCHAR(50) PRIMARY KEY DEFAULT NEWID(),
    Recipient NVARCHAR(200) NOT NULL,
    Subject NVARCHAR(500) NOT NULL,
    Body NVARCHAR(MAX) NOT NULL,
    SendAt DATETIME2 NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    SentAt DATETIME2 NULL,
    Error NVARCHAR(MAX) NULL
);

PRINT 'Migration complete. Probably.'
