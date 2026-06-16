-- Bhai Gee Crockery Store POS — Microsoft SQL Server Schema
-- Run in SSMS to create the database manually, or let EF Core EnsureCreated on first app launch.

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'BhaiGeePOS')
    CREATE DATABASE BhaiGeePOS;
GO

USE BhaiGeePOS;
GO

CREATE TABLE Users (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Username      NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(256) NOT NULL,
    PasswordSalt  NVARCHAR(128) NOT NULL,
    Role          INT NOT NULL,              -- 0=Salesman, 1=Owner
    DisplayName   NVARCHAR(100) NOT NULL,
    IsActive      BIT NOT NULL DEFAULT 1,
    CreatedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Products (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(200) NOT NULL,
    Category  NVARCHAR(100) NOT NULL DEFAULT 'Crockery',
    Price     DECIMAL(18,2) NOT NULL,
    Stock     INT NOT NULL DEFAULT 0,
    Barcode   NVARCHAR(50) NULL,
    Sku       NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive  BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Products_Barcode UNIQUE (Barcode)
);

CREATE TABLE Sales (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptNo       NVARCHAR(20) NOT NULL UNIQUE,
    Subtotal        DECIMAL(18,2) NOT NULL,
    DiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total           DECIMAL(18,2) NOT NULL,
    ReturnedAmount  DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaymentMethod   INT NOT NULL,
    AmountReceived  DECIMAL(18,2) NOT NULL,
    ChangeAmount    DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status          INT NOT NULL DEFAULT 0,
    SoldByUserId    INT NOT NULL,
    SoldByUsername  NVARCHAR(50) NOT NULL,
    SaleDate        DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- UnitPriceAtSale: frozen at invoice time for date-wise rate recovery on returns
CREATE TABLE SaleItems (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    SaleId            INT NOT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
    ProductId         INT NOT NULL,
    ProductName       NVARCHAR(200) NOT NULL,
    UnitPriceAtSale   DECIMAL(18,2) NOT NULL,
    Quantity          INT NOT NULL,
    ReturnedQuantity  INT NOT NULL DEFAULT 0,
    LineTotal         DECIMAL(18,2) NOT NULL
);

CREATE TABLE SaleReturns (
    Id                      INT IDENTITY(1,1) PRIMARY KEY,
    SaleId                  INT NOT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
    SaleItemId              INT NOT NULL FOREIGN KEY REFERENCES SaleItems(Id),
    ProductId               INT NOT NULL,
    ProductName             NVARCHAR(200) NOT NULL,
    Quantity                INT NOT NULL,
    UnitPriceAtOriginalSale DECIMAL(18,2) NOT NULL,
    RefundAmount            DECIMAL(18,2) NOT NULL,
    Reason                  NVARCHAR(500) NULL,
    ProcessedByUserId       INT NOT NULL,
    ProcessedByUsername     NVARCHAR(50) NOT NULL,
    ReturnDate              DATETIME2 NOT NULL DEFAULT GETDATE()
);

CREATE TABLE AuditLogs (
    Id         BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UserId     INT NULL,
    Username   NVARCHAR(50) NOT NULL,
    Action     INT NOT NULL,
    EntityType NVARCHAR(50) NOT NULL,
    EntityId   NVARCHAR(50) NULL,
    Details    NVARCHAR(MAX) NOT NULL,
    OldValues  NVARCHAR(MAX) NULL,
    NewValues  NVARCHAR(MAX) NULL
);

CREATE TABLE ShopSettings (
    Id    INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL UNIQUE,
    Value NVARCHAR(500) NOT NULL
);

CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
CREATE INDEX IX_Sales_SaleDate ON Sales(SaleDate);
CREATE INDEX IX_Products_Category ON Products(Category);
GO
