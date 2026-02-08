using UnityEngine;
using System.Collections;

public class Skill_MeteorRain : MonoBehaviour
{
    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Start()
    {
        StartCoroutine(IYieldClose());
    }

    private IEnumerator IYieldClose()
    {
        yield return new WaitForSeconds(LocalData.Skill_0EffectDuration + 2);
        Destroy(gameObject);
    }
}
