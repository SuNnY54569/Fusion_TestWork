using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class CoinSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef coinPrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Vector3 spawnAreaMin;
    [SerializeField] private Vector3 spawnAreaMax;
    [SerializeField] private int maxCoinsOnMap = 10;
    [SerializeField] private int currentCoinCount = 0;
    
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

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
            SpawnCoin();
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
                SpawnCoin();
            }
        }
    }

    private void SpawnCoin()
    {
        if (!Runner.IsServer) return; // Only the server spawns coins

        Vector3 randomPosition = GetRandomPositionInArea();
        NetworkObject spawnedCoin = Runner.Spawn(coinPrefab, randomPosition, Quaternion.identity);

        // Ensure the coin has a NetworkTransform to synchronize its position
        if (!spawnedCoin.TryGetComponent(out NetworkTransform _))
        {
            spawnedCoin.gameObject.AddComponent<NetworkTransform>();
        }
        
        currentCoinCount++;
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
}
