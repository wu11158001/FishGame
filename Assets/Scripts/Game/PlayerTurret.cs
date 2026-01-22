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
    // 座位
    [Networked] int SeatIndex { get; set; }
    // 子彈花費變更
    [Networked, OnChangedRender(nameof(OnCostChanged))]
    public double NetworkedCost { get; set; }

    // 紀錄前次滑鼠點擊
    NetworkButtons ButtonsPrevious { get; set; }

    Camera MainCamera;
    Transform BulletPool;

    List<Transform> CurrShotPoints = new();
    Transform CurrBarrel;

    GameObject Skill_Locking;
    Animator Skill_LockingAni;
    Fish TargetLockingFish;
    Transform TargetLockingObj;

    GameView GameView;
    Coroutine UpdateUICoroutine;

    // 本次發射不射擊
    bool DoNotFireThisTime;

    const float LockingSidePosX = 9.5f;
    const float LockingSidePosY = 5.5f;

    private void OnDestroy()
    {
        StopAllCoroutines();

        if (FirestoreManagement.Instance != null)
            FirestoreManagement.Instance.AccountTurretDataChangeDelegate -= AccountTurretDataChange;

        if (TempDataManagement.Instance != null)
            TempDataManagement.Instance.CurrCostChangeDelegate -= CurrCostChange;
    }

    private void Start()
    {
        if (FirestoreManagement.Instance != null)
            FirestoreManagement.Instance.AccountTurretDataChangeDelegate += AccountTurretDataChange;

        if(TempDataManagement.Instance != null)
            TempDataManagement.Instance.CurrCostChangeDelegate += CurrCostChange;
    }

    public void SetData(int turretIndex, int seatIndex)
    {
        TurretIndex = turretIndex;
        SeatIndex = seatIndex;
    }

    public override void Spawned()
    {
        BulletPool = GameObject.Find(FusionPoolNameEnum.BulletPool.ToString()).transform;

        if(Object.HasStateAuthority)
        {            
            TempDataManagement.Instance.StartTimingUpdateAccountData();
            TempDataManagement.Instance.StartTimingUpdateLevelDataJackpot();

            // 產生鎖定技能
            _ = AddressableManagement.Instance.CreateGamePrefab(
                prefabType: GamePrefabEnum.Skill_Locking,
                callback: (obj) =>
                {
                    Skill_Locking = obj;
                    Skill_Locking.transform.position = new(0, -1, 0);
                    Skill_Locking.transform.rotation = Quaternion.Euler(90, 0, 0);
                    Skill_Locking.SetActive(false);

                    Skill_LockingAni = Skill_Locking.GetComponent<Animator>();
                });

            CurrCostChange(TempDataManagement.Instance.CurrentLevelData.DefaultCost);
            StartCoroutine(IYieldShow());
        }

        ChangeTurret();
        OnCostChanged();
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

        SelfLocking();
        OnSkill_Locking();
        OnRotationControl();
        OnFire();
    }

    /// <summary>
    /// 延遲顯示
    /// </summary>
    private IEnumerator IYieldShow()
    {
        yield return new WaitForSeconds(1);

        Canvas_Global.Instance.CloseLoading();
        Canvas_Global.Instance.CloseSceneLoadingView();
    }

    /// <summary>
    /// 子彈花費變更
    /// </summary>
    private void CurrCostChange(double cost)
    {
        NetworkedCost = cost;
    }

    /// <summary>
    /// 子彈花費變更事件
    /// </summary>
    public void OnCostChanged()
    {
        if (UpdateUICoroutine != null)
            StopCoroutine(UpdateUICoroutine);

        UpdateUICoroutine = StartCoroutine(IYieldUpdateUI());
    }

    /// <summary>
    /// 等待獲取GameView
    /// </summary>
    private IEnumerator IYieldUpdateUI()
    {
        if (GameView == null)
            GameView = FindFirstObjectByType<GameView>();

        // 重試直到找到 GameView
        while (GameView == null)
        {
            GameView = FindFirstObjectByType<GameView>();
            if (GameView == null) yield return new WaitForSeconds(0.2f);
        }

        GameView.PlayerCostChange(seatIndex: SeatIndex, cost: NetworkedCost);
    }

    #region 鎖定技能

    /// <summary>
    /// 鎖定技能
    /// </summary>
    private void OnSkill_Locking()
    {
        if (!Object.HasStateAuthority)
            return;

        bool isLocking = TempDataManagement.Instance.IsSkill_Locking;

        // 檢測鎖定技能是否關閉
        if (Skill_Locking != null && Skill_Locking.activeSelf && !isLocking)
        {
            TargetLockingFish = null;
            Skill_Locking.SetActive(false);
            TargetLockingObj = null;
            return;
        }

        if (isLocking)
        {
            bool isAuto = TempDataManagement.Instance.IsSkill_Auto;

            // 有鎖定目標，但目標消失
            if (TargetLockingFish != null && (!TargetLockingFish.gameObject.activeInHierarchy || !TargetLockingFish .Object.IsValid || TargetLockingFish.IsDie))
            {
                if (!isAuto)
                {
                    // 自動射擊未開啟，目標消失，關閉鎖定技能
                    TargetLockingFish = null;
                    Skill_Locking.SetActive(false);
                    TargetLockingObj = null;
                    return;
                }
                else
                {
                    TakeNewLockingTarget();
                }
            }

            // 自動射擊開啟，沒有鎖定目標，隨機獲取新目標
            if (isAuto && TargetLockingFish == null)
            {
                TakeNewLockingTarget();
            }

            // 鎖定圖標跟隨目標
            if (TargetLockingFish != null && TargetLockingFish.Object.IsValid && TargetLockingFish.gameObject.activeInHierarchy && !TargetLockingFish.IsDie && TargetLockingObj != null)
            {
                // 檢測是否在畫面內
                bool isInSceneX = TargetLockingObj.position.x >= -LockingSidePosX && TargetLockingObj.position.x <= LockingSidePosX;
                bool isInSceneZ = TargetLockingObj.position.z >= -LockingSidePosY && TargetLockingObj.position.z <= LockingSidePosY;

                if (!isInSceneX || !isInSceneZ)
                {
                    if(isAuto)
                    {
                        TargetLockingFish = null;
                        TargetLockingObj = null;
                        Skill_Locking.SetActive(false);
                        TakeNewLockingTarget();
                    }
                    else
                    {
                        TargetLockingFish = null;
                        TargetLockingObj = null;
                        Skill_Locking.SetActive(false);
                    }

                    return;
                }

                Vector3 targetPos = TargetLockingObj.transform.position;
                Skill_Locking.transform.position = new(targetPos.x, Skill_Locking.transform.position.y, targetPos.z);

                // 砲台轉向
                Vector3 direction = TargetLockingObj.position - CurrBarrel.position;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    NetworkedAngle = targetRotation.eulerAngles.y;
                }
            }            
        }
    }

    /// <summary>
    /// 獲取新鎖定目標
    /// </summary>
    private void TakeNewLockingTarget()
    {
        // 自動射擊開啟，目標消失，隨機獲取新目標
        int fishLayer = LayerMask.NameToLayer("Fish");
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<Fish> fishList = new();
        List<Transform> targetObjList = new();
        foreach (GameObject obj in allObjects)
        {
            Fish fish = obj.GetComponentInParent<Fish>();
            if (fish == null)
                continue;

            BoxCollider[] colliders = fish.GetComponentsInChildren<BoxCollider>();

            foreach (var box in colliders)
            {
                bool isInSceneX = box.gameObject.transform.position.x >= -LockingSidePosX && box.gameObject.transform.position.x <= LockingSidePosX;
                bool isInSceneZ = box.gameObject.transform.position.z >= -LockingSidePosY && box.gameObject.transform.position.z <= LockingSidePosY;

                if (obj.layer == fishLayer && isInSceneX && isInSceneZ && fish != null)
                {
                    fishList.Add(fish);
                    targetObjList.Add(box.gameObject.transform);
                    break;
                }
            }
        }
        Fish[] fishs = fishList.ToArray();

        if (fishs.Length > 0)
        {
            int randomIndex = Random.Range(0, fishs.Length);
            TargetLockingFish = fishs[randomIndex];
            TargetLockingObj = targetObjList[randomIndex];

            Skill_LockingAni.SetTrigger("Restart");
            Skill_Locking.SetActive(true);
        }
        else
        {
            TargetLockingFish = null;
            TargetLockingObj = null;
            Skill_Locking.SetActive(false);
        }
    }

    /// <summary>
    /// 手動鎖定
    /// </summary>
    private void SelfLocking()
    {
        if (GetInput(out NetworkInputData input))
        {
            var pressed = input.Buttons.GetPressed(ButtonsPrevious);
            ButtonsPrevious = input.Buttons;

            if (pressed.IsSet(NetworkInputData.MOUSE_LEFT) && TempDataManagement.Instance.IsSkill_Locking)
            {
                Vector2 mousePos = input.MousePosition;
                Ray ray = MainCamera.ScreenPointToRay(mousePos);
                LayerMask layerMask = LayerMask.GetMask("Fish");
                      
                if (Physics.Raycast(ray, out RaycastHit hit, 100, layerMask))
                {
                    Fish fish = hit.transform.GetComponentInParent<Fish>();

                    if (Skill_Locking != null && fish != null)
                    {
                        DoNotFireThisTime = true;
                        Skill_LockingAni.SetTrigger("Restart");
                        TargetLockingFish = fish;
                        TargetLockingObj = hit.transform;
                        Skill_Locking.SetActive(true);
                    }
                }
            }            
        }
    }

    #endregion

    #region 砲台控制

    /// <summary>
    /// 轉向控制
    /// </summary>
    private void OnRotationControl()
    {
        // 是否有鎖定目標
        bool isLocking = TempDataManagement.Instance.IsSkill_Locking && TargetLockingFish != null;

        if (GetInput(out NetworkInputData input) && !isLocking)
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
        bool isAuto = TempDataManagement.Instance.IsSkill_Auto;
        bool isLocking = TempDataManagement.Instance.IsSkill_Locking;
        bool isOpenView = TempDataManagement.Instance.IsStopShot;

        // 自動 & 鎖定 但沒有目標
        if (isAuto && isLocking && TargetLockingFish == null)
            return;

        if (GetInput(out NetworkInputData input) || isAuto)
        {
            // 點擊UI
            if ((!isAuto && EventSystem.current.IsPointerOverGameObject()) || isOpenView)
                return;

            if (CurrShotPoints == null || CurrShotPoints.Count == 0)
                return;

            if (CurrBarrel == null)
                return;

            if(DoNotFireThisTime)
            {
                DoNotFireThisTime = false;
                return;
            }

            bool manualFire = input.Buttons.IsSet(NetworkInputData.MOUSE_LEFT);

            if ((manualFire || isAuto) && Delay.ExpiredOrNotRunning(Runner))
            {
                TurretData turretData = TempDataManagement.Instance.GetTurrethData((TurretEnum)TurretIndex);
                // 重製冷卻時間
                Delay = TickTimer.CreateFromSeconds(Runner, turretData.Rate);

                // 判斷子彈花費
                double accountCoin = TempDataManagement.Instance.TempAccountData.Coins;
                double currCost = TempDataManagement.Instance.CurrentLevelData.DefaultCost;
                double totalCost = currCost * CurrShotPoints.Count;

                if (accountCoin < totalCost)
                {
                    Debug.Log("金幣不足!");
                    AddressableManagement.Instance.ShowToast("Insufficient Coin");

                    TempDataManagement.Instance.IsStopShot = true;
                    _ = AddressableManagement.Instance.OpenCoinStoreView(closeAction: () =>
                    {
                        TempDataManagement.Instance.IsStopShot = false;
                    });

                    // 自動狀態下強制關閉自動
                    if (isAuto)
                    {
                        TempDataManagement.Instance.IsSkill_AutoCloseEvent();
                    }

                    return;
                }

                // 扣除金幣
                if (Runner.IsForward)
                {
                    TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: -totalCost, isInvokeChange: true);
                    TempDataManagement.Instance.RecodJackpot += totalCost;
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
                        player: Object.InputAuthority,
                        callback: (networkObj) =>
                        {
                            Bullet bullet = networkObj.gameObject.GetComponent<Bullet>();
                            if (bullet != null)
                            {
                                bullet.SetData(targetLockingFish: TargetLockingFish, targetLockingObj: TargetLockingObj);
                            }
                        });
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

        Transform bottom = activeTurret.transform.Find("Bottom");

        // 設定底座
        bottom.rotation =
            SeatIndex == 1 || SeatIndex == 3 ?
            Quaternion.Euler(0, 180, 0) :
            Quaternion.Euler(0, 0, 0);

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

    #endregion
}
