using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;



//[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
//public class FollowCarJobs : JobComponentSystem
//{
//    private static BootStrap Bootstrap;
//
//    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
//    public static void InitWithScene()
//    {
//        Bootstrap = GameObject.FindObjectOfType<BootStrap>();
//    }
//
//    private struct PlayerMaterialJob : IJobProcessComponentData<PlayerMaterial1, MeshInstanceRenderer>
//    {
//        public float DeltaTime;
//        public Material material1;
//        public Material material2;
//        public void Execute(ref PlayerMaterial1 playerMaterial ref MeshInstanceRenderer mesh)
//        {
//            material1 = playerMaterial.material1;
//            material2 = playerMaterial.material2;
//    
//            //position.Value.x += speed.Value * input.Horizontal * DeltaTime;
//            //position.Value.z += speed.Value * input.Vertical * DeltaTime;
//        }
//    }
//
//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        PlayerMaterialJob job = new PlayerMaterialJob
//        return job.Schedule(this, inputDeps);
//    }
//
//
//}

