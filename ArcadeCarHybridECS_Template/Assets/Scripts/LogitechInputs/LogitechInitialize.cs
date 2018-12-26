using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogitechInitialize : MonoBehaviour {

    private BootStrap Bootstrap;
    private GameObject CarObject;
    private CarInputComponent CarInput;
    public int SpringForce_OffsetPercentage;
    [Range(-100, 100)] public int DamperForce;
    [Range(-100, 100)] public int SpringForce_SaturationPercentage;
    [Range(-100, 100)] public int SpringForce_CoefficientPercentage;
    // Use this for initialization
    void OnEnable () {
        LogitechGSDK.LogiSteeringInitialize(false);

        Bootstrap = GameObject.FindObjectOfType<BootStrap>();
        CarObject = Bootstrap.hybridEntityObject;
    }

    // Update is called once per frame
    void FixedUpdate () {

        if (CarObject == null) return;
        //if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0)){
        //
        //  if (LogitechGSDK.LogiIsPlaying(0, LogitechGSDK.LOGI_FORCE_SPRING))
        //  {
        //      LogitechGSDK.LogiStopSpringForce(0);
        //  }
        //  else
        //  {
        //      LogitechGSDK.LogiPlaySpringForce(0, 50, 50, 50);
        //  }
        //}


        if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
        {
            if (LogitechGSDK.LogiIsPlaying(0, LogitechGSDK.LOGI_FORCE_DAMPER))
            {
                LogitechGSDK.LogiStopDamperForce(0);
            }
            else
            {
                LogitechGSDK.LogiPlayDamperForce(0, DamperForce);
            }
        }


                SpringForce_SaturationPercentage = Mathf.Max(25, Mathf.Abs(Mathf.RoundToInt(Input.GetAxis("SteeringWheel") * CarObject.GetComponent<CarInputComponent>().instance.Acceleration/60 * 100)));
        SpringForce_CoefficientPercentage = SpringForce_SaturationPercentage;


        if (Input.GetAxis("SteeringWheel") > 0.001f)
        {
            if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
            {
                if (LogitechGSDK.LogiIsPlaying(0, LogitechGSDK.LOGI_FORCE_SPRING))
                {
                    LogitechGSDK.LogiStopSpringForce(0);
                    //Debug.Log(" -- Turn Right");
                }
                else
                {
                    LogitechGSDK.LogiPlaySpringForce(0, SpringForce_OffsetPercentage, SpringForce_SaturationPercentage, SpringForce_CoefficientPercentage);
                    //Debug.Log("Turn Right");
                }
            }
        }
        else if (Input.GetAxis("SteeringWheel") < -0.001f)
        {
            if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
            {
                if (LogitechGSDK.LogiIsPlaying(0, LogitechGSDK.LOGI_FORCE_SPRING))
                {
                    LogitechGSDK.LogiStopSpringForce(0);
                    //Debug.Log(" -- Turn Left");
                }
                else
                {
                    LogitechGSDK.LogiPlaySpringForce(0, SpringForce_OffsetPercentage, SpringForce_SaturationPercentage, SpringForce_CoefficientPercentage);
                    //Debug.Log("Turn Left");
                }
            }

        }

    }

    void OnDisable()
    {
        LogitechGSDK.LogiSteeringShutdown();
    }
}
