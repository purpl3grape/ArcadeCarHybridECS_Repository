using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;

public enum SpawnOptions
{
    PureECSSpawn,
    PureECSMultiSpawn,
    HybridECSSpawn
}

public class EntityBootstrap : MonoBehaviour
{
    public SpawnOptions spawnOptions;
    public bool multipleSpawn;
    public float Speed;
    public Mesh Mesh;
    public Material Material;
    public Material Material2;

    public GameObject HybridEntityPrefab;
    private GameObject hybridEntityObject;

    public int AddEntitiesCount = 10000; // how many entities we'll spawn on scene start
    public int DestroyEntitiesCount = 10000; // how many entities we'll spawn on scene start
    public static EntityArchetype archetype1 { get; private set; }
    public EntityManager entityManager;

    [HideInInspector] public int totalEntityCount = 0;

    // Use this for initialization
    void Start()
    {
        totalEntityCount = 0;
        entityManager = World.Active.GetOrCreateManager<EntityManager>();

        spawnOptions = SpawnOptions.PureECSSpawn;
        CreateArchetypes(entityManager);

    }

    private void Update()
    {
        if (spawnOptions.Equals(SpawnOptions.PureECSMultiSpawn))
        {
            if (Input.GetKeyDown(KeyCode.Equals))
                CreateMultipleEntities(entityManager, AddEntitiesCount);
        }
        else if (spawnOptions.Equals(SpawnOptions.PureECSSpawn))
        {
            if (Input.GetKeyDown(KeyCode.Equals))
                CreateEntities(entityManager);
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
            DestroyEntities(entityManager, DestroyEntitiesCount);

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (spawnOptions.Equals(SpawnOptions.PureECSSpawn))
            {
                spawnOptions = SpawnOptions.PureECSMultiSpawn;
            }
            else if (spawnOptions.Equals(SpawnOptions.PureECSMultiSpawn))
            {
                spawnOptions = SpawnOptions.HybridECSSpawn;
            }
            else if (spawnOptions.Equals(SpawnOptions.HybridECSSpawn))
            {
                spawnOptions = SpawnOptions.PureECSSpawn;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AnimateEntities_Jump(entityManager);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AnimateEntities_Run(entityManager);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
            AddEntitiesCount = AddEntitiesCount + 1000 < 30000 ? AddEntitiesCount + 1000 : AddEntitiesCount;
        if (Input.GetKeyDown(KeyCode.DownArrow))
            AddEntitiesCount = AddEntitiesCount - 1000 > 0 ? AddEntitiesCount - 1000 : AddEntitiesCount;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            DestroyEntitiesCount = DestroyEntitiesCount + 1000 < 30000 ? DestroyEntitiesCount + 1000 : DestroyEntitiesCount;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            DestroyEntitiesCount = DestroyEntitiesCount - 1000 > 0 ? DestroyEntitiesCount - 1000 : DestroyEntitiesCount;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.None)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;
            if (Cursor.visible == true)
                Cursor.visible = false;
            else
                Cursor.visible = true;
        }

    }

    private void CreateEntities(EntityManager entityManager)
    {
        for (int i = 0; i < AddEntitiesCount; i++)
        {
            var playerEntity = entityManager.CreateEntity(
            ComponentType.Create<BulletSpeed>(),
            ComponentType.Create<BulletDirection>(),
            ComponentType.Create<Position>(),
            //ComponentType.Create<Transform>(),
            ComponentType.Create<MeshInstanceRenderer>()
        );
            entityManager.SetComponentData(playerEntity, new BulletSpeed { Value = UnityEngine.Random.Range(-50f, 50f) });
            entityManager.SetComponentData(playerEntity, new BulletDirection { Value = new float3(0, 0, 1) });
            entityManager.SetSharedComponentData(playerEntity, new MeshInstanceRenderer
            {
                mesh = Mesh,
                material = Material
            });
        }

        totalEntityCount += AddEntitiesCount;
    }

