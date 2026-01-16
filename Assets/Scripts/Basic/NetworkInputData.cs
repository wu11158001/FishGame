using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MousePosition;
    public NetworkButtons Buttons;

    public const int MOUSE_LEFT = 0;
}
