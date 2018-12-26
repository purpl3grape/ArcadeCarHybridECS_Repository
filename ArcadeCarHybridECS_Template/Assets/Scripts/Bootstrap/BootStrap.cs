using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public enum InputType
{
    KeyboardAndMouse,
    LogitechG920Wheel
}

public enum DriveType
{
    TwoWheel,
    FourWheel,
    Drift,
}

public class BootStrap : MonoBehaviour
{
    public GameObject HybridEntityPrefab;
    [HideInInspector] public GameObject hybridEntityObject;

    [Range(30, 120)] public int PhysicsFramesPerSecond;
    public InputType InputType;
    public DriveType DriveType;
    public BootStrap instance;

    public Mesh mesh;
    public Material material;

    void Awake()
    {
        SpawnCar();
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void SpawnCar()
    {
        hybridEntityObject = GameObject.Instantiate(HybridEntityPrefab, transform.position, Quaternion.identity);
        hybridEntityObject.GetComponent<CarComponent>().instance.Health = 100;
    }

    private void RespawnCar()
    {
        hybridEntityObject.transform.position = new Vector3(100, 0.5f, 100);
        hybridEntityObject.transform.rotation = Quaternion.identity;
        hybridEntityObject.GetComponent<CarComponent>().instance.Health = 100;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (InputType.Equals(InputType.KeyboardAndMouse))
            {
                InputType = InputType.LogitechG920Wheel;
            }
            else
            {
                InputType = InputType.KeyboardAndMouse;
            }
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (DriveType.Equals(DriveType.TwoWheel))
            {
                DriveType = DriveType.FourWheel;
            }
            else if (DriveType.Equals(DriveType.FourWheel))
            {
                DriveType = DriveType.Drift;
            }
            else if (DriveType.Equals(DriveType.Drift))
            {
                DriveType = DriveType.TwoWheel;
            }
        }

        if (hybridEntityObject.GetComponent<CarComponent>().instance.Health <= 0)
        {
            RespawnCar();
        }

    }
}