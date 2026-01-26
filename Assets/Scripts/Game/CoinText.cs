using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CoinText : MonoBehaviour
{
    [SerializeField] TextMeshPro MainText;
    [SerializeField] float YieldCloseTime;
    [SerializeField] List<EruptionAndRecycleEffect> EruptionEffects = new();

    Coroutine CloseCoroutine;
    bool IsSetMirror;

    private void OnEnable()
    {
        if (CloseCoroutine != null)
            StopCoroutine(CloseCoroutine);

        CloseCoroutine = StartCoroutine(IYieldClose());
    }

    public void SetData(string str, int recycleSeatIndex)
    {
        if (FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
            return;

        if (MainText != null)
            MainText.text = str;

        if(!IsSetMirror)
        {
            IsSetMirror = true;

            transform.localRotation =
                FirestoreDataManagement.Instance.GameTempData.IsMirror ?
                Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 180) :
                Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }

        // 有噴發效果給予回收目標位置
        Vector3 seatPos = FirestoreDataManagement.Instance.GameTempData.SeatPositions[recycleSeatIndex];
        foreach (var effect in EruptionEffects)
        {
            effect.SetData(targetRecycle: seatPos);
        }
    }

    /// <summary>
    /// 關閉
    /// </summary>
    /// <returns></returns>
    private IEnumerator IYieldClose()
    {
        yield return new WaitForSeconds(YieldCloseTime);
        gameObject.SetActive(false);
    }
}
