using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pool for enemies. Auto-loads all enemy prefabs from Resources/Prefabs/
/// that have an Enemy component. Reuses GameObjects instead of Instantiate/Destroy.
/// </summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    // Prefab registry: prefabName -> prefab reference
    private Dictionary<string, GameObject> _prefabRegistry = new Dictionary<string, GameObject>();

    // Pool storage: prefabName -> queue of inactive GameObjects
    private Dictionary<string, Queue<GameObject>> _pool = new Dictionary<string, Queue<GameObject>>();

    // Track all active enemies for despawn checks
    private List<GameObject> _activeEnemies = new List<GameObject>();
    public IReadOnlyList<GameObject> ActiveEnemies => _activeEnemies;

    // Names of enemy prefabs available for spawning (excludes boss)
    private List<string> _spawnableEnemyNames = new List<string>();
    public IReadOnlyList<string> SpawnableEnemyNames => _spawnableEnemyNames;

    private Transform _poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _poolRoot = new GameObject("EnemyPool_Inactive").transform;
        _poolRoot.SetParent(transform);
        _poolRoot.gameObject.SetActive(false);

        LoadAllPrefabs();
        PrewarmPools();
    }

    /// <summary>
    /// Ensures the singleton exists. Call from any script that needs the pool.
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("EnemyPool");
        go.AddComponent<EnemyPool>();
    }

    /// <summary>
    /// Load all prefabs from Resources/Prefabs/ that have an Enemy component.
    /// </summary>
    private void LoadAllPrefabs()
    {
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("Prefabs");

        foreach (GameObject prefab in allPrefabs)
        {
            Enemy enemyComp = prefab.GetComponent<Enemy>();
            if (enemyComp == null) continue;

            string key = prefab.name;
            if (_prefabRegistry.ContainsKey(key)) continue;

            _prefabRegistry[key] = prefab;
            _pool[key] = new Queue<GameObject>();

            // Boss prefab should not be in the spawnable list
            bool isBoss = prefab.GetComponent<Boss>() != null || key.ToLower().Contains("boss");
            if (!isBoss)
            {
                _spawnableEnemyNames.Add(key);
            }

            Debug.Log($"[EnemyPool] Registered prefab: {key} (isBoss={isBoss})");
        }

        Debug.Log($"[EnemyPool] Total registered: {_prefabRegistry.Count}, spawnable: {_spawnableEnemyNames.Count}");
    }

    /// <summary>
    /// Pre-instantiate a few enemies per type to avoid frame spikes at game start.
    /// </summary>
    private void PrewarmPools()
    {
        foreach (var kvp in _prefabRegistry)
        {
            // Boss doesn't need prewarm
            if (kvp.Key.ToLower().Contains("boss")) continue;

            int prewarmCount = 5;
            for (int i = 0; i < prewarmCount; i++)
            {
                GameObject obj = Instantiate(kvp.Value, Vector3.zero, Quaternion.identity, _poolRoot);
                obj.name = kvp.Key; // Consistent naming for pool lookup
                obj.SetActive(false);
                _pool[kvp.Key].Enqueue(obj);
            }
        }
    }

    /// <summary>
    /// Get an enemy from the pool (or instantiate if pool is empty).
    /// The returned GameObject is active and positioned at spawnPos.
    /// </summary>
    public GameObject GetEnemy(string prefabName, Vector3 spawnPos, Quaternion rotation)
    {
        if (!_prefabRegistry.ContainsKey(prefabName))
        {
            Debug.LogWarning($"[EnemyPool] Prefab '{prefabName}' not registered!");
            return null;
        }

        GameObject obj;

        if (_pool[prefabName].Count > 0)
        {
            obj = _pool[prefabName].Dequeue();

            // Safety: if the pooled object was destroyed externally
            if (obj == null)
            {
                obj = Instantiate(_prefabRegistry[prefabName], spawnPos, rotation);
                obj.name = prefabName;
            }
            else
            {
                obj.transform.SetParent(null);
                obj.transform.position = spawnPos;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
        }
        else
        {
            obj = Instantiate(_prefabRegistry[prefabName], spawnPos, rotation);
            obj.name = prefabName;
        }

        // Reset enemy state
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.ResetForPool();
        }

        _activeEnemies.Add(obj);
        return obj;
    }

    /// <summary>
    /// Overload: get enemy using the prefab GameObject directly.
    /// </summary>
    public GameObject GetEnemy(GameObject prefab, Vector3 spawnPos, Quaternion rotation)
    {
        if (prefab == null) return null;

        string key = prefab.name;

        // If not registered (e.g. a one-off prefab), register it now
        if (!_prefabRegistry.ContainsKey(key))
        {
            _prefabRegistry[key] = prefab;
            _pool[key] = new Queue<GameObject>();
        }

        return GetEnemy(key, spawnPos, rotation);
    }

    /// <summary>
    /// Return an enemy to the pool instead of destroying it.
    /// </summary>
    public void ReturnEnemy(GameObject enemyObj)
    {
        if (enemyObj == null) return;

        _activeEnemies.Remove(enemyObj);

        string key = enemyObj.name;

        // Clean up key (remove "(Clone)" if present)
        if (key.Contains("(Clone)"))
        {
            key = key.Replace("(Clone)", "").Trim();
            enemyObj.name = key;
        }

        // If we don't have a pool for this key, just destroy
        if (!_pool.ContainsKey(key))
        {
            Destroy(enemyObj);
            return;
        }

        // Deactivate and reparent
        enemyObj.SetActive(false);
        enemyObj.transform.SetParent(_poolRoot);
        enemyObj.transform.position = Vector3.zero;

        _pool[key].Enqueue(enemyObj);
    }

    /// <summary>
    /// Get the prefab reference by name (used by WaveSpawner for multiplier setup).
    /// </summary>
    public GameObject GetPrefab(string prefabName)
    {
        if (_prefabRegistry.TryGetValue(prefabName, out GameObject prefab))
            return prefab;
        return null;
    }

    /// <summary>
    /// Clean up: remove null entries from active list (destroyed externally).
    /// Called periodically by SmartSpawner.
    /// </summary>
    public void CleanupActiveList()
    {
        _activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
    }

    /// <summary>
    /// Get how many enemies are currently alive.
    /// </summary>
    public int AliveCount
    {
        get
        {
            CleanupActiveList();
            return _activeEnemies.Count;
        }
    }
}
