using UnityEngine;
using System.Collections;

public class Skill_CrystalsCrossfade : MonoBehaviour
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
        yield return new WaitForSeconds(2);
        Destroy(gameObject);
    }
}
