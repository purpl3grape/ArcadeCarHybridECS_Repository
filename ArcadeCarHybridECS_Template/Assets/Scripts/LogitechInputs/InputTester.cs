using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputTester : MonoBehaviour {

    public float SteeringWheel, GasPedal,BreakPedal, reverseTemp, ClutchPedal, seven, eight, nine, ten;
    public bool Gear1Shifter, Gear2Shifter, Gear3Shifter, Gear4Shifter, Gear5Shifter, ReverseShifter;
    void Update()
    {
        SteeringWheel = Input.GetAxis("SteeringWheel");
        GasPedal = Input.GetAxis("GasPedal");
        BreakPedal = Input.GetAxis("BreakPedal");
        ClutchPedal = Input.GetAxis("ClutchPedal");
        Gear1Shifter = Input.GetButtonDown("Gear1Shifter");
        Gear2Shifter = Input.GetButtonDown("Gear2Shifter");
        Gear3Shifter = Input.GetButtonDown("Gear3Shifter");
        Gear4Shifter = Input.GetButtonDown("Gear4Shifter");
        Gear5Shifter = Input.GetButtonDown("Gear5Shifter");
        ReverseShifter = Input.GetButtonDown("ReverseShifter");
        //Gear1Shifter = Input.GetKey("joystick 1 button 12");
        if (Gear1Shifter) Debug.Log("Gear 1");
        if (Gear2Shifter) Debug.Log("Gear 2");
        if (Gear3Shifter) Debug.Log("Gear 3");
        if (Gear4Shifter) Debug.Log("Gear 4");
        if (Gear5Shifter) Debug.Log("Gear 5");
        if (ReverseShifter) Debug.Log("Reverse");


        //for (int joystick = 1; joystick < 5; joystick++ ) {
        //    for (int button = 0; button < 20; button++ ) {
        //        if (Input.GetKey("joystick " + joystick + " button " + button))
        //        {
        //            // that's the right button, now i need to assign a KeyCode to list[ix]
        //            Debug.Log("Joystick: " + joystick + ", Button: " + button);
        //        }
        //    }
        //}

    }
}
