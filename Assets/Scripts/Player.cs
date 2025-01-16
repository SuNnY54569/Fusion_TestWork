using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private MeshRenderer[] modelParts;
    [SerializeField] private SimpleKCC kcc;
    [SerializeField] private Transform camTarget;
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpImpulse = 10f;
    public LayerMask collisionTestMask;

    public int Score;
    public bool IsReady;
    
    [Networked] public string Name { get; private set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private InputManager inputManager;
    private Vector2 baseLookRotation;
    Collider[] collisionTestColliders = new Collider[8];
    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 2f);

        if (HasInputAuthority)
        {
            foreach (MeshRenderer renderer in modelParts)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            inputManager = Runner.GetComponent<InputManager>();
            inputManager.LocalPlayer = this;
            Name = PlayerPrefs.GetString("Photon.Menu.UserName");
            RPC_PlayerName(Name);
            CameraFollow.Singleton.SetTarget(camTarget);
            kcc.Settings.ForcePredictedLookRotation = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetInput input))
        {
            kcc.AddLookRotation(input.LookDelta * lookSensitivity);
            UpdateCamTarget();
            Vector3 worldDirection = kcc.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y);
            float jump = 0f;

            if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc .IsGrounded)
            {
                jump = jumpImpulse;
            }
            
            kcc.Move(worldDirection.normalized * speed, jump);
            PreviousButtons = input.Buttons;
            baseLookRotation = kcc.GetLookRotation();
        }
        
        int collectablesInRange = Runner.GetPhysicsScene().OverlapCapsule(transform.position,
            transform.position + Vector3.up * 2, 1f, collisionTestColliders, collisionTestMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < collectablesInRange; i++)
        {
            var pickUpObject = collisionTestColliders[i].GetComponent<Coin>();
            if (pickUpObject != null)
            {
                CollectObject(pickUpObject);
                Debug.Log("Collect Object");
            }
        }
    }
    
    private bool CollectObject(Coin pickUp)
    {
        if (pickUp == null || pickUp.Object?.IsValid != true)
            return false;

        if (pickUp.CanPickUp)
        {
            pickUp.OnPickUpLocal(this);
        }

        return true;
    }

    public override void Render()
    {
        if (kcc.Settings.ForcePredictedLookRotation)
        {
            Vector2 predictedLookRotation = baseLookRotation + inputManager.AccumulatedMouseDelta * lookSensitivity;
            kcc.SetLookRotation(predictedLookRotation);
        }
        
        UpdateCamTarget();
    }

    private void UpdateCamTarget()
    {
        camTarget.localRotation = Quaternion.Euler(kcc.GetLookRotation().x, 0f, 0f);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.InputAuthority | RpcTargets.StateAuthority)]
    public void RPC_SetReady()
    {
        IsReady = true;
        if (HasInputAuthority)
        {
            UIManager.Singleton.DidSetReady();
        }
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        kcc.SetPosition(position);
        kcc.SetLookRotation(rotation);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PlayerName(string name)
    {
        Name = name;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_Reward(int scoreValue)
    {
        Score += scoreValue;
    }
}
