using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? SecurityQuestion1 { get; set; }
    public string? SecurityAnswer1Hash { get; set; }
    public string? SecurityAnswer1Salt { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityAnswer2Hash { get; set; }
    public string? SecurityAnswer2Salt { get; set; }
}
