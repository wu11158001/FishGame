using UnityEngine;
using UnityEngine.UI;

public class CameraResizer : MonoBehaviour
{
    private Camera MainCamera;
    private CanvasScaler Canvas_SceneScaler;
    private CanvasScaler Canvas_GlobalScaler;

    // 攝影機 Orthographic Size
    private float DefaultSize = 5.4f;

    private void Start()
    {
        MainCamera = GetComponent<Camera>();
        Canvas canvas_Scene = GameObject.Find("Canvas_Scene")?.GetComponent<Canvas>();
        Canvas canvas_Global = GameObject.Find("Canvas_Global")?.GetComponent<Canvas>();

        if(MainCamera != null) MainCamera.orthographicSize = DefaultSize;

        if(canvas_Scene != null) 
            SetCanvas(
                canvas: canvas_Scene, 
                scaler: ref Canvas_SceneScaler, 
                order: 100);      

        if(canvas_Global != null) 
            SetCanvas(
                canvas: canvas_Global, 
                scaler: ref Canvas_GlobalScaler, 
                order: 200);
    }

    private void Update()
    {
        float targetAspect = AddressableManagement.Instance.TargetResolution.x / AddressableManagement.Instance.TargetResolution.y;
        float windowAspect = (float)Screen.width / (float)Screen.height;


        if (windowAspect >= targetAspect)
        {
            // 視窗比設計的還要寬 (例如 21:9)
            // Camera 維持高度固定
            MainCamera.orthographicSize = DefaultSize;

            // UI固定高度
            if (Canvas_SceneScaler != null) Canvas_SceneScaler.matchWidthOrHeight = 1;
            if (Canvas_GlobalScaler != null) Canvas_GlobalScaler.matchWidthOrHeight = 1;
        }
        else
        {
            // 視窗比設計的還要窄 (例如手機直向 9:16)
            // Camera 增加 Size (縮小畫面) 確保寬度內容顯示
            float differenceInSize = targetAspect / windowAspect;
            MainCamera.orthographicSize = DefaultSize * differenceInSize;

            // UI固定高度
            if (Canvas_SceneScaler != null) Canvas_SceneScaler.matchWidthOrHeight = 0;
            if (Canvas_GlobalScaler != null) Canvas_GlobalScaler.matchWidthOrHeight = 0;
        }
    }

    /// <summary>
    /// 設置Canvas
    /// </summary>
    private void SetCanvas(Canvas canvas, ref CanvasScaler scaler, int order)
    {
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = MainCamera;
        canvas.planeDistance = 100;
        canvas.sortingOrder = order;

        if (scaler == null) scaler = canvas.gameObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = AddressableManagement.Instance.TargetResolution;
    }
}