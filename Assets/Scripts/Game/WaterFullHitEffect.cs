using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaterFullHitEffect : MonoBehaviour
{
    [Header("Water Impact")]
    [SerializeField] GameObject WaterImpact;

    [Header("Water Splash List")]
    [SerializeField] List<GameObject> WaterSplashs = new();
    // 下個水花開啟延遲時間
    [SerializeField] float YieldSplashsTime = 0.5f;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Initialize()
    {
        // 水紋始關閉
        WaterImpact.SetActive(false);

        // 水花初始關閉
        foreach (var splash in WaterSplashs)
        {
            splash.SetActive(false);
        }
    }

    private void OnEnable()
    {
        Initialize();

        StopAllCoroutines();
        StartCoroutine(IWaterEffect());
    }

    /// <summary>
    /// 亂數排列
    /// </summary>
    /// <param name="list"></param>
    private void Shuffle(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            // 隨機取出一個索引
            int randomIndex = Random.Range(i, list.Count);

            // 交換元素
            GameObject temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 水柱效果顯示
    /// </summary>
    private IEnumerator IWaterEffect()
    {
        // 水花
        Shuffle(WaterSplashs);
        foreach (var splash in WaterSplashs)
        {
            splash.SetActive(true);
            yield return new WaitForSeconds(YieldSplashsTime);
        }

        // 水紋
        WaterImpact.SetActive(true);

        yield return new WaitForSeconds(5);
        Destroy(gameObject);
    }
}
