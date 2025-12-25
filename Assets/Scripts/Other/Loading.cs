using UnityEngine;
using System;
using System.Collections;

public class Loading : BasicView
{
    const float RemoveTime = 5f;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Start()
    {
        StartCoroutine(IYieldRemove());
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }

    /// <summary>
    /// 計時移除
    /// </summary>
    /// <returns></returns>
    private IEnumerator IYieldRemove()
    {
        yield return new WaitForSeconds(RemoveTime);
        Close();
    }
}
