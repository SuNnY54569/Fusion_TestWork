using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkCharacterController characterController;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] [Range(0.01f,1f)] private float mouseSensitivity = 0.5f;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform bulletSpawn;
    private Vector3 _forward = Vector3.forward;
    
    [Networked] private TickTimer delay { get; set; }
    public override void Spawned()
    {
        /*if (Object.HasInputAuthority)
        {
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 2, -5);
        }*/
    }
    
    void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<NetworkCharacterController>();
        }
    }

    public override void FixedUpdateNetwork()//Every Tick
    {
        if (GetInput(out NetworkInputData inputData))
        {
            Vector3 moveDirection = inputData.direction.normalized;
            
            //float rotationInput = inputData.rotationInput * mouseSensitivity;
            //transform.Rotate(0, rotationInput * rotationSpeed * Runner.DeltaTime, 0);
            
            characterController.Move(moveDirection * moveSpeed * Runner.DeltaTime);

            if (inputData.direction.sqrMagnitude > 0)
            {
                _forward = inputData.direction;
            }

            if (HasInputAuthority && delay.ExpiredOrNotRunning(Runner))
            {
                if (inputData.buttons.IsSet(NetworkInputData.MouseButton0))
                {
                    Runner.Spawn(bulletPrefab, bulletSpawn.position + _forward, Quaternion.LookRotation(_forward),
                        Object.InputAuthority, (Runner, o) =>
                        {
                            o.GetComponent<Bullet>().Init();
                        });
                }
            }
            
            
        }
    }
}
