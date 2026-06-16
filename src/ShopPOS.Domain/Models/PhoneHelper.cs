namespace ShopPOS.Domain.Models;

public static class PhoneHelper
{
    public static string Normalize(string phone) =>
        new string(phone.Where(char.IsDigit).ToArray());
}
