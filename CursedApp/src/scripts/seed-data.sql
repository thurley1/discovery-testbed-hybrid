-- Seed data for CursedApp
-- Run after migrate.sql. Or don't. The app works (sort of) without it.

-- Admin user (password: Admin123! hashed with MD5)
INSERT INTO Users (Id, Email, PasswordHash, Role) VALUES
('admin', 'admin@cursedapp.com', '0192023a7bbd73250516f069df18b500', 'SuperAdmin');

-- Bob from accounting
INSERT INTO Users (Id, Email, PasswordHash, Role) VALUES
('bob', 'bob@cursedapp.com', 'ee10c315eba2c75b403ea99136f5b48d', 'Bob');

-- Test customers
INSERT INTO Customers (Id, Name, Email, Phone, Tier) VALUES
('VIP-001', 'Bob (Accounting)', 'bob@cursedapp.com', '555-BOB-HELP', 'Bob'),
('CUST-001', 'Acme Corp', 'orders@acme.com', '555-0100', 'Premium'),
('CUST-002', 'Globex Inc', 'purchasing@globex.com', '555-0200', 'Standard'),
('CUST-003', 'Initech', 'tps-reports@initech.com', '555-0300', 'Standard');

-- Products
INSERT INTO Products (Id, Name, Sku, Price, StockQuantity, Category, IsActive, Weight) VALUES
('PROD-001', 'Widget A', 'WDG-A', 29.99, 150, 'Widgets', 1, 250),
('PROD-002', 'Widget B', 'WDG-B', 49.99, 75, 'Widgets', 1, 500),
('PROD-003', 'Gadget X', 'GDG-X', 99.99, 30, 'Gadgets', 1, 1200),
('PROD-004', 'Standing Desk', 'DSK-001', 599.99, 12, 'Furniture', 1, 35),
('PROD-005', 'Legacy Part', 'LEG-001', 9.99, 3, 'Legacy', 0, 100);

-- Inventory (intentionally out of sync with Products.StockQuantity)
INSERT INTO Inventory (ProductId, Quantity, Name) VALUES
('PROD-001', 148, 'Widget A'),
('PROD-002', 75, 'Widget B'),
('PROD-003', 28, 'Gadget X'),
('PROD-004', 12, 'Standing Desk'),
('PROD-005', 5, 'Legacy Part');

-- Webhook subscriptions
INSERT INTO Webhooks (EventType, Url) VALUES
('order.created', 'http://localhost:3000/hooks/orders'),
('order.shipped', 'http://warehouse-internal:8080/api/notifications');

PRINT 'Seed data inserted. Bob is ready.'
