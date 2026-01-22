/// <summary>
/// 場景
/// </summary>
public enum SceneEnum
{
    Login = 1,
    Lobby,
    Game,
}

/// <summary>
/// Canvas
/// </summary>
public enum CanvasEnum
{
    Canvas_Scene,
    Canvas_Global,
}

/// <summary>
/// 語言
/// </summary>
public enum Language
{
    /// <summary> 中文 </summary>
    zh_TW,

    /// <summary> 英文 </summary>
    en,
}

/// <summary>
/// 遊戲狀態
/// </summary>
public enum GameState
{
    /// <summary> 一般 </summary>
    Normal,

    /// <summary> 浪潮 </summary>
    WaterWave,

    /// <summary> 浪潮魚群 </summary>
    WaterWaveFishs,

    /// <summary> 特殊魚 </summary>
    SpecialFish,
}

/// <summary>
/// 介面
/// </summary>
public enum ViewEnum
{
    /// <summary> 等待畫面 </summary>
    LoadingView,

    /// <summary> 吐司訊息 </summary>
    ToastView,

    /// <summary> 獲得物品 </summary>
    GetItemView,

    /// <summary> 遊戲浮層按鈕 </summary>
    GameFloatBtn,

    /// <summary> 確認彈窗 </summary>
    ConfirmView,

    /// <summary> 登入介面 </summary>
    LoginView,

    /// <summary> 大廳介面 </summary>
    LobbyView,

    /// <summary> 遊戲介面 </summary>
    GameView,

    /// <summary> 砲台商店介面 </summary>
    TurretStoreView,

    /// <summary> 金幣商店介面 </summary>
    CoinStoreView,

    /// <summary> 分配表介面 </summary>
    GuideView,

    /// <summary> 特殊魚獲取介面 </summary>
    SpecialFishCatchView
}

/// <summary>
/// Firestore 集合名稱
/// </summary>
public enum FirestoreCollectionNameEnum
{
    /// <summary> 帳戶資料 </summary>
    AccountData,

    /// <summary> 魚群資料 </summary>
    FishData,

    /// <summary> 關卡資料 </summary>
    LevelData,

    /// <summary> 砲台資料 </summary>
    TurretData,

    /// <summary> 金幣商店資料 </summary>
    CoinStoreData,
}

/// <summary>
/// Firestore 識別碼
/// </summary>
public enum FirestoreStatusEnum
{
    /// <summary> 錯誤 </summary>
    Error,

    /// <summary> 成功 </summary>
    Success,

    /// <summary> 帳號資料不存在 </summary>
    AccountNotFound,

    /// <summary> 寫入資料失敗 </summary>
    WriteFail,

    /// <summary> 更新資料失敗 </summary>
    UpdateFail,

    /// <summary> 刪除資料失敗 </summary>
    DeleteError,

    /// <summary> 監聽資料變更 </summary>
    DataChanged
}

/// <summary>
/// 網路物件
/// </summary>
public enum NetworkPrefabEnum
{
    /// <summary> 預設 </summary>
    None = -1,

    /// <summary> 遊戲地形 </summary>
    GameTerrain = 0,

    /// <summary> 玩家砲台 </summary>
    PlayerTurret = 1,

    /// <summary> 子彈物件 </summary>
    Bullet = 2,

    /// <summary> 擊中特效 </summary>
    HitEffect = 3,

    /// <summary> 魚擊中特效 </summary>
    FishHitEffect = 4,

    /// <summary> 一般魚魚物件 </summary>
    NormalFish_0 = 100,
    NormalFish_1 = 101,
    NormalFish_2 = 102,
    NormalFish_3 = 103,
    NormalFish_4 = 104,
    NormalFish_5 = 105,
    NormalFish_6 = 106,
    NormalFish_7 = 107,
    NormalFish_8 = 108,
    NormalFish_9 = 109,

    /// <summary> 特殊魚物件 </summary>
    StingrayFish = 200,
    SharkFish = 201,
    DragonFish = 202,
}

