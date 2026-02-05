using UnityEngine;

public static class LocalData
{
    //原始解析度比例
    public static Vector2 TargetResolution { get; private set; } = new(1920, 1080);

    /// <summary> 單次最大道具購買數量 </summary>
    public static int MaxPropsBuyCount = 99;

    /// <summary> 冰凍技能時間 </summary>
    public static float FreezeTime = 4f;

    /// <summary> 免費子彈增加倍率 </summary>
    public static float FreeBulletAddOdds = 10f;
}
