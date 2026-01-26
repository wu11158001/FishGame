using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;

public class TurretStoreView : BasicView
{
    [Header("TurretStoreView")]
    [SerializeField] List<Sprite> StroeTurretSprites = new();
    [SerializeField] ScrollViewTool StoreContentScrollViewTool;
    [SerializeField] RectTransform ContentRect;
    [SerializeField] TurretStoreUnit TurretStoreUnit;
    [SerializeField] ToggleGroup ContentToggleGroup;

    [Header("Turret3DModel")]
    // 3D模型每秒旋轉角度
    [SerializeField] float Model3DRotate = 90;
    [SerializeField] float Model3DDragSpeed = 0.5f;
    [SerializeField] EventSystemsHandler Model3DEvenySystemHandler;
    [SerializeField] RawImage Model3DRawImage;
    [SerializeField] List<Transform> Model3DObjects = new();

    [Header("AbilityText")]
    [SerializeField] TextMeshProUGUI TurretAbilityText;
    [SerializeField] float TurretAbilityTextEffectSpeed = 0.05f;

    [Header("ShowTurret3D")]
    [SerializeField] float ShowDuration = 1;
    [SerializeField] float ShowTargetAlpha = 180;

    Dictionary<TurretEnum, TurretData> TurretDataDic = new();
    List<TurretStoreUnit> TurretStoreUnits = new();
    bool IsModel3DAuto;
    Coroutine ShowTurret3DCoroutine;
    Coroutine AbilityTextCoroutine;
    TurretEnum CurrSelectTurretType = TurretEnum.None;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Model3DEvenySystemHandler.DragHandlerDelegate -= Model3DDragHandler;

