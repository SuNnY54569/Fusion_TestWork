using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
     public static CameraFollow Singleton
     {
          get => _singleton;
          set
          {
               if (value == null)
               {
                    _singleton = null;
               }
               else if(_singleton == null)
               {
                    _singleton = value;
               }
               else if (_singleton != value)
               {
                    Destroy(value);
                    Debug.LogError($"There should only ever be one instance of {nameof(CameraFollow)}!");
               }
          }
     }

     private static CameraFollow _singleton;

     private Transform target;
     
     [SerializeField] private float smoothSpeed = 0.125f;

     private void Awake()
     {
          Singleton = this;
     }

     private void OnDestroy()
     {
          if (Singleton == this)
          {
               Singleton = null;
          }
     }

     private void LateUpdate()
     {
          if (target != null)
          {
               Vector3 smoothedPosition = Vector3.Lerp(transform.position, target.position, smoothSpeed);
               transform.position = smoothedPosition;

               Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, target.rotation, smoothSpeed);
               transform.rotation = smoothedRotation;
          }
     }

     public void SetTarget(Transform newTarget)
     {
          target = newTarget;
     }
}
