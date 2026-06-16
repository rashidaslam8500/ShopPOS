using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

/// <summary>
/// Holds the authenticated user for the current application session.
/// </summary>
public class CurrentSession
{
    public UserSession? User { get; private set; }
    public bool IsAuthenticated => User is not null;
    public bool IsOwner => User?.IsOwner == true;

    public void SetUser(UserSession session) => User = session;
    public void Clear() => User = null;
}
