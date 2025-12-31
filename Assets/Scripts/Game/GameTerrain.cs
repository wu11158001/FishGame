using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class GameTerrain : NetworkBehaviour
{
    [SerializeField] List<GameObject> Seats;

    /// <summary> 紀錄座位上玩家ID </summary>
    [Networked, Capacity(4)]
    [OnChangedRender(nameof(OnSpawnLocalObject))]
    private NetworkArray<int> SeatPlayerIDs { get; }

    bool isLocalSpawn;

    private void OnDestroy()
    {
        NetworkRunnerManagement.Instance.PlayerLeftEvent -= LeftRoom;
    }

    private void Start()
    {
        NetworkRunnerManagement.Instance.PlayerLeftEvent += LeftRoom;
    }

    public override void Spawned()
    {
        Debug.Log("產生地形");

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
                isLocalSpawn = true;

                var pos = Vector3.zero;

                NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                    key: NetworkPrefabEnum.Player,
                    Pos: Seats[i].transform.position,
                    rot: Quaternion.identity,
                    parent: Seats[i].transform,
                    player: Runner.LocalPlayer);

                break;
            }
        }

        AddressableManagement.Instance.CloseLoading();
    }

    /// <summary>
    /// 離開房間
    /// </summary>
    private void LeftRoom(NetworkRunner runner, PlayerRef player)
    {
        RPC_LeftRoom(player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_LeftRoom(PlayerRef player)
    {
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if(SeatPlayerIDs[i] == player.PlayerId)
            {
                SeatPlayerIDs.Set(i, -1);
                break;
            }
        }
    }

    /// <summary>
    /// 加入座位
    /// </summary>
    private void JoinSeat()
    {
        RPC_JoinSeat(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_JoinSeat(PlayerRef player)
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
