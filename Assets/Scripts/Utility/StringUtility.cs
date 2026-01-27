using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using TMPro;

public static class StringUtility
{
    /// <summary>
    /// UI元件跟隨在文字後方
    /// </summary>
    public static void RectFollowTextBehind(RectTransform rt, TextMeshProUGUI tmpText, Vector2 offset)
    {
        tmpText.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmpText.textInfo;
        int lastCharIndex = textInfo.characterCount - 1;

        Vector3 lastCharPos = textInfo.characterInfo[lastCharIndex].bottomLeft;
        rt.localPosition = new Vector3(lastCharPos.x + offset.x, lastCharPos.y + offset.y, 0);
    }

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
    public static string CurrencyFormat(double value)
    {
        return value.ToString("#,##0.##");
    }
}
