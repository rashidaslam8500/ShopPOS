using System.Security.Cryptography;
using System.Text;

namespace ShopPOS.Data.Security;

public static class SettingsSecretProtector
{
    private const string Prefix = "DPAPI:";

    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
            return string.Empty;

        if (!storedValue.StartsWith(Prefix, StringComparison.Ordinal))
            return storedValue;

        var cipher = Convert.FromBase64String(storedValue[Prefix.Length..]);
        var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }

    public static bool IsProtected(string value) =>
        value.StartsWith(Prefix, StringComparison.Ordinal);
}
