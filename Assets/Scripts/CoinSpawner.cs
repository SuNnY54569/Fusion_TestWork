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
    
    private bool isSpawning = false;

    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        StartCoroutine(SpawnCoinsPeriodically());
    }

    private IEnumerator SpawnCoinsPeriodically()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnCoin();
        }
    }

    private void SpawnCoin()
    {
        if (!Runner.IsServer) return; // Only the server spawns coins

        Vector3 randomPosition = GetRandomPositionInArea();
        NetworkObject spawnedCoin = Runner.Spawn(coinPrefab, randomPosition, Quaternion.identity);
        
        // Ensure synchronization by attaching a NetworkTransform if not already present
        if (!spawnedCoin.TryGetComponent(out NetworkTransform _))
        {
            spawnedCoin.gameObject.AddComponent<NetworkTransform>();
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
        isSpawning = false;
    }
}