    private void AnimateEntities_Run(EntityManager entityManager)
    {
        NativeArray<Entity> entities = entityManager.GetAllEntities(Allocator.Temp);

        if (entities.Length <= 0)
        {
            Debug.Log("No Entities to animate");
            entities.Dispose();
            return;
        }
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entityManager.HasComponent<MeshInstanceRenderer>(entities[i]))
            {
                continue;
            }
            if (!entityManager.HasComponent<BulletSpeed>(entities[i]))
            {
                continue;
            }
            BulletSpeed speed = entityManager.GetComponentData<BulletSpeed>(entities[i]);
            if (speed.Value > 2.5f)
            {
                MeshInstanceRenderer newmesh = entityManager.GetSharedComponentData<MeshInstanceRenderer>(entities[i]);
                newmesh.material = Material2;
                entityManager.SetSharedComponentData(entities[i], newmesh);
            }
        }
        entities.Dispose();
    }

    private void AnimateEntities_Jump(EntityManager entityManager)
    {
        NativeArray<Entity> entities = entityManager.GetAllEntities(Allocator.Temp);
        if (entities.Length <= 0)
        {
            Debug.Log("No Entities to animate");
            entities.Dispose();
            return;
        }
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entityManager.HasComponent<MeshInstanceRenderer>(entities[i]))
            {
                continue;
            }
            if (!entityManager.HasComponent<BulletSpeed>(entities[i]))
            {
                continue;
            }
            BulletSpeed speed = entityManager.GetComponentData<BulletSpeed>(entities[i]);
            if (speed.Value <= 2.5f)
            {
                MeshInstanceRenderer newmesh = entityManager.GetSharedComponentData<MeshInstanceRenderer>(entities[i]);
                newmesh.material = Material;
                entityManager.SetSharedComponentData(entities[i], newmesh);
            }
        }
        entities.Dispose();
    }


    private void CreateArchetypes(EntityManager em)
    {
        // ComponentType.Create<> is slightly more efficient than using typeof()
        // em.CreateArchetype(typeof(Position), typeof(Heading), typeof(Health), typeof(MoveSpeed));
        var pos = ComponentType.Create<Position>();
        var moveSpeed = ComponentType.Create<BulletSpeed>();
        var playerInput = ComponentType.Create<BulletDirection>();
        //var tr = ComponentType.Create<Transform>();
        var meshInstanceRenderer = ComponentType.Create<MeshInstanceRenderer>();
        var collider = ComponentType.Create<Collider>();
        var rBody = ComponentType.Create<Rigidbody>();
        archetype1 = em.CreateArchetype(pos, moveSpeed, playerInput, meshInstanceRenderer, collider, rBody);
        // that's exactly how you set your entities archetype, it's like LEGO
    }

    private void CreateMultipleEntities(EntityManager em, int count)
    {
        // if you spawn more entities, it's more performant to do it with NativeArray
        // if you want to spawn just one entity, do:
        // var entity = em.CreateEntity(archetype1);

        NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.Temp);
        em.CreateEntity(archetype1, entities); // Spawns entities and attach to them all components from archetype1
                                               // If we don't set components, their values will be default
        for (int i = 0; i < count; i++)
        {
            // Heading is build in Unity component and you need to set it
            // because default is float3(0, 0, 0), which is position
            // where you can't look towards, so you'll get error from TransformSystem.
            em.SetComponentData(entities[i], new BulletSpeed { Value = Speed });
            em.SetComponentData(entities[i], new BulletDirection { Value = new float3(0, 0, 1) });
            em.SetSharedComponentData(entities[i], new MeshInstanceRenderer
            {
                mesh = Mesh,
                material = Material
            });

        }
        Debug.Log("Created " + count + " Entities");
        entities.Dispose(); // all NativeArrays you need to dispose manually, it won't destroy our entities, just dispose not used anymore array
                            // that's it, entities exists in world and are ready to be injected into systems

        totalEntityCount += count;
    }

    private void DestroyEntities(EntityManager em, int count)
    {
        // if you spawn more entities, it's more performant to do it with NativeArray
        // if you want to spawn just one entity, do:
        // var entity = em.CreateEntity(archetype1);

        //NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.Temp);
        NativeArray<Entity> entities = em.GetAllEntities(Allocator.Temp);
        if (entities.Length <= 0)
        {
            Debug.Log("No more Entities to destroy");
            entities.Dispose();
            return;
        }

        if (count > entities.Length)
        {
            count = entities.Length;
        }

        for (int i = 0; i < count; i++)
        {
            if (!em.HasComponent<MeshInstanceRenderer>(entities[i]))
            {
                totalEntityCount += 1;
                continue;
            }
            if (!em.HasComponent<BulletSpeed>(entities[i]))
            {
                totalEntityCount += 1;
                continue;
            }

            em.DestroyEntity(entities[i]);
        }
        Debug.Log("Destroyed " + count + " Entities");
        entities.Dispose(); // all NativeArrays you need to dispose manually, it won't destroy our entities, just dispose not used anymore array
                            // that's it, entities exists in world and are ready to be injected into systems
        totalEntityCount -= count;
    }

}