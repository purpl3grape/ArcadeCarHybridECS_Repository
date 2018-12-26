using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;

public class BulletMovementSystem : JobComponentSystem
{
    ComponentGroup allBullets;
    static GameObject CarObj;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        CarObj = GameObject.FindGameObjectWithTag("Player");
    }

    protected void OnBeforeCreateManagerInternal(World world, int capacity)
    {
        allBullets = GetComponentGroup(typeof(Position), typeof(BulletDirection), typeof(BulletSpeed));
    }


    private struct BulletGroup
    {
        public ComponentDataArray<BulletSpeed> BulletSpeed;
        public ComponentDataArray<BulletDirection> BulletDirection;
        public ComponentDataArray<Position> BulletPosition;
        public EntityArray BulletEntityArray;
        public readonly int Length;
        public readonly int GroupIndex;
    }

    [Inject] BulletGroup bulletGroup;

    //BASIC JOB
    [Unity.Burst.BurstCompile]
    private struct BulletMovementJob : IJobProcessComponentData<BulletSpeed, BulletDirection, Position>
    {
        public float DeltaTime;

        public void Execute(ref BulletSpeed speed, ref BulletDirection direction, ref Position position)
        {
            position.Value.x += speed.Value * direction.Value.x * DeltaTime;
            position.Value.y += speed.Value * direction.Value.y * DeltaTime;
            position.Value.z += speed.Value * direction.Value.z * DeltaTime;
        }
    }

    //PARALLEL JOBS
    private struct BulletMovementParallelJob : IJobParallelFor
    {
        //This is how to get Component Data from entities
        public float DeltaTime;
        public ComponentDataArray<Position> bulletPositions;
        public ComponentDataArray<BulletDirection> bulletDirections;
        public ComponentDataArray<BulletSpeed> bulletSpeeds;
        [ReadOnly] public Vector3 carPosition;
        public float randomSpeed;

        public void Execute(int i)
        {
            float3 carPos = new float3(carPosition.x, bulletPositions[i].Value.y, carPosition.z);
            float3 difference = math.normalize(carPos - bulletPositions[i].Value);
            float3 newPosition = bulletPositions[i].Value + difference * randomSpeed * DeltaTime;
            bulletSpeeds[i] = new BulletSpeed { Value = randomSpeed };
            bulletDirections[i] = new BulletDirection { Value = difference };
            bulletPositions[i] = new Position { Value = newPosition };

        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        //BulletMovementJob job = new BulletMovementJob
        //{
        //    DeltaTime = Time.deltaTime,
        //};
        //return job.Schedule(this, inputDeps);

        if (allBullets == null)
        {
            allBullets = GetComponentGroup(typeof(Position), typeof(BulletDirection), typeof(BulletSpeed));
            //Debug.Log("Getting Car " + CarObj.name);
            //Debug.Log("Getting All Bullets " + allBullets.CalculateLength());
        }

        var positions = allBullets.GetComponentDataArray<Position>();
        var directions = allBullets.GetComponentDataArray<BulletDirection>();
        var speeds = allBullets.GetComponentDataArray<BulletSpeed>();
        //Debug.Log("Position 0: " + positions[0]);

        var steerJob = new BulletMovementParallelJob
        {
            DeltaTime = Time.deltaTime,
            bulletPositions = positions,
            bulletDirections = directions,
            bulletSpeeds = speeds,
            carPosition = CarObj.transform.position,
            randomSpeed = UnityEngine.Random.Range(2, 10)
        };

        var steerJobHandle = steerJob.Schedule(allBullets.CalculateLength(), 64);
        steerJobHandle.Complete();

        inputDeps = steerJobHandle;
        return inputDeps;

    }

}