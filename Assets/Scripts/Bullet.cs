using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public PlayerRef shooter;
    private int damage;
    private float speed;

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

    public void DestroyBullet()
    {
        Runner.Despawn(Object);
    }
}
