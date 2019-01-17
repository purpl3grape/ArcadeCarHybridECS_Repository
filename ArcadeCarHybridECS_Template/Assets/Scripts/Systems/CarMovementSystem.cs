using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class CarMovementSystem : ComponentSystem
{
    private struct Group
    {
        public Transform Transform;
        public CarInputComponent CarInput;
        public Rigidbody Rigidbody;
        public SubtractiveComponent<CarBodyComponent> CarBody;
        public SubtractiveComponent<TurretComponent> TurretComponent;
    }

    private static BootStrap Bootstrap;
    private static GameObject CarHeadingObj;
    private static GameObject CarBodyObj;
    private static GameObject[] turretObjs;
    private static Quaternion originalRotation;
    private static Quaternion tiltRotation;

    private static LogitechInitialize LogitechInitialize;


    //GROUNDCHECK
    private int layerMask = ~1 << 2;    //Everything BUT Layer 2
    private string iceTag = "Ice";
    private bool iceFloor = false;
    private RaycastHit hit;
    private float mass = 100;

    //GETSLOPE
    private RaycastHit lr;
    private RaycastHit rr;
    private RaycastHit lf;
    private RaycastHit rf;
    private Vector3 upDir;
    private int slopeLayerMask = 1 << 9;    //Everything BUT Layer 2

    //DRIFT
    private float angle;
    private float angularVelocity;
    private float velocityX;
    private float velocityY;
    private float velocityZ;
    private float drag = .9f;
    private float angularDrag = .9f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        Bootstrap = GameObject.FindObjectOfType<BootStrap>();
        turretObjs = GameObject.FindGameObjectsWithTag("GameController");
        foreach (GameObject turret in turretObjs)
        {
            originalRotation = turret.transform.localRotation;
        }
        CarHeadingObj = GameObject.FindGameObjectWithTag("CarHeading");
        LogitechInitialize = Bootstrap.GetComponent<LogitechInitialize>();
    }


    protected override void OnUpdate()
    {
        var DeltaTime = Time.deltaTime;
        float3 normal;
        float gravity = 9.8f;


        foreach (var entity in GetEntities<Group>())
        {
            var position = entity.Transform.position;
            var rotation = entity.Transform.rotation;

            //Tilt the car toward ground
            //            float tiltLerpValue = Mathf.Max(.1f, (.5f - entity.CarInput.instance.Acceleration / 60));
            float tiltLerpValue = Mathf.Max(0.1f, (0.5f - entity.CarInput.instance.Acceleration / 60));
            entity.Transform.up = Vector3.Lerp(entity.Transform.up, GetSlope(entity.Transform), tiltLerpValue);

            if (GroundCheck(entity.Transform, out normal, 2f))
            {
                //Ground Move
                if (Bootstrap.DriveType.Equals(DriveType.TwoWheel) || Bootstrap.DriveType.Equals(DriveType.FourWheel))
                {
                    if (iceFloor == true) {
                        LogitechInitialize.DamperForce = (int)(entity.CarInput.instance.Acceleration / 60 * -100);
                    }
                    else
                    {
                        LogitechInitialize.DamperForce = 0;
                    }
                    entity.Rigidbody.velocity = (CarHeadingObj.transform.forward * entity.CarInput.instance.Acceleration);
                }
                else if (Bootstrap.DriveType.Equals(DriveType.Drift))
                {
                    entity.Rigidbody.velocity = ((1 - entity.CarInput.instance.Break) * (CarHeadingObj.transform.forward * entity.CarInput.instance.Acceleration) + (entity.CarInput.instance.Break) * (entity.CarInput.instance.Horizontal * -CarHeadingObj.transform.right * entity.CarInput.instance.Acceleration));
                }
            }
            else
            {
                //Air Move
                LogitechInitialize.DamperForce = 0;
                entity.Rigidbody.velocity = (CarHeadingObj.transform.forward * entity.CarInput.instance.Acceleration - entity.Transform.up * gravity);
            }

            //Turrets
            Quaternion yQuaternion = Quaternion.AngleAxis(entity.CarInput.instance.TurretRotY, Vector3.left);

            foreach (GameObject turret in turretObjs)
            {
                turret.transform.localRotation = originalRotation * yQuaternion;
            }
        }
    }

    private Vector3 GetSlope(Transform tr)
    {
        Physics.Raycast(tr.position - Vector3.forward * 2f - (Vector3.right * 2f) + Vector3.up * 1, Vector3.down, out lr, 5, slopeLayerMask);
        Physics.Raycast(tr.position - Vector3.forward * 2f + (Vector3.right * 2f) + Vector3.up * 1, Vector3.down, out rr, 5, slopeLayerMask);
        Physics.Raycast(tr.position + Vector3.forward * 2f - (Vector3.right * 2f) + Vector3.up * 1, Vector3.down, out lf, 5, slopeLayerMask);
        Physics.Raycast(tr.position + Vector3.forward * 2f + (Vector3.right * 2f) + Vector3.up * 1, Vector3.down, out rf, 5, slopeLayerMask);
        upDir = (Vector3.Cross(rr.point - Vector3.up * 1, lr.point - Vector3.up * 1) +
                 Vector3.Cross(lr.point - Vector3.up * 1, lf.point - Vector3.up * 1) +
                 Vector3.Cross(lf.point - Vector3.up * 1, rf.point - Vector3.up * 1) +
                 Vector3.Cross(rf.point - Vector3.up * 1, rr.point - Vector3.up * 1)
                ).normalized;
    
        Debug.DrawRay(tr.position - Vector3.forward * 2 - Vector3.right + Vector3.up, Vector3.down, Color.red);
        Debug.DrawRay(tr.position - Vector3.forward * 2 + Vector3.right + Vector3.up, Vector3.down, Color.red);
        Debug.DrawRay(tr.position + Vector3.forward * 2 - Vector3.right + Vector3.up, Vector3.down, Color.red);
        Debug.DrawRay(tr.position + Vector3.forward * 2 + Vector3.right + Vector3.up, Vector3.down, Color.red);
    
        upDir = new Vector3(upDir.x, tr.up.y, upDir.z);
        return upDir;
    }

    private bool GroundCheck(Transform tr, out float3 hitNormal, float distance)
    {
        iceFloor = false;
        if (Physics.Raycast(tr.position, Vector3.down, out hit, distance, layerMask))
        {
            hitNormal = hit.normal;
            if (hit.transform.CompareTag(iceTag))
            {
                iceFloor = true;
            }
            return true;
        }
        else if (Physics.Raycast(tr.position + tr.forward + tr.right, Vector3.down, out hit, distance, layerMask))
        {
            hitNormal = hit.normal;
            if (hit.transform.CompareTag(iceTag))
            {
                iceFloor = true;
            }
            return true;
        }
        else if (Physics.Raycast(tr.position + tr.forward - tr.right, Vector3.down, out hit, distance, layerMask))
        {
            hitNormal = hit.normal;
            if (hit.transform.CompareTag(iceTag))
            {
                iceFloor = true;
            }
            return true;
        }
        else if (Physics.Raycast(tr.position - tr.forward + tr.right, Vector3.down, out hit, distance, layerMask))
        {
            hitNormal = hit.normal;
            if (hit.transform.CompareTag(iceTag))
            {
                iceFloor = true;
            }
            return true;
        }
        else if (Physics.Raycast(tr.position - tr.forward - tr.right, Vector3.down, out hit, distance, layerMask))
        {
            hitNormal = hit.normal;
            if (hit.transform.CompareTag(iceTag))
            {
                iceFloor = true;
            }
            return true;
        }
        hitNormal = float3.zero;


        return false;
    }


}