using DG.Tweening;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 噴發與回收效果
/// </summary>
public class EruptionAndRecycleEffect : MonoBehaviour
{
    [Header("Eruption")]
    [SerializeField] GamePrefabEnum EruptionType;
    [SerializeField] float EruptionRadius = 1;          // 噴發半徑
    [SerializeField] float EruptionDuration = 0.5f;     // 噴發時間
    [SerializeField] int EruptionCount = 3;             // 噴發數量
    [SerializeField] float EruptionStayTime = 1;        // 噴發停留時間

    [Header("Recycle")]
    [SerializeField] float RecycleDuration = 1;         // 回收時間

    List<GameObject> ObjList = new();

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

    private void OnEnable()
    {
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
    private void EruptionEffect()
    {
        if(ObjList == null || ObjList.Count == 0)
        {
            for (int i = 0; i < EruptionCount; i++)
            {
                int index = i;

                _ = AddressableManagement.Instance.CreateGamePrefab(
                    prefabType: EruptionType,
                    parent: transform,
                    callback: (obj) =>
                    {
                        ObjList.Add(obj);
                        DoEruptionEffect(obj, index);
                    });
            }
        }
        else
        {
            for (int i = 0; i < ObjList.Count; i++)
            {
                int index = i;

                ObjList[i].SetActive(true);
                DoEruptionEffect(ObjList[i], index);
            }
        }        
    }

    /// <summary>
    /// 執行噴發效果
    /// </summary>
    /// <param name="obj"></param>
    private void DoEruptionEffect(GameObject obj, int index)
    {
        obj.transform.position = transform.position;

        float angle = 360f / EruptionCount * index * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 target = (Vector2)obj.transform.localPosition + dir * EruptionRadius;

        obj.transform.DOKill();
        obj.transform.DOLocalMove(target, EruptionDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => { StartCoroutine(IDoRecycle(obj)); });
    }

    /// <summary>
    /// 執行回收效果
    /// </summary>
    /// <param name="obj"></param>
    private IEnumerator IDoRecycle(GameObject obj)
    {
        yield return new WaitForSeconds(EruptionStayTime);

        Vector3 SeatPos = GameTempDataManagement.Instance.SeatPosition;

        obj.transform.DOKill();
        obj.transform.DOMove(SeatPos, RecycleDuration)
                 .SetEase(Ease.OutCubic);
    }
}
