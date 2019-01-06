using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class GameHUDSystem : ComponentSystem
{
    private static BootStrap BootStrap;
    private static GameObject CarObject;
    private static GameObject GameHUD;
    private static Text CarVelocityText;
    private static Text DesiredVelocityText;
    private static Text FPSText;
    private static Text InputTypeText;
    private static Text DriveTypeText;
    private static Text GearText;
    private static Text CarHealthText;
    private static RawImage CarMirrorRawImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        BootStrap = GameObject.FindObjectOfType<BootStrap>();
        CarObject = BootStrap.hybridEntityObject;
        GameHUD = GameObject.FindGameObjectWithTag("UI");
        CarVelocityText = GameHUD.GetComponent<UIElements>().CarVelocityText;
        DesiredVelocityText = GameHUD.GetComponent<UIElements>().DeiredVelocityText;
        FPSText = GameHUD.GetComponent<UIElements>().FPSText;
        InputTypeText = GameHUD.GetComponent<UIElements>().InputTypeText;
        DriveTypeText = GameHUD.GetComponent<UIElements>().DriveTypeText;
        GearText = GameHUD.GetComponent<UIElements>().GearText;
        CarHealthText = GameHUD.GetComponent<UIElements>().CarHealthText;
        CarMirrorRawImage = GameHUD.GetComponent<UIElements>().CarMirrorRawImage;
    }


    private struct CarMirror
    {
        public CarMirrorCameraComponent CarMirrorCamera;
        public Camera Camera;
    }


    protected override void OnUpdate()
    {
        float DeltaTime = Time.deltaTime;

        float msec = DeltaTime * 1000.0f;
        float fps = 1.0f / DeltaTime;

        CarVelocityText.text = "Car Velocity: " + (Mathf.RoundToInt(CarObject.GetComponent<Rigidbody>().velocity.magnitude)).ToString();
        DesiredVelocityText.text = "Desired Velocity: " + (Mathf.RoundToInt(CarObject.GetComponent<CarInputComponent>().instance.Acceleration)).ToString();
        FPSText.text = "FPS: " + string.Format("\t{0:0.0} ms ({1:0.} fps)", msec, fps);
        InputTypeText.text = "INPUT: " + BootStrap.instance.InputType.ToString();
        DriveTypeText.text = "DRIVE: " + BootStrap.instance.DriveType.ToString();
        GearText.text = "GEAR: " + CarObject.GetComponent<CarInputComponent>().instance.Gear.ToString();
        CarHealthText.text = "HP: " + (Mathf.RoundToInt(CarObject.GetComponent<CarComponent>().instance.Health)).ToString();

        foreach (var entity in GetEntities<CarMirror>())
        {
            if (Input.GetKey(KeyCode.Tab))
            {
                if (BootStrap.RearDisplay.Equals(RearDisplay.Disabled))
                {
                    BootStrap.RearDisplay = RearDisplay.Enabled;
                    CarMirrorRawImage.enabled = true;
                    entity.Camera.enabled = true;
                }
            }
            else
            {
                if (BootStrap.RearDisplay.Equals(RearDisplay.Enabled))
                {
                    BootStrap.RearDisplay = RearDisplay.Disabled;
                    CarMirrorRawImage.enabled = false;
                    entity.Camera.enabled = false;
                }
            }

        }


    }
}