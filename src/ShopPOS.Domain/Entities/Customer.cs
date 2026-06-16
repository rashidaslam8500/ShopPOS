namespace ShopPOS.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime FirstVisit { get; set; } = DateTime.Now;
    public DateTime LastVisit { get; set; } = DateTime.Now;
    public int VisitCount { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
