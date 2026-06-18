using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public static bool PauseSpawn = false;
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public int count;
    }

    [System.Serializable]
    public class Wave
    {
        [Header("Wave Info")]
        public string waveName;

        [Header("Spawn this wave when total kills reach")]
        public int requiredKills;

        [Header("Enemies")]
        public EnemySpawnInfo[] enemies;

        [HideInInspector]
        public bool hasSpawned = false;
    }

    [Header("Wave Settings")]
    public Wave[] waves;

    public float spawnRadius = 10f;

    private Transform player;

    private int totalKills = 0;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        // Spawn all waves có requiredKills = 0
        CheckWaveUnlock();
    }

    // Gọi hàm này khi enemy chết
    public void OnEnemyKilled()
    {
        totalKills++;

        Debug.Log("Total Kills: " + totalKills);

        CheckWaveUnlock();
    }

    private void CheckWaveUnlock()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            Wave wave = waves[i];

            if (!wave.hasSpawned && totalKills >= wave.requiredKills)
            {
                wave.hasSpawned = true;

                Debug.Log("Spawn Wave: " + wave.waveName);

                StartCoroutine(SpawnWave(wave));
            }
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
            while (true)
            {

                if (!PauseSpawn)
                {
                    SpawnEnemy(enemyPrefab);
                }

                float randomDelay = Random.Range(0.5f, 2f);

                yield return new WaitForSeconds(randomDelay);
            }
        }
    }

    private GameObject FindEnemyBPrefab()
    {
        if (waves != null)
        {
            foreach (var w in waves)
            {
                if (w.enemies != null)
                {
                    foreach (var info in w.enemies)
                    {
                        if (info.enemyPrefab != null && info.enemyPrefab.name == "EnemyB")
                        {
                            return info.enemyPrefab;
                        }
                    }
                }
            }
        }
        return null;
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (player == null) return;

        // "xóa cái con ngựa của địch á, nó xuất hiện trong map 1 á"
        // Nếu bản đồ đang chọn là Map 1 (index 0) và địch là EnemyA (ngựa), đổi sang EnemyB (đi bộ)
        if (PlayerPrefs.GetInt("SelectedMap", 0) == 0 && enemyPrefab != null && enemyPrefab.name == "EnemyA")
        {
            GameObject enemyB = FindEnemyBPrefab();
            if (enemyB != null)
            {
                enemyPrefab = enemyB;
            }
        }

        Vector2 randomCircle =
            Random.insideUnitCircle.normalized * spawnRadius;

        Vector3 spawnPosition =
            player.position +
            new Vector3(randomCircle.x, 0f, randomCircle.y);

        GameObject enemyObj = Instantiate(
            enemyPrefab,
            spawnPosition,
            enemyPrefab.transform.rotation
        );

        // Auto gán WaveSpawner vào enemy
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.waveSpawner = this;
        }
    }
}