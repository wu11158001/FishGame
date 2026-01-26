using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelUnit : MonoBehaviour
{
    [SerializeField] Button MainBtn;
    [SerializeField] RectTransform MainRect;
    [SerializeField] Image BgImage;
    [SerializeField] Image LevelIcon;
    [SerializeField] TextMeshProUGUI LevelName;

    LevelEnum LevelType;
    Action NotSelectClickAction;

    private void Start()
    {
        MainBtn.onClick.AddListener(CheckPos);
    }

    public void SetData(LevelUnitData data)
    {
        LevelType = data.LevelType;
        BgImage.sprite = data.LevelBg;
        LevelIcon.sprite = data.LevelIcon;
        LevelName.colorGradient = data.LevelNameColor;
        LevelName.text = LocalizationManagement.Instance.GetLocalizedString(data.LevelNameKey);
        NotSelectClickAction = data.NotSelectClickAction;
    }

    /// <summary>
    /// 檢測位置判斷是要移動到正中心還是加入遊戲
    /// </summary>
    private void CheckPos()
    {
        if(MainRect.localScale.x > 1)
        {
            //JoinGame();
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
}
