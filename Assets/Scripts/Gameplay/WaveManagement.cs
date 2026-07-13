using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialWaveType { None, ChickenStampede, ChickenBomb, Surround, MassiveLinh2, FinalBoss }

public class WaveSpawner : MonoBehaviour
{
    public static bool PauseSpawn = false;
    
    // Kept for backward compatibility with inspector (though no longer used for logic)
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public int count;
    }

    [System.Serializable]
    public class Wave
    {
        [Header("Wave Info (Legacy)")]
        public string waveName;
        public int requiredKills;
        public SpecialWaveType specialWaveType;
        public EnemySpawnInfo[] enemies;
        [HideInInspector] public bool hasSpawned = false;
    }

    [Header("Legacy Settings")]
    public Wave[] waves;
    public float spawnRadius = 10f;

    private Transform player;

    private int totalKills = 0;
    private bool levelComplete = false;
    
    // Mode State
    private bool isInfiniteMode = false;
    private int mapIndex;
    private SpecialWaveManager specialWaveManager;

    // Boss Progression
    private int killsToSpawnBoss = 20;
    private bool bossSpawned = false;

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

        // Set kills required to spawn Boss based on map
        switch (mapIndex)
        {
            case 0: killsToSpawnBoss = 20; break;
            case 1: killsToSpawnBoss = 30; break;
            case 2: killsToSpawnBoss = 40; break;
            default: killsToSpawnBoss = 20; break;
        }

        // Ensure EnemyPool exists
        EnemyPool.EnsureExists();

        // Check if map was finished before
        if (PlayerPrefs.GetInt("MapFinished_" + mapIndex, 0) == 1)
        {
            isInfiniteMode = true;
            Debug.Log("Map was previously finished. Starting Smart Spawner (Infinite Mode).");
            StartSmartSpawner(storyMode: false);
            return;
        }

        Debug.Log($"Story Mode Started. Kills to spawn Boss: {killsToSpawnBoss}");

        // Start smart spawner as ambient spawn
        StartSmartSpawner(storyMode: true);
    }

    /// <summary>
    /// Initialize the SmartSpawner system.
    /// </summary>
    private void StartSmartSpawner(bool storyMode)
    {
        SmartSpawner spawner = gameObject.GetComponent<SmartSpawner>();
        if (spawner == null)
        {
            spawner = gameObject.AddComponent<SmartSpawner>();
        }
        spawner.Initialize(mapIndex, storyMode, this, specialWaveManager);
    }

    public void OnEnemyKilled(Vector3 deathPos)
    {
        totalKills++;
        Debug.Log("Total Kills: " + totalKills);

        if (isInfiniteMode)
        {
            // Infinite mode just scales up infinitely, no boss win condition
            return;
        }

        // Story Mode Progression
        if (!levelComplete && !bossSpawned && totalKills >= killsToSpawnBoss)
        {
            bossSpawned = true;
            Debug.Log("Boss Spawn Threshold Reached!");
            
            // Spawn Boss
            specialWaveManager.TriggerSpecialWave(SpecialWaveType.FinalBoss);
            
            // Stop ambient and special spawns while fighting boss
            SmartSpawner spawner = GetComponent<SmartSpawner>();
            if (spawner != null)
            {
                spawner.SetActive(false);
            }
        }
    }

    public void OnBossKilled(Vector3 deathPos)
    {
        if (levelComplete) return;

        levelComplete = true;
        PlayerPrefs.SetInt("MapFinished_" + mapIndex, 1);
        PlayerPrefs.Save();
        
        Debug.Log("Boss Killed! Level Complete!");
        SpawnMapDrop(deathPos);
    }

    private void SpawnMapDrop(Vector3 pos)
    {
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

    // Helper for manual spawning (used by SpecialWaveManager)
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

        // Use EnemyPool instead of Instantiate
        GameObject enemyObj;
        if (EnemyPool.Instance != null)
        {
            enemyObj = EnemyPool.Instance.GetEnemy(enemyPrefab, spawnPosition, enemyPrefab.transform.rotation);
        }
        else
        {
            enemyObj = Instantiate(enemyPrefab, spawnPosition, enemyPrefab.transform.rotation);
        }

        if (enemyObj == null) return null;
        
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
}