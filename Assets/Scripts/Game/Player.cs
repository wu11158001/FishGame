using UnityEngine;
using Fusion;

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

    Transform BulletPool;

    public override void Spawned()
    {
        BulletPool = GameObject.Find("BulletPool").transform;

        transform.localPosition = Vector3.zero;
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
            float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(input.MousePosition.x, input.MousePosition.y, distanceToCamera));
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
            if(input.IsFirePressed && Delay.ExpiredOrNotRunning(Runner))
            {
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
