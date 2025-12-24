using UnityEngine;
using System;

public class LoginView : BasicView
{
    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }
}
