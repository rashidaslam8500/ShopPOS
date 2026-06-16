namespace ShopPOS.Domain.Entities;

public class WorkerAdvance
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public bool IsSettled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Worker Worker { get; set; } = null!;
}
