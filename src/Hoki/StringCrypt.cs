using System.Text;

namespace Hoki;

/// <summary>
/// Provides static methods for string cryptography
/// </summary>
public class StringCrypt
{
    /// <summary>
    /// Returns a string's MD5 hash
    /// </summary>
    /// <remarks>Taken from http://www.spiration.co.uk/post/1203</remarks>
    public static string MD5(string source)
    {
        byte[] textBytes = Encoding.Default.GetBytes(source);
        try
        {
            System.Security.Cryptography.MD5 cryptHandler = System.Security.Cryptography.MD5.Create();
            byte[] hash = cryptHandler.ComputeHash(textBytes);
            string ret = "";
            foreach (byte a in hash)
            {
                if (a < 16) ret += "0" + a.ToString("x");
                else ret += a.ToString("x");
            }
            return ret;
        }
        catch
        {
            throw;
        }
    }
}
