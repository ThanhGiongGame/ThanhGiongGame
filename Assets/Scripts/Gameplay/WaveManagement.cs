using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialWaveType { None, ChickenStampede, ChickenBomb, Surround, MassiveLinh2, FinalBoss }

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

        public SpecialWaveType specialWaveType;

        [Header("Enemies (Ignored if Special Wave)")]
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
    
    // Infinite Mode State
    private bool isInfiniteMode = false;
    private int infiniteWaveIndex = 0;
    private int mapIndex;
    private SpecialWaveManager specialWaveManager;
    private int infiniteEnemiesAlive = 0;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        mapIndex = PlayerPrefs.GetInt("SelectedMap", 0);
        specialWaveManager = gameObject.AddComponent<SpecialWaveManager>();
        specialWaveManager.waveSpawner = this;

        // Check if map was finished before
        if (PlayerPrefs.GetInt("MapFinished_" + mapIndex, 0) == 1)
        {
            isInfiniteMode = true;
            Debug.Log("Map was previously finished. Starting Infinite Mode.");
            StartCoroutine(InfiniteModeRoutine());
            return;
        }

        foreach (Wave wave in waves)
        {
            if (wave.specialWaveType != SpecialWaveType.None)
            {
                // Special waves might not count towards strict total enemies for level completion,
                // but let's assume they have a fixed kill requirement that we can optionally track.
                // For simplicity, we just count standard enemies.
            }

            if (wave.enemies != null)
            {
                foreach (EnemySpawnInfo info in wave.enemies)
                {
                    if (info.enemyPrefab == null) continue;
                    string pName = info.enemyPrefab.name;
                    if (pName == "EnemyA" || pName == "EnemyB" || pName == "EnemyC" || pName == "chicken" || 
                        pName == "linh-1" || pName == "linh-2" || pName == "linh-3")
                    {
                        totalEnemiesToSpawn += info.count;
                    }
                }
            }
        }
        
        // Add a small buffer for special spawned enemies (like chickens in stampede)
        // actually, we will only check level complete if all specific waves are spawned.
        Debug.Log("Total enemies to spawn (standard): " + totalEnemiesToSpawn);

        CheckWaveUnlock();
    }

    public void OnEnemyKilled(Vector3 deathPos)
    {
        if (isInfiniteMode)
        {
            infiniteEnemiesAlive--;
            totalKills++;
            return; // Handled by InfiniteModeRoutine
        }

        totalKills++;
        Debug.Log("Total Kills: " + totalKills);

        CheckWaveUnlock();

        // Level Complete Condition
        if (!levelComplete && CheckAllWavesSpawned() && GameObject.FindGameObjectsWithTag("Enemy").Length <= 1)
        {
            levelComplete = true;
            PlayerPrefs.SetInt("MapFinished_" + mapIndex, 1);
            PlayerPrefs.Save();
            SpawnMapDrop(deathPos);
        }
    }

    private bool CheckAllWavesSpawned()
    {
        foreach (Wave wave in waves)
        {
            if (!wave.hasSpawned) return false;
        }
        return true;
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
        if (isInfiniteMode) return;

        for (int i = 0; i < waves.Length; i++)
        {
            Wave wave = waves[i];

            if (!wave.hasSpawned && totalKills >= wave.requiredKills)
            {
                wave.hasSpawned = true;
                Debug.Log("Spawn Wave: " + wave.waveName);

                if (wave.specialWaveType != SpecialWaveType.None)
                {
                    specialWaveManager.TriggerSpecialWave(wave.specialWaveType);
                }
                else
                {
                    StartCoroutine(SpawnWave(wave));
                }
            }
        }
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        List<GameObject> enemiesToSpawn = new List<GameObject>();

        if (wave.enemies != null)
        {
            foreach (EnemySpawnInfo enemyInfo in wave.enemies)
            {
                if (enemyInfo.enemyPrefab == null) continue;
                for (int i = 0; i < enemyInfo.count; i++)
                {
                    enemiesToSpawn.Add(enemyInfo.enemyPrefab);
                }
            }
        }

        // Shuffle
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            int randomIndex = Random.Range(i, enemiesToSpawn.Count);
            GameObject temp = enemiesToSpawn[i];
            enemiesToSpawn[i] = enemiesToSpawn[randomIndex];
            enemiesToSpawn[randomIndex] = temp;
        }

        foreach (GameObject enemyPrefab in enemiesToSpawn)
        {
            while (PauseSpawn)
            {
                yield return new WaitForSeconds(5f);
            }

            SpawnEnemy(enemyPrefab, 1f, 1f); // standard scale

            float randomDelay = Random.Range(0.5f, 2f);
            yield return new WaitForSeconds(randomDelay);
        }
    }

    public GameObject SpawnEnemy(GameObject enemyPrefab, float healthMultiplier, float speedMultiplier, Vector3? specificPos = null)
    {
        if (player == null || enemyPrefab == null) return null;
        
        Vector3 spawnPosition;
        if (specificPos.HasValue)
        {
            spawnPosition = specificPos.Value;
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            spawnPosition = player.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, enemyPrefab.transform.rotation);
        
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.waveSpawner = this;
            enemy.maxHealth *= healthMultiplier;
            enemy.currentHealth = enemy.maxHealth;
            enemy.moveSpeed *= speedMultiplier;
        }
        return enemyObj;
    }

    // --- INFINITE MODE ---

    private IEnumerator InfiniteModeRoutine()
    {
        // Load prefabs based on map
        List<GameObject> nativeEnemies = new List<GameObject>();
        GameObject chicken = Resources.Load<GameObject>("Prefabs/chicken");
        GameObject linh1 = Resources.Load<GameObject>("Prefabs/linh-1");
        GameObject linh2 = Resources.Load<GameObject>("Prefabs/linh-2");
        GameObject linh3 = Resources.Load<GameObject>("Prefabs/linh-3");

        if (mapIndex == 0) // Map 1
        {
            nativeEnemies.Add(chicken);
            nativeEnemies.Add(linh1);
        }
        else if (mapIndex == 1) // Map 2
        {
            nativeEnemies.Add(linh1);
            nativeEnemies.Add(linh2);
        }
        else // Map 3
        {
            nativeEnemies.Add(linh1);
            nativeEnemies.Add(linh2);
            nativeEnemies.Add(linh3);
        }

        while (true)
        {
            while (PauseSpawn) yield return new WaitForSeconds(1f);

            // Wait until enemies are mostly cleared
            if (infiniteEnemiesAlive <= 5)
            {
                infiniteWaveIndex++;
                Debug.Log("Starting Infinite Wave: " + infiniteWaveIndex);

                float hpMult = 1f + (infiniteWaveIndex * 0.1f);
                float spdMult = 1f + (infiniteWaveIndex * 0.02f);
                int spawnCount = 10 + (infiniteWaveIndex * 2);

                if (infiniteWaveIndex % 5 == 0)
                {
                    // Special massive wave!
                    Debug.Log("Massive Infinite Wave!");
                    GameObject bossPrefab = nativeEnemies[nativeEnemies.Count - 1]; // highest tier
                    specialWaveManager.SpawnMassiveEnemy(bossPrefab, hpMult * 5f, spdMult * 0.8f);
                    infiniteEnemiesAlive++;
                    
                    // Also spawn normal enemies
                    spawnCount /= 2;
                }

                for (int i = 0; i < spawnCount; i++)
                {
                    GameObject randomEnemy = nativeEnemies[Random.Range(0, nativeEnemies.Count)];
                    SpawnEnemy(randomEnemy, hpMult, spdMult);
                    infiniteEnemiesAlive++;
                    yield return new WaitForSeconds(Random.Range(0.2f, 1f));
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }
}