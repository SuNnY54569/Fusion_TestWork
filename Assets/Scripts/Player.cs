using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("Player Model Settings")]
    [SerializeField, Tooltip("Array of mesh renderers for the player model parts.")]
    private MeshRenderer[] modelParts;
    
    [Header("Player Movement Settings")]
    [SerializeField, Tooltip("KCC (Kinematic Character Controller) for player movement.")]
    private SimpleKCC kcc;
    
    [SerializeField, Tooltip("Camera target that the player view will follow.")]
    private Transform camTarget;
    
    [SerializeField, Tooltip("Sensitivity for the player's look rotation.")]
    private float lookSensitivity = 0.15f;
    
    [SerializeField, Tooltip("Movement speed of the player.")]
    private float speed = 5f;
    
    [SerializeField, Tooltip("Impulse applied when the player jumps.")]
    private float jumpImpulse = 10f;

    [SerializeField, Tooltip("Layer mask used for collision detection.")]
    private LayerMask collisionTestMask;
    
    [SerializeField, Tooltip("Layer mask used for Raycast detection.")]
    private LayerMask raycastLayerMask;

    [Header("Player Health")]
    [SerializeField, Tooltip("Maximum health of the player.")]
    private int maxHealth;
    
    [Networked, Tooltip("Current health of the player.")]
    public int Health { get; private set; } = 100;

    [Header("Shooting Settings")]
    [SerializeField, Tooltip("Bullet prefab to be instantiated when shooting.")]
    private GameObject bulletPrefab;

    [SerializeField, Tooltip("Position where the bullet will spawn from.")]
    private Transform bulletSpawnPoint;
    
    [SerializeField, Tooltip("Score cost to shoot a bullet.")]
    private int bulletScoreCost = 10;
    
    [SerializeField, Tooltip("Damage dealt by the bullet.")]
    private int bulletDamage = 30;
    
    [SerializeField, Tooltip("Speed of the bullet.")]
    private float bulletSpeed = 10f;
    
    [SerializeField, Tooltip("Cooldown time in ticks before the player can shoot again.")]
    private float shootCooldown = 30f;

    private int lastShotTick;

    [Header("Player Score and Status")]
    [Tooltip("Indicates whether the player is ready.")]
    public bool IsReady;
    
    [Networked, Tooltip("The player's current score.")]
    public int Score { get; set; }
    
    [Networked, Tooltip("The player's name.")]
    public string Name { get; private set; }

    [Networked, Tooltip("Stores the previous input buttons for the player.")]
    private NetworkButtons PreviousButtons { get; set; }

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
            Name = PlayerPrefs.GetString("PlayerName");
            RPC_PlayerName(Name);
            CameraFollow.Instance.SetTarget(camTarget);
            kcc.Settings.ForcePredictedLookRotation = true;
            Health = maxHealth;
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

            // Handle jump action
            if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.IsGrounded)
            {
                jump = jumpImpulse;
            }
            
            // Handle shooting action
            if (input.Buttons.WasPressed(PreviousButtons, InputButton.Fire) && HasInputAuthority)
            {
                if (Runner.Tick - lastShotTick >= shootCooldown)
                {
                    RPC_Shoot(); // Call the Shoot RPC method to shoot a bullet
                    lastShotTick = Runner.Tick;
                }
                else
                {
                    Debug.Log("Shooting is on cooldown!");
                }
            }
            
            kcc.Move(worldDirection.normalized * speed, jump);
            PreviousButtons = input.Buttons;
            baseLookRotation = kcc.GetLookRotation();
        }
        
        // Detect if any objects (such as coins or bullets) are within range
        int objectInRange = Runner.GetPhysicsScene().OverlapCapsule(transform.position,
            transform.position + Vector3.up * 2, 1f, collisionTestColliders, collisionTestMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < objectInRange; i++)
        {
            var pickUpObject = collisionTestColliders[i].GetComponent<Coin>();
            if (pickUpObject != null)
            {
                CollectObject(pickUpObject); // Collect the coin if it's within range
                Debug.Log("Collect Object");
            }

            var bulletObject = collisionTestColliders[i].GetComponent<Bullet>();
            if (bulletObject != null && Object.InputAuthority != bulletObject.shooter)
            {
                if (Object.HasStateAuthority)
                {
                    RPC_TakeDamage(bulletDamage); // Take damage if the bullet hits the player
                }
                bulletObject.DestroyBullet();
            }
        }
    }
    
    // Handles collecting a coin if the conditions are met
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
        if (kcc.Settings.ForcePredictedLookRotation && HasInputAuthority)
        {
            Vector2 predictedLookRotation = baseLookRotation + inputManager.AccumulatedMouseDelta * lookSensitivity;
            kcc.SetLookRotation(predictedLookRotation);
        }
        
        UpdateCamTarget();
        if (HasInputAuthority)
        {
            // Call your UI update here
            UIManager.Instance.UpdatePlayerScore(Score);
            UIManager.Instance.UpdatePlayerHealth(Health);
        }
    }

    // Updates the camera target based on the player's look rotation
    private void UpdateCamTarget()
    {
        camTarget.localRotation = Quaternion.Euler(kcc.GetLookRotation().x, 0f, 0f);
    }

    // Sets the player as ready to play, only after a minimum number of players have joined
    [Rpc(RpcSources.InputAuthority, RpcTargets.InputAuthority | RpcTargets.StateAuthority)]
    public void RPC_SetReady()
    {
        if (!GameManager.Instance.reachMinimumPlayer) { return; }
        
        IsReady = true;
        if (HasInputAuthority)
        {
            UIManager.Instance.DidSetReady(); // Update the UI to reflect the player is ready
        }
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        kcc.SetPosition(position);
        kcc.SetLookRotation(rotation);
    }

    // Set the player's name across the network
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PlayerName(string name)
    {
        Name = name;
    }

    // Rewards the player by adding a score value
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_Reward(int scoreValue)
    {
        Score += scoreValue;
        Debug.Log($"RPC Score Updated: {Score}");
    }
    
    // Handles the shooting action by spawning a bullet, deducting score, and applying the bullet mechanics
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_Shoot()
    {
        if (!Object.HasStateAuthority)
            return;

        if (Score < bulletScoreCost)
        {
            Debug.Log("Not enough score to shoot!");
            return;
        }

        // Deduct score for shooting
        Score -= bulletScoreCost;
        
        Ray ray = new Ray(camTarget.transform.position, camTarget.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            
            bulletSpawnPoint.LookAt(hit.point);
        }
        else
        {
            bulletSpawnPoint.LookAt(camTarget.transform.position + camTarget.transform.forward * 1000);
        }
        // Spawn the bullet at the bullet spawn point, looking towards the hit point
        NetworkObject bullet = Runner.Spawn(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet.GetComponent<Bullet>().Initialize(Object.InputAuthority, bulletSpeed);

        Debug.Log($"Player {Name} shot a bullet!");
    }

    // Handles taking damage, reducing the player's health, and triggering the death process if health reaches zero
    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage)
    {
        Health -= damage;

        if (Health <= 0)
        {
            GameManager.Instance.HandlePlayerDeath(this);
            Health = maxHealth;
            Debug.Log($"{Name} has been eliminated!");
        }
    }
}
