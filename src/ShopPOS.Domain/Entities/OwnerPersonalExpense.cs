using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Entities;

public class OwnerPersonalExpense
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public OwnerExpenseCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
