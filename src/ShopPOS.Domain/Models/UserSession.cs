using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Models;

public class UserSession
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsOwner => Role == UserRole.Owner;
}
