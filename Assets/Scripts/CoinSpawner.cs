using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class CoinSpawner : NetworkBehaviour
{
    #region Variables
    
    [Header("Coin Prefabs")]
    [Tooltip("Prefab for the standard coin to spawn.")]
    [SerializeField] private NetworkPrefabRef coinPrefab;

    [Tooltip("Prefab for the dropped coin, used when a coin is from player dead")]
    [SerializeField] private NetworkPrefabRef dropCoinPrefab;

    [Header("Spawning Parameters")]
    [Tooltip("Time interval between coin spawns.")]
    [SerializeField] private float spawnInterval = 5f;

    [Tooltip("Minimum bounds for the coin spawning area.")]
    [SerializeField] private Vector3 spawnAreaMin;

    [Tooltip("Maximum bounds for the coin spawning area.")]
    [SerializeField] private Vector3 spawnAreaMax;

    [Tooltip("Maximum number of coins that can exist at once.")]
    [SerializeField] private int maxCoinsOnMap = 10;

    [Tooltip("Current number of coins on the map.")]
    [SerializeField] private int currentCoinCount = 0;

    [Tooltip("Value of each coin spawned.")]
    [SerializeField] private int coinValue = 10;

    private bool isSpawning = false;
    private Coroutine spawnCoroutine;
    private List<NetworkObject> spawnedCoins = new List<NetworkObject>();
    
    #endregion
    
    #region Coin Spawning Logic

    //Starts the coin spawning process
    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        
        SpawnCoinsToMax();

        spawnCoroutine = StartCoroutine(SpawnCoinsPeriodically());
    }
    
    //Spawns coins to reach the maximum coin count.
    private void SpawnCoinsToMax()
    {
        while (currentCoinCount < maxCoinsOnMap)
        {
            SpawnCoin(GetRandomPositionInArea(), coinValue);
        }
    }

    //Spawns coins periodically based on the specified interval.
    private IEnumerator SpawnCoinsPeriodically()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (currentCoinCount < maxCoinsOnMap)
            {
                SpawnCoin(GetRandomPositionInArea(), coinValue);
            }
        }
    }
    
    public void StopSpawning()
    {
        if (!isSpawning) return;

        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
    
    #endregion
    
    #region Coin Management

    //Spawns a single coin at a specified position.
    public void SpawnCoin(Vector3 position, int value, bool countCoin = true)
    {
        if (!Runner.IsServer) return;

        NetworkObject spawnedCoin;

        if (countCoin)
        {
            spawnedCoin = Runner.Spawn(coinPrefab, position, Quaternion.identity);
        }
        else
        {
            spawnedCoin = Runner.Spawn(dropCoinPrefab, position, Quaternion.identity);
        }
        
        if (spawnedCoin.TryGetComponent<Coin>(out Coin coinComponent))
        {
            coinComponent.SetCoinValue(value);
        }
        else
        {
            Debug.LogWarning("The spawned coin does not have a Coin component!");
        }
        
        if (!spawnedCoin.TryGetComponent(out NetworkTransform _))
        {
            spawnedCoin.gameObject.AddComponent<NetworkTransform>();
        }
        
        spawnedCoins.Add(spawnedCoin);

        if (countCoin)
        {
            currentCoinCount++;
        }
    }
    
    //Removes all spawned coins from the game.
    public void RemoveAllCoins()
    {
        if (!Runner.IsServer) return;

        for (int i = spawnedCoins.Count - 1; i >= 0; i--)
        {
            NetworkObject coin = spawnedCoins[i];
            if (coin != null && coin.IsValid)
            {
                Runner.Despawn(coin);
            }
            spawnedCoins.RemoveAt(i);
        }
        
        currentCoinCount = 0;
    }
    
    public void OnCoinCollected()
    {
        currentCoinCount--;
    }
    
    #endregion
    
    #region Utility Methods

    //Generates a random position within the defined spawn area that is not occupied.
    private Vector3 GetRandomPositionInArea()
    {
        Vector3 spawnPosition = Vector3.zero;
        bool validPosition = false;
        
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            float z = Random.Range(spawnAreaMin.z, spawnAreaMax.z);

            spawnPosition = new Vector3(x, y, z);
            
            if (!IsPositionOccupied(spawnPosition))
            {
                validPosition = true;
                break; 
            }
        }
        
        if (!validPosition)
        {
            Debug.LogWarning("Could not find a valid spawn position. Using default position.");
        }

        return spawnPosition;
    }
    
    //Checks if a given position is already occupied by a collider.
    private bool IsPositionOccupied(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f);
        
        return colliders.Length > 0;
    }
    
    #endregion
    
    #region Debugging
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        
        Vector3 size = spawnAreaMax - spawnAreaMin;
        Vector3 center = (spawnAreaMin + spawnAreaMax) / 2;

        Gizmos.DrawWireCube(center, size);
    }
    
    #endregion
}
