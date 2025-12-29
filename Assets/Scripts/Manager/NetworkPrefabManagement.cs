using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;

public class NetworkPrefabManagement : SingletonMonoBehaviour<NetworkPrefabManagement>
{
    [Serializable]
    public struct NetworkPrefabEntry
    {
        public NetworkPrefabEnum Key;
        public NetworkPrefabRef PrefabRef;
    }

    [SerializeField]
    private List<NetworkPrefabEntry> NetworkPrefabList = new();

    /// <summary>
    /// 產生網路物件
    /// </summary>
    public void SpawnNetworkPrefab(NetworkPrefabEnum key, Vector3 Pos, Quaternion rot, PlayerRef player, Transform parent = null, Action<NetworkObject> callback = null)
    {
        NetworkPrefabEntry entry = NetworkPrefabList.Find(x => x.Key == key);

        if (entry.PrefabRef.IsValid)
        {           
            var NetworkRunner = NetworkRunnerManagement.Instance.NetworkRunner;

            NetworkRunner.Spawn(
                prefabRef: entry.PrefabRef, 
                position: Pos, 
                rotation: rot,
                inputAuthority: player,
                onBeforeSpawned: (runner, obj) =>
                {
                    if (parent != null)
                        obj.transform.SetParent(parent);

                    callback?.Invoke(obj);
                });
        }
        else
        {
            Debug.LogError($"產生網路物件null: {key}");
        }
    }
}
