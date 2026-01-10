using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class SceneManagement : SingletonMonoBehaviour<SceneManagement>
{
    protected override void OnDestroy()
    {
        base.OnDestroy();

        StopAllCoroutines();
    }

    /// <summary>
    /// 載入場景
    /// </summary>
    public void LoadScene(SceneEnum sceneEnum, Action callback = null)
    {
        if (!Application.isPlaying)
            return;

        StartCoroutine(ILoadScene(sceneEnum, callback));
    }

    private IEnumerator ILoadScene(SceneEnum sceneEnum, Action callback)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync((int)sceneEnum);

        while (operation != null && !operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }

        if(AddressableManagement.Instance != null)
        {
            AddressableManagement.Instance.ClearAllSceneViews();
            AddressableManagement.Instance.SetCanvase();

            callback?.Invoke();
        }
    }
}
