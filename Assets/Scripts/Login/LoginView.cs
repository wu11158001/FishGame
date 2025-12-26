using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Localization.Components;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class LoginView : BasicView
{
    [Header("Login InputField")]
    [SerializeField] TMP_InputField AccountIF_Login;
    [SerializeField] TMP_InputField PasswordIF_Login;

    [Header("Register InputField")]
    [SerializeField] TMP_InputField AccountIF_Register;
    [SerializeField] TMP_InputField PasswordIF_Register;
    [SerializeField] TMP_InputField ConfirmPasswordIF_Register;

    [Header("SwitchArea")]
    [SerializeField] Toggle LoginTog;
    [SerializeField] Toggle RegisterTog;

    [Header("LanguageArea")]
    [SerializeField] Toggle ChineseTog;
    [SerializeField] Toggle EnglishTog;

    [Header("SwitchArea")]
    [SerializeField] LocalizeStringEvent TitleText;
    [SerializeField] GameObject LoginArea;
    [SerializeField] GameObject RegisterArea;

    [Header("Btn")]
    [SerializeField] Button LoginBtn;
    [SerializeField] Button RegisterBtn;

    // 當前面板TAB可切換輸入框
    List<TMP_InputField> TABFields = new();
    // 當前面板發送按鈕事件
    Action EnterAction;

    int TABIndex;

    // 最小資料長度
    const int MiniLength = 4;

    /// <summary>
    /// 切換面板類型
    /// </summary>
    private enum PanelType
    {
        Login,      // 登入
        Register    // 註冊
    }

    private void Initialize()
    {
        MainCanvasGroup.alpha = 0;

        SwitchPanel(PanelType.Login);

        LoginTog.isOn = true;
        ChineseTog.isOn = true;

        CheckLoginData();
        CheckRegisterData();

        StartCoroutine(IYieldShow());
    }

    private void Start()
    {
        LoginTog.onValueChanged.AddListener((value) =>
        {
            if(value == true)
                SwitchPanel(PanelType.Login);
        });

        RegisterTog.onValueChanged.AddListener((value) =>
        {
            if (value == true)
                SwitchPanel(PanelType.Register);
        });

        ChineseTog.onValueChanged.AddListener((value) =>
        {
            if (value == true)
                LocalizationManagement.Instance.ChangeLanguage(Language.zh_TW);
        });

        EnglishTog.onValueChanged.AddListener((value) =>
        {
            if (value == true)
                LocalizationManagement.Instance.ChangeLanguage(Language.en);
        });

        AccountIF_Login.onValueChanged.AddListener((value) => { CheckLoginData(); });
        PasswordIF_Login.onValueChanged.AddListener((value) => { CheckLoginData(); });

        AccountIF_Register.onValueChanged.AddListener((value) => { CheckRegisterData(); });
        PasswordIF_Register.onValueChanged.AddListener((value) => { CheckRegisterData(); });
        ConfirmPasswordIF_Register.onValueChanged.AddListener((value) => { CheckRegisterData(); });

        LoginBtn.onClick.AddListener(SendLogin);

        RegisterBtn.onClick.AddListener(SendRegister);
    }

    private void Update()
    {
        // TAB
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TABIndex++;
            if (TABIndex >= TABFields.Count)
                TABIndex = 0;

            SelectInputField(TABFields[TABIndex]);
        }

        // Enter
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            EnterAction?.Invoke();
        }
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        Initialize();
    }

    /// <summary>
    /// 檢查登入資料
    /// </summary>
    private void CheckLoginData()
    {
        bool checkAccount = AccountIF_Login.text.Length >= MiniLength;
        bool checkPassword = PasswordIF_Login.text.Length >= MiniLength;

        LoginBtn.interactable = checkAccount && checkPassword;
    }

    /// <summary>
    /// 檢查註冊資料
    /// </summary>
    private void CheckRegisterData()
    {
        bool checkAccount = AccountIF_Register.text.Length >= MiniLength;
        bool checkPassword = PasswordIF_Register.text.Length >= MiniLength;
        bool checkConfirmPassword = ConfirmPasswordIF_Register.text == PasswordIF_Register.text;

        RegisterBtn.interactable = checkAccount && checkPassword && checkConfirmPassword;
    }

    /// <summary>
    /// 切換面板
    /// </summary>
    /// <param name="panelType"></param>
    private void SwitchPanel(PanelType panelType)
    {
        LoginArea.SetActive(panelType == PanelType.Login);
        RegisterArea.SetActive(panelType == PanelType.Register);

        string newKey =
            panelType == PanelType.Login ?
            "Log In" :
            "Register";

        LocalizationManagement.Instance.UpdateKey(
            localizeEvent: TitleText,
            newKey: newKey);

        SelectInputField(panelType == PanelType.Login ? AccountIF_Login : AccountIF_Register);

        // 設置TAB可切換輸入框
        TABIndex = 0;
        TABFields.Clear();
        if (panelType == PanelType.Login)
        {
            TABFields.Add(AccountIF_Login);
            TABFields.Add(PasswordIF_Login);
        }
        else if(panelType == PanelType.Register)
        {
            TABFields.Add(AccountIF_Register);
            TABFields.Add(PasswordIF_Register);
            TABFields.Add(ConfirmPasswordIF_Register);
        }

        // 設置Enter按鈕事件
        EnterAction = 
            panelType == PanelType.Login ?
            SendLogin :
            SendRegister;
    }

    /// <summary>
    /// 輸入框選中激活
    /// </summary>
    /// <param name="field"></param>
    private void SelectInputField(TMP_InputField field)
    {
        if (field == null)
            return;

        field.Select();
        field.ActivateInputField();
    }

    /// <summary>
    /// 登入發送
    /// </summary>
    private void SendLogin()
    {
        if (!LoginBtn.interactable)
            return;

        AddressableManagement.Instance.ShowLoading();

        FirestoreManagement.Instance.GetDataFromFirestore(
            path: FirestoreCollectionName.AccountData,
            docId: AccountIF_Login.text,
            callback: SendLoginCallback);
    }

    /// <summary>
    /// 登入Callback
    /// </summary>
    public void SendLoginCallback(FirestoreResponse response)
    {
        AddressableManagement.Instance.CloseLoading();

        if(response == null)
        {
            AddressableManagement.Instance.ShowToast("Wiring Error");
            Debug.LogError("資料回傳 null");
            return;
        }

        if(response.IsSuccess)
        {
            try
            {
                AccountData data = JsonUtility.FromJson<AccountData>(response.JsonData);
                if (data != null)
                {
                    string currPsw = StringUtility.ToHash256(PasswordIF_Login.text);
                    if (data.Password == currPsw)
                    {
                        PlayerPrefs.SetString(PlayerPrefsKeys.USER_ACCOUNT, AccountIF_Login.text);
                        InLobby();
                        Debug.Log("登入成功");
                    }
                    else
                    {
                        AddressableManagement.Instance.ShowToast("Password Error");
                        Debug.LogError("密碼錯誤");
                    }
                }
            }
            catch (Exception e)
            {
                AddressableManagement.Instance.ShowToast("Wiring Error");
                Debug.LogError($"JSON 解析異常: {e.Message}");
            }
        }
        else
        {
            FirestoreManagement.Instance.CallbackFailHandle(response.ResponseStatus);
        } 
    }

    /// <summary>
    /// 註冊發送
    /// </summary>
    private void SendRegister()
    {
        if (!RegisterBtn.interactable)
            return;

        AddressableManagement.Instance.ShowLoading();
        
        // 檢查註冊帳戶是否存在
        FirestoreManagement.Instance.GetDataFromFirestore(
            path: FirestoreCollectionName.AccountData,
            docId: AccountIF_Register.text,
            callback: CheckRegisterAccount);
    }

    /// <summary>
    /// 檢查註冊帳戶是否存在
    /// </summary>
    public void CheckRegisterAccount(FirestoreResponse response)
    {
        AddressableManagement.Instance.CloseLoading();

        if (response == null)
        {
            AddressableManagement.Instance.ShowToast("Wiring Error");
            Debug.LogError("資料回傳 null");
            return;
        }

        try
        {
            if(response.ResponseStatus == FirestoreStatus.Error)
            {
                
                AddressableManagement.Instance.ShowToast("Wiring Error");
                Debug.LogError($"連線錯誤: {response.JsonData}");
                return;
            }

            if(response.ResponseStatus ==  FirestoreStatus.AccountNotFound)
            {
                AddressableManagement.Instance.ShowLoading();

                // 寫入註冊資料
                AccountData data = new()
                {
                    Account = AccountIF_Register.text,
                    Password = StringUtility.ToHash256(PasswordIF_Register.text),
                    Coins = 0,
                };

                string json = JsonUtility.ToJson(data);

                FirestoreManagement.Instance.SaveDataToFirestore(
                    path: FirestoreCollectionName.AccountData,
                    docId: AccountIF_Register.text,
                    jsonData: json,
                    SendRegisterCallback);
            }
            else
            {
                AddressableManagement.Instance.ShowToast("Account Exist");
                Debug.LogError("帳號已存在");
            }
        }
        catch (Exception e)
        {
            AddressableManagement.Instance.ShowToast("Wiring Error");
            Debug.LogError($"JSON 解析異常: {e.Message}");
        }
    }

    /// <summary>
    /// 註冊Callback
    /// </summary>
    public void SendRegisterCallback(FirestoreResponse response)
    {
        AddressableManagement.Instance.CloseLoading();

        if (response == null)
        {
            AddressableManagement.Instance.ShowToast("Wiring Error");
            Debug.LogError("資料回傳 null");
            return;
        }

        if (response.ResponseStatus == FirestoreStatus.Success)
        {
            PlayerPrefs.SetString(PlayerPrefsKeys.USER_ACCOUNT, AccountIF_Register.text);
            InLobby();
            Debug.Log("註冊成功");
        }
        else
        {
            AddressableManagement.Instance.ShowToast("Registration Failed");
            Debug.LogError($"註冊失敗: {response.Status}");
        }
    }

    /// <summary>
    /// 進入大廳
    /// </summary>
    private void InLobby()
    {
        SceneManagement.Instance.LoadScene(
                sceneEnum: SceneEnum.Lobby,
                callback: async () =>
                {
                    await AddressableManagement.Instance.OpenLobbyView();
                });

        Close();
    }
}