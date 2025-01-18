using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
     public static CameraFollow Instance;
     
     [Header("Camera Follow Settings")]
     [SerializeField, Tooltip("Smooth speed for camera movement. The higher the value, the faster the camera will follow.")]
     private float smoothSpeed = 0.125f;
     
     private Transform target;

     private void Awake()
     {
          if (Instance == null)
          {
               Instance = this;
          }
          else
          {
               Destroy(gameObject);
          }
     }

     private void LateUpdate()
     {
          if (target != null)
          {
               //Smoothly interpolate between the camera's current position and the target's position
               Vector3 smoothedPosition = Vector3.Lerp(transform.position, target.position, smoothSpeed);
               transform.position = smoothedPosition;

               //Smoothly interpolate between the camera's current rotation and the target's rotation
               Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, target.rotation, smoothSpeed);
               transform.rotation = smoothedRotation;
          }
     }

     //Sets the target for the camera to follow.
     public void SetTarget(Transform newTarget)
     {
          target = newTarget;
     }
}
