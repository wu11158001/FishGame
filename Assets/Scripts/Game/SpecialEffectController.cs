using UnityEngine;
using Fusion;
using System.Collections;
using System;

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
        StartCoroutine(IDragonFullHit(data));
    }

    /// <summary>
    /// 金龍全屏捕獲魚_效果總控制
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private IEnumerator IDragonFullHit(WaterFullHitData data)
    {
        if (GameView == null)
            GameView = FindFirstObjectByType<GameView>();

        // 開啟遮罩
        GameView.MaskEnable(true);

        // 等待魚擊中效果特效結束
        yield return new WaitForSeconds(0.5f);

        // 產生全屏水攻擊特效
        _ = AddressableManagement.Instance.CreateGamePrefab(prefabType: GamePrefabEnum.DragonFullHitEffect);

        // 等待特效結束
        yield return new WaitForSeconds(3);            

        // 場上所有魚擊中處理
        double totalReward = DragonFullHit_FishsHandle(data);

        // 其他玩家回復
        if (data.PlayerRef != Runner.LocalPlayer)
        {
            // 關閉遮罩
            GameView.MaskEnable(false);
            yield break;
        }

        // 產生金龍獲得場上魚群倍率效果物件
        if(data.PlayerRef == Runner.LocalPlayer)
        {
            // 擊中者獲得獎勵
            DragonFullHit_UpdateCoin(data, totalReward);

            _ = AddressableManagement.Instance.CreateGamePrefab(
            prefabType: GamePrefabEnum.DragonFullOdds,
            callback: (obj) =>
            {
                // 完成事件
                Action finishCallback = () =>
                {
                    // 射擊回復
                    TempDataManagement.Instance.IsStopShot = false;
                    // 關閉遮罩
                    GameView.MaskEnable(false);
                    // 更新顯示金幣
                    TempDataManagement.Instance.InvokeTempAccountCoinChangeDelegate();
                };

                // 金龍獲得場上魚群倍率效果
                DragonFullOdds dragonFullOdds = obj.GetComponent<DragonFullOdds>();
                if (dragonFullOdds != null)
                {
                    dragonFullOdds.SetData(
                        targetRecycle: TempDataManagement.Instance.SeatPositions[data.SeatIndex],
                        targerOdds: data.Odds,
                        totalReward: totalReward,
                        finishCallback: finishCallback);
                }
            });
        }
    }

    /// <summary>
    /// 金龍全屏捕獲魚_場上所有魚擊中處理
    /// </summary>
    private double DragonFullHit_FishsHandle(WaterFullHitData data)
    {
        double totalReward = 0;

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

        return totalReward;
    }

    /// <summary>
    /// 金龍全屏捕獲魚_擊中者獲得獎勵
    /// </summary>
    private void DragonFullHit_UpdateCoin(WaterFullHitData data, double totalReward)
    {
        if (data.PlayerRef == Runner.LocalPlayer)
        {
            // 金龍基本獎勵已發送-1
            double dragonTotal = data.DragonReward * (data.Odds - 1);
            // 魚群獎勵
            double fishTotal = totalReward * data.Odds;
            // 總獎勵
            double totalCoin = dragonTotal + fishTotal;

            // 更新獎池與玩家金幣
            TempDataManagement.Instance.RecodJackpot -= totalCoin;
            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: totalCoin, isInvokeChange: false);
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

    /// <summary> 全屏水捕獲魚倍率 </summary>
    public int Odds;

    /// <summary> 金龍獲得獎勵 </summary>
    public double DragonReward;
}
