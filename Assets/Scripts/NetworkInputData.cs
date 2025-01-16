using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction; // เก็บข้อมูลการเคลื่อนที่ (WASD)
    //public float rotationInput; // เก็บข้อมูลการหมุน (Mouse X)
    public const byte MouseButton0 = 1;
    public NetworkButtons buttons;
}
