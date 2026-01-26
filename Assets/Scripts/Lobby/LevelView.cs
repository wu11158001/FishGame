using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelView : BasicView, IDragHandler, IEndDragHandler
{
    [Header("Level Unit")]
    [SerializeField] RectTransform LevelUnitArea;
    [SerializeField] LevelUnit LevelUnit;
    [SerializeField] List<Sprite> UnitBgs = new();
    [SerializeField] List<Sprite> UnitIcons = new();
    [SerializeField] List<string> LevelNameKeys = new();
    [SerializeField] List<VertexGradient> LevelNameColors = new();

    [Header("Level Unit Rotate Settings")]
    // 關卡單位距離
    [SerializeField] float Radius = 500f;
    // 關卡單位滑動靈敏度
    [SerializeField] float Sensitivity = 0.2f;
    // 關卡單位最小縮放
    [SerializeField] float MinScale = 0.6f;
    // 關卡單位最大縮放
    [SerializeField] float MaxScale = 1.2f;
    // 關卡單位最小透明值
    [SerializeField] float MinAlpha = 0.4f;
    // 關卡單位最前方Y軸高度
    [SerializeField] float baseHeight = -90f;
    // 關卡單位後方Y軸高出多少高度
    [SerializeField] float verticalOffset = 260f;

    // 紀錄關卡單位按鈕
    List<RectTransform> UnitButtons = new();
    // 紀錄當前滑動角度
    private float CurrentAngle = 0f;
    // 關卡單位數量
    private int ButtonCount;
    // 角度偏移量
    private float AngleStep;
    // 滑動速度
    private float DragVelocity;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        DOTween.Kill("RotateTween");
    }

    private void Initialize()
    {
        foreach (var btn in UnitButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        UnitButtons.Clear();

        ButtonCount = Enum.GetNames(typeof(LevelEnum)).Length;
        AngleStep = (Mathf.PI * 2f) / ButtonCount;
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;

        Initialize();
        CreateLevelUnit();
        Canvas.ForceUpdateCanvases();
        UpdateLayout();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 創建關卡單位
    /// </summary>
    private void CreateLevelUnit()
    {
        LevelUnit.gameObject.SetActive(false);
        int index = 0;
        foreach (LevelEnum levelType in Enum.GetValues(typeof(LevelEnum)))
        {
            int currIndex = index;

            GameObject obj = Instantiate(LevelUnit.gameObject, LevelUnitArea);
            obj.SetActive(true);
            LevelUnit levelUnit = obj.GetComponent<LevelUnit>();
            if(levelUnit != null)
            {
                LevelUnitData data = new()
                {
                    LevelType = levelType,
                    LevelBg = UnitBgs[index],
                    LevelIcon = UnitIcons[index],
                    LevelNameKey = LevelNameKeys[index],
                    LevelNameColor = LevelNameColors[index],
                    NotSelectClickAction = () => { RotateToIndex(currIndex); },
                };

                levelUnit.SetData(data);
                UnitButtons.Add(obj.GetComponent<RectTransform>());

                index++;
            }
        }
    }

    /// <summary>
    /// 拖拽處理
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        DOTween.Kill("RotateTween");

        // 計算這一幀的角度變化
        float deltaAngle = eventData.delta.x * Sensitivity * 0.01f;
        CurrentAngle += deltaAngle;

        // 紀錄速度（用於放開後的慣性判斷）
        DragVelocity = deltaAngle / Time.deltaTime;

        UpdateLayout();
    }

    /// <summary>
    /// 結束拖曳
    /// </summary>
    /// <param name="eventData"></param>
    public void OnEndDrag(PointerEventData eventData)
    {
        // 設定一個速度門檻，例如 5f
        float velocityThreshold = 5f;
        float offset = 0f;

        // 如果滑得夠快，根據方向決定要「多轉一格」
        if (Mathf.Abs(DragVelocity) > velocityThreshold)
        {
            // 往左滑 (速度負) 或 往右滑 (速度正)
            offset = (DragVelocity > 0) ? AngleStep * 0.4f : -AngleStep * 0.4f;
        }

        // 將「當前角度 + 速度偏移量」後再進行四捨五入吸附
        float targetAngle = Mathf.Round((CurrentAngle + offset) / AngleStep) * AngleStep;

        DOTween.To(() => CurrentAngle, x => CurrentAngle = x, targetAngle, 0.5f)
               .SetId("RotateTween")
               .SetEase(Ease.OutCubic) // 改用 OutCubic 會比 OutBack 更像真實物理
               .OnUpdate(UpdateLayout);
    }

    /// <summary>
    /// 非選種點擊
    /// </summary>
    private void RotateToIndex(int index)
    {
        // 計算目標的基本角度 (目標索引 * 每一格的角度)
        float targetAngle = -index * AngleStep;

        // 獲取當前角度相對於 2PI 的餘數，算出「目前的循環位置」
        float currentCycle = Mathf.Round(CurrentAngle / (Mathf.PI * 2f));

        // 將目標角度偏移到跟當前角度同一個「圈數」
        targetAngle += currentCycle * (Mathf.PI * 2f);

        // 計算最短路徑：如果差距大於 PI (180度)，代表往反方向轉更近
        if (targetAngle - CurrentAngle > Mathf.PI)
        {
            targetAngle -= Mathf.PI * 2f;
        }
        else if (targetAngle - CurrentAngle < -Mathf.PI)
        {
            targetAngle += Mathf.PI * 2f;
        }

        // 執行平滑旋轉
        DOTween.Kill("RotateTween");
        DOTween.To(() => CurrentAngle, x => CurrentAngle = x, targetAngle, 0.5f)
               .SetId("RotateTween")
               .SetEase(Ease.OutCubic)
               .OnUpdate(UpdateLayout);
    }

    /// <summary>
    /// 更新關卡單位位置
    /// </summary>
    private void UpdateLayout()
    {
        if (UnitButtons == null || UnitButtons.Count == 0) return;

        // 1. 計算位置 (嚴禁在迴圈內對 UnitButtons 進行 Sort)
        for (int i = 0; i < UnitButtons.Count; i++)
        {
            // 確保 i 永遠對應正確的按鈕
            float angle = CurrentAngle + (i * AngleStep);

            float x = Mathf.Sin(angle) * Radius;
            float z = Mathf.Cos(angle);
            float y = baseHeight + ((1f - z) * 0.5f * verticalOffset);

            UnitButtons[i].anchoredPosition = new Vector2(x, y);

            float t = (z + 1f) / 2f;
            UnitButtons[i].localScale = Vector3.one * Mathf.Lerp(MinScale, MaxScale, t);

            if (UnitButtons[i].TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup.alpha = Mathf.Lerp(MinAlpha, 1f, t);
            }
        }

        // 2. 僅為了層級顯示做臨時排序，不影響原本的 List
        List<RectTransform> renderOrder = new List<RectTransform>(UnitButtons);
        renderOrder.Sort((a, b) => a.localScale.x.CompareTo(b.localScale.x));
        for (int i = 0; i < renderOrder.Count; i++)
        {
            renderOrder[i].SetAsLastSibling();
        }
    }
}

/// <summary>
/// 選擇關卡單位資料
/// </summary>
public struct LevelUnitData
{
    /// <summary> 關卡類型 </summary>
    public LevelEnum LevelType;

    /// <summary> 關卡背景 </summary>
    public Sprite LevelBg;

    /// <summary> 關卡圖片 </summary>
    public Sprite LevelIcon;

    /// <summary> 關卡名稱Key </summary>
    public string LevelNameKey;

    /// <summary> 關卡名稱顏色 </summary>
    public VertexGradient LevelNameColor;

    /// <summary> 非選種點擊按鈕回傳 </summary>
    public Action NotSelectClickAction;
}
