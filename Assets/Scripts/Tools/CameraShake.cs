using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [SerializeField] float ShakeDuration = 0.5f;
    [SerializeField] float ShakeMagnitude = 0.5f;

    /// <summary>
    /// 攝影機震動
    /// </summary>
    /// <returns></returns>
    public void DoShake()
    {
        StartCoroutine(IShake());
    }

    public IEnumerator IShake()
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < ShakeDuration)
        {
            // 在微小範圍內隨機產生偏移量
            float x = Random.Range(-1f, 1f) * ShakeMagnitude;
            float z = Random.Range(-1f, 1f) * ShakeMagnitude;

            transform.localPosition = new Vector3(x, originalPos.y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}