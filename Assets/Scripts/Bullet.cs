using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [Networked] private TickTimer life { get; set; }

    [SerializeField]
    private float bulletLifeSpan;

    public void Init()
    {
        life = TickTimer.CreateFromSeconds(Runner, bulletLifeSpan);
    }
    
    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
        else
        {
            transform.position += 5 * transform.forward * Runner.DeltaTime;
        }
    }
}
