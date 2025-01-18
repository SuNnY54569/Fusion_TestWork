using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("The player who shot the bullet.")]
    public PlayerRef shooter;

    [Tooltip("The speed of the bullet.")]
    private float speed;

    [Header("Collision Settings")]
    [Tooltip("LayerMask for obstacles the bullet can collide with.")]
    [SerializeField] private LayerMask obstacleLayer;
    
    [Tooltip("Time before the bullet despawns if it doesn't hit anything.")]
    [SerializeField] private float despawnTime = 5f;
    
    private float timer;

    public override void Spawned()
    {
        timer = despawnTime;
    }

    public void Initialize(PlayerRef shooterRef, float bulletSpeed)
    {
        shooter = shooterRef;
        speed = bulletSpeed;
    }

    public override void FixedUpdateNetwork()
    {
        transform.Translate(Vector3.forward * speed * Runner.DeltaTime);
        timer -= Runner.DeltaTime;
        
        if (timer <= 0)
        {
            DestroyBullet();
            Debug.Log("Despawn Bullet");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsInLayerMask(other.gameObject.layer, obstacleLayer))
        {
            Debug.Log("Bullet hit an obstacle!");
            DestroyBullet();
        }
    }
    
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return ((1 << layer) & layerMask) != 0;
    }
    
    public void DestroyBullet()
    {
        Runner.Despawn(Object);
    }
}
