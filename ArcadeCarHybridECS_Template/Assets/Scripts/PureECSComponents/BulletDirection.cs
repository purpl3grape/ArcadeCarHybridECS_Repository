using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct BulletDirection : IComponentData
{
    public float3 Value;
}
