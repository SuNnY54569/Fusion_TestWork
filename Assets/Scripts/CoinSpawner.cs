using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class CoinSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef coinPrefab;
    [SerializeField] private NetworkPrefabRef dropCoinPrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Vector3 spawnAreaMin;
    [SerializeField] private Vector3 spawnAreaMax;
    [SerializeField] private int maxCoinsOnMap = 10;
    [SerializeField] private int currentCoinCount = 0;
    [SerializeField] private int coinValue = 10;
    
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;
    private List<NetworkObject> spawnedCoins = new List<NetworkObject>();

    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;

        // Start by spawning enough coins to reach the maximum limit
        SpawnCoinsToMax();

        spawnCoroutine = StartCoroutine(SpawnCoinsPeriodically());
    }
    
    private void SpawnCoinsToMax()
    {
        // Spawn coins to fill the map up to maxCoinsOnMap
        while (currentCoinCount < maxCoinsOnMap)
        {
            SpawnCoin(GetRandomPositionInArea(), coinValue);
        }
    }

    private IEnumerator SpawnCoinsPeriodically()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Only spawn if there are fewer than the max allowed coins
            if (currentCoinCount < maxCoinsOnMap)
            {
                SpawnCoin(GetRandomPositionInArea(), coinValue);
            }
        }
    }

    public void SpawnCoin(Vector3 position, int value, bool countCoin = true)
    {
        if (!Runner.IsServer) return; // Only the server spawns coins

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

        // Ensure the coin has a NetworkTransform to synchronize its position
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

            // Check if the spawn position is free (no colliders in the area)
            if (!IsPositionOccupied(spawnPosition))
            {
                validPosition = true;
                break; // Exit the loop when a valid position is found
            }
        }
        
        if (!validPosition)
        {
            Debug.LogWarning("Could not find a valid spawn position. Using default position.");
        }

        return spawnPosition;
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
    
    public void OnCoinCollected()
    {
        currentCoinCount--;
    }
    
    public void RemoveAllCoins()
    {
        if (!Runner.IsServer) return; // Only the server removes coins

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
    
    private bool IsPositionOccupied(Vector3 position)
    {
        // Use Physics.OverlapSphere or any other suitable method to check for colliders
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f); // 0.5f is the radius for checking the area

        // If any colliders are found, the position is occupied
        return colliders.Length > 0;
    }
    
    private void OnDrawGizmos()
    {
        // Set the color of the gizmo (change it as you like)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Green with transparency

        // Draw the wireframe cube to represent the spawn area
        Vector3 size = spawnAreaMax - spawnAreaMin;
        Vector3 center = (spawnAreaMin + spawnAreaMax) / 2;

        Gizmos.DrawWireCube(center, size);
    }
}
