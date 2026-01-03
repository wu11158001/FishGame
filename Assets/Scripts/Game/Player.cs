using UnityEngine;
using Fusion;
using UnityEngine.EventSystems;

public class Player : NetworkBehaviour
{
    [SerializeField] Transform ShotPoint;
    [SerializeField] float FireRate = 0.5f;

    // 同步角度變數
    [Networked]
    private float NetworkedAngle { get; set; }

    //射速
    [Networked]
    private TickTimer Delay { get; set; }

    Camera MainCamera;
    Transform BulletPool;
    bool IsSetPosition;

    public override void Spawned()
    {
        BulletPool = GameObject.Find(PoolNameEnum.BulletPool.ToString()).transform;

        if(Object.HasStateAuthority)
        {
            AddressableManagement.Instance.CloseLoading();
            TempDataManagement.Instance.StartTimingUpdateAccountData();
        }
    }

    public override void Render()
    {
        if(!IsSetPosition && transform.parent != null)
        {
            IsSetPosition = true;
        }
    }

    public override void FixedUpdateNetwork()   
    {
        OnRotation();
        OnFire();
    }

    /// <summary>
    /// 轉向
    /// </summary>
    private void OnRotation()
    {
        if (GetInput(out NetworkInputData input))
        {
            if (MainCamera == null)
            {
                MainCamera = Camera.main;
                if (MainCamera == null) return;
            }

            float distanceToCamera = Mathf.Abs(MainCamera.transform.position.z - transform.position.z);
            Vector3 mouseWorldPos = MainCamera.ScreenToWorldPoint(new Vector3(input.MousePosition.x, input.MousePosition.y, distanceToCamera));
            Vector2 dir = (Vector2)mouseWorldPos - (Vector2)transform.position;

            NetworkedAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        transform.rotation = Quaternion.Euler(0, 0, NetworkedAngle);
    }

    /// <summary>
    /// 發射
    /// </summary>
    private void OnFire()
    {
        if(GetInput(out NetworkInputData input))
        {
            // 點擊UI
            if (EventSystem.current.IsPointerOverGameObject())
            {                
                return;
            }

            if (input.IsFirePressed && Delay.ExpiredOrNotRunning(Runner))
            {
                // 判斷子彈花費
                int accountCoin = TempDataManagement.Instance.TempAccountData.Coins;
                int currCost = TempDataManagement.Instance.CurrentLevelData.DefaultCost;

                if(accountCoin < currCost)
                {
                    Debug.Log("金幣不足!");
                    return;
                }

                TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: -currCost);

                // 重製冷卻時間
                Delay = TickTimer.CreateFromSeconds(Runner, FireRate);

                NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                 key: NetworkPrefabEnum.Bullet,
                 Pos: ShotPoint.position,
                 rot: transform.localRotation,
                 parent: BulletPool,
                 player: Object.InputAuthority);
            }
        }
    }
}
