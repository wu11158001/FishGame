using UnityEngine;
using Fusion;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class PlayerTurret : NetworkBehaviour
{
    [Header("Turrets")]
    [SerializeField] List<GameObject> Turrets = new();

    [Header("Fire")]
    [SerializeField] float FireRate = 0.5f;

    [Header("HandleRecoil")]
    [SerializeField] float RecoilDistance = 0.2f; // 後退距離
    [SerializeField] float ReturnSpeed = 5f;      // 回彈速度

    // 使用砲台
    [OnChangedRender(nameof(ChangeTurret))]
    [Networked] int TurretIndex { get; set; }

    // 同步角度變數
    [Networked] float NetworkedAngle { get; set; }

    //射速
    [Networked] TickTimer Delay { get; set; }

    // 當前後座力
    [Networked] float CurrentRecoil { get; set; }

    Camera MainCamera;
    Transform BulletPool;

    List<Transform> CurrShotPoints = new();
    Transform CurrBarrel;

    public void SetData(int turretIndex)
    {
        TurretIndex = turretIndex;
        ChangeTurret();
    }

    public override void Spawned()
    {
        BulletPool = GameObject.Find(FusionPoolNameEnum.BulletPool.ToString()).transform;

        if(Object.HasStateAuthority)
        {
            AddressableManagement.Instance.CloseLoading();
            TempDataManagement.Instance.StartTimingUpdateAccountData();
        }
    }

    public override void FixedUpdateNetwork()   
    {       
        OnFire();
        OnRotation();
        HandleRecoil();
    }

    /// <summary>
    /// 轉向
    /// </summary>
    private void OnRotation()
    {
        if (CurrBarrel == null)
            return;

        if (GetInput(out NetworkInputData input))
        {
            if (MainCamera == null)
            {
                MainCamera = Camera.main;
                if (MainCamera == null) return;
            }

            Ray ray = MainCamera.ScreenPointToRay(input.MousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 mouseWorldPos = ray.GetPoint(enter);
                Vector3 dir = mouseWorldPos - transform.position;
                dir.y = 0;

                if (dir.sqrMagnitude > 0.1f) // 避免向量過小時產生抖動
                {
                    NetworkedAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                }
            }
        }

        CurrBarrel.rotation = Quaternion.Euler(0, NetworkedAngle, 0);
    }

    /// <summary>
    /// 發射
    /// </summary>
    private void OnFire()
    {
        if (GetInput(out NetworkInputData input))
        {
            // 點擊UI
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (CurrShotPoints == null || CurrShotPoints.Count == 0)
                return;

            if (CurrBarrel == null)
                return;

            if (input.IsFirePressed && Delay.ExpiredOrNotRunning(Runner))
            {
                // 判斷子彈花費
                double accountCoin = TempDataManagement.Instance.TempAccountData.Coins;
                double currCost = TempDataManagement.Instance.CurrentLevelData.DefaultCost;
                double totalCost = currCost * CurrShotPoints.Count;

                if (accountCoin < currCost)
                {
                    Debug.Log("金幣不足!");
                    return;
                }

                // 扣除金幣
                if (Runner.IsForward)
                    TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: -currCost);

                // 觸發後座力
                CurrentRecoil = RecoilDistance;

                for (int i = 0; i < CurrShotPoints.Count; i++)
                {
                    // 重製冷卻時間
                    Delay = TickTimer.CreateFromSeconds(Runner, FireRate);

                    Vector3 pos = CurrShotPoints[i].position;
                    pos.y = 0;

                    NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                        key: NetworkPrefabEnum.Bullet,
                        Pos: pos,
                        rot: CurrBarrel.localRotation,
                        parent: BulletPool,
                        player: Object.InputAuthority);
                }
            }
        }
    }

    /// <summary>
    /// 發射後座力
    /// </summary>
    private void HandleRecoil()
    {
        if (CurrBarrel == null) return;

        CurrentRecoil = Mathf.Lerp(CurrentRecoil, 0, Runner.DeltaTime * ReturnSpeed);
        CurrBarrel.localPosition = -CurrBarrel.transform.forward * CurrentRecoil;
    }

    /// <summary>
    /// 更換砲台
    /// </summary>
    private void ChangeTurret()
    {
        Debug.Log($"更換砲台 : {TurretIndex}");

        // 隱藏所有砲台
        for (int i = 0; i < Turrets.Count; i++)
        {
            if (Turrets[i] != null)
                Turrets[i].SetActive(i == TurretIndex);
        }

        // 獲取當前使用的砲台物件
        GameObject activeTurret = Turrets[TurretIndex];
        if (activeTurret == null) return;

        // 設定砲管
        CurrBarrel = activeTurret.transform.Find("Barrel");

        // 更新發射點 (優化：只找當前砲台下的 ShotPoint)
        CurrShotPoints.Clear();

        // 更新發射點
        var childTransforms = activeTurret.GetComponentsInChildren<Transform>();
        foreach (var t in childTransforms)
        {
            if (t.name.StartsWith("ShotPoint"))
            {
                CurrShotPoints.Add(t);
            }
        }
    }
}
