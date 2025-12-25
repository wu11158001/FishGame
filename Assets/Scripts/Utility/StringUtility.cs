using System.Security.Cryptography;
using System.Text;

public static class StringUtility
{
    /// <summary>
    /// SHA256加密
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToHash256(string str)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(str));

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
