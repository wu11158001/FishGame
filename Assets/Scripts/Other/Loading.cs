using UnityEngine;
using System;
using System.Collections;

public class Loading : MonoBehaviour
{
    const float CloseTime = 30f;

    Coroutine CloseCoroutine;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void OnDisable()
    {
        if (CloseCoroutine != null)
            StopCoroutine(CloseCoroutine);
    }

    private void OnEnable()
    {
        if (CloseCoroutine != null)
            StopCoroutine(CloseCoroutine);

        CloseCoroutine = StartCoroutine(IYieldRemove());
    }

    /// <summary>
    /// 計時移除
    /// </summary>
    /// <returns></returns>
    private IEnumerator IYieldRemove()
    {
        yield return new WaitForSeconds(CloseTime);
    }
}
