using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

public enum InputRange
{
    Zero_One,
    MinusOne_Zero,
    MinusOne_One,
}


[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class CarInputSystem : ComponentSystem
{

    public static Dictionary<int, int> GearSpeedRatio = new Dictionary<int, int>();
    private static BootStrap Bootstrap;
    private int _gear = 1;

    private struct Group
    {
        public CarInputComponent CarInput;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        Bootstrap = GameObject.FindObjectOfType<BootStrap>();
        GearSpeedRatio.Add(-1, 60);
        GearSpeedRatio.Add(1, 20);
        GearSpeedRatio.Add(2, 30);
        GearSpeedRatio.Add(3, 40);
        GearSpeedRatio.Add(4, 50);
        GearSpeedRatio.Add(5, 60);
    }

    protected override void OnUpdate()
    {
        float DeltaTime = Time.deltaTime;

        var _horizontal = Input.GetAxis("SteeringWheel") * 45;
        var _vertical = Input.GetAxis("GasPedal");
        var _break = Input.GetAxis("BreakPedal");
        var _clutch = Input.GetAxis("ClutchPedal");

            if (Input.GetButton("Gear1Shifter"))
            {
                _gear = 1;
            }
            else if (Input.GetButton("Gear2Shifter"))
            {
                _gear = 2;
            }
            else if (Input.GetButton("Gear3Shifter"))
            {
                _gear = 3;
            }
            else if (Input.GetButton("Gear4Shifter"))
            {
                _gear = 4;
            }
            else if (Input.GetButton("Gear5Shifter"))
            {
                _gear = 5;
            }
            else if (Input.GetButton("ReverseShifter"))
            {
                _gear = -1;
            }

        if (Bootstrap.instance.InputType == InputType.KeyboardAndMouse)
        {
            _horizontal = Input.GetAxis("SteeringInput") * 45;
            _vertical = Input.GetAxis("GasInput");
            _break = Input.GetAxis("BreakInput");
            _clutch = Input.GetAxis("ClutchInput");

            if (Input.GetButton("Gear1Input"))
            {
                _gear = 1;
            }
            else if (Input.GetButton("Gear2Input"))
            {
                _gear = 2;
            }
            else if (Input.GetButton("Gear3Input"))
            {
                _gear = 3;
            }
            else if (Input.GetButton("Gear4Input"))
            {
                _gear = 4;
            }
            else if (Input.GetButton("Gear5Input"))
            {
                _gear = 5;
            }
            else if (Input.GetButton("ReverseInput"))
            {
                _gear = -1;
            }
        }

        var _aimTurretUp = Input.GetKey(KeyCode.UpArrow);
        var _aimTurretDown = Input.GetKey(KeyCode.DownArrow);
        //var _fire = Input.GetKeyDown(KeyCode.Space);


        _vertical = Bootstrap.instance.InputType == InputType.LogitechG920Wheel ? NormalizeInput(_vertical, InputRange.MinusOne_One) : NormalizeInput(_vertical, InputRange.Zero_One);
        _break = Bootstrap.instance.InputType == InputType.LogitechG920Wheel ? NormalizeInput(_break, InputRange.MinusOne_One) : NormalizeInput(_break, InputRange.Zero_One);
        _clutch = Bootstrap.instance.InputType == InputType.LogitechG920Wheel ? NormalizeInput(_clutch, InputRange.MinusOne_One) : NormalizeInput(_clutch, InputRange.Zero_One);

        foreach (var entity in GetEntities<Group>())
        {
            /** MOVEMENT INPUTS [Accelerate / Break / Reverse] 
             */
            //Normalizing Inputs if necessary.

            entity.CarInput.instance.Horizontal = _horizontal;
            entity.CarInput.instance.Vertical = _vertical;
            entity.CarInput.instance.Break = _break;
            //Set Gears
            entity.CarInput.instance.Gear = _gear;

            if (_vertical > 0)
            {
                //Apply Acceleration Force [Forward / Reverse]
                if (entity.CarInput.instance.Gear == -1)
                    entity.CarInput.instance.Acceleration = entity.CarInput.instance.Acceleration > -GearSpeedRatio[entity.CarInput.instance.Gear] ? entity.CarInput.instance.Acceleration -= (5 * _vertical * DeltaTime) : -GearSpeedRatio[entity.CarInput.instance.Gear];
                else
                    entity.CarInput.instance.Acceleration = entity.CarInput.instance.Acceleration < GearSpeedRatio[entity.CarInput.instance.Gear] ? entity.CarInput.instance.Acceleration += (5 * _vertical * DeltaTime) : GearSpeedRatio[entity.CarInput.instance.Gear];
            }
            else
            {
                //Apply Break Force [Forward / Reverse / Idle]
                if (entity.CarInput.instance.Acceleration > 0)
                    entity.CarInput.instance.Acceleration = entity.CarInput.instance.Break > 0 ? entity.CarInput.instance.Acceleration -= 20 * _break * DeltaTime : entity.CarInput.instance.Acceleration -= 4 * DeltaTime;
                else if (entity.CarInput.instance.Acceleration < 0)
                    entity.CarInput.instance.Acceleration = entity.CarInput.instance.Break > 0 ? entity.CarInput.instance.Acceleration += 20 * _break * DeltaTime : entity.CarInput.instance.Acceleration += 4 * DeltaTime;
                else
                    entity.CarInput.instance.Acceleration = 0;

            }



            /** COMBAT INPUTS [Turret Aim Up / Turret Aim Down / Turret Fire] 
             */

            //Apply Turret Aim
            if (_aimTurretUp)
                entity.CarInput.instance.TurretRotY += 100 * DeltaTime;
            else if (_aimTurretDown)
                entity.CarInput.instance.TurretRotY -= 100 * DeltaTime;
            entity.CarInput.instance.TurretRotY = Mathf.Clamp(entity.CarInput.instance.TurretRotY, -30, 30);

            //Apply Turret Firing State
            //entity.CarInput.myCar.Fire = _fire;

        }
    }



    private float NormalizeInput(float input, InputRange inputRange)
    {
        //Range: -1, 1, normalized to 0, 1

        if (inputRange.Equals(InputRange.MinusOne_One))
        {
            input = ((input + 1) / 2);
            return input;
        }
        else if (inputRange.Equals(InputRange.MinusOne_Zero))
        {
            input = input + 1;
            return input;
        }
        else if (inputRange.Equals(InputRange.Zero_One))
        {
            return input;
        }
        else
        {
            return input;
        }
    }

}