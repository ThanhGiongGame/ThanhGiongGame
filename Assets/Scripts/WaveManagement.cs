using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public int count;
    }

    [System.Serializable]
    public class Wave
    {
        public EnemySpawnInfo[] enemies;
    }

    [Header("Wave Settings")]
    public Wave[] waves;

    public float timeBetweenWaves = 5f;
    public float spawnRadius = 10f;

    private int currentWave = 0;

    private Transform player;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        StartCoroutine(SpawnWaveLoop());
    }

    private System.Collections.IEnumerator SpawnWaveLoop()
    {
        while (currentWave < waves.Length)
        {
            StartCoroutine(SpawnWave(waves[currentWave]));
            currentWave++;

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        List<GameObject> enemiesToSpawn = new List<GameObject>();

        // Add all enemies into one list
        foreach (EnemySpawnInfo enemyInfo in wave.enemies)
        {
            for (int i = 0; i < enemyInfo.count; i++)
            {
                enemiesToSpawn.Add(enemyInfo.enemyPrefab);
            }
        }

        // Shuffle list randomly
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            int randomIndex = Random.Range(i, enemiesToSpawn.Count);

            GameObject temp = enemiesToSpawn[i];
            enemiesToSpawn[i] = enemiesToSpawn[randomIndex];
            enemiesToSpawn[randomIndex] = temp;
        }

        // Spawn one by one with random delay
        foreach (GameObject enemyPrefab in enemiesToSpawn)
        {
            SpawnEnemy(enemyPrefab);

            float randomDelay = Random.Range(0.5f, 2f);
            yield return new WaitForSeconds(randomDelay);
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (player == null) return;

        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;

        Vector3 spawnPosition = player.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}