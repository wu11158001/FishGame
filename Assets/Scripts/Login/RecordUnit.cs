using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RecordUnit : MonoBehaviour
{
    [SerializeField] Button MainBtn;
    [SerializeField] TextMeshProUGUI RecordUnitText;
    [SerializeField] GameObject Line;

    Action ClickAction;

    private void Start()
    {
        MainBtn.onClick.AddListener(() => { ClickAction?.Invoke(); });
    }

    public void SetData(string account, bool isFinal, Action clickAction)
    {
        ClickAction = clickAction;

        RecordUnitText.text = account;
        Line.SetActive(!isFinal);
    }
}
