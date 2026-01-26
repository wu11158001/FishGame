using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isShuttingDown = false;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_isShuttingDown)
            {
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                    {
                        var singletonGO = new GameObject(typeof(T).Name);
                        _instance = singletonGO.AddComponent<T>();

                        DontDestroyOnLoad(singletonGO);
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.Log($"[Singleton] 偵測到重複的 {typeof(T)} 在 {gameObject.name}，已自動刪除。");
            Destroy(gameObject);
            return;
        }

        if (_instance == null)
        {
            _instance = this as T;
        }

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

#if UNITY_EDITOR
        if (Application.isPlaying)
            UnityEditor.SceneVisibilityManager.instance.Show(gameObject, false);
#endif

        DontDestroyOnLoad(gameObject);

        _isShuttingDown = false;
    }

    protected virtual void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            _isShuttingDown = true;
        }
    }
}