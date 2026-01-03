using System.Security.Cryptography;
using System.Text;
using UnityEngine;

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

    /// <summary>
    /// 獲取顏色錯誤
    /// </summary>
    /// <param name="htmlString"></param>
    /// <returns></returns>
    public static Color GetColor(string htmlString)
    {
        if (string.IsNullOrEmpty(htmlString)) return Color.white;

        htmlString = htmlString.Replace(" ", "");

        if (!htmlString.StartsWith("#"))
        {
            htmlString = "#" + htmlString;
        }
        if (ColorUtility.TryParseHtmlString(htmlString, out Color color))
        {
            return color;
        }
        else
        {
            Debug.LogError($"獲取顏色錯誤: {htmlString}，預設使用白色");
            return Color.white;
        }
    }

    /// <summary>
    /// 貨幣格式
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string CurrencyFormat(int value)
    {
        return $"{value:N0}";
    }
}
