using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public enum GameState
{
    Waiting,
    Countdown,
    Playing,
}

public class GameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform spawnpoint;
    [SerializeField] private Transform spawnpointPivot;
    [SerializeField] private CoinSpawner coinSpawner;
    [SerializeField] private float gameDuration;
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private string menuSceneName = "Menu";
    
    [Networked] private Player Winner { get; set; }
    [Networked] public float Timer { get; set; }
    [Networked] private float CountdownTimer { get; set; } 
    [Networked, OnChangedRender(nameof(GameStateChanged))] private GameState State { get; set; }
    [Networked] private NetworkDictionary<PlayerRef, Player> Players => default;

    public override void Spawned()
    {
        Timer = gameDuration;
        CountdownTimer = countdownDuration;
        Winner = null;
        State = GameState.Waiting;
        UIManager.Singleton?.SetUI(State, Winner);
        Runner.SetIsSimulated(Object, true);
    }

    public override void FixedUpdateNetwork()
    {
        if (Players.Count < 1) return;

        if (Runner.IsServer)
        {
            switch (State)
            {
                case GameState.Waiting:
                    HandleWaitingState();
                    break;

                case GameState.Countdown:
                    HandleCountdownState();
                    break;

                case GameState.Playing:
                    HandlePlayingState();
                    break;
            }
        }

        if (!Runner.IsResimulation)
        {
            if (State == GameState.Playing)
            {
                UIManager.Singleton.UpdateTimer(Timer);
                UIManager.Singleton.UpdateLeaderBoard(Players.OrderByDescending(p => p.Value.Score).ToArray());
            }
            else if (State == GameState.Countdown)
            {
                UIManager.Singleton.UpdateCountdown(CountdownTimer);
            }
        }
    }
    
    private void HandleWaitingState()
    {
        // Check if all players are ready
        bool areAllReady = Players.All(p => p.Value.IsReady);

        if (areAllReady)
        {
            // Transition to Countdown state
            CountdownTimer = countdownDuration;
            State = GameState.Countdown;
        }
    }
    
    private void HandleCountdownState()
    {
        CountdownTimer -= Runner.DeltaTime;

        if (CountdownTimer <= 0)
        {
            // Transition to Playing state
            CountdownTimer = 0;
            State = GameState.Playing;
            PreparePlayers();
            coinSpawner.StartSpawning();

            foreach (var player in Players)
            {
                player.Value.Score = 0;        // Reset score
                player.Value.IsReady = false; // Unready players
            }
        }
    }
    
    private void HandlePlayingState()
    {
        Timer -= Runner.DeltaTime; // Countdown game duration
        if (Timer <= 0)
        {
            EndGame();
        }
    }
    
    private void EndGame()
    {
        Winner = Players.Select(kvp => kvp.Value).OrderByDescending(p => p.Score).FirstOrDefault();
        State = GameState.Waiting;
        coinSpawner.StopSpawning();
        coinSpawner.RemoveAllCoins();
        GameStateChanged();
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
    
    public void ExitRoom()
    {
        if (Runner != null)
        {
            Debug.Log("Player exiting the room...");

            // Shut down the runner session (this will trigger OnShutdown for clients)
            Runner.Shutdown();
        }

        // Load the menu scene
        SceneManager.LoadScene(menuSceneName);
    }
}
