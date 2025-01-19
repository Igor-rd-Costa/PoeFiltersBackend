

using System.Security.Cryptography;

public class EncryptionService
{
    private readonly IConfiguration m_Configuration;
    public EncryptionService(IConfiguration configuration)
    {
        m_Configuration = configuration;
    }
    public string Encrypt(string text)
    {
        var key = m_Configuration["Encryption:Key"];
        var iv = m_Configuration["Encryption:IV"];
        if (key == null || iv == null)
        {
            throw new Exception("Failed to retrieve Ecryption Data");
        }
        Aes aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.IV = Convert.FromBase64String(iv);
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var ms = new MemoryStream();
        var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        var sw = new StreamWriter(cs);
        sw.Write(text);
        sw.Close();
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cypher)
    {
        var buffer = Convert.FromBase64String(cypher);
        var key = m_Configuration["Encryption:Key"];
        var iv = m_Configuration["Encryption:IV"];
        if (key == null || iv == null)
        {
            throw new Exception("Failed to retrieve Ecryption Data");
        }
        Aes aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.IV = Convert.FromBase64String(iv);
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        var ms = new MemoryStream(buffer);
        var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}