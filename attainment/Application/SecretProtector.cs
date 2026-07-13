using System.Security.Cryptography;
using System.Text;

namespace attainment.Application;

public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
    bool IsProtected(string value);
}

public sealed class DpapiSecretProtector : ISecretProtector
{
    private const string Prefix = "dpapi:v1:";

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext) || IsProtected(plaintext))
        {
            return plaintext;
        }

        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue) || !IsProtected(protectedValue))
        {
            return protectedValue;
        }

        var bytes = Convert.FromBase64String(protectedValue[Prefix.Length..]);
        var plaintextBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public bool IsProtected(string value) => value.StartsWith(Prefix, StringComparison.Ordinal);
}
