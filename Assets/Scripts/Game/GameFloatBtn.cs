using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

public class GameFloatBtn : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] CanvasGroup MainCanvasGroup;
    [SerializeField] RectTransform MainRect;
    [SerializeField] Image MaskImage;
    [SerializeField] Button MainBtn;
    [SerializeField] EventSystemsHandler MainBtnEventSystemsHandler;

    [Header("Btns")]
    [SerializeField] Button HomeBtn;
    [SerializeField] Button GuideBtn;

    [Header("Effect")]
    // 回到兩側時間
    [SerializeField] float ToSideDuration = 0.5f;
    // 彈出按鈕距離
    [SerializeField] float OpenDistance = 280;
    // 彈出按鈕時間
    [SerializeField] float OpenDuration = 0.3f;
    // 彈出按鈕延遲時間
    [SerializeField] float OpenDelay = 0.2f;

    Canvas MainCanvas;
    RectTransform SafeAreaRect;
    List<RectTransform> Btns = new();
    bool IsOpen = true;

    Action CloseAction;

    private void OnDestroy()
    {
        StopAllCoroutines();
        MainRect.DOKill();
        CloseAction?.Invoke();
    }

    private void Start()
    {
        Btns = new()
        {
            GuideBtn.GetComponent<RectTransform>(),
            HomeBtn.GetComponent<RectTransform>(),
        };
        foreach (var btn in Btns)
        {
            btn.gameObject.SetActive(false);
        }

        // 主按鈕
        MainBtn.onClick.AddListener(SwitchShowBtns);

        // 返回大廳按鈕
        HomeBtn.onClick.AddListener(() =>
        {
            if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
            {
                FirestoreDataManagement.Instance.GameTempData.IsStopShot = true;
            }           

            if(AddressableManagement.Instance != null)
            {
                AddressableManagement.Instance.ShowConfirmView(
                contentKey: "Leave the game?",
                comfirmAction: () =>
                {
                    if (Canvas_Global.Instance != null)
                        Canvas_Global.Instance.ShowLoading();

                    if (NetworkRunnerManagement.Instance != null)
                    {
                        NetworkRunnerManagement.Instance.IsSafeShutdown = true;
                        NetworkRunnerManagement.Instance.Shutdown();
                    }

                    if (SceneManagement.Instance != null)
                    {
                        SceneManagement.Instance.LoadScene(sceneEnum: SceneEnum.Lobby);
                    }
                },
                cancelAction: () =>
                {
                    if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
                    {
                        FirestoreDataManagement.Instance.GameTempData.IsStopShot = false;
                    }
                });
            }
        });

        // 分配表按鈕
        GuideBtn.onClick.AddListener(() =>
        {
            if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
            {
                FirestoreDataManagement.Instance.GameTempData.IsStopShot = true;
            }

            AddressableManagement.Instance.OpenGuideView(
                closeAction: () =>
                {
                    if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
                    {
                        FirestoreDataManagement.Instance.GameTempData.IsStopShot = false;
                    }
                });
        });

        MainCanvas = GetComponentInParent<Canvas>();
        SafeAreaRect = MainCanvas.transform.Find("SafeArea").GetComponent<RectTransform>();

        MainBtnEventSystemsHandler.BeginDragHandlerDelegate += BeginDragHandler;
        MainBtnEventSystemsHandler.DragHandlerDelegate += DragHandler;

        MaskImage.raycastTarget = false;
        SnapToSide();
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
        SetEnable(true);
    }

    /// <summary>
    /// 設置物件顯示
    /// </summary>
    public void SetEnable(bool isShow)
    {
        MainCanvasGroup.alpha = isShow ? 1 : 0;
    }

    /// <summary>
    /// 開始拖曳處理
    /// </summary>
    /// <param name="eventData"></param>
    public void BeginDragHandler(PointerEventData eventData)
    {
        MainRect.DOKill();
        MaskImage.raycastTarget = true;

        IsOpen = false;
        SwitchShowBtns();
    }

    /// <summary>
    /// 拖曳處理
    /// </summary>
    public void DragHandler(PointerEventData eventData, bool isDrag)
    {
        if(isDrag)
        {
            MainBtn.interactable = false;

            float scale = (MainCanvas != null) ? MainCanvas.scaleFactor : 1.0f;
            MainRect.anchoredPosition += eventData.delta / scale;

            KeepInsideCanvas();
        }
        else
        {
            MaskImage.raycastTarget = false;
            MainBtn.interactable = true;
            SnapToSide();
        }
    }

    /// <summary>
    /// 保持在畫面裡
    /// </summary>
    private void KeepInsideCanvas()
    {
        // 取得父物件的寬高範圍
        float parentWidth = SafeAreaRect.rect.width;
        float parentHeight = SafeAreaRect.rect.height;

        // 取得物件本身的寬高（考慮縮放）
        float selfWidth = MainRect.rect.width * MainRect.localScale.x;
        float selfHeight = MainRect.rect.height * MainRect.localScale.y;

        // 計算可移動的半徑範圍
        float limitX = (parentWidth - (selfWidth / 2)) / 2f;
        float limitY = (parentHeight - selfHeight) / 2f;

        Vector3 pos = MainRect.anchoredPosition;

        limitX = Mathf.Max(limitX, 0);
        limitY = Mathf.Max(limitY, 0);

        pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
        pos.y = Mathf.Clamp(pos.y, -limitY, limitY);

        MainRect.anchoredPosition = pos;
    }

    /// <summary>
    /// 吸附到兩側
    /// </summary>
    private void SnapToSide()
    {
        MainRect.DOKill();

        float limitX = (SafeAreaRect.rect.width - (MainRect.rect.width / 2)) / 2f;
        float targetX = (MainRect.anchoredPosition.x > 0) ? limitX : -limitX;

        MainRect.DOAnchorPosX(targetX, ToSideDuration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// 切換顯示功能按鈕
    /// </summary>
    private void SwitchShowBtns()
    {
        foreach (var btn in Btns)
        {
            btn.DOKill();
        }

        if (IsOpen)
        {
            for (int i = 0; i < Btns.Count; i++)
            {
                int index = i;
                OpenMenu(Btns[i], index);
            }
        }
        else
        {
            for (int i = 0; i < Btns.Count; i++)
            {
                CloseMenu(Btns[i]);
            }
        }

        IsOpen = !IsOpen;
    }

    /// <summary>
    /// 開啟按鈕菜單
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="index"></param>
    private void OpenMenu(RectTransform rt, int index)
    {
        rt.gameObject.SetActive(true);
        float direction = (MainRect.anchoredPosition.x > 0) ? -1f : 1f;
        rt.DOAnchorPosX(direction * OpenDistance * (index + 1), OpenDuration).SetEase(Ease.OutBack).SetDelay(OpenDelay * (index - 1));
    }

    /// <summary>
    /// 關閉按鈕菜單
    /// </summary>
    public void CloseMenu(RectTransform rt)
    {
        rt.DOAnchorPosX(0, OpenDuration).SetEase(Ease.InBack).OnComplete(() => { rt.gameObject.SetActive(false); });
    }
}
