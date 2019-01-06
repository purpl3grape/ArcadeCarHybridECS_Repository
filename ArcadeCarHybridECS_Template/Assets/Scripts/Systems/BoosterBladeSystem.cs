using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class BoosterBladeSystem : ComponentSystem
{
    private static BootStrap Bootstrap;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        Bootstrap = GameObject.FindObjectOfType<BootStrap>();
    }

    private struct BoosterBlade
    {
        public CarBoosterBladeComponent CarBoosterBlade;
        public CarInputComponent CarInput;
        public Transform Transform;
    }

    protected override void OnUpdate()
    {
        float DeltaTime = Time.deltaTime;

        foreach (var blade in GetEntities<BoosterBlade>())
        {
            //Y Axis: Rotate all wheels based on Car Acceleration (FBX wheel importation Rotation correction)
            blade.Transform.rotation *= (Quaternion.Euler(0, blade.CarInput.instance.Acceleration * .7f + 5f, 0));
        }
    }

}