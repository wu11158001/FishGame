using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MousePosition;
    public NetworkBool IsFirePressed;
}
