using UnityEngine;
using Unity.NetCode;

public struct SimpleRPC : IRpcCommand
{
    public int Value;
}
