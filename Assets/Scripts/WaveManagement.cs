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
            SpawnWave(waves[currentWave]);

            currentWave++;

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void SpawnWave(Wave wave)
    {
        foreach (EnemySpawnInfo enemyInfo in wave.enemies)
        {
            for (int i = 0; i < enemyInfo.count; i++)
            {
                SpawnEnemy(enemyInfo.enemyPrefab);
            }
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