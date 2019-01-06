using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class CarRotateSystem : ComponentSystem
{
    private static BootStrap Bootstrap;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        Bootstrap = GameObject.FindObjectOfType<BootStrap>();
    }

    private struct MyGroup
    {
        public CarBodyComponent CarBody;
        public CarInputComponent CarInput;
        public Transform Transform;
        public Rigidbody Rigidbody;
    }

    private int layerMask = ~1 << 2;    //Everything BUT Layer 2
    private RaycastHit hit;
    float3 hitNormal;

    protected override void OnUpdate()
    {
        var DeltaTime = Time.deltaTime;
        foreach (var entity in GetEntities<MyGroup>())
        {
            if (entity.CarInput.instance.Gear != -1)
            {
                if (!Bootstrap.instance.DriveType.Equals(DriveType.FourWheel))
                {
                    entity.Transform.rotation *= (Quaternion.Euler(0, entity.CarInput.instance.Horizontal * 2 * DeltaTime, 0));
                }
                else if (Bootstrap.instance.DriveType.Equals(DriveType.FourWheel))
                {
                    entity.Transform.rotation *= (Quaternion.Euler(0, entity.CarInput.instance.Horizontal * 4 * DeltaTime, 0));
                }

            }
            else
            {
                if (Bootstrap.instance.DriveType.Equals(DriveType.TwoWheel))
                {
                    entity.Transform.rotation *= (Quaternion.Euler(0, -entity.CarInput.instance.Horizontal * 2 * DeltaTime, 0));
                }
                else if (Bootstrap.instance.DriveType.Equals(DriveType.FourWheel))
                {
                    entity.Transform.rotation *= (Quaternion.Euler(0, -entity.CarInput.instance.Horizontal * 4 * DeltaTime, 0));
                }

            }

            //if (entity.CarInput.instance.Acceleration > 0f && entity.CarInput.instance.Gear != -1)
            //{
            //    //Forward Accel, Forward Gear
            //    entity.Transform.rotation *= (Quaternion.Euler(0, entity.CarInput.instance.Horizontal * 2 * DeltaTime, 0));
            //}
            //else if (entity.CarInput.instance.Acceleration < 0f && entity.CarInput.instance.Gear == -1)
            //{
            //    //Backward Accel, Reverse Gear
            //    entity.Transform.rotation *= (Quaternion.Euler(0, -entity.CarInput.instance.Horizontal * 2 * DeltaTime, 0));
            //}
            //else if (entity.CarInput.instance.Acceleration > 0f && entity.CarInput.instance.Gear == -1)
            //{
            //    //Forward Accel, Reverse Gear
            //    entity.Transform.rotation *= (Quaternion.Euler(0, entity.CarInput.instance.Horizontal * 2 * DeltaTime, 0));
            //}
            //else if (entity.CarInput.instance.Acceleration < 0f && entity.CarInput.instance.Gear != -1)
            //{
            //    //Backward Accel, Forward Gear
            //    entity.Transform.rotation *= (Quaternion.Euler(0, -entity.CarInput.instance.Horizontal * 2 * DeltaTime, 0));
            //}
        }
    }

}