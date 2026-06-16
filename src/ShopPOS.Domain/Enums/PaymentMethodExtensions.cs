using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Enums;

public static class PaymentMethodExtensions
{
    public static string ToDisplayName(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Cash",
        PaymentMethod.Card => "Card",
        PaymentMethod.MobileWallet => "Mobile Wallet / Bank",
        _ => method.ToString()
    };
}
