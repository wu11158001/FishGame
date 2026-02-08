using UnityEngine;
using Fusion;
using System.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// 遊戲特殊效果控制中心
/// </summary>
public class SpecialEffectController : NetworkBehaviour
{
    Transform EffectPool;
    GameView GameView;
    FishManager FishManager;
    Transform BulletPool;

    public override void Spawned()
    {
        EffectPool = GameObject.Find(FusionPoolNameEnum.EffectPool.ToString()).transform;
        BulletPool = GameObject.Find(FusionPoolNameEnum.BulletPool.ToString()).transform;
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
    private IEnumerator IDragonFullHit(WaterFullHitData data)
    {
        if (GameView == null)
            GameView = FindFirstObjectByType<GameView>();

        // 開啟遮罩
        GameView.MaskEnable(true);

        // 等待魚擊中效果特效結束
        yield return new WaitForSeconds(0.5f);

        // 產生全屏水攻擊特效
        if (AddressableManagement.Instance != null)
            _ = AddressableManagement.Instance.CreateGamePrefab(prefabType: GamePrefabEnum.DragonFullHitEffect);

        // 等待特效結束
        yield return new WaitForSeconds(3);            

        // 場上所有魚捕獲處理
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

            if(AddressableManagement.Instance != null && FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
            {
                _ = AddressableManagement.Instance.CreateGamePrefab(
                    prefabType: GamePrefabEnum.DragonFullOdds,
                    callback: (obj) =>
                    {
                        obj.transform.rotation =
                            FirestoreDataManagement.Instance.GameTempData.IsMirror ?
                            Quaternion.Euler(0, 180, 0) :
                            Quaternion.Euler(0, 0, 0);

                        // 完成事件
                        Action finishCallback = () =>
                        {
                            // 射擊回復
                            FirestoreDataManagement.Instance.GameTempData.IsStopShot = false;
                            // 關閉遮罩
                            GameView.MaskEnable(false);
                            // 更新顯示金幣
                            FirestoreDataManagement.Instance.GameTempData.InvokeTempAccountCoinChangeDelegate();
                        };

                        // 金龍獲得場上魚群倍率效果
                        DragonFullOdds dragonFullOdds = obj.GetComponent<DragonFullOdds>();
                        if (dragonFullOdds != null)
                        {
                            dragonFullOdds.SetData(
                                targetRecycle: FirestoreDataManagement.Instance.GameTempData.SeatPositions[data.SeatIndex],
                                targerOdds: data.Odds,
                                totalReward: totalReward,
                                finishCallback: finishCallback);
                        }
                    });
            }            
        }
    }

    /// <summary>
    /// 金龍全屏捕獲魚_場上所有魚捕獲處理
    /// </summary>
    private double DragonFullHit_FishsHandle(WaterFullHitData data)
    {
        if (data.PlayerRef != Runner.LocalPlayer)
            return 0;

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
                                    key: NetworkPrefabEnum.FishCatchEffect,
                                    Pos: collider.transform.position,
                                    rot: Quaternion.identity,
                                    parent: EffectPool,
                                    player: data.PlayerRef);
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
                    fish.GetCatch(fishHitData);
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
            if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
            {
                FirestoreDataManagement.Instance.GameTempData.RecodJackpot -= totalCoin;
                FirestoreDataManagement.Instance.GameTempData.ChangeTempAccountCoin(changeValue: totalCoin, isInvokeChange: false);
            }
        }
    }

    #endregion

    #region 能量技能0_流星雨

    /// <summary>
    /// 能量技能0_流星雨
    /// </summary>
    public void MeteorRain(EenergSkillData data)
    {
        RPC_MeteorRain(data);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_MeteorRain(EenergSkillData data)
    {
        StartCoroutine(IMeteorRainHandle(data));
    }

    /// <summary>
    /// 能量技能0_流星雨效果總控
    /// </summary>
    /// <returns></returns>
    private IEnumerator IMeteorRainHandle(EenergSkillData data)
    {
        // 產生流星雨特效
        if (AddressableManagement.Instance != null)
            _ = AddressableManagement.Instance.CreateGamePrefab(prefabType: GamePrefabEnum.Skill_MeteorRain);

        if(data.PlayerRef == Runner.LocalPlayer)
        {
            // 尋找擊中目標
            yield return IMeteorRainHit(data);
        }      
    }

    /// <summary>
    /// 能量技能0_流星雨尋找擊中目標
    /// </summary>
    private IEnumerator IMeteorRainHit(EenergSkillData data)
    {
        yield return new WaitForSeconds(1);

        if (FishManager == null)
            FishManager = UnityEngine.Object.FindFirstObjectByType<FishManager>();
        if (FishManager != null)
        {
            // 效果持續時間
            float durationTime = LocalData.Skill_0EffectDuration;
            // 最多嘗試捕獲魚數量
            int maxHitFishs = LocalData.Skill_0MaxHitFish;
            // 等待尋找下一隻魚時間
            float yieldTime = durationTime / maxHitFishs;           

            for (int i = 0; i < maxHitFishs; i++)
            {
                ActiveFishData activeFishData = FishManager.GetActiveFishes();

                if (activeFishData.FishList == null || activeFishData.FishList.Count == 0)
                    yield break;

                // 隨機挑選目標
                int hitTarget = UnityEngine.Random.Range(0, activeFishData.FishList.Count);

                // 初始花費(下注)
                double initCost = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.MinCost;
                // 增加倍率
                double addOdds = 0;
                // 擊中效果
                NetworkPrefabEnum hitEffect = NetworkPrefabEnum.ExplosionHitEffect;

                activeFishData.FishList[hitTarget].GetHit(initCost, addOdds, hitEffect);
               
                yield return new WaitForSeconds(yieldTime);
            }
        }
    }

    #endregion

    #region 能量技能1_冰之爆裂

    /// <summary>
    /// 能量技能1_冰之爆裂
    /// </summary>
    public void CrystalsCrossfade(EenergSkillData data)
    {
        RPC_CrystalsCrossfade(data);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_CrystalsCrossfade(EenergSkillData data)
    {
        StartCoroutine(ICrystalsCrossfadeHandle(data));
    }

    /// <summary>
    /// 能量技能1_冰之爆裂效果總控
    /// </summary>
    /// <returns></returns>
    private IEnumerator ICrystalsCrossfadeHandle(EenergSkillData data)
    {
        // 產生流星雨特效
        if (AddressableManagement.Instance != null)
            _ = AddressableManagement.Instance.CreateGamePrefab(prefabType: GamePrefabEnum.Skill_CrystalsCrossfade);

        if (data.PlayerRef == Runner.LocalPlayer)
        {
            // 尋找擊中目標
            yield return ICrystalsCrossfadeHit(data);
        }
    }

    /// <summary>
    /// 能量技能1_冰之爆裂尋找擊中目標
    /// </summary>
    private IEnumerator ICrystalsCrossfadeHit(EenergSkillData data)
    {
        yield return new WaitForSeconds(1);

        if (FishManager == null)
            FishManager = UnityEngine.Object.FindFirstObjectByType<FishManager>();
        if (FishManager != null)
        {
            ActiveFishData activeFishData = FishManager.GetActiveFishes();
            foreach (Fish fish in activeFishData.FishList)
            {
                if (fish == null)
                    continue;

                // 初始花費(下注)
                double initCost = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.MinCost;
                // 增加倍率
                double addOdds = 0;
                // 擊中效果
                NetworkPrefabEnum hitEffect = NetworkPrefabEnum.SnowHitEffect;
                fish.GetHit(initCost, addOdds, hitEffect);
            }
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

/// <summary>
/// 能量技能資料
/// </summary>
public struct EenergSkillData: INetworkStruct
{
    /// <summary> 發起玩家 </summary>
    public PlayerRef PlayerRef;

    /// <summary> 子彈花費 </summary>
    public double DefaultCost;

    /// <summary> 發起玩家座位 </summary>
    public int SeatIndex;
}
