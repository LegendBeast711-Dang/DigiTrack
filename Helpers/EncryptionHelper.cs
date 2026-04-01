using System.Security.Cryptography;
using System.Text;

namespace DigiTrack.Helpers;

public static class EncryptionHelper
{
    // Marker prefix stored as first line to detect encrypted files
    private const string EncryptedMarker = "DigiTrack_ENCRYPTED:";

    private static (byte[] key, byte[] iv) DeriveKeyIV()
    {
        var passphrase = Encoding.UTF8.GetBytes("DigiTrack-SecureKey-v1.0");
        var salt = Encoding.UTF8.GetBytes("TT_SALT_2024");

        using var deriveBytes = new Rfc2898DeriveBytes(passphrase, salt, 10000, HashAlgorithmName.SHA256);
        return (deriveBytes.GetBytes(32), deriveBytes.GetBytes(16));
    }

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        var (key, iv) = DeriveKeyIV();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return EncryptedMarker + Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        if (!cipherText.StartsWith(EncryptedMarker))
            throw new InvalidOperationException("Content is not encrypted by DigiTrack.");

        var base64 = cipherText[EncryptedMarker.Length..];
        var (key, iv) = DeriveKeyIV();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(base64);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public static bool IsEncrypted(string content)
        => !string.IsNullOrEmpty(content) && content.StartsWith(EncryptedMarker);
}
