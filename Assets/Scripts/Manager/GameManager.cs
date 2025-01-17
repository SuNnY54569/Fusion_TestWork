using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;

public enum GameState
{
    Waiting,
    Playing,
}

public class GameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform spawnpoint;
    [SerializeField] private Transform spawnpointPivot;
    [SerializeField] private CoinSpawner coinSpawner;
    [SerializeField] private float gameDuration;
    
    [Networked] private Player Winner { get; set; }
    
    [Networked] public float Timer { get; set; }
    [Networked, OnChangedRender(nameof(GameStateChanged))] private GameState State { get; set; }
    [Networked] private NetworkDictionary<PlayerRef, Player> Players => default;

    public override void Spawned()
    {
        Timer = gameDuration;
        Winner = null;
        State = GameState.Waiting;
        if (UIManager.Singleton != null)
        {
            UIManager.Singleton.SetUI(State, Winner);
        }
        Runner.SetIsSimulated(Object, true);
    }

    public override void FixedUpdateNetwork()
    {
        if (Players.Count < 1) return;

        if (Runner.IsServer && State == GameState.Waiting)
        {
            bool areAllReady = true;
            foreach (KeyValuePair<PlayerRef, Player> player in Players)
            {
                if (!player.Value.IsReady)
                {
                    areAllReady = false;
                    break;
                }
            }

            if (areAllReady)
            {
                Winner = null;
                State = GameState.Playing;
                PreparePlayers();
                coinSpawner.StartSpawning();
                foreach (var player in Players)
                {
                    player.Value.Score = 0;// Reset score
                    player.Value.IsReady = false; // Unready the player
                }
            }
        }
        
        if (State == GameState.Playing && Runner.IsServer)
        {
            Timer -= Runner.DeltaTime; // Countdown
            if (Timer <= 0)
            {
                EndGame();
            }
        }

        if (State == GameState.Playing && !Runner.IsResimulation)
        {
            UIManager.Singleton.UpdateTimer(Timer);
            UIManager.Singleton.UpdateLeaderBoard(Players.OrderByDescending(p => p.Value.Score).ToArray());
        }
    }
    
    private void EndGame()
    {
        // Determine the player with the highest score
        Winner = Players.Select(kvp => kvp.Value).OrderByDescending(p => p.Score).FirstOrDefault();
    
        // Set the game state back to waiting
        State = GameState.Waiting;
    
        // Stop spawning coins
        coinSpawner.StopSpawning();
        coinSpawner.RemoveAllCoins();
    
        // Update UI
        GameStateChanged();

        // Unready all players to prepare for the next round
        UnreadyAll();
        
        Timer = gameDuration;
    }

    private void GameStateChanged()
    {
        UIManager.Singleton.SetUI(State, Winner);
    }

    private void PreparePlayers()
    {
        float spacingAngle = 360f / Players.Count;
        spawnpointPivot.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        foreach (KeyValuePair<PlayerRef, Player> player in Players)
        {
            GetNextSpawnPoint(spacingAngle, out Vector3 position, out Quaternion rotation);
            player.Value.Teleport(position, rotation);
        }
    }

    private void UnreadyAll()
    {
        foreach (KeyValuePair<PlayerRef, Player> player in Players)
        {
            player.Value.IsReady = false;
        }
    }

    private void GetNextSpawnPoint(float spacingAngle, out Vector3 position, out Quaternion rotation)
    {
        position = spawnpoint.position;
        rotation = spawnpoint.rotation;
        spawnpointPivot.Rotate(0f, spacingAngle, 0f);
    }
    
    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            GetNextSpawnPoint(90f, out Vector3 position, out Quaternion rotation);
            NetworkObject playerObject = Runner.Spawn(playerPrefab, Vector3.up, Quaternion.identity, player);
            Players.Add(player, playerObject.GetComponent<Player>());
            Debug.Log("Spawn Player");
        }
    }
    
    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        if (Players.TryGet(player, out Player playerBehaviour))
        {
            Players.Remove(player);
            Runner.Despawn(playerBehaviour.Object);
            Debug.Log("DeSpawn Player");
        }
    }
}
