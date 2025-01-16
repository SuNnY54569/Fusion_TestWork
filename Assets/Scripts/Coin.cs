using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    [SerializeField] private int scoreValue = 1;

    [Networked] public NetworkBool Collected { get; set; } = false;

    public bool CanPickUp
    {
        get { return !Collected; }
    }
    
    public void OnPickUpLocal(Player player)
    {
        RPC_Collect(player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Collect(Player collector)
    {
        if (!Object.HasStateAuthority || Collected) return;

        collector.RPC_Reward(scoreValue);
        Collected = true;
        Runner.Despawn(this.Object);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (Collected) return;

        Player player = other.GetComponent<Player>();
        if (player != null && CanPickUp)
        {
            OnPickUpLocal(player);
        }
    }
}
