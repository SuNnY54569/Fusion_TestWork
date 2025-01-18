using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    [Header("Coin Settings")]
    [Tooltip("The score value this coin will give when picked up.")]
    [SerializeField] private int scoreValue = 10;

    [Tooltip("The rotation speed of the coin, in degrees per second.")]
    [SerializeField] private float rotationSpeed = 360f;

    [Tooltip("The 3D model of the coin.")]
    [SerializeField] private GameObject coinModel;

    [Tooltip("The material used for the coin when it is dropped.")]
    [SerializeField] private Material dropCoinMat;

    [Networked] public NetworkBool Collected { get; set; } = false;

    public bool CanPickUp => !Collected;
    
    // Local method called when the player attempts to pick up the coin.
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

    //rewards the player with points, marks the coin as collected, and despawn it.
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

    public void SetCoinValue(int value)
    {
        scoreValue = value;
    }
}
