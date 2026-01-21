using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using System;

public class SpinWheel : MonoBehaviour
{
    [Header("Wheel")]
    [SerializeField] List<TextMeshPro> TextMeshs = new();
    [SerializeField] Transform WheelTr;
    [SerializeField] Transform PointerTr;
    // 選轉時間
    [SerializeField] float Duration = 3;
    // 共旋轉幾圈
    [SerializeField] int ExtraRounds = 12;

    [Header("Eruption Effect")]
    [SerializeField] float EruptionRadius = 3;                      // 噴發半徑
    [SerializeField] float EruptionDuration = 0.5f;                 // 噴發時間(秒)
    [SerializeField] float EruptionStayTime = 3;                    // 噴發停留時間(秒)

    [Header("Recycle Effect")]
    [SerializeField] float RecycleDuration = 1;                     // 回收時間(秒)

    [Header("Reward Effect")]
    [SerializeField] float ShowTime = 3;                            // 獎勵顯示時間
    [SerializeField] GameObject RewardEffect;
    [SerializeField] TextMeshPro RewardText;

    SpinWhellData SpinWhellData;

    // 幾等分
    const int SegmentCount = 8;

    /// <summary>
    /// 座位座標
    /// </summary>
    private readonly Vector3[] SeatPositions = new Vector3[]
    {
        new(-6, 0, -1.5f),
        new(-6, 0, 1.5f),
        new(6, 0, -1.5f),
        new(6, 0, 1.5f)
    };

    private void OnDestroy()
    {
        StopAllCoroutines();

        WheelTr.DOKill();
        transform.DOKill();
        PointerTr.DOKill();
        RewardText.transform.DOKill();
    }

    private void Initialize()
    {
        if (SpinWhellData == null)
            return;

        // 設置輪盤內容
        int segmentCount = 8;
        // 原始 step
        double rawStep = (SpinWhellData.MaxValu - SpinWhellData.MinValu) / (double)(segmentCount - 1); 
        // 四捨五入到最近的「漂亮數字」(例如 5 或 10)
        int step = (int)(Math.Round(rawStep / 5.0) * 5);

        int[] values = new int[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            values[i] = (int)(SpinWhellData.MinValu + step * i);
            if (values[i] >= SpinWhellData.MaxValu) values[i] = (int)SpinWhellData.MaxValu;

            if (TextMeshs[i] != null)
                TextMeshs[i].text = $"{StringUtility.CurrencyFormat(values[i])}X";
        }

        // 中獎效果
        RewardEffect.SetActive(false);
        RewardText.gameObject.SetActive(false);
    }

    public void SetData(SpinWhellData data)
    {
        SpinWhellData = data;

        RewardText.text = SpinWhellData.RewardStr;

        Initialize();
        DoEruptionEffect();
    }

    /// <summary>
    /// 噴發效果
    /// </summary>
    private void DoEruptionEffect()
    {
        // 放大效果
        transform.DOKill();
        transform.DOScale(1f, EruptionDuration) .SetEase(Ease.OutBack);

        // 位置偏移效果
        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
        float radius = UnityEngine.Random.Range(EruptionRadius * 0.3f, EruptionRadius);
        Vector2 offset = dir * radius; 
        Vector3 target = transform.localPosition + (Vector3)offset;

        transform.DOLocalMove(target, EruptionDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => { StartCoroutine(IDoRecycle()); });
    }

    /// <summary>
    /// 執行回收效果
    /// </summary>
    private IEnumerator IDoRecycle()
    {
        yield return new WaitForSeconds(EruptionStayTime);

        int index = TempDataManagement.Instance.IsMirror ? 3 - SpinWhellData.SeatIndex : SpinWhellData.SeatIndex;
        Vector3 targetRecycle = SeatPositions[index];

        transform.DOKill();
        transform.DOMove(targetRecycle, RecycleDuration).SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                // 指針效果
                PointerTr.DOLocalMoveY(1.8f, 0.5f).SetLoops(-1, LoopType.Yoyo);

                DoSpinEffect();
            });
    }

    /// <summary>
    /// 輪轉效果
    /// </summary>
    private void DoSpinEffect()
    {
        if (SpinWhellData.TargetIndex < 0)
            SpinWhellData.TargetIndex = 0;

        if (SpinWhellData.TargetIndex >= SegmentCount)
            SpinWhellData.TargetIndex = SegmentCount - 1;

        float segmentAngle = 360f / SegmentCount; 
        float targetAngle = (segmentAngle * SpinWhellData.TargetIndex) + (segmentAngle / 2); 
        float totalRotation = 360f * ExtraRounds + targetAngle;

        WheelTr.DORotate(new Vector3(90, 0, totalRotation), Duration, RotateMode.FastBeyond360).SetEase(Ease.OutQuart)
            .OnComplete(() =>
            {
                StartCoroutine(IShowReward());
            });
    }

    /// <summary>
    /// 顯示中獎效果
    /// </summary>
    private IEnumerator IShowReward()
    {
        // 中獎特效與金額
        RewardEffect.SetActive(true);
        RewardText.gameObject.SetActive(true);

        RewardText.transform.localScale = Vector3.zero;
        RewardText.transform.DOScale(1, 0.5f).SetEase(Ease.OutElastic);

        yield return new WaitForSeconds(ShowTime);

        // 縮小效果
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOScale(0f, EruptionDuration).SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                Destroy(gameObject);
            });
    }
}

/// <summary>
/// 輪盤資料
/// </summary>
public class SpinWhellData
{
    /// <summary> 中獎文字 </summary>
    public string RewardStr;

    /// <summary> 最小倍率 </summary>
    public double MinValu;

    /// <summary> 最大倍率 </summary>
    public double MaxValu;

    /// <summary> 轉盤目標 </summary>
    public int TargetIndex;

    /// <summary> 移動座位 </summary>
    public int SeatIndex;
}