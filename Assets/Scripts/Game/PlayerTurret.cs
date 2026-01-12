using UnityEngine;
using Fusion;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

public class PlayerTurret : NetworkBehaviour
{
    [Header("Turrets")]
    [SerializeField] List<GameObject> Turrets = new();  

    [Header("HandleRecoil")]
    // 後座力後退距離
    [SerializeField] float RecoilDistance = 0.2f;
    // 後座力回彈速度
    [SerializeField] float ReturnSpeed = 5f;      

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
    public Transform CurrBarrel;

    private void OnDestroy()
    {
        if (FirestoreManagement.Instance != null)
            FirestoreManagement.Instance.AccountTurretDataChangeDelegate -= AccountTurretDataChange;
    }

    private void Start()
    {
        if (FirestoreManagement.Instance != null)
            FirestoreManagement.Instance.AccountTurretDataChangeDelegate += AccountTurretDataChange;
    }

    public void SetData(int turretIndex)
    {
        TurretIndex = turretIndex;
    }

    public override void Spawned()
    {
        BulletPool = GameObject.Find(FusionPoolNameEnum.BulletPool.ToString()).transform;

        if(Object.HasStateAuthority)
        {            
            GameTempDataManagement.Instance.StartTimingUpdateAccountData();
            GameTempDataManagement.Instance.StartTimingUpdateLevelDataJackpot();

            StartCoroutine(IYieldShow());
        }

        ChangeTurret();
    }

    public override void Render()
    {
        if (Object == null || !Object.IsValid)
            return;

        OnRotation();
        HandleRecoil();
    }

    public override void FixedUpdateNetwork()   
    {
        if (Object == null || !Object.IsValid)
            return;

        OnFire();
        OnRotationControl();
    }

    /// <summary>
    /// 延遲顯示
    /// </summary>
    private IEnumerator IYieldShow()
    {
        yield return new WaitForSeconds(1);

        Canvas_Global.Instance.CloseLoading();
        Canvas_Global.Instance.ClosSceneLoadingView();
    }

    /// <summary>
    /// 轉向控制
    /// </summary>
    private void OnRotationControl()
    {      
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
                TurretData turretData = GameTempDataManagement.Instance.GetTurrethData((TurretEnum)TurretIndex);
                // 重製冷卻時間
                Delay = TickTimer.CreateFromSeconds(Runner, turretData.Rate);

                // 判斷子彈花費
                double accountCoin = GameTempDataManagement.Instance.TempAccountData.Coins;
                double currCost = GameTempDataManagement.Instance.CurrentLevelData.DefaultCost;
                double totalCost = currCost * CurrShotPoints.Count;

                if (accountCoin < totalCost)
                {
                    Debug.Log("金幣不足!");
                    AddressableManagement.Instance.ShowToast("Insufficient Coin");
                    _ = AddressableManagement.Instance.OpenCoinStoreView();
                    return;
                }

                // 扣除金幣
                if (Runner.IsForward)
                {
                    GameTempDataManagement.Instance.ChangeTempAccountCoin(changeValue: -totalCost);
                    GameTempDataManagement.Instance.RecodJackpot += totalCost;
                }                    

                // 觸發後座力
                CurrentRecoil = RecoilDistance;

                for (int i = 0; i < CurrShotPoints.Count; i++)
                {
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
        if (CurrBarrel != null)
        {
            CurrentRecoil = Mathf.Lerp(CurrentRecoil, 0, Runner.DeltaTime * ReturnSpeed);
            CurrBarrel.localPosition = -CurrBarrel.transform.forward * CurrentRecoil;
        }
    }

    /// <summary>
    /// 轉向
    /// </summary>
    private void OnRotation()
    {
        if (CurrBarrel != null)
        {
            CurrBarrel.rotation = Quaternion.Euler(0, NetworkedAngle, 0);
        }
    }

    /// <summary>
    /// 帳戶砲台更換
    /// </summary>
    private void AccountTurretDataChange(AccountData accountData)
    {
        if(Object.HasStateAuthority && accountData != null)
        {
            TurretIndex = accountData.DefaultTurret;
        }
    }

    /// <summary>
    /// 更換砲台
    /// </summary>
    private void ChangeTurret()
    {
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

        // 更新發射點
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
