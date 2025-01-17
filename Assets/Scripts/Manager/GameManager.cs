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
    public static GameManager Instance;
    
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform spawnpoint;
    [SerializeField] private Transform spawnpointPivot;
    [SerializeField] private CoinSpawner coinSpawner;
    [SerializeField] private float gameDuration;
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] [Range(0f, 1f)] private float minLostCoinRatio = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float maxLostCoinRatio = 0.5f;
    
    public int minimumPlayers = 2;
    
    [Networked] private Player Winner { get; set; }
    [Networked] public float Timer { get; set; }
    [Networked] private float CountdownTimer { get; set; } 
    [Networked, OnChangedRender(nameof(GameStateChanged))] public GameState State { get; set; }
    [Networked] private NetworkDictionary<PlayerRef, Player> Players => default;

    [Networked] public bool reachMinimumPlayer { get; set; }

    private InputManager inputManager;

    public override void Spawned()
    {
        Timer = gameDuration;
        CountdownTimer = countdownDuration;
        Winner = null;
        State = GameState.Waiting;
        
        int readyCount = Players.Count(p => p.Value.IsReady);
        int totalPlayers = Players.Count;
        
        UIManager.Instance?.SetUI(State, Winner, readyCount, totalPlayers);
        Runner.SetIsSimulated(Object, true);
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        inputManager = FindObjectOfType<InputManager>();
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
                UIManager.Instance.UpdateTimer(Timer);
                UIManager.Instance.UpdateLeaderBoard(Players.OrderByDescending(p => p.Value.Score).ToArray());
            }
            else if (State == GameState.Countdown)
            {
                UIManager.Instance.UpdateCountdown(CountdownTimer);
            }
        }
    }
    
    private void HandleWaitingState()
    {
        if (Players.Count < minimumPlayers)
        {
            // Do not proceed with game start, keep waiting
            UIManager.Instance.NotEnoughPlayer(minimumPlayers);
            reachMinimumPlayer = false;
            return;
        }
        else
        {
            int readyCount = Players.Count(p => p.Value.IsReady);
            int totalPlayers = Players.Count;
            
            reachMinimumPlayer = true;
            UIManager.Instance.SetUI(State, Winner, readyCount, totalPlayers);
            
            if (HasInputAuthority && inputManager.LocalPlayer.IsReady)
            {
                UIManager.Instance.DidSetReady();
            }
        }
        
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
        foreach (var player in Players)
        {
            player.Value.Score = 0;        // Reset score
        }
    }

    private void GameStateChanged()
    {
        int readyCount = Players.Count(p => p.Value.IsReady);
        int totalPlayers = Players.Count;
        
        UIManager.Instance.SetUI(State, Winner, readyCount, totalPlayers);
    }

    private void PreparePlayers()
    {
        float radius = Vector3.Distance(spawnpoint.position, spawnpointPivot.position);

        spawnpointPivot.rotation = Quaternion.Euler(0f, 0f, 0f); // Random starting rotation

        foreach (KeyValuePair<PlayerRef, Player> player in Players)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 position = spawnpointPivot.position + new Vector3(
                Mathf.Cos(randomAngle), 
                0, 
                Mathf.Sin(randomAngle)
            ) * radius;
            Quaternion rotation = Quaternion.LookRotation(spawnpointPivot.position - position);
            
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
    
    public void HandlePlayerDeath(Player player)
    {
        if (!HasStateAuthority || player == null)
            return;

        // Deduct player's score
        float lostCoinRatio = Random.Range(minLostCoinRatio, maxLostCoinRatio);
        int lostScore = Mathf.Max(5, Mathf.CeilToInt(player.Score * lostCoinRatio / 5f) * 5);
        player.Score -= lostScore;
        
        if (coinSpawner == null)
        {
            Debug.LogError("CoinSpawner is not assigned!");
            return;
        }
        
        Vector3 randomOffset = new Vector3(
            Random.Range(-3f, 3f), 
            1f, 
            Random.Range(-3f, 3f)
        );
        Vector3 spawnPosition = player.transform.position + randomOffset;

        if (lostScore > 0)
        {
            coinSpawner.SpawnCoin(spawnPosition, lostScore, false);
        }

        float radius = Vector3.Distance(spawnpoint.position, spawnpointPivot.position);
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 position = spawnpointPivot.position + new Vector3(
            Mathf.Cos(randomAngle), 
            0, 
            Mathf.Sin(randomAngle)
        ) * radius;
        Quaternion rotation = Quaternion.LookRotation(spawnpointPivot.position - position);

        player.Teleport(position, rotation);
    }
    
    private void OnDrawGizmos()
    {
        if (spawnpointPivot == null || spawnpoint == null) return;

        Gizmos.color = Color.green;

        // Draw the spawn circle
        float radius = Vector3.Distance(spawnpoint.position, spawnpointPivot.position);
        DrawWireCircle(spawnpointPivot.position, radius);

        // Draw lines to potential spawn points
        
        float angleStep = 360f / 10;
        for (int i = 0; i < 10; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 spawnPosition = spawnpointPivot.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(spawnpointPivot.position, spawnPosition);
        }
        
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments = 36)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
