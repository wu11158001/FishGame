using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float Speed = 20;
    [SerializeField] float RayDistance = 100;
    [SerializeField] float BulletRadius = 0.4f;
    [SerializeField] SpriteRenderer BulletSR;

    [Networked] Vector3 Direction { get; set; }
    [Networked] NetworkId TargetFishId { get; set; }
    [Networked] int BulletSpriteIndex { get; set; }

    Transform EffectPool;
    Fish LocalTargetFish;
    Transform TargetLockingObj;

    bool IsFreeBullet;

    readonly Vector2 MinBounds = new(-10f, -6f);
    readonly Vector2 MaxBounds = new(10f, 6f);

    public void SetData(Fish targetLockingFish, Transform targetLockingObj, bool isFreeBullet, int bulletSpriteIndex)
    {
        if (Object.HasStateAuthority)
        {
            TargetFishId = (targetLockingFish != null) ? targetLockingFish.Object.Id : default;
            TargetLockingObj = targetLockingObj;
            IsFreeBullet = isFreeBullet;
            BulletSpriteIndex = bulletSpriteIndex;
        }
    }

    public override void Spawned()
    {
        EffectPool = GameObject.Find(FusionPoolNameEnum.EffectPool.ToString()).transform;
        LocalTargetFish = null;

        if (Object != null && Object.HasStateAuthority)
        {
            Direction = transform.forward;
        }

        BulletSR.sprite = TextureManagement.Instance.GetBulletSprite(BulletSpriteIndex);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
            return;

        Move();

        if (Object.HasStateAuthority)
        {
            CheckBounds();
            CheckHit();
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        // 目標獲取與合法性檢查
        if (TargetFishId.IsValid)
        {
            // 如果目前沒有緩存，或是緩存的物件已經被銷毀
            if (LocalTargetFish == null || LocalTargetFish.Object == null || !LocalTargetFish.Object.IsValid)
            {
                if (Runner.TryFindObject(TargetFishId, out var netObj))
                {
                    LocalTargetFish = netObj.GetComponent<Fish>();
                }
            }

            if (CheckLocalTarget())
            {
                LocalTargetFish = null;
            }
        }
        else
        {
            LocalTargetFish = null;
        }

        // 有鎖定
        if (LocalTargetFish != null && LocalTargetFish.Object != null && LocalTargetFish.Object.IsValid && TargetLockingObj != null)
        {
            Vector3 targetPos = TargetLockingObj.position;
            targetPos.y = transform.position.y;
            Vector3 followDir = (targetPos - transform.position).normalized;

            if (followDir != Vector3.zero)
                transform.forward = followDir;
        }
        else
        {
            transform.forward = Direction;
        }

        transform.Translate(Vector3.forward * Speed * Runner.DeltaTime);
    }

    /// <summary>
    /// 邊界判斷反彈
    /// </summary>
    private void CheckBounds()
    {
        Vector3 pos = transform.position;
        Vector3 currentDir = Direction;
        bool hasBounced = false;

        if (pos.x <= MinBounds.x || pos.x >= MaxBounds.x)
        {
            currentDir.x = -currentDir.x;
            hasBounced = true;
        }

        if (pos.z <= MinBounds.y || pos.z >= MaxBounds.y)
        {
            currentDir.z = -currentDir.z;
            hasBounced = true;
        }

        if (hasBounced)
        {
            Direction = currentDir;
        }
    }

    /// <summary>
    /// 檢測擊中
    /// </summary>
    private void CheckHit()
    {
        if (!Object.HasStateAuthority)
            return;

        // 鎖定目標不存在
        if (LocalTargetFish != null)
        {
            if (CheckLocalTarget())
            {
                TargetLockingObj = null;
                LocalTargetFish = null;
                TargetFishId = default; 
            }
        }

        LayerMask mask = LayerMask.GetMask("Fish");
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, BulletRadius, Vector3.down, RayDistance, mask);

        if (hits.Length > 0)
        {
            if (LocalTargetFish != null)
            {
                foreach (var hit in hits)
                {
                    Fish hitFish = hit.collider.GetComponentInParent<Fish>();
                    if (hitFish != null && hitFish == LocalTargetFish)
                    {
                        HitTarget(hitFish);
                        return;
                    }
                }
            }
            else
            {
                Fish hitFish = hits[0].collider.GetComponentInParent<Fish>();
                if (hitFish != null)
                {
                    HitTarget(hitFish);
                }
            }
        }
    }

    /// <summary>
    /// 檢測鎖定目標(True = 沒有目標)
    /// </summary>
    private bool CheckLocalTarget()
    {
        return 
            LocalTargetFish == null || LocalTargetFish.Object == null || !LocalTargetFish.Object.IsValid || 
            !LocalTargetFish.gameObject.activeInHierarchy || LocalTargetFish.IsDie || TargetLockingObj == null;
    }

    /// <summary>
    /// 擊中目標
    /// </summary>
    public void HitTarget(Fish fish, double bulletCost = -1)
    {
        if (fish == null)
        {
            Debug.LogError("找不到擊中魚的腳本");
            Runner.Despawn(Object);
            return;
        }

        if (FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
        {
            Runner.Despawn(Object);
            return;
        }

        // 初始花費(下注)
        double initCost = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.DefaultCost;
        // 免費子彈增加倍率
        double addOdds = IsFreeBullet ? LocalData.FreeBulletAddOdds : 0;
        // 子彈擊中效果
        NetworkPrefabEnum hitEffect = NetworkPrefabEnum.NormalHitEffect;
        if (BulletSpriteIndex == 1)
            hitEffect = NetworkPrefabEnum.ElectroHitEffect;

        fish.GetHit(initCost, addOdds, hitEffect);

        Runner.Despawn(Object);
    }

    private void OnDrawGizmosSelected()
    {
        // 設定 Gizmos 顏色
        Gizmos.color = Color.cyan;
        Vector3 startPos = transform.position;

        // 1. 畫出起點球體
        Gizmos.DrawWireSphere(startPos, BulletRadius);

        // 2. 計算終點（假設向下射）
        Vector3 direction = Vector3.down;
        Vector3 endPos = startPos + direction * RayDistance;

        // 3. 畫出路徑線（畫四條線讓它看起來像圓柱體掃過）
        Gizmos.DrawLine(startPos + Vector3.left * BulletRadius, endPos + Vector3.left * BulletRadius);
        Gizmos.DrawLine(startPos + Vector3.right * BulletRadius, endPos + Vector3.right * BulletRadius);
        Gizmos.DrawLine(startPos + Vector3.forward * BulletRadius, endPos + Vector3.forward * BulletRadius);
        Gizmos.DrawLine(startPos + Vector3.back * BulletRadius, endPos + Vector3.back * BulletRadius);

        // 4. 畫出終點球體
        Gizmos.DrawWireSphere(endPos, BulletRadius);
    }
}
