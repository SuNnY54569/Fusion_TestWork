using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private float rotationSpeed = 360f;

    [Networked] public NetworkBool Collected { get; set; } = false;

    public bool CanPickUp => !Collected;
    
    public void OnPickUpLocal(Player player)
    {
        RPC_Collect(player);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Collected)
        {
            // Rotate the coin around the Y-axis (you can adjust this for other axes)
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Collect(Player collector)
    {
        if (!Object.HasStateAuthority || Collected) return;

        collector.RPC_Reward(scoreValue);
        Collected = true;
        var coinSpawner = FindObjectOfType<CoinSpawner>(); // Or better, reference the spawner directly
        if (coinSpawner != null)
        {
            coinSpawner.OnCoinCollected();
        }
        Runner.Despawn(Object);
    }
}
