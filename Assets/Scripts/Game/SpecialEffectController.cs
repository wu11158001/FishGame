using UnityEngine;
using Fusion;
using System.Collections;

/// <summary>
/// 特殊效果控制中心
/// </summary>
public class SpecialEffectController : NetworkBehaviour
{
    Transform EffectPool;
    GameView GameView;

    public override void Spawned()
    {
        EffectPool = GameObject.Find(FusionPoolNameEnum.EffectPool.ToString()).transform;
    }

    #region 金龍全屏捕獲魚

    /// <summary>
    /// 金龍全屏捕獲魚
    /// </summary>
    public void DragonFullHit(WaterFullHitData data)
    {
        RPC_DragonFullHit(data);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_DragonFullHit(WaterFullHitData data)
    {
        if (Object.HasStateAuthority)
        {
            StartCoroutine(IDragonFullHit(data));
        }
    }

    private IEnumerator IDragonFullHit(WaterFullHitData data)
    {
        // 等待魚擊中效果特效結束
        yield return new WaitForSeconds(0.5f);

        // 產生全屏水攻擊特效
        _ = AddressableManagement.Instance.CreateGamePrefab(
            prefabType: GamePrefabEnum.DragonFullHitEffect);

        // 等待特效結束
        yield return new WaitForSeconds(3);

        if (!Object.HasStateAuthority)
            yield break;

        double totalReward = 0;

        // 場上所有魚擊中處理
        foreach (var netObj in Runner.GetAllNetworkObjects())
        {
            if (netObj != null && netObj.IsValid && netObj.gameObject.activeInHierarchy)
            {
                Fish fish = netObj.GetComponent<Fish>();
                if (fish != null && !fish.IsDie)
                {
                    // 產生魚擊中效果
                    BoxCollider[] colliders = fish.GetComponentsInChildren<BoxCollider>();
                    foreach (var collider in colliders)
                    {
                        NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                                    key: NetworkPrefabEnum.FishHitEffect,
                                    Pos: collider.transform.position,
                                    rot: Quaternion.identity,
                                    parent: EffectPool,
                                    player: Object.InputAuthority);
                    }

                    //魚擊中處理
                    FishData_Network fishData = fish.GetFishData();
                    double reward = data.DefaultCost * fishData.Magnification;
                    totalReward += reward;

                    // 爆金文字
                    string eruptionCoinString = StringUtility.CurrencyFormat(reward);

                    FishHitData fishHitData = new()
                    {
                        Player = data.PlayerRef,
                        EruptionCoinString = eruptionCoinString,
                        Reward = reward,
                        SeatIndex = data.SeatIndex,
                        IsLocalShow = false,
                        SpinWheelIndex = -1,
                    };
                    fish.GetHit(fishHitData);
                }
            }
        }

        // 擊中者獲得獎勵
        if(data.PlayerRef == Runner.LocalPlayer)
        {
            Debug.LogError($"擊中者獲得獎勵:{totalReward}");

            // 更新獎池與玩家金幣
            TempDataManagement.Instance.RecodJackpot -= totalReward;
            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: totalReward, isInvokeChange: false);

            if (GameView == null)
                GameView = FindFirstObjectByType<GameView>();

            // 關閉遮罩
            GameView.MaskEnable(false);
            // 射擊回復
            TempDataManagement.Instance.IsStopShot = false;
        }
    }

    #endregion
}

/// <summary>
/// 全屏水捕獲魚資料
/// </summary>
public struct WaterFullHitData: INetworkStruct
{
    /// <summary> 發起玩家 </summary>
    public PlayerRef PlayerRef;

    /// <summary> 觸發時的子彈花費 </summary>
    public double DefaultCost;

    /// <summary> 發起玩家座位 </summary>
    public int SeatIndex;
}