/// <summary>
/// 遊戲預製物
/// </summary>
public enum GamePrefabEnum
{
    /// <summary> 魚群路線主物件 </summary>
    WayPointMain = 0,

    /// <summary> 場景特效 </summary>
    SceneEffect = 1,

    /// <summary> 本地物件池 </summary>
    LocalPool = 2,

    /// <summary> 浪潮效果 </summary>
    WaterWave = 3,

    /// <summary> 技能_鎖定 </summary>
    Skill_Locking = 50,

    /// <summary> 噴發物件 </summary>
    StingrayFishCoin = 90,      // 特殊魚硬幣_魟魚
    SharkFishCoin = 91,         // 特殊魚硬幣_鯊魚
    DragonFishCoin = 92,        // 特殊魚硬幣_金龍
    SpinWheel = 99,             // 輪盤
    Coin = 100,                 // 金幣物件

    /// <summary> 爆金文字 </summary>
    CoinText_0 = 101,   // 顏色Alpha變化
    CoinText_1 = 102,   // 文字大小變化
    CoinText_2 = 103,   // 文字大小變化 + 圓形比例噴發3枚
    CoinText_3 = 104,   // 文字大小變化 + 圓形比例噴發5枚
    CoinText_4 = 105,   // 文字大小變化 + 圓形比例噴發7枚
    CoinText_5 = 106,   // (所有人)隨機噴發數枚 + 特殊魚硬幣_魟魚
    CoinText_6 = 107,   // (所有人)特殊魚硬幣_鯊魚
    CoinText_7 = 108,   // (所有人)隨機噴發數枚 + 特殊魚硬幣_金龍
}

/// <summary>
/// Fusion物件池容器名稱
/// </summary>
public enum FusionPoolNameEnum
{
    /// <summary> 子彈容器 </summary>
    BulletPool,

    /// <summary> 魚群容器 </summary>
    FishPool,

    /// <summary> 效果容器 </summary>
    EffectPool,
}

/// <summary>
/// 本地物件池容器名稱
/// </summary>
public enum LocalPoolNamEnum
{
    /// <summary> 爆金文字容器 </summary>
    CoinTextPool,
}

/// <summary>
/// 關卡名稱
/// </summary>
public enum LevelEnum
{
    /// <summary> 經典關卡 </summary>
    ClassicLevel,

    /// <summary> 鯊魚關卡 </summary>
    SharkLevel,

    /// <summary> 金龍關卡 </summary>
    DragonLevel,
}

/// <summary>
/// 路線
/// </summary>
public enum WayPointEnum
{
    /// <summary> 一般路線 </summary>
    NormalWay,

    /// <summary> 浪潮路線 </summary>
    WaterWaveWay,

    /// <summary> 特殊路線_金龍 </summary>
    DragonWaveWay,
}

/// <summary>
/// 遊戲階段
/// </summary>
public enum GamePeriod
{
    /// <summary> 休閒期 </summary>
    IdlePeriod,

    /// <summary> 咬分期 </summary>
    SuckingPeriod,

    /// <summary> 吐分期 </summary>
    PayoutPeriod,
}

/// <summary>
/// 檢查進入房間資料獲取
/// </summary>
public enum CheckJoinRoomDataEnum
{
    /// <summary> 魚群資料 </summary>
    FishData,

    /// <summary> 關卡資料 </summary>
    LevelData,

    /// <summary> 帳戶資料 </summary>
    AccountData,

    /// <summary> 砲台資料 </summary>
    TurretData,
}

/// <summary>
/// 砲台
/// </summary>
public enum TurretEnum
{
    None = -1,
    Turret_0,
    Turret_1,
    Turret_2,
    Turret_3,
}

/// <summary>
/// 金幣商店
/// </summary>
public enum StoreCoinEnum
{
    None = -1,
    StoreCoin_0,
    StoreCoin_1,
    StoreCoin_2,
}