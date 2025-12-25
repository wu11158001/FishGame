using UnityEngine;

public class Canvas_Global : SingletonMonoBehaviour<Canvas_Global>
{
    public Canvas GlobalCanvas { get; private set; }

    private void Start()
    {
        GlobalCanvas = GetComponent<Canvas>();
    }
}
