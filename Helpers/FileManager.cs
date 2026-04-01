namespace DigiTrack.Helpers;

public static class FileManager
{
    public static void ExportToFile(string text, string filePath, bool encrypt)
    {
        var content = encrypt ? EncryptionHelper.Encrypt(text) : text;
        File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
    }

    public static (string text, bool wasEncrypted) LoadFromFile(string filePath)
    {
        var raw = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        if (EncryptionHelper.IsEncrypted(raw))
        {
            try
            {
                return (EncryptionHelper.Decrypt(raw), true);
            }
            catch
            {
                throw new InvalidDataException(
                    "File appears to be encrypted but could not be decrypted. " +
                    "It may have been encrypted by a different application.");
            }
        }

        return (raw, false);
    }
}
