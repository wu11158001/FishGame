using UnityEngine;

public static class UIUtility
{
    /// <summary>
    /// 設置最大圖片大小
    /// </summary>
    public static void SetMaxUISize(RectTransform targetRt, float maxSize = 256f)
    {
        // 取得圖片目前的寬高
        float width = targetRt.sizeDelta.x;
        float height = targetRt.sizeDelta.y;

        // 如果寬或高超過了限制
        if (width > maxSize || height > maxSize)
        {
            // 計算縮放比率，取其最長的一邊作為基準
            float ratio = maxSize / Mathf.Max(width, height);
            targetRt.sizeDelta = new Vector2(width * ratio, height * ratio);
        }
    }
}
