using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public PlayerRef shooter;
    private int damage;
    private float speed;
    
    [SerializeField] private LayerMask obstacleLayer;

    public void Initialize(PlayerRef shooterRef, int bulletDamage, float bulletSpeed)
    {
        shooter = shooterRef;
        damage = bulletDamage;
        speed = bulletSpeed;
    }

    public override void FixedUpdateNetwork()
    {
        transform.Translate(Vector3.forward * speed * Runner.DeltaTime);
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
