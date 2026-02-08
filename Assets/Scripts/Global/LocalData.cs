using UnityEngine;

public static class LocalData
{
    /// <summary> 原始解析度比例 </summary>
    public static Vector2 TargetResolution { get; private set; } = new(1920, 1080);

    /// <summary> 單次最大道具購買數量 </summary>
    public static int MaxPropsBuyCount = 99;

    /// <summary> 冰凍技能時間 </summary>
    public static float FreezeTime = 4f;

    /// <summary> 免費子彈增加倍率 </summary>
    public static float FreeBulletAddOdds = 10f;

    /// <summary> 最大能量值(每發子彈能量-1) </summary>
    public static int MaxEnergy = 160;

    /// <summary> 能量技能CD時間(秒) </summary>
    public static float EnergySkillCd = 5;

    /// <summary> 能量技能0_流星雨所需能量(每發子彈能量-1) </summary>
    public static int Skill_0NeedEnergy = 50;

    /// <summary> 能量技能0_流星雨最多捕獲魚數量 </summary>
    public static int Skill_0MaxHitFish = 8;

    /// <summary> 能量技能0_流星雨特效持續時間(秒) </summary>
    public static float Skill_0EffectDuration = 3;

    /// <summary> 能量技能1_冰之爆裂所需能量(每發子彈能量-1) </summary>
    public static int Skill_1NeedEnergy = 80;
}
