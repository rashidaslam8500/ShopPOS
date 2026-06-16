namespace ShopPOS.Domain.Enums;

public enum AuditActionType
{
    Login = 0,
    Logout = 1,
    SaleCreated = 2,
    SaleAmended = 3,
    SaleDeleted = 4,
    ReturnProcessed = 5,
    ProductAdded = 6,
    ProductUpdated = 7,
    ProductDeleted = 8,
    PriceChanged = 9,
    SettingsChanged = 10,
    WorkerAdded = 11,
    WorkerDeleted = 12,
    ExpenseLogged = 13,
    AuditLogDeleted = 14
}
