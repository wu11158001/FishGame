using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollViewTool : MonoBehaviour
{
    [SerializeField] ScrollRect MainScrollRect;
    [SerializeField] RectTransform ContentRect;
    // 滾動花費時間
    [SerializeField] float Duration = 0.2f;

    Coroutine SnapToCoroutine;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// 卷軸跳至選物件位置
    /// </summary>
    public void SnapTo(RectTransform target)
    {
        if (SnapToCoroutine != null)
            StopCoroutine(SnapToCoroutine);

        SnapToCoroutine = StartCoroutine(ISnapTo(target));
    }

    private IEnumerator ISnapTo(RectTransform target)
    {
        // 1. 強制更新佈局，確保 sizeDelta 和位置是正確的
        Canvas.ForceUpdateCanvases();
        // 如果你有使用 Layout Group，有時需要這行來強制計算
        LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRect);

        // 2. 計算目標在 Viewport 空間中的位置
        Vector3 targetLocalPos = MainScrollRect.viewport.InverseTransformPoint(target.position);

        // 3. 修正：對齊左邊緣
        // 因為 targetLocalPos 是中心點，我們要向右偏移「半個物件寬度」
        // 這樣物件的左邊才會剛好對齊 Viewport 的左邊
        float halfItemWidth = target.rect.width * 0.5f;
        float targetX = ContentRect.anchoredPosition.x - (targetLocalPos.x - halfItemWidth);

        // 4. 限制範圍
        // 計算最大可滾動距離（通常是負值）
        float maxScroll = ContentRect.sizeDelta.x - MainScrollRect.viewport.rect.width;

        // 如果 Content 比 Viewport 還窄，就不需要滾動
        if (maxScroll < 0) maxScroll = 0;

        // 確保 targetX 不會超過 0 (最左) 也不會小於 -maxScroll (最右)
        targetX = Mathf.Clamp(targetX, -maxScroll, 0);

        // 5. 開始平滑移動
        Vector2 startPos = ContentRect.anchoredPosition;
        Vector2 endPos = new Vector2(targetX, startPos.y);

        float elapsed = 0f;
        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;
            t = t * t * (3f - 2f * t);

            ContentRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        ContentRect.anchoredPosition = endPos;
    }
}
