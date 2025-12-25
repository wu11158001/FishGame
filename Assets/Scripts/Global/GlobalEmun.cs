/// <summary>
/// 場景
/// </summary>
public enum SceneEnum
{
    Login = 1,
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
    /// <summary> 吐司訊息 </summary>
    Toast,

    /// <summary> 登入 </summary>
    LoginView,  
}

/// <summary>
/// Firestore Database 集合名稱
/// </summary>
public enum FirestoreCollectionName
{
    /// <summary> 帳戶資料 </summary>
    AccountData,
}