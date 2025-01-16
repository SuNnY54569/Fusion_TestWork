using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkCharacterController characterController;
    void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<NetworkCharacterController>();
        }
    }

    public override void FixedUpdateNetwork()//Every Tick
    {

        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            characterController.Move(10 * data.direction * Runner.DeltaTime);
        }
        
    }
}
