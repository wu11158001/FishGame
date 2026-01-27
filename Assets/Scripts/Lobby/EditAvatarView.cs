using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class EditAvatarView : BasicView
{
    [Header("Switch Area")]
    [SerializeField] Toggle AvatarTog;
    [SerializeField] Toggle AvatarFrameTog;

    [Header("Preview Area")]
    [SerializeField] AvatatUnit PreviewAvatatUnit;

    [Header("Avatar Area")]
    [SerializeField] RectTransform AvaratContent;
    [SerializeField] AvatatUnit AvatatUnit;

    [Header("Avatar Frame Area")]
    [SerializeField] RectTransform AvaratFrameContent;
    [SerializeField] AvatatUnit AvatatFrameUnit;

    // 判斷是否已初始化(避免重複創建單位)
    bool IsInit = false;

    // 預覽紀錄
    int currAvatarIndex = 0;
    int currAvatarFrameIndex = 0;

    List<Toggle> AvatarTogs = new();
    List<Toggle> AvatarFrameTogs = new();

    protected override void Close()
    {
        if(FirestoreManagement.Instance != null)
        {
            if (Canvas_Global.Instance)
                Canvas_Global.Instance.ShowLoading();

            // 更新帳戶頭像與頭相框
            var updates = new Dictionary<string, object>
            {
                { "Avatar", currAvatarIndex },
                { "AvatarFrame", currAvatarFrameIndex },
            };

            FirestoreManagement.Instance.UpdateDataToFirestore(
                path: FirestoreCollectionNameEnum.AccountData,
                docId: FirestoreDataManagement.Instance.CurrLoginInfo.Account,
                updates: updates,
                callback: (res) =>
                {
                    if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶帳戶頭像與頭相框資料失敗");

                    Canvas_Global.Instance.CloseLoading();
                    CloseAction?.Invoke();
                });
        }
        else
        {
            CloseAction?.Invoke();
        }        
    }

    private void Initialize()
    {
        if(TextureManagement.Instance != null)
        {
            if(!IsInit)
            {
                // 創建頭像列表
                AvatatUnit.gameObject.SetActive(false);
                int avaratIndex = 0;
                foreach (var avatarSprite in TextureManagement.Instance.AvatarList)
                {
                    int index = avaratIndex;

                    GameObject obj = Instantiate(AvatatUnit.gameObject, AvaratContent);
                    obj.SetActive(true);
                    AvatatUnit unit = obj.GetComponent<AvatatUnit>();
                    if (unit != null)
                    {
                        unit.SetData(avatarImg: avatarSprite, avatarFrameImg: null);
                    }

                    Toggle tog = obj.GetComponent<Toggle>();
                    if(tog != null)
                    {
                        tog.onValueChanged.AddListener((isOn) =>
                        {
                            if(isOn)
                            {
                                SetPreview(avaratIndex: index);
                            }
                        });
                    }
                    AvatarTogs.Add(tog);

                    avaratIndex++;
                }

                // 創建頭像框列表
                AvatatFrameUnit.gameObject.SetActive(false);
                int avaratFrameIndex = 0;
                foreach (var avatarFrameSprite in TextureManagement.Instance.AvatarFrameList)
                {
                    int index = avaratFrameIndex;

                    GameObject obj = Instantiate(AvatatFrameUnit.gameObject, AvaratFrameContent);
                    obj.SetActive(true);
                    AvatatUnit unit = obj.GetComponent<AvatatUnit>();
                    if (unit != null)
                    {
                        if (avatarFrameSprite != null)
                            unit.SetData(avatarImg: null, avatarFrameImg: avatarFrameSprite);
                    }

                    Toggle tog = obj.GetComponent<Toggle>();
                    if (tog != null)
                    {
                        tog.onValueChanged.AddListener((isOn) =>
                        {
                            if (isOn)
                            {
                                SetPreview(avatarFrameIndex: index);
                            }
                        });
                    }
                    AvatarFrameTogs.Add(tog);

                    avaratFrameIndex++;
                }
            }
        }

        IsInit = true;
    }

    protected override void Start()
    {
        base.Start();

        // 切換面板_頭像
        AvatarTog.onValueChanged.AddListener((isOn) =>
        {
            if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.CurrAccountData != null)
            {
                AvatarTogs[currAvatarIndex].isOn = true;
            }
        });

        // 切換面板_頭像框
        AvatarFrameTog.onValueChanged.AddListener((isOn) =>
        {
            if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.CurrAccountData != null)
            {
                AvatarFrameTogs[currAvatarFrameIndex].isOn = true;
            }
        });
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
        MainCanvasGroup.alpha = 0;

        Initialize();

        if(FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.CurrAccountData != null)
        {
            int accountAvatar = FirestoreDataManagement.Instance.CurrAccountData.Avatar;
            int accountAvatarFrame = FirestoreDataManagement.Instance.CurrAccountData.AvatarFrame;

            AvatarTogs[accountAvatar].isOn = true;
            AvatarFrameTogs[accountAvatarFrame].isOn = true;

            SetPreview(avaratIndex: accountAvatar, avatarFrameIndex: accountAvatarFrame);
        }

        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 設置預覽
    /// </summary>
    private void SetPreview(int avaratIndex = -1, int avatarFrameIndex = -1)
    {
        currAvatarIndex = avaratIndex >= 0 ? avaratIndex : currAvatarIndex;
        currAvatarFrameIndex = avatarFrameIndex >= 0 ? avatarFrameIndex : currAvatarFrameIndex;

        Sprite avatar = TextureManagement.Instance.GetAvatar(currAvatarIndex);
        Sprite avatarFrame = TextureManagement.Instance.GetAvatarFrame(currAvatarFrameIndex);

        PreviewAvatatUnit.SetData(avatarImg: avatar, avatarFrameImg: avatarFrame);
    }
}
