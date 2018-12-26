using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct BulletPosition : IComponentData
{
    public float3 Value;
}
