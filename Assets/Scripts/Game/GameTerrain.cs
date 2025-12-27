using Fusion;
using UnityEngine;

public class GameTerrain : NetworkBehaviour
{
    [SerializeField] GameObject[] Seats;

    /// <summary> 紀錄座位上玩家ID </summary>
    [Networked, Capacity(4)]
    public NetworkArray<int> SeatPlayerIDs => default;

    public override void Spawned()
    {
        if(Object.HasStateAuthority)
        {
            // 初始化座位
            for(int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                SeatPlayerIDs.Set(i, -1);
            }
        }
    }

    /// <summary>
    /// 加入座位
    /// </summary>
    public void JoinSeat()
    {
        RPC_RequestSeat(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSeat(PlayerRef player)
    {
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == player.PlayerId) return;

            // 找到第一個空位
            if (SeatPlayerIDs[i] == -1)
            {
                SeatPlayerIDs.Set(i, player.PlayerId);                
                return;
            }
        }
    }
}
