namespace ShopPOS.Domain.Entities;

public class Vendor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<VendorKhataEntry> KhataEntries { get; set; } = new List<VendorKhataEntry>();
}
