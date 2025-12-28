using UnityEngine;

public class CameraResizer : MonoBehaviour
{
    //原始解析度比例
    public Vector2 targetResolution = new(1920, 1080);

    void Update()
    {
        float targetAspect = targetResolution.x / targetResolution.y;
        float windowAspect = (float)Screen.width / (float)Screen.height;

        // 攝影機 Orthographic Size
        float defaultSize = 5f;

        if (windowAspect >= targetAspect)
        {
            // 視窗比設計的還要寬 (例如 21:9)
            // 維持高度不變，玩家會看到左右更多背景
            Camera.main.orthographicSize = defaultSize;
        }
        else
        {
            // 視窗比設計的還要窄 (例如 4:3 或手機直行)
            // 必須增加 Size (縮小畫面) 確保寬度內容能全部顯示
            float differenceInSize = targetAspect / windowAspect;
            Camera.main.orthographicSize = defaultSize * differenceInSize;
        }
    }
}