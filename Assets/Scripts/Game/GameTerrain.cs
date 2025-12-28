using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class GameTerrain : NetworkBehaviour
{
    [SerializeField] List<GameObject> Seats;

    /// <summary> 紀錄座位上玩家ID </summary>
    [HideInInspector]
    [Networked, Capacity(4)]
    [OnChangedRender(nameof(OnSpawnLocalObject))]
    public NetworkArray<int> SeatPlayerIDs => default;

    bool isLocalSpawn;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // 初始化座位
            for(int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                SeatPlayerIDs.Set(i, -1);
            }
        }

        JoinSeat();
    }

    /// <summary>
    /// 產生本地玩家物件
    /// </summary>
    private void OnSpawnLocalObject()
    {
        if (isLocalSpawn)
            return;

        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == Runner.LocalPlayer.PlayerId)
            {
                var pos = Vector3.zero;

                NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                    key: NetworkPrefabEnum.Player,
                    Pos: pos,
                    parent: Seats[i].transform,
                    player: Runner.LocalPlayer);
                break;
            }
        }

        AddressableManagement.Instance.CloseLoading();
    }

    /// <summary>
    /// 加入座位
    /// </summary>
    private void JoinSeat()
    {
        RPC_RequestSeat(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSeat(PlayerRef player)
    {
        // 已經有位置
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == player.PlayerId) return; 
        }

        // 設置座位
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == -1)
            {
                SeatPlayerIDs.Set(i, player.PlayerId);
                return;
            }
        }
    }
}
