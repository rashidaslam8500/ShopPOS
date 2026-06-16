namespace ShopPOS.Domain.Entities;

public class ExpenseAndCash
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal DailyCashIntake { get; set; }
    public decimal TotalExpense { get; set; }
    public string ExpenseDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