        if (FirestoreDataManagement.Instance != null)
            FirestoreDataManagement.Instance.AccountTurretDataChangeDelegate -= AccountTurretDataChange;
    }

    protected override void Start()
    {
        base.Start();

        IsModel3DAuto = true;

        Model3DEvenySystemHandler.DragHandlerDelegate += Model3DDragHandler;

        if (FirestoreDataManagement.Instance != null)
            FirestoreDataManagement.Instance.AccountTurretDataChangeDelegate += AccountTurretDataChange;
    }

    private void Update()
    {
        // 3D模型自動旋轉
        Turret3DRotate();
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
        MainCanvasGroup.alpha = 0;

        GetAllTurretData();
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    private void AccountTurretDataChange(AccountData accountData)
    {
        if (accountData != null)
        {
            foreach (var unit in TurretStoreUnits)
            {
                unit.CheckTurret(accountData);
            }
        }
    }

    #region 砲台資料

    /// <summary>
    /// 獲取所有砲台資料
    /// </summary>
    private void GetAllTurretData()
    {
        List<TurretEnum> turretTypes = Enum.GetValues(typeof(TurretEnum))
            .Cast<TurretEnum>()
            .Where(e => e.ToString().StartsWith("Turret"))
            .ToList();

        FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.TurretData,
                callback: GetAllTurretDataCallback);
    }

    /// <summary>
    /// 獲取所有砲台資料Callback
    /// </summary>
    private void GetAllTurretDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            TurretDataDic.Clear();
            List<TurretData> turretList = JsonConvert.DeserializeObject<List<TurretData>>(response.JsonData);

            foreach (var data in turretList)
            {
                TurretDataDic.Add(data.TurretType, data);
            }

            ReciveAllDataComplete();            
        }
        else
        {
            Debug.LogError($"獲取所有砲台資料失敗");
            AddressableManagement.Instance.ShowToast("Wiring Error");
        }
    }

    /// <summary>
    /// 獲取砲台資料
    /// </summary>
    private TurretData GetTurrethData(TurretEnum turretType)
    {
        // 嘗試從字典中獲取資料
        if (TurretDataDic.TryGetValue(turretType, out TurretData data))
        {
            return data;
        }

        Debug.LogWarning($"找不到砲台資料: {turretType}");
        return null;
    }

    #endregion

    /// <summary>
    /// 接收資料完成
    /// </summary>
    private void ReciveAllDataComplete()
    {
        ContentToggleGroup.allowSwitchOff = true;

        CreateTurretUnit();
        StartCoroutine(IYieldShow());

        ContentToggleGroup.allowSwitchOff = false;
    }

    /// <summary>
    /// 創建砲台商品
    /// </summary>
    private void CreateTurretUnit()
    {
        for (int i = 0; i < TurretStoreUnits.Count; i++)
        {
            Destroy(TurretStoreUnits[i].gameObject);
        }
        TurretStoreUnits.Clear();

        int index = 0;
        TurretStoreUnit.gameObject.SetActive(false);
        foreach (TurretEnum turretType in Enum.GetValues(typeof(TurretEnum)))
        {
            if (turretType == TurretEnum.None)
                continue;

            GameObject obj = Instantiate(TurretStoreUnit.gameObject, ContentRect);
            obj.SetActive(true);
            TurretStoreUnit storeTuretUnit = obj.GetComponent<TurretStoreUnit>();

            if(storeTuretUnit != null)
            {
                TurretStoreUnitData data = new()
                {
                   AccountData = FirestoreDataManagement.Instance?.GameTempData?.TempAccountData,
                   TurretData = GetTurrethData(turretType),
                   CoverSprite = StroeTurretSprites[index],
                   Model3D = Model3DObjects[index],
                   SelectCallback = OnSelectTurret
                };

                storeTuretUnit.SetData(data);
                TurretStoreUnits.Add(storeTuretUnit);
            }
            else
            {
                Debug.LogError($"創建砲台商品錯誤: {turretType}");
            }

            index++;
        }
    }

    /// <summary>
    /// 3D模型旋轉
    /// </summary>
    private void Turret3DRotate()
    {
        if (IsModel3DAuto)
        {
            foreach (var model3D in Model3DObjects)
            {
                model3D.Rotate(0, Model3DRotate * Time.deltaTime, 0);
            }
        } 
    }

    /// <summary>
    /// 3D模型拖曳事件
    /// </summary>
    private void Model3DDragHandler(PointerEventData eventData, bool isDrag)
    {
        IsModel3DAuto = !isDrag;

        if (isDrag)
        {
            float rotationY = eventData.delta.x * Model3DDragSpeed;

            foreach (var model3D in Model3DObjects)
            {
                model3D.Rotate(0, -rotationY, 0, Space.Self);
            }
        }
    }

    /// <summary>
    /// 選擇砲台
    /// </summary>
    private void OnSelectTurret(TurretData turretData, RectTransform unitRect)
    {
        if (CurrSelectTurretType != turretData.TurretType && turretData != null)
        {
            CurrSelectTurretType = turretData.TurretType;

            // 射擊頻率
            string rateStr = $"{LocalizationManagement.Instance.GetLocalizedString("Firing Rate")} : {turretData.Rate}";
            // 砲孔數量:
            string holeCount = $"{LocalizationManagement.Instance.GetLocalizedString("Hole Count")} : {turretData.HoleCount}";

            string abilityString = $"{rateStr}\n{holeCount}";

            if (AbilityTextCoroutine != null)
                StopCoroutine(AbilityTextCoroutine);

            AbilityTextCoroutine = StartCoroutine(ITurretAbilityTextEffect(abilityString));

            if (ShowTurret3DCoroutine != null)
                StopCoroutine(ShowTurret3DCoroutine);

            ShowTurret3DCoroutine = StartCoroutine(IShowTurret3D()); ;
        }

        StoreContentScrollViewTool.SnapTo(unitRect);
    }

    /// <summary>
    /// 顯示3D砲台
    /// </summary>
    private IEnumerator IShowTurret3D()
    {
        float targetAlpha = ShowTargetAlpha / 255f;
        float startAlpha = 0f;
        float currentTime = 0f;

        Color tempColor = Model3DRawImage.color;
        tempColor.a = startAlpha;
        Model3DRawImage.color = tempColor;

        while (currentTime < ShowDuration)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / ShowDuration;
            tempColor.a = Mathf.Lerp(startAlpha, targetAlpha, progress);
            Model3DRawImage.color = tempColor;

            yield return null;
        }

        tempColor.a = targetAlpha;
        Model3DRawImage.color = tempColor;
    }

    /// <summary>
    /// 砲台能力文字效果
    /// </summary>
    private IEnumerator ITurretAbilityTextEffect(string str)
    {
        TurretAbilityText.text = str;
        TurretAbilityText.maxVisibleCharacters = 0;

        int totalCharacters = str.Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            TurretAbilityText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(TurretAbilityTextEffectSpeed);
        }
    }
}

/// <summary>
/// 砲台商品資料
/// </summary>
public class TurretStoreUnitData
{
    /// <summary> 帳戶資料 </summary>
    public AccountData AccountData;

    /// <summary> 砲台資料 </summary>
    public TurretData TurretData;

    /// <summary> 砲台圖 </summary>
    public Sprite CoverSprite;

    /// <summary> 對應3D模型 </summary>
    public Transform Model3D;

    /// <summary> 選擇Callback </summary>
    public Action<TurretData, RectTransform> SelectCallback;
}
