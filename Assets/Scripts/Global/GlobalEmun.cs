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
    None,

    /// <summary> 遊戲地形 </summary>
    GameTerrain,

    /// <summary> 玩家遊戲物件 </summary>
    Player,

    /// <summary> 子彈物件 </summary>
    Bullet,

    /// <summary> 一般魚魚物件 </summary>
    NormalFish_0,
    NormalFish_1,
    NormalFish_2,
    NormalFish_3,
    NormalFish_4,
}

/// <summary>
/// 遊戲預製物
/// </summary>
public enum GamePrefabEnum
{
    /// <summary> 路線主物件 </summary>
    MainWayPoint,
}

/// <summary>
/// 物件池容器名稱
/// </summary>
public enum PoolNameEnum
{
    /// <summary> 子彈容器 </summary>
    BulletPool,

    /// <summary> 魚容器 </summary>
    FishPool
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
    Account
}