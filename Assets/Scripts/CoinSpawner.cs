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
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        float z = Random.Range(spawnAreaMin.z, spawnAreaMax.z);

        return new Vector3(x, y, z);
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
}
