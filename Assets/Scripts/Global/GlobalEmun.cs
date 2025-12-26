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
    Canvas_Overlay,
    Canvas_Camera,

    /// <summary> 最高層級Canvas </summary>
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
}

/// <summary>
/// Firestore 集合名稱
/// </summary>
public enum FirestoreCollectionName
{
    /// <summary> 帳戶資料 </summary>
    AccountData,
}

/// <summary>
/// Firestore 識別碼
/// </summary>
public enum FirestoreStatus
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