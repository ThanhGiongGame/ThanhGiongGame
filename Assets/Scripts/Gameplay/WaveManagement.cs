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
    private int totalEnemiesToSpawn = 0;
    private bool levelComplete = false;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        foreach (Wave wave in waves)
        {
            foreach (EnemySpawnInfo info in wave.enemies)
            {
                if (info.enemyPrefab == null) continue;
                string pName = info.enemyPrefab.name;
                if (pName == "EnemyA" || pName == "EnemyB" || pName == "EnemyC" || pName == "chicken")
                {
                    totalEnemiesToSpawn += info.count;
                }
            }
        }
        Debug.Log("Total enemies to spawn: " + totalEnemiesToSpawn);

        // Spawn all waves có requiredKills = 0
        CheckWaveUnlock();
    }

    // Gọi hàm này khi enemy chết
    public void OnEnemyKilled(Vector3 deathPos)
    {
        totalKills++;

        Debug.Log("Total Kills: " + totalKills + "/" + totalEnemiesToSpawn);

        CheckWaveUnlock();

        if (!levelComplete && totalEnemiesToSpawn > 0 && totalKills >= totalEnemiesToSpawn)
        {
            levelComplete = true;
            SpawnMapDrop(deathPos);
        }
    }

    private void SpawnMapDrop(Vector3 pos)
    {
        Debug.Log("Level Complete! Spawning Map Drop.");
        GameObject mapDrop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mapDrop.name = "MapUnlockDrop";
        mapDrop.transform.position = pos + Vector3.up * 1f;
        mapDrop.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        mapDrop.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Collider oldCol = mapDrop.GetComponent<Collider>();
        if (oldCol != null) Destroy(oldCol);

        SphereCollider trigger = mapDrop.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 3f;

        Renderer rend = mapDrop.GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(1f, 0.84f, 0f);

        mapDrop.AddComponent<MapUnlockItem>();
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

        // Add all enemies into one list (filtering out non-standard enemies)
        foreach (EnemySpawnInfo enemyInfo in wave.enemies)
        {
            if (enemyInfo.enemyPrefab == null) continue;
            string prefabName = enemyInfo.enemyPrefab.name;

            // Only allow standard enemy types: EnemyA, EnemyB, EnemyC, chicken
            // Skip villagers, minions (they have broken visuals / sink below ground)
            if (prefabName != "EnemyA" && prefabName != "EnemyB" && prefabName != "EnemyC" && prefabName != "chicken")
            {
                continue;
            }
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
            while (PauseSpawn)
            {
                yield return new WaitForSeconds(0.5f);
            }

            SpawnEnemy(enemyPrefab);

            float randomDelay = Random.Range(0.5f, 2f);
            yield return new WaitForSeconds(randomDelay);
        }
    }


    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (player == null || enemyPrefab == null) return;
        if (enemyPrefab.name != "EnemyA" && enemyPrefab.name != "EnemyB" && enemyPrefab.name != "EnemyC" && enemyPrefab.name != "chicken") return;
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

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.waveSpawner = this;
        }
    }
}