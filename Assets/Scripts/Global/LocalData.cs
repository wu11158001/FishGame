using UnityEngine;

public static class LocalData
{
    //原始解析度比例
    public static Vector2 TargetResolution { get; private set; } = new(1920, 1080);

    /// <summary> 冰凍技能時間 </summary>
    public static float FreezeTime = 4f;
}
