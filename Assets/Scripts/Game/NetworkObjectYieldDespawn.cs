using UnityEngine;
using Fusion;
using System.Collections;

public class NetworkObjectYieldDespawn : NetworkBehaviour
{
    [SerializeField] float DespawnTime = 1f;

    public override void Spawned()
    {
        if(Object.HasStateAuthority)
        {
            StartCoroutine(IYieldDespawn());
        }
    }

    /// <summary>
    /// 延遲關閉
    /// </summary>
    /// <returns></returns>
    private IEnumerator IYieldDespawn()
    {
        yield return new WaitForSeconds(DespawnTime);

        Runner.Despawn(Object);
    }
}
