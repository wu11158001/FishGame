using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class TurretStoreView : BasicView
{
    [Header("TurretStoreView")]
    [SerializeField] List<Sprite> StroeTurretSprites = new();
    [SerializeField] RectTransform ContentRect;
    [SerializeField] StoreTuretUnit TempStoreTuretUnit;

    List<StoreTuretUnit> StoreTuretUnits = new();

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
        for (int i = 0; i < StoreTuretUnits.Count; i++)
        {
            Destroy(StoreTuretUnits[i].gameObject);
        }
        StoreTuretUnits.Clear();

        int index = 0;
        foreach (TurretEnum turretType in Enum.GetValues(typeof(TurretEnum)))
        {
            if (turretType == TurretEnum.None)
                continue;

            GameObject obj = Instantiate(TempStoreTuretUnit.gameObject, ContentRect);
            StoreTuretUnit storeTuretUnit = obj.GetComponent<StoreTuretUnit>();

            if(storeTuretUnit != null)
            {
                storeTuretUnit.SetData(
                    turretType: turretType,
                    turretIndex: index,
                    turretSprite: StroeTurretSprites[index]);
            }
            else
            {
                Debug.LogError($"創建砲台商品錯誤: {turretType}");
            }

            index++;
        }

        TempStoreTuretUnit.gameObject.SetActive(false);
    }
}
