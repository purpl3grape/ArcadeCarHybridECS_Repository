using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Entities;

[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class FollowCarSystem : MonoBehaviour
{

    private NativeArray<Vector3> velocities;
    private NativeArray<Vector3> directions;
    private NativeArray<Quaternion> rotations;
    private NativeArray<Vector3> positions;
    private NativeArray<Matrix4x4> renderMatrices;
    private Matrix4x4[] renderMatrixArray;
    private NativeArray<int> sleepingTimer;
    private NativeQueue<int> asleep;
    private Vector3 gravity;
    private Vector3 carPos;
    private int objectCount = 1023; // the most we can fit into a single call to Graphics.DrawInstance
    [SerializeField] private bool isFiringProjectile = false;
    [Range(30, 120)] public int PhysicsFramesPerSecond;

    public Mesh mesh;
    public Material material;
    public Transform carTransform;

    public float ChaseTargetSpeed;

    struct GravityJob : IJobParallelFor
    {
        public float DeltaTime;

        public Vector3 Gravity;
        public NativeArray<Vector3> Positions;
        public NativeArray<Vector3> Velocities;
        public NativeArray<int> Sleeping;
        public Vector3 CarPos;

        float currentSpeed;
        Vector3 headingVector;

        public void Execute(int i)
        {
            // has the object been marked as sleeping?
            // sleeping objects are object that have settled, so they dont
            // move at all in this case we skip adding gravity to make them
            // appear more stable
            if (Sleeping[i] > 0)
            {
                Velocities[i] = new Vector3(0, 0, 0);
                return;
            }

            headingVector = (CarPos - Positions[i]).normalized * DeltaTime;
            
            currentSpeed = Velocities[i].magnitude; 

            // simply integrate gravity based on time since last step.
            Velocities[i] += (Gravity) * DeltaTime;
            Velocities[i] = new Vector3(Velocities[i].x + headingVector.x, Velocities[i].y, Velocities[i].z + headingVector.z);
            
        }
    }

    // this job creates a batch of RaycastCommands we are going to use for
    // collision detection against the world. these can be sent to PhysX
    // as a batch that will be executed in a job, rather than us having to
    // call Physics.Raycast in a loop just on the main thread!
    struct PrepareRaycastCommands : IJobParallelFor
    {
        public float DeltaTime;

        public NativeArray<RaycastCommand> Raycasts;
        [ReadOnly]
        public NativeArray<Vector3> Velocities;
        [ReadOnly]
        public NativeArray<Vector3> Positions;


        public void Execute(int i)
        {
            // figure out how far the object we are testing collision for
            // wants to move in total. Our collision raycast only needs to be
            // that far.
            float distance = (Velocities[i] * DeltaTime).magnitude * 10;
            Raycasts[i] = new RaycastCommand(Positions[i], Velocities[i], distance);
        }
    }

    // Integrate the velocity into all the objects positions. We use the
    // Raycast hit data to decide how much of the velocity to integrate -
    // we dont want to tunnel though the colliders in the scene!
    struct IntegratePhysics : IJobParallelFor
    {
        public float DeltaTime;
        [ReadOnly]
        public NativeArray<Vector3> Velocities;
        [ReadOnly]
        public NativeArray<RaycastHit> Hits;
        public NativeArray<int> Sleeping;
        public NativeArray<Vector3> Positions;
        public NativeArray<Quaternion> Rotations;
        public Vector3 CarPos;
        public float ChaseTargetSpeed;

        public void Execute(int i)
        {
            if (Sleeping[i] > 0) // if the object is sleeping, we dont have to integrate anything
            {
                Sleeping[i]++;
                return;
            }

            Vector3 tempCarPos = CarPos;
            tempCarPos.y = 0;
            Vector3 tempPos = Positions[i];
            tempPos.y = 0;

            Rotations[i] = Quaternion.LookRotation(tempCarPos - tempPos, Vector3.up);

            if (Hits[i].normal == Vector3.zero)
            {
                // if there has been no colision, intergrate all of the velocity we want for this frame
                Positions[i] += Velocities[i] * (Velocities[i] * DeltaTime).magnitude;// + (CarPos - Positions[i]).normalized * ChaseTargetSpeed * DeltaTime;
            }
            else
            {
                // there has been a collision! just move up to the point of the collion with a tiny offset for stability
                Positions[i] = Hits[i].point + new Vector3(0, .1f, 0);// + (CarPos - Positions[i]).normalized * DeltaTime;
            }
        }
    }

    // Respond to collisions
    struct CalculateCollisionResponse : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<RaycastHit> Hits;

        public NativeArray<Vector3> Velocities;
        public NativeArray<int> Sleeping;
        public float DeltaTime;
        public void Execute(int i)
        {
            // If there has been a collision, the RaycastHit normal will be
            // non zero. When we know there has been a collisoin we can
            // respond to it.
            if (Hits[i].normal != Vector3.zero)
            {
                // first, lets check if the velocity has got very low before this collision. if so
                // we are going to put the object to sleep. this will stop it from
                // reciveing gravity and will make the simulation appear more stable
                if (Velocities[i].magnitude <= 2f)
                {
                    Velocities[i] = Vector3.zero;
                    Sleeping[i]++; // increment the sleeping counter. any value over 1 is sleeping. We can use this to respawn objects after they have been static for while
                }
                // lets implement a very simple bounce effect - just be reflecting the velocity by the
                // collison normal (the normal is a vector pointing perpendicular away from the surface
                // that we have hit).
                // were also going to apply a damping effect to the velocity by removing force from the resulting
                // reflected velocity. You can think of this as a really simple simulation of force lost
                // due to friction against the surface we have collided with.
                else
                {
                    Velocities[i] = Vector3.Reflect(Velocities[i], Hits[i].normal);

                    var angleBetweenNormalAndUp = Vector3.Angle(Hits[i].normal, Vector3.up); // returns a value between 0-180
                    var lerp = angleBetweenNormalAndUp / 180f;
                    Velocities[i] = Velocities[i] * Mathf.Lerp(.5f, 1f, lerp * DeltaTime);
                }
            }
        }
    }

    // Calculate and store a matrix we can use to draw each object. As we dont support rotation in this demo, we pretty
    // much only supply the position. A fixed scale is also applied to make sure the meshes we draw arnt giant.
    struct CalculateDrawMatricies : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> Positions;
        [ReadOnly]
        public NativeArray<Quaternion> Rotations;
        public NativeArray<Matrix4x4> renderMatrices;

        public void Execute(int i)
        {
            renderMatrices[i] = Matrix4x4.TRS(Positions[i] + new Vector3(0f, 5/3f, 0f), Rotations[i], new Vector3(1f, 1f, 1f)/3);
        }
    }

    struct FindSleepingObjects : IJobParallelFor
    {
        public NativeQueue<int>.Concurrent SleepQueue;
        [ReadOnly]
        public NativeArray<int> Sleeping;

        public void Execute(int index)
        {
            if (Sleeping[index] > 15)
            {
                SleepQueue.Enqueue(index);
            }
        }
    }





    private void Respawn(int i)
    {
        // Random cannot be used from jobs so this always has to execute on the main thread.
        //velocities[i] = (Random.onUnitSphere + (((carTransform.position - transform.position).normalized).normalized) * 1.3f).normalized * Random.Range(2f, 10f); // spawn with velocities arching towards a target object
        //velocities[i] = new Vector3(Random.onUnitSphere.x - carTransform.position.x, Mathf.Abs(Random.onUnitSphere.x), Random.onUnitSphere.z - carTransform.position.z).normalized * Random.Range(5, 10);
        //velocities[i] = new Vector3(carTransform.position.x - Random.onUnitSphere.x, Mathf.Abs(Random.onUnitSphere.x), carTransform.position.z - Random.onUnitSphere.z).normalized * Random.Range(5, 10);
        velocities[i] = new Vector3(carTransform.position.x - Random.onUnitSphere.x, 0, carTransform.position.z - Random.onUnitSphere.z).normalized * Random.Range(5, 10);
        positions[i] = transform.position;
        sleepingTimer[i] = 0;
        renderMatrices[i] = Matrix4x4.identity;
    }

    private void Init(int i)
    {
        // Random cannot be used from jobs so this always has to execute on the main thread.
        //velocities[i] = (Random.onUnitSphere + (((carTransform.position - transform.position).normalized).normalized) * 1.3f).normalized * Random.Range(2f, 10f); // spawn with velocities arching towards a target object
        //velocities[i] = new Vector3(carTransform.position.x - Random.onUnitSphere.x, Mathf.Abs(Random.onUnitSphere.x), carTransform.position.z - Random.onUnitSphere.z).normalized * Random.Range(5, 10);
        velocities[i] = new Vector3(carTransform.position.x - Random.onUnitSphere.x, 0, carTransform.position.z - Random.onUnitSphere.z).normalized * Random.Range(5, 10);
        positions[i] = transform.position;
        sleepingTimer[i] = 0;
        renderMatrices[i] = Matrix4x4.identity;
    }


    private struct Group
    {
        public CarInputComponent CarInput;
    }

    private void Update()
    {
        ProjectileUpdate();
    }

    void ProjectileUpdate()
    {
        // only do an update if the data has been initialized
        if (!positions.IsCreated)
            return;
        if (!carTransform) return;

        carPos = carTransform.position;

        var deltaTime = Time.deltaTime;

        // FORCE ACCUMILATION

        // First off lets apply gravity to all the object in our scene that arnt currently asleep
        var gravityJob = new GravityJob()
        {
            DeltaTime = deltaTime,
            Gravity = gravity,
            Velocities = velocities,
            Sleeping = sleepingTimer,
            Positions = positions,
            CarPos = carPos,
        };
        var gravityDependency = gravityJob.Schedule(objectCount, 64);

        // TODO accumilate other forces here! gravity is just the simplest force to apply to our objects,
        // but theres no limit on the kind of forces we can simulate. We could add friction, air resistnace,
        // constant acceleration, joints and constrainsts, boyancy.. anything we can think of that can effect
        // the velocity of an object can have its own job scheduled here.

        // INTERGRATION AND COLLISION

        // Create temporary arrays for the raycast info. Lets use the TempJob allocator,
        // this means we have to dispose them when the job has finished - its short lived data
        var raycastCommands = new NativeArray<RaycastCommand>(objectCount, Allocator.TempJob);
        var raycastHits = new NativeArray<RaycastHit>(objectCount, Allocator.TempJob);

        // Lets schedule jobs to do a collision raycast for each object. One job Prepare    s all the raycast commands,
        // the second actually does the raycasts.
        var setupRaycastsJob = new PrepareRaycastCommands()
        {
            DeltaTime = deltaTime,
            Positions = positions,
            Raycasts = raycastCommands,
            Velocities = velocities
        };

        var setupDependency = setupRaycastsJob.Schedule(objectCount, 64, gravityDependency);

        var raycastDependency = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 64, setupDependency);

        // Now we know if there is a collision along our velocity vector, its time to integrate the velocity into
        // our objects for the current timeset.
        var integrateJob = new IntegratePhysics()
        {
            DeltaTime = deltaTime,
            Positions = positions,
            Velocities = velocities,
            Sleeping = sleepingTimer,
            Hits = raycastHits,
            Rotations = rotations,
            CarPos = carPos,
            ChaseTargetSpeed = ChaseTargetSpeed,
        };
        var integrateDependency = integrateJob.Schedule(objectCount, 64, raycastDependency);

        // finally, respond to any collisions that happened in the lsat update step.
        var collisionResponeJob = new CalculateCollisionResponse()
        {
            Hits = raycastHits,
            Velocities = velocities,
            Sleeping = sleepingTimer,
            DeltaTime = deltaTime,
        };
        var collisionDependency = collisionResponeJob.Schedule(objectCount, 64, integrateDependency);

        // Now the physics is done, we need to create a drawing matrix for every object. This simple demo dosnt
        // implment roation, so only the translation values in the matrix reallllly matter.
        var renderMatrixJob = new CalculateDrawMatricies()
        {
            Positions = positions,
            Rotations = rotations,
            renderMatrices = renderMatrices
        };
        var matrixDependency = renderMatrixJob.Schedule(objectCount, 64, collisionDependency);

        // All the jobs we want to execute have been scheduled! By calling .Complete() on the last job in the
        // chain, Unity makes the main thread help out with scheduled jobs untill they are all complete. 
        // then we can move on and use the data caluclated in the jobs safely, without worry about data being changed
        // by other threads as we try to use it - we *know* all the work is done
        matrixDependency.Complete();


        foreach (RaycastHit raycasthit in raycastHits)
        {
            if (raycasthit.transform == null)
                continue;
            if (raycasthit.transform.CompareTag("Player"))
            {
            //    raycasthit.transform.GetComponent<CarComponent>().instance.Health -= 1;
            }
        }

        // make sure we dispose of the temporary NativeArrays we used for raycasting
        raycastCommands.Dispose();
        raycastHits.Dispose();

        // lets schedule a job to figure out which objects are sleeping - this can run in the background whilst
        // we dispach the drawing commands for this frame.
        var sleepJob = new FindSleepingObjects()
        {
            Sleeping = sleepingTimer,
            SleepQueue = asleep.ToConcurrent()
        };
        var sleepDependancy = sleepJob.Schedule(objectCount, 64, matrixDependency);

        //  lets actually issue a draw!
        renderMatrices.CopyTo(renderMatrixArray); // copy to a preallocated array, we would get garbage from ToArray()
        Graphics.DrawMeshInstanced(mesh, 0, material, renderMatrixArray);


        sleepDependancy.Complete();
        for (int i = asleep.Count; i != 0; i--)
        {
            int index = asleep.Dequeue();
            Respawn(index);
        }
    }


    IEnumerator BeginCoroutine;
    IEnumerator Begin()
    {
        // lets wait for half a second before we set off the system -- just to avoid any stutters
        yield return new WaitForSeconds(.5f);

        // lets define an rough approximation for gravity
        gravity = new Vector3(0, -4.5f, 0) / 5;

        carTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // and set up all our NativeArrays with initial values
        directions = new NativeArray<Vector3>(objectCount, Allocator.Persistent);
        rotations = new NativeArray<Quaternion>(objectCount, Allocator.Persistent);
        velocities = new NativeArray<Vector3>(objectCount, Allocator.Persistent);
        positions = new NativeArray<Vector3>(objectCount, Allocator.Persistent);
        sleepingTimer = new NativeArray<int>(objectCount, Allocator.Persistent);
        renderMatrices = new NativeArray<Matrix4x4>(objectCount, Allocator.Persistent);
        renderMatrixArray = new Matrix4x4[objectCount];
        asleep = new NativeQueue<int>(Allocator.Persistent);

        for (int i = 0; i < objectCount; i++)
        {
            Init(i);
        }
    }

    private void OnEnable()
    {
        if (BeginCoroutine != null)
        {
            StopCoroutine(BeginCoroutine);
        }
        BeginCoroutine = Begin();
        StartCoroutine(BeginCoroutine);
    }

    private void OnDisable()
    {
        directions.Dispose();
        rotations.Dispose();
        velocities.Dispose();
        positions.Dispose();
        renderMatrices.Dispose();
        sleepingTimer.Dispose();
        asleep.Dispose();


    }
}
