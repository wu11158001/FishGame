using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class SpecialFishCatchView : MonoBehaviour
{
    [SerializeField] CanvasGroup MainCanvausGroup;
    [SerializeField] RectTransform UnitRect;
    [SerializeField] Image FishCover;
    [SerializeField] TextMeshProUGUI CoinText;

    Action CloseAction;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// 座位座標
    /// </summary>
    private readonly Vector2[] SeatPositions = new Vector2[]
    {
        new(-600, -200),
        new(-600, 200),
        new(600, -200),
        new(600, 200)
    };

    public void SetData(int seatIndex, Sprite sprite, string rewardStr, Action closeAction)
    {
        CloseAction = closeAction;

        if (seatIndex >= 0 && seatIndex < SeatPositions.Length)
        {
            int index = TempDataManagement.Instance.IsMirror ? 3 - seatIndex : seatIndex;
            UnitRect.anchoredPosition = SeatPositions[index];
        }

        if (sprite != null)
            FishCover.sprite = sprite;

        CoinText.text = rewardStr;

        StartCoroutine(IShow());
    }

    /// <summary>
    /// 顯示效果
    /// </summary>
    private IEnumerator IShow()
    {
        MainCanvausGroup.alpha = 0;

        float currentTime = 0f;
        float duration = 0.5f;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            MainCanvausGroup.alpha = Mathf.Lerp(0f, 1f, currentTime / duration);
            yield return null;
        }
        MainCanvausGroup.alpha = 1f;


        yield return new WaitForSeconds(3);

        CloseAction?.Invoke();
    }
}
