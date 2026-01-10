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
/// 介面
/// </summary>
public enum ViewEnum
{
    /// <summary> 等待畫面 </summary>
    Loading,

    /// <summary> 吐司訊息 </summary>
    Toast,

    /// <summary> 登入介面 </summary>
    LoginView,

    /// <summary> 大廳介面 </summary>
    LobbyView,

    /// <summary> 遊戲介面 </summary>
    GameView,

    /// <summary> 遊戲砲台商店介面 </summary>
    GameTurretStoreView,

    /// <summary> 遊戲金幣商店介面 </summary>
    GameCoinStoreView,
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
/// 網路物件(只能往下加，不然要改產生的字典)
/// </summary>
public enum NetworkPrefabEnum
{
    /// <summary> 預設 </summary>
    None,

    /// <summary> 遊戲地形 </summary>
    GameTerrain,

    /// <summary> 玩家砲台 </summary>
    PlayerTurret,

    /// <summary> 子彈物件 </summary>
    Bullet,

    /// <summary> 擊中特效 </summary>
    HitEffect,

    /// <summary> 一般魚魚物件 </summary>
    NormalFish_0,
    NormalFish_1,
    NormalFish_2,
    NormalFish_3,
}

/// <summary>
/// 遊戲預製物
/// </summary>
public enum GamePrefabEnum
{
    /// <summary> 魚群路線主物件 </summary>
    WayPointMain,

    /// <summary> 場景特效 </summary>
    SceneEffect,

    /// <summary> 本地物件池 </summary>
    LocalPool,

    /// <summary> 爆金文字 </summary>
    Coin,
    CoinText_0,
    CoinText_1,
    CoinText_2,
    CoinText_3,
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