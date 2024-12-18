using System.Security.Cryptography;
using System.Text;

namespace Caching.Security;

public class AesEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(byte[] key, byte[] iv)
    {
        _key = key;
        _iv = iv;
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var buffer = Convert.FromBase64String(cipherText);
        var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(decrypted);
    }
}
