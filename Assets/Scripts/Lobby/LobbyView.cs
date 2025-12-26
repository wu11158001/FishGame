using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;

public class LobbyView : BasicView
{
    Action CloseAction;

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }

    private void Update()
    {
        if(Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            string docid = PlayerPrefs.GetString(PlayerPrefsKeys.USER_ACCOUNT);
            FirestoreManagement.Instance.DeleteDataFromFirestore(FirestoreCollectionName.AccountData, docid, null);
        }

        if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
        {
            string docid = PlayerPrefs.GetString(PlayerPrefsKeys.USER_ACCOUNT);
            AccountData data = new()
            {
                Coins = 1414
            };

            string json = JsonUtility.ToJson(data);

            FirestoreManagement.Instance.UpdateDataToFirestore(FirestoreCollectionName.AccountData, docid, json, null);
        }
    }
}
