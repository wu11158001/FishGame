using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class GameTurretStoreView : BasicView
{
    [Header("TurretStoreView")]
    [SerializeField] List<Sprite> StroeTurretSprites = new();
    [SerializeField] ScrollViewTool StoreContentScrollViewTool;
    [SerializeField] RectTransform StoreContentContentRect;
    [SerializeField] GameTurretStoreUnit GameTurretStoreUnit;

    [Header("Turret3DModel")]
    // 3D模型每秒旋轉角度
    [SerializeField] float Model3DRotate = 90;
    [SerializeField] float Model3DDragSpeed = 0.5f;
    [SerializeField] EventSystemsHandler Model3DRawImage;
    [SerializeField] List<Transform> Model3DObjects = new();

    [Header("AbilityText")]
    [SerializeField] TextMeshProUGUI TurretAbilityText;
    [SerializeField] float TurretAbilityTextEffectSpeed = 0.05f;

    List<GameTurretStoreUnit> GameTurretStoreUnits = new();
    bool IsModel3DAuto;
    Coroutine AbilityTextCoroutine;
    TurretEnum CurrSelectTurretType = TurretEnum.None;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    protected override void Start()
    {
        base.Start();

        IsModel3DAuto = true;
        Model3DRawImage.DragHandlerDelegate += Model3DDragHandler;
    }

    private void Update()
    {
        // 3D模型自動旋轉
        Turret3DRotate();
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        CreateTurretUnit();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 創建砲台商品
    /// </summary>
    private void CreateTurretUnit()
    {
        for (int i = 0; i < GameTurretStoreUnits.Count; i++)
        {
            Destroy(GameTurretStoreUnits[i].gameObject);
        }
        GameTurretStoreUnits.Clear();

        GameTurretStoreUnit.gameObject.SetActive(false);
        int index = 0;
        foreach (TurretEnum turretType in Enum.GetValues(typeof(TurretEnum)))
        {
            if (turretType == TurretEnum.None)
                continue;

            GameObject obj = Instantiate(GameTurretStoreUnit.gameObject, StoreContentContentRect);
            obj.SetActive(true);
            GameTurretStoreUnit storeTuretUnit = obj.GetComponent<GameTurretStoreUnit>();

            if(storeTuretUnit != null)
            {
                storeTuretUnit.SetData(
                    turretType: turretType,
                    turretIndex: index,
                    turretSprite: StroeTurretSprites[index],
                    targetModel3D: Model3DObjects[index],
                    selectAction: OnSelectTurret);
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
            string rateStr = $"{LocalizationManagement.Instance.GetLocalizedString("Firing Rate")}: {turretData.Rate}";
            // 砲孔數量:
            string holeCount = $"{LocalizationManagement.Instance.GetLocalizedString("Hole Count")}: {turretData.HoleCount}";

            string abilityString = $"{rateStr}\n{holeCount}";

            if (AbilityTextCoroutine != null)
                StopCoroutine(AbilityTextCoroutine);

            AbilityTextCoroutine = StartCoroutine(ITurretAbilityTextEffect(abilityString));            
        }

        StoreContentScrollViewTool.SnapTo(unitRect);
    }

    /// <summary>
    /// 砲台能力文字效果
    /// </summary>
    /// <returns></returns>
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
