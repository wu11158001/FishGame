using UnityEngine;
using TMPro;
using System.Collections;

public class CoinText : MonoBehaviour
{
    [SerializeField] TextMeshPro MainText;
    [SerializeField] float YieldCloseTime;

    Coroutine CloseCoroutine;

    private void OnEnable()
    {
        if (CloseCoroutine != null)
            StopCoroutine(CloseCoroutine);

        CloseCoroutine = StartCoroutine(IYieldClose());   
    }

    public void SetData(double value)
    {
        MainText.text = StringUtility.CurrencyFormat(value);
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
