using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Threading.Tasks;

public class EntryView : MonoBehaviour
{
    [SerializeField] Slider ProgressBar;

    float TargetProgress;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Initialize()
    {
        ProgressBar.value = 0;
    }

    private void Start()
    {
        Initialize();
        DownloadPreAssets();
    }

    /// <summary>
    /// 下載預載資源
    /// </summary>
    private void DownloadPreAssets()
    {
        StartCoroutine(UpdateProgressBarSmoothly());

        // 進度條Action
        Action<float> progressAction = (progress) =>
        {
            TargetProgress = progress;
        };

        // 完成Action
        Action finishAction = async () =>
        {
            TargetProgress = 1;

            float timeout = 0;
            while (ProgressBar.value < 0.9f && timeout < 2f)
            {
                timeout += Time.deltaTime;
                await Task.Yield();
            }
            ProgressBar.value = 1f;

            // 完成後等待
            timeout = 0;
            while (timeout < 1f)
            {
                timeout += Time.deltaTime;
                await Task.Yield();
            }

            // 進入登入場景
            SceneManagement.Instance.LoadScene(
                sceneEnum: SceneEnum.Login,
                callback: async () =>
                {
                    await AddressableManagement.Instance.OpenLoginView();
                });
        };

        AddressableManagement.Instance.DownloadPreAssets(
            progressCallback: progressAction, 
            finishCallback: finishAction);
    }

    /// <summary>
    /// 更新進度條
    /// </summary>
    private IEnumerator UpdateProgressBarSmoothly()
    {
        while (ProgressBar.value < 1f)
        {
            // 平滑地讓當前值趨近目標值
            // 這裡 0.1f 是平滑係數，數值越大追越快
            ProgressBar.value = Mathf.MoveTowards(ProgressBar.value, TargetProgress, Time.deltaTime * 2f);

            yield return null;
        }
    }
}
