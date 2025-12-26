using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class SceneManagement : SingletonMonoBehaviour<SceneManagement>
{
    /// <summary>
    /// 載入場景
    /// </summary>
    public void LoadScene(SceneEnum sceneEnum, Action callback = null)
    {
        StartCoroutine(ILoadScene(sceneEnum, callback));
    }

    private IEnumerator ILoadScene(SceneEnum sceneEnum, Action callback)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync((int)sceneEnum);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }

        AddressableManagement.Instance.ClearAllViews();
        AddressableManagement.Instance.SetCanvase();
        callback?.Invoke();
    }
}
