using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

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

        ContentToggleGroup.allowSwitchOff = true;

        CreateTurretUnit();
        StartCoroutine(IYieldShow());

        ContentToggleGroup.allowSwitchOff = false;
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

    /// <summary>
    /// 控制顯示
    /// </summary>
    public void CanvasGroupShow(bool isShow)
    {
        MainCanvasGroup.alpha = isShow ? 1 : 0;
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
                storeTuretUnit.SetData(
                    turretType: turretType,
                    coverSprite: StroeTurretSprites[index],
                    model3D: Model3DObjects[index],
                    selectCallback: OnSelectTurret);

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
