using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class CarWheelSystem : ComponentSystem
{
    private static BootStrap Bootstrap;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        Bootstrap = GameObject.FindObjectOfType<BootStrap>();
    }

    private struct Wheels
    {
        public CarWheelComponent CarWheel;
        public CarInputComponent CarInput;
        public Transform Transform;
    }
    private struct CarRearAxelGroup
    {
        public CarRearAxelComponent CarRearAxel;
        public CarInputComponent CarInput;
        public Transform Transform;
    }
    private struct CarFrontAxelGroup
    {
        public CarFrontAxelComponent CarFrontAxel;
        public CarInputComponent CarInput;
        public Transform Transform;
    }

    protected override void OnUpdate()
    {
        float DeltaTime = Time.deltaTime;


        foreach (var entity in GetEntities<CarFrontAxelGroup>())
        {
            //Y Axis: Rotate the wheels based on the Steering of the Car
            //if (entity.CarInput.instance.Gear == -1)
            //{
            //    entity.Transform.localRotation = Quaternion.Lerp(entity.Transform.localRotation, Quaternion.Euler(0, -entity.CarInput.instance.Horizontal, 0), Mathf.Abs(entity.CarInput.instance.Horizontal));
            //}
            //else
            //    entity.Transform.localRotation = Quaternion.Lerp(entity.Transform.localRotation, Quaternion.Euler(0, entity.CarInput.instance.Horizontal, 0), Mathf.Abs(entity.CarInput.instance.Horizontal));

            //entity.Transform.localRotation = Quaternion.Euler(0, entity.CarInput.instance.Horizontal, 0);

            //Z Axis: Steer all wheels based on Car Acceleration (FBX wheel importation Rotation correction)
            entity.Transform.localRotation = Quaternion.Euler(0, entity.CarInput.instance.Horizontal, 0);
        }

        if (Bootstrap.DriveType.Equals(DriveType.FourWheel))
        {
            foreach (var entity in GetEntities<CarRearAxelGroup>())
            {
                //if (entity.CarInput.instance.Gear == -1)
                //    entity.Transform.localRotation = Quaternion.Lerp(entity.Transform.localRotation, Quaternion.Euler(0, entity.CarInput.instance.Horizontal, 0), Mathf.Abs(entity.CarInput.instance.Horizontal));
                //
                ////entity.Transform.localRotation = Quaternion.Euler(0, entity.CarInput.instance.Horizontal, 0);
                //else
                //    entity.Transform.localRotation = Quaternion.Lerp(entity.Transform.localRotation, Quaternion.Euler(0, -entity.CarInput.instance.Horizontal, 0), Mathf.Abs(entity.CarInput.instance.Horizontal));

                //entity.Transform.localRotation = Quaternion.Euler(0, -entity.CarInput.instance.Horizontal, 0);

                //Z Axis: Steer all wheels based on Car Acceleration (FBX wheel importation Rotation correction)
                entity.Transform.localRotation = Quaternion.Euler(0, -entity.CarInput.instance.Horizontal, 0);

            }
        }
        else
        {
            foreach (var entity in GetEntities<CarRearAxelGroup>())
            {
                entity.Transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }

            foreach (var wheel in GetEntities<Wheels>())
        {
            //X Axis: Rotate all wheels based on Car Acceleration
            //wheel.Transform.rotation *= (Quaternion.Euler(wheel.CarInput.instance.Acceleration * .35f, 0, 0));

            //Y Axis: Rotate all wheels based on Car Acceleration (FBX wheel importation Rotation correction)
            wheel.Transform.rotation *= (Quaternion.Euler(0, wheel.CarInput.instance.Acceleration * .35f, 0));
            //Debug.Log(wheel.CarInput.instance.Vertical);
        }
    }

}