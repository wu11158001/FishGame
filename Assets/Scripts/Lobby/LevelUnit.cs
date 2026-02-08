using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Newtonsoft.Json;
using DG.Tweening;

public class LevelUnit : MonoBehaviour
{
#if !UNITY_WEBGL || UNITY_EDITOR
    // 專門存儲 Editor 環境下的監聽器(Key: docId, Value: 監聽器執行個體)
    private Dictionary<string, ListenerRegistration> EditorListeners = new();
#endif

    [SerializeField] Button MainBtn;
    [SerializeField] RectTransform MainRect;
    [SerializeField] Image BgImage;
    [SerializeField] Image LevelIcon;
    [SerializeField] TextMeshProUGUI LevelName;
    [SerializeField] TextMeshProUGUI JackpotText;

    LevelEnum LevelType;
    Action NotSelectClickAction;

    // 紀錄當前獎池
    double CurrentJackpot = 0;

    private void OnDestroy()
    {
        StopListenLevelData(levelType: LevelType);
    }

    private void Start()
    {
        MainBtn.onClick.AddListener(CheckPos);
    }

    public void SetData(LevelEnum levelType, Action notSelectClickAction)
    {
        LevelType = levelType;
        NotSelectClickAction = notSelectClickAction;

        LevelInfoEntry levelInfo = TextureManagement.Instance.GetLevelInfo(levelType);

        BgImage.sprite = levelInfo.LevelBg;
        LevelIcon.sprite = levelInfo.LevelIcon;
        LevelName.colorGradient = levelInfo.LevelNameColors;
        LevelName.text = LocalizationManagement.Instance.GetLocalizedString(levelInfo.LevelNameKey);

        if (FirestoreDataManagement.Instance != null)
        {
            double jackpot = FirestoreDataManagement.Instance.GetLevelData(levelType: levelType).Jackpot;
            CurrentJackpot = jackpot;
            SetJackpotText();
        }

        StartListenLevelData(levelType: levelType);
    }

    /// <summary>
    /// 檢測位置判斷是要移動到正中心還是加入遊戲
    /// </summary>
    private void CheckPos()
    {
        if(MainRect.localScale.x > 1)
        {
            JoinGame();
        }
        else
        {
            NotSelectClickAction?.Invoke();
        }
    }

    /// <summary>
    /// 進入關卡
    /// </summary>
    private void JoinGame()
    {
        Canvas_Global.Instance.ShowSceneLoadingView();

        // 進入遊戲場景
        SceneManagement.Instance.LoadScene(
            sceneEnum: SceneEnum.Game,
            callback: async () =>
            {
                await AddressableManagement.Instance.CreateGamePrefab(
                    prefabType: GamePrefabEnum.GameEntry,
                    callback: (obj) =>
                    {
                        GameEntry gameEntry = obj.GetComponent<GameEntry>();
                        if (gameEntry != null)
                        {
                            gameEntry.SetData(levelType: LevelType);
                        }
                    });
            });
    }

    #region 關卡資料監聽

    /// <summary>
    /// 停止監聽關卡資料
    /// </summary>
    public void StopListenLevelData(LevelEnum levelType)
    {
        string docId = levelType.ToString();

#if UNITY_WEBGL && !UNITY_EDITOR
        
        FirestoreManagement.StopListenToFirestoreData(docId);              
#else
        if (EditorListeners.ContainsKey(docId))
        {
            EditorListeners[docId].Stop();
            EditorListeners.Remove(docId);
        }
#endif
    }

    /// <summary>
    /// 開始監聽關卡資料
    /// </summary>
    public void StartListenLevelData(LevelEnum levelType)
    {
        string path = FirestoreCollectionNameEnum.LevelData.ToString();
        string docId = levelType.ToString();

#if UNITY_WEBGL && !UNITY_EDITOR
        FirestoreManagement.ListenToFirestoreData(path, docId, gameObject.name, nameof(OnLevelDataChanged));
#else
        StopListenLevelData(levelType);

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection(path).Document(docId);

        ListenerRegistration registration = docRef.Listen(snapshot => {
            if (snapshot == null) return;

            bool exists = snapshot.Exists;
            string innerJson = exists ? JsonConvert.SerializeObject(snapshot.ToDictionary()) : "";

            var response = new
            {
                IsSuccess = exists,
                Status = exists ? "DataChanged" : "AccountNotFound",
                JsonData = innerJson
            };

            OnLevelDataChanged(JsonConvert.SerializeObject(response));
        });

        EditorListeners.Add(docId, registration);
#endif
    }

    /// <summary>
    /// 關卡資料變更
    /// </summary>
    public void OnLevelDataChanged(string jsonResponse)
    {
        var response = JsonUtility.FromJson<FirestoreResponse>(jsonResponse);
        LevelData levelData = JsonConvert.DeserializeObject<LevelData>(response.JsonData);

        double newJackpot = levelData.Jackpot;

        // 更新獎池
        DOTween.To(() => CurrentJackpot, x => CurrentJackpot = x, newJackpot, 1f)
            .OnUpdate(() => {
                SetJackpotText();
            });
    }

    /// <summary>
    /// 設置獎池文字內容
    /// </summary>
    private void SetJackpotText()
    {
        double currJackpot = CurrentJackpot;

        if(currJackpot < 10000)
            currJackpot = UnityEngine.Random.Range(10000, 15000);

        JackpotText.text = currJackpot.ToString("#,##0");
    }

    #endregion
}
