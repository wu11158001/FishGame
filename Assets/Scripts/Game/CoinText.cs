using UnityEngine;
using TMPro;
using System.Collections;

public class CoinText : MonoBehaviour
{
    [SerializeField] TextMeshPro MainText;
    [SerializeField] float YieldCloseTime;

    Coroutine CloseCoroutine;
    bool IsSetMirror;

    private void OnEnable()
    {
        if (CloseCoroutine != null)
            StopCoroutine(CloseCoroutine);

        CloseCoroutine = StartCoroutine(IYieldClose());
    }

    public void SetData(double value)
    {
        MainText.text = StringUtility.CurrencyFormat(value);

        if(!IsSetMirror)
        {
            IsSetMirror = true;

            transform.localRotation =
                TempDataManagement.Instance.IsMirror ?
                Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 180) :
                Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
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
