using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;

namespace ShopPOS.Data;

/// <summary>
/// Applies incremental schema updates for databases created before amendment tracking.
/// </summary>
public static class DatabaseSchemaUpdater
{
    public static async Task ApplyAsync(PosDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('Sales', 'AddedAmount') IS NULL
                ALTER TABLE Sales ADD AddedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_AddedAmount DEFAULT 0;
            IF COL_LENGTH('Sales', 'NetTotal') IS NULL
                ALTER TABLE Sales ADD NetTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_NetTotal DEFAULT 0;
            IF COL_LENGTH('SaleItems', 'IsAmendmentLine') IS NULL
                ALTER TABLE SaleItems ADD IsAmendmentLine BIT NOT NULL CONSTRAINT DF_SaleItems_IsAmendmentLine DEFAULT 0;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID('SaleAmendments', 'U') IS NULL
            BEGIN
                CREATE TABLE SaleAmendments (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    SaleId INT NOT NULL,
                    Action INT NOT NULL,
                    SaleItemId INT NULL,
                    ProductId INT NOT NULL,
                    ProductName NVARCHAR(200) NOT NULL,
                    Quantity INT NOT NULL,
                    UnitPrice DECIMAL(18,2) NOT NULL,
                    AmountDelta DECIMAL(18,2) NOT NULL,
                    Reason NVARCHAR(500) NULL,
                    ProcessedByUserId INT NOT NULL,
                    ProcessedByUsername NVARCHAR(50) NOT NULL,
                    AmendedAt DATETIME2 NOT NULL,
                    CONSTRAINT FK_SaleAmendments_Sales FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_SaleAmendments_SaleItems FOREIGN KEY (SaleItemId) REFERENCES SaleItems(Id)
                );
                CREATE INDEX IX_SaleAmendments_SaleId ON SaleAmendments(SaleId);
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            UPDATE Sales
            SET NetTotal = Total - ReturnedAmount + AddedAmount
            WHERE NetTotal = 0 AND (ReturnedAmount <> 0 OR AddedAmount <> 0);
            UPDATE Sales SET NetTotal = Total WHERE NetTotal = 0;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('Sales', 'CustomerPhone') IS NULL
                ALTER TABLE Sales ADD CustomerPhone NVARCHAR(20) NULL;
            IF COL_LENGTH('Sales', 'CustomerEmail') IS NULL
                ALTER TABLE Sales ADD CustomerEmail NVARCHAR(200) NULL;
            IF COL_LENGTH('Sales', 'IsDeleted') IS NULL
                ALTER TABLE Sales ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Sales_IsDeleted DEFAULT 0;
            IF COL_LENGTH('Sales', 'DeletedAt') IS NULL
                ALTER TABLE Sales ADD DeletedAt DATETIME2 NULL;
            IF COL_LENGTH('Sales', 'DeletedByUserId') IS NULL
                ALTER TABLE Sales ADD DeletedByUserId INT NULL;
            IF COL_LENGTH('Products', 'Description') IS NULL
                ALTER TABLE Products ADD Description NVARCHAR(1000) NULL;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID('Customers', 'U') IS NULL
            BEGIN
                CREATE TABLE Customers (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Phone NVARCHAR(20) NOT NULL,
                    Name NVARCHAR(100) NULL,
                    FirstVisit DATETIME2 NOT NULL,
                    LastVisit DATETIME2 NOT NULL,
                    VisitCount INT NOT NULL DEFAULT 1,
                    UpdatedAt DATETIME2 NOT NULL
                );
                CREATE UNIQUE INDEX IX_Customers_Phone ON Customers(Phone);
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @fkName NVARCHAR(200);
            SELECT @fkName = fk.name
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.columns c ON fkc.parent_column_id = c.column_id AND fkc.parent_object_id = c.object_id
            WHERE fk.parent_object_id = OBJECT_ID('SaleReturns')
              AND c.name = 'SaleItemId';

            IF @fkName IS NOT NULL
            BEGIN
                EXEC('ALTER TABLE SaleReturns DROP CONSTRAINT ' + @fkName);
                ALTER TABLE SaleReturns
                    ADD CONSTRAINT FK_SaleReturns_SaleItems_SaleItemId
                    FOREIGN KEY (SaleItemId) REFERENCES SaleItems(Id) ON DELETE CASCADE;
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID('Workers', 'U') IS NULL
            BEGIN
                CREATE TABLE Workers (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Phone NVARCHAR(20) NOT NULL,
                    Role NVARCHAR(50) NOT NULL,
                    MonthlySalary DECIMAL(18,2) NOT NULL,
                    FingerprintTemplate NVARCHAR(4000) NULL,
                    IsActive BIT NOT NULL CONSTRAINT DF_Workers_IsActive DEFAULT 1,
                    CreatedAt DATETIME2 NOT NULL
                );
            END

            IF OBJECT_ID('Attendances', 'U') IS NULL
            BEGIN
                CREATE TABLE Attendances (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    WorkerId INT NOT NULL,
                    Date DATE NOT NULL,
                    TimeIn TIME NOT NULL,
                    TimeToLeave TIME NULL,
                    Status INT NOT NULL,
                    CONSTRAINT FK_Attendances_Workers FOREIGN KEY (WorkerId) REFERENCES Workers(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_Attendances_WorkerId_Date ON Attendances(WorkerId, Date);
            END

            IF OBJECT_ID('ExpenseAndCashEntries', 'U') IS NULL
            BEGIN
                CREATE TABLE ExpenseAndCashEntries (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Date DATE NOT NULL,
                    DailyCashIntake DECIMAL(18,2) NOT NULL,
                    TotalExpense DECIMAL(18,2) NOT NULL,
                    ExpenseDescription NVARCHAR(500) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL
                );
                CREATE INDEX IX_ExpenseAndCashEntries_Date ON ExpenseAndCashEntries(Date);
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('Workers', 'StandardShiftHours') IS NULL
                ALTER TABLE Workers ADD StandardShiftHours DECIMAL(5,2) NOT NULL CONSTRAINT DF_Workers_ShiftHours DEFAULT 8;
            IF COL_LENGTH('Workers', 'HourlyOvertimeRate') IS NULL
                ALTER TABLE Workers ADD HourlyOvertimeRate DECIMAL(18,2) NOT NULL CONSTRAINT DF_Workers_OTRate DEFAULT 0;
            IF COL_LENGTH('Attendances', 'OvertimeHours') IS NULL
                ALTER TABLE Attendances ADD OvertimeHours DECIMAL(8,2) NOT NULL CONSTRAINT DF_Attendances_OT DEFAULT 0;

            IF OBJECT_ID('WorkerDailyCash', 'U') IS NULL
            BEGIN
                CREATE TABLE WorkerDailyCash (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    WorkerId INT NOT NULL,
                    Date DATE NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    Notes NVARCHAR(300) NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    CONSTRAINT FK_WorkerDailyCash_Workers FOREIGN KEY (WorkerId) REFERENCES Workers(Id) ON DELETE CASCADE
                );
            END

            IF OBJECT_ID('WorkerAdvances', 'U') IS NULL
            BEGIN
                CREATE TABLE WorkerAdvances (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    WorkerId INT NOT NULL,
                    Date DATE NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    Notes NVARCHAR(300) NULL,
                    IsSettled BIT NOT NULL CONSTRAINT DF_WorkerAdvances_Settled DEFAULT 0,
                    CreatedAt DATETIME2 NOT NULL,
                    CONSTRAINT FK_WorkerAdvances_Workers FOREIGN KEY (WorkerId) REFERENCES Workers(Id) ON DELETE CASCADE
                );
            END

            IF OBJECT_ID('WorkerLeaves', 'U') IS NULL
            BEGIN
                CREATE TABLE WorkerLeaves (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    WorkerId INT NOT NULL,
                    Date DATE NOT NULL,
                    LeaveType INT NOT NULL,
                    IsPaid BIT NOT NULL,
                    Reason NVARCHAR(300) NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    CONSTRAINT FK_WorkerLeaves_Workers FOREIGN KEY (WorkerId) REFERENCES Workers(Id) ON DELETE CASCADE
                );
            END

            IF OBJECT_ID('OwnerPersonalExpenses', 'U') IS NULL
            BEGIN
                CREATE TABLE OwnerPersonalExpenses (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Date DATE NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    Category INT NOT NULL,
                    Description NVARCHAR(2000) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL
                );
                CREATE INDEX IX_OwnerPersonalExpenses_Date ON OwnerPersonalExpenses(Date);
            END

            IF OBJECT_ID('Vendors', 'U') IS NULL
            BEGIN
                CREATE TABLE Vendors (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name NVARCHAR(150) NOT NULL,
                    Phone NVARCHAR(20) NOT NULL,
                    Address NVARCHAR(300) NULL,
                    IsActive BIT NOT NULL CONSTRAINT DF_Vendors_IsActive DEFAULT 1,
                    CreatedAt DATETIME2 NOT NULL
                );
            END

            IF OBJECT_ID('VendorKhataEntries', 'U') IS NULL
            BEGIN
                CREATE TABLE VendorKhataEntries (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    VendorId INT NOT NULL,
                    Date DATE NOT NULL,
                    InvoiceNumber NVARCHAR(50) NOT NULL,
                    TotalBill DECIMAL(18,2) NOT NULL,
                    CashPaid DECIMAL(18,2) NOT NULL,
                    PaymentMode INT NOT NULL CONSTRAINT DF_VendorKhataEntries_PaymentMode DEFAULT 0,
                    Notes NVARCHAR(500) NULL,
                    AttachmentPath NVARCHAR(500) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_VendorKhataEntries_IsDeleted DEFAULT 0,
                    DeletedAt DATETIME2 NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    CONSTRAINT FK_VendorKhataEntries_Vendors FOREIGN KEY (VendorId) REFERENCES Vendors(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_VendorKhataEntries_VendorId_IsDeleted ON VendorKhataEntries(VendorId, IsDeleted);
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('VendorKhataEntries', 'IsDeleted') IS NULL
                ALTER TABLE VendorKhataEntries ADD IsDeleted BIT NOT NULL CONSTRAINT DF_VendorKhataEntries_IsDeleted2 DEFAULT 0;
            IF COL_LENGTH('VendorKhataEntries', 'DeletedAt') IS NULL
                ALTER TABLE VendorKhataEntries ADD DeletedAt DATETIME2 NULL;
            IF COL_LENGTH('VendorKhataEntries', 'PaymentMode') IS NULL
                ALTER TABLE VendorKhataEntries ADD PaymentMode INT NOT NULL CONSTRAINT DF_VendorKhataEntries_PaymentMode2 DEFAULT 0;
            IF COL_LENGTH('VendorKhataEntries', 'AttachmentPath') IS NULL
                ALTER TABLE VendorKhataEntries ADD AttachmentPath NVARCHAR(500) NULL;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('Users', 'SecurityQuestion1') IS NULL
                ALTER TABLE Users ADD SecurityQuestion1 NVARCHAR(200) NULL;
            IF COL_LENGTH('Users', 'SecurityAnswer1Hash') IS NULL
                ALTER TABLE Users ADD SecurityAnswer1Hash NVARCHAR(200) NULL;
            IF COL_LENGTH('Users', 'SecurityAnswer1Salt') IS NULL
                ALTER TABLE Users ADD SecurityAnswer1Salt NVARCHAR(100) NULL;
            IF COL_LENGTH('Users', 'SecurityQuestion2') IS NULL
                ALTER TABLE Users ADD SecurityQuestion2 NVARCHAR(200) NULL;
            IF COL_LENGTH('Users', 'SecurityAnswer2Hash') IS NULL
                ALTER TABLE Users ADD SecurityAnswer2Hash NVARCHAR(200) NULL;
            IF COL_LENGTH('Users', 'SecurityAnswer2Salt') IS NULL
                ALTER TABLE Users ADD SecurityAnswer2Salt NVARCHAR(100) NULL;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('ShopSettings', 'Value') IS NOT NULL
            BEGIN
                DECLARE @valueLen INT = COL_LENGTH('ShopSettings', 'Value');
                IF @valueLen < 4000
                    ALTER TABLE ShopSettings ALTER COLUMN Value NVARCHAR(4000) NOT NULL;
            END
            """);
    }
}
