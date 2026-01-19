using DG.Tweening;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NaughtyAttributes;
using Cysharp.Threading.Tasks;

/// <summary>
/// 噴發與回收效果
/// </summary>
public class EruptionAndRecycleEffect : MonoBehaviour
{
    [Header("SpawnPrefabType")]
    [SerializeField] GamePrefabEnum EruptionType = GamePrefabEnum.CoinText_0;

    [Header("Random")]
    [SerializeField] bool IsRandomPos = false;                      // 是否隨機位置

    [Header("Eruption")]
    [SerializeField] float EruptionRadius = 1;                      // 噴發半徑
    [SerializeField] float EruptionDuration = 0.5f;                 // 噴發時間(秒)
    [SerializeField] int EruptionCount = 3;                         // 噴發數量
    [SerializeField] float EruptionStayTime = 1;                    // 噴發停留時間(秒)
    [SerializeField] float EruptionYieldTime = 0;                   // 每次噴發延遲時間(秒)

    [Header("Recycle")]
    [SerializeField] float RecycleDuration = 1;                     // 回收時間(秒)

    GameObject CreateObj;
    List<GameObject> ObjList = new();
    Vector3 TargetRecycle;

    private void OnDestroy()
    {
        StopAllCoroutines();
        transform.DOKill();

        foreach (var obj in ObjList)
        {
            obj.transform.DOKill();
            obj.SetActive(false);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        transform.DOKill();

        foreach (var obj in ObjList)
        {
            obj.transform.DOKill();
            obj.SetActive(false);
        }
    }

    public void SetData(Vector3 targetRecycle)
    {
        TargetRecycle = targetRecycle;

        StopAllCoroutines();

        foreach (var obj in ObjList)
        {
            obj.transform.DOKill();
            obj.SetActive(false);
        }

        EruptionEffect();
    }

    /// <summary>
    /// 噴發效果
    /// </summary>
    private async void EruptionEffect()
    {
        if (CreateObj == null || ObjList == null || ObjList.Count == 0)
        {
            await AddressableManagement.Instance.CreateGamePrefab(
                    prefabType: EruptionType,
                    parent: transform,
                    callback: (obj) =>
                    {
                        CreateObj = obj;
                        CreateObj.SetActive(false);
                    }); 

            for (int i = 0; i < EruptionCount; i++)
            {
                int index = i;

                GameObject obj = Instantiate(CreateObj, transform);
                obj.SetActive(true);
                ObjList.Add(obj);
                DoEruptionEffect(obj, index);

                if (EruptionYieldTime > 0)
                    await UniTask.Delay((int)(EruptionYieldTime * 1000));
            }
        }
        else
        {
            for (int i = 0; i < ObjList.Count; i++)
            {
                int index = i;

                ObjList[i].SetActive(true);
                DoEruptionEffect(ObjList[i], index);

                if (EruptionYieldTime > 0)
                    await UniTask.Delay((int)(EruptionYieldTime * 1000));
            }
        }        
    }

    /// <summary>
    /// 執行噴發效果
    /// </summary>
    /// <param name="obj"></param>
    private void DoEruptionEffect(GameObject obj, int index)
    {
        if(IsRandomPos)
        {
            // 隨機噴發
            obj.transform.localPosition = Vector3.zero;
            Vector2 dir = Random.insideUnitCircle.normalized;
            float radius = Random.Range(EruptionRadius * 0.3f, EruptionRadius);
            Vector2 target = dir * radius;

            obj.transform.DOKill();
            obj.transform.DOLocalMove(target, EruptionDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => { StartCoroutine(IDoRecycle(obj)); });
        }
        else
        {
            // 圓形比例噴發
            obj.transform.position = transform.position;

            float angle = 360f / EruptionCount * index * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 target = (Vector2)obj.transform.localPosition + dir * EruptionRadius;

            obj.transform.DOKill();
            obj.transform.DOLocalMove(target, EruptionDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => { StartCoroutine(IDoRecycle(obj)); });
        }
    }

    /// <summary>
    /// 執行回收效果
    /// </summary>
    /// <param name="obj"></param>
    private IEnumerator IDoRecycle(GameObject obj)
    {
        yield return new WaitForSeconds(EruptionStayTime);

        obj.transform.DOKill();
        obj.transform.DOMove(TargetRecycle, RecycleDuration)
                 .SetEase(Ease.OutCubic);
    }
}
