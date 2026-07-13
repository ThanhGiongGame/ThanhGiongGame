using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Smart proximity-based enemy spawner.
/// Replaces infinite mode and provides ambient spawning during story mode.
/// 
/// Features:
/// - Spawn ring: 20-35m around player
/// - Camera frustum rejection: never spawn where player can see
/// - Line-of-sight check: raycast from camera
/// - Auto map config: different enemy types/counts per map
/// - Difficulty scaling over time
/// - Despawn system: return far-away enemies to pool
/// </summary>
public class SmartSpawner : MonoBehaviour
{
    public static SmartSpawner Instance { get; private set; }

    // --- Spawn Ring ---
    [Header("Spawn Ring")]
    public float innerRadius = 20f;
    public float outerRadius = 35f;

    // --- Despawn ---
    [Header("Despawn")]
    public float despawnDistance = 60f;
    public float despawnCheckInterval = 2f;

    // --- State ---
    private Transform _player;
    private Camera _mainCamera;
    private int _mapIndex;
    private bool _isActive = false;
    private bool _isStoryMode = false; // true = story waves active, spawn ambient alongside

    // --- Map Config (auto-determined) ---
    private int _maxEnemies;
    private float _spawnIntervalMin;
    private float _spawnIntervalMax;
    private int _spawnBatchMin;
    private int _spawnBatchMax;
    private List<string> _enemyTypes = new List<string>();
    private Dictionary<string, float> _enemyWeights = new Dictionary<string, float>();

    // --- Difficulty Scaling ---
    private float _hpMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _difficultyTimer = 0f;
    private float _maxEnemyBonusTimer = 0f;
    private int _maxEnemyBonus = 0;

    private const float HP_SCALE_INTERVAL = 60f;   // HP tăng mỗi 60s
    private const float HP_SCALE_AMOUNT = 0.05f;    // +5% mỗi lần
    private const float HP_MAX_MULTIPLIER = 3f;

    private const float SPD_SCALE_INTERVAL = 60f;   // Speed tăng mỗi 60s
    private const float SPD_SCALE_AMOUNT = 0.02f;   // +2% mỗi lần
    private const float SPD_MAX_MULTIPLIER = 1.5f;

    private const float BUDGET_SCALE_INTERVAL = 120f; // Max enemy tăng mỗi 120s
    private const int BUDGET_SCALE_AMOUNT = 2;

    // WaveSpawner reference for kill tracking
    [HideInInspector]
    public WaveSpawner waveSpawner;
    [HideInInspector]
    public SpecialWaveManager specialWaveManager;

    private List<SpecialWaveType> _allowedSpecialEvents = new List<SpecialWaveType>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Initialize and start spawning. Called by WaveSpawner.
    /// </summary>
    public void Initialize(int mapIndex, bool storyMode, WaveSpawner spawner, SpecialWaveManager sManager)
    {
        _mapIndex = mapIndex;
        _isStoryMode = storyMode;
        waveSpawner = spawner;
        specialWaveManager = sManager;

        // Ensure pool exists
        EnemyPool.EnsureExists();

        ConfigureForMap(mapIndex);

        _isActive = true;
        StartCoroutine(SpawnLoop());
        StartCoroutine(DespawnLoop());
        StartCoroutine(DifficultyScalingLoop());
        StartCoroutine(SpecialEventLoop());

        Debug.Log($"[SmartSpawner] Initialized for Map {mapIndex + 1}. " +
                  $"MaxEnemies={_maxEnemies}, Types=[{string.Join(", ", _enemyTypes)}], " +
                  $"StoryMode={storyMode}");
    }

    /// <summary>
    /// Configure enemy types, counts, and spawn rates based on map.
    /// Auto-detects available prefabs from the pool.
    /// </summary>
    private void ConfigureForMap(int mapIndex)
    {
        // Wait for pool to be ready
        if (EnemyPool.Instance == null)
        {
            EnemyPool.EnsureExists();
        }

        _enemyTypes.Clear();
        _enemyWeights.Clear();

        switch (mapIndex)
        {
            case 0: // Map 1 — Ải Thạch Thất (làng quê, dễ)
                _maxEnemies = 12;
                _spawnIntervalMin = 2f;
                _spawnIntervalMax = 3f;
                _spawnBatchMin = 1;
                _spawnBatchMax = 2;

                // Chicken + Linh-1 (chủ yếu chicken, ít linh-1)
                TryAddEnemyType("chicken", 0.65f);
                TryAddEnemyType("linh-1", 0.35f);

                _allowedSpecialEvents = new List<SpecialWaveType> { SpecialWaveType.ChickenStampede, SpecialWaveType.ChickenBomb, SpecialWaveType.Surround };
                break;

            case 1: // Map 2 — Ải Trâu Sơn (chiến trường, trung bình)
                _maxEnemies = 18;
                _spawnIntervalMin = 1.5f;
                _spawnIntervalMax = 2.5f;
                _spawnBatchMin = 1;
                _spawnBatchMax = 3;

                // Linh-1 + Linh-2
                TryAddEnemyType("linh-1", 0.55f);
                TryAddEnemyType("linh-2", 0.35f);
                TryAddEnemyType("chicken", 0.10f);

                _allowedSpecialEvents = new List<SpecialWaveType> { SpecialWaveType.ChickenBomb, SpecialWaveType.Surround, SpecialWaveType.MassiveLinh2 };
                break;

            case 2: // Map 3 — Rừng U Minh (rừng rậm, khó)
                _maxEnemies = 25;
                _spawnIntervalMin = 1f;
                _spawnIntervalMax = 2f;
                _spawnBatchMin = 2;
                _spawnBatchMax = 3;

                // Linh-1 + Linh-2 + Linh-3 (chủ yếu linh-2 và linh-3)
                TryAddEnemyType("linh-1", 0.25f);
                TryAddEnemyType("linh-2", 0.40f);
                TryAddEnemyType("linh-3", 0.35f);

                _allowedSpecialEvents = new List<SpecialWaveType> { SpecialWaveType.Surround, SpecialWaveType.MassiveLinh2 };
                break;

            default: // Fallback
                _maxEnemies = 12;
                _spawnIntervalMin = 2f;
                _spawnIntervalMax = 3f;
                _spawnBatchMin = 1;
                _spawnBatchMax = 2;
                TryAddEnemyType("chicken", 0.5f);
                TryAddEnemyType("linh-1", 0.5f);
                _allowedSpecialEvents = new List<SpecialWaveType> { SpecialWaveType.Surround };
                break;
        }

        // If story mode is active, reduce ambient spawn budget
        if (_isStoryMode)
        {
            _maxEnemies = Mathf.Max(5, _maxEnemies / 2);
            _spawnIntervalMin *= 1.5f;
            _spawnIntervalMax *= 1.5f;
        }
    }

    /// <summary>
    /// Try to add an enemy type. Only adds if the prefab exists in the pool.
    /// </summary>
    private void TryAddEnemyType(string prefabName, float weight)
    {
        if (EnemyPool.Instance != null && EnemyPool.Instance.SpawnableEnemyNames.Contains(prefabName))
        {
            _enemyTypes.Add(prefabName);
            _enemyWeights[prefabName] = weight;
        }
        else
        {
            Debug.LogWarning($"[SmartSpawner] Prefab '{prefabName}' not found in pool, skipping.");
        }
    }

    /// <summary>
    /// Pick a random enemy type based on configured weights.
    /// </summary>
    private string PickRandomEnemyType()
    {
        if (_enemyTypes.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var type in _enemyTypes)
        {
            totalWeight += _enemyWeights[type];
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var type in _enemyTypes)
        {
            cumulative += _enemyWeights[type];
            if (roll <= cumulative)
            {
                return type;
            }
        }

        return _enemyTypes[_enemyTypes.Count - 1];
    }

    // ===================================================================
    //  SPAWN LOOP
    // ===================================================================

    private IEnumerator SpawnLoop()
    {
        // Initial delay to let things initialize
        yield return new WaitForSeconds(1f);

        while (_isActive)
        {
            // Wait for player reference
            if (_player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) _player = playerObj.transform;
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // Check pause
            if (WaveSpawner.PauseSpawn)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Check budget
            int effectiveMax = _maxEnemies + _maxEnemyBonus;
            int aliveCount = EnemyPool.Instance != null ? EnemyPool.Instance.AliveCount : 0;

            if (aliveCount < effectiveMax)
            {
                int batchSize = Random.Range(_spawnBatchMin, _spawnBatchMax + 1);
                int canSpawn = effectiveMax - aliveCount;
                batchSize = Mathf.Min(batchSize, canSpawn);

                for (int i = 0; i < batchSize; i++)
                {
                    SpawnOneEnemy();
                    yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));
                }
            }

            // Wait before next spawn cycle
            float interval = Random.Range(_spawnIntervalMin, _spawnIntervalMax);
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// Spawn a single enemy at a valid position.
    /// </summary>
    private void SpawnOneEnemy()
    {
        if (EnemyPool.Instance == null || _player == null || _mainCamera == null) return;
        if (_enemyTypes.Count == 0) return;

        // Find a valid spawn point
        if (!SpawnValidator.FindValidSpawnPoint(
                _player.position, _mainCamera,
                innerRadius, outerRadius,
                out Vector3 spawnPoint))
        {
            return; // No valid point found, skip this cycle
        }

        // Pick enemy type
        string enemyType = PickRandomEnemyType();
        if (enemyType == null) return;

        // Get from pool
        GameObject prefab = EnemyPool.Instance.GetPrefab(enemyType);
        Quaternion rotation = prefab != null ? prefab.transform.rotation : Quaternion.identity;

        GameObject enemyObj = EnemyPool.Instance.GetEnemy(enemyType, spawnPoint, rotation);
        if (enemyObj == null) return;

        // Apply difficulty scaling
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.waveSpawner = waveSpawner;
            enemy.maxHealth *= _hpMultiplier;
            enemy.currentHealth = enemy.maxHealth;
            enemy.moveSpeed *= _speedMultiplier;
        }
    }

    // ===================================================================
    //  DESPAWN LOOP
    // ===================================================================

    private IEnumerator DespawnLoop()
    {
        yield return new WaitForSeconds(3f); // Initial delay

        while (_isActive)
        {
            yield return new WaitForSeconds(despawnCheckInterval);

            if (_player == null || _mainCamera == null) continue;
            if (EnemyPool.Instance == null) continue;

            // Iterate backwards to safely remove
            var activeList = EnemyPool.Instance.ActiveEnemies;
            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                GameObject enemyObj = activeList[i];
                if (enemyObj == null || !enemyObj.activeInHierarchy) continue;

                float distToPlayer = Vector3.Distance(enemyObj.transform.position, _player.position);

                // Only despawn if far AND not visible
                if (distToPlayer > despawnDistance)
                {
                    // Check if outside camera view
                    if (!SpawnValidator.IsInCameraFrustum(_mainCamera, enemyObj.transform.position))
                    {
                        // Check if enemy was recently in combat (don't despawn mid-fight)
                        Enemy enemy = enemyObj.GetComponent<Enemy>();
                        if (enemy != null && enemy.lastCombatTime > 0f &&
                            Time.time - enemy.lastCombatTime < 5f)
                        {
                            continue; // Recently in combat, skip
                        }

                        // Return to pool
                        EnemyPool.Instance.ReturnEnemy(enemyObj);
                    }
                }
            }
        }
    }

    // ===================================================================
    //  DIFFICULTY SCALING
    // ===================================================================

    private IEnumerator DifficultyScalingLoop()
    {
        while (_isActive)
        {
            yield return new WaitForSeconds(1f);

            _difficultyTimer += 1f;
            _maxEnemyBonusTimer += 1f;

            // HP scaling
            if (_difficultyTimer >= HP_SCALE_INTERVAL)
            {
                _difficultyTimer = 0f;

                if (_hpMultiplier < HP_MAX_MULTIPLIER)
                {
                    _hpMultiplier += HP_SCALE_AMOUNT;
                    _hpMultiplier = Mathf.Min(_hpMultiplier, HP_MAX_MULTIPLIER);
                }

                if (_speedMultiplier < SPD_MAX_MULTIPLIER)
                {
                    _speedMultiplier += SPD_SCALE_AMOUNT;
                    _speedMultiplier = Mathf.Min(_speedMultiplier, SPD_MAX_MULTIPLIER);
                }

                Debug.Log($"[SmartSpawner] Difficulty scaled: HP x{_hpMultiplier:F2}, Speed x{_speedMultiplier:F2}");
            }

            // Max enemy budget scaling
            if (_maxEnemyBonusTimer >= BUDGET_SCALE_INTERVAL)
            {
                _maxEnemyBonusTimer = 0f;
                int maxBonus = _maxEnemies; // Can at most double the base amount
                if (_maxEnemyBonus < maxBonus)
                {
                    _maxEnemyBonus += BUDGET_SCALE_AMOUNT;
                    _maxEnemyBonus = Mathf.Min(_maxEnemyBonus, maxBonus);
                    Debug.Log($"[SmartSpawner] Max enemies increased to {_maxEnemies + _maxEnemyBonus}");
                }
            }
        }
    }

    // ===================================================================
    //  SPECIAL EVENT LOOP
    // ===================================================================

    private IEnumerator SpecialEventLoop()
    {
        // Initial delay before first special event
        yield return new WaitForSeconds(Random.Range(45f, 60f));

        while (_isActive)
        {
            if (_player != null && !WaveSpawner.PauseSpawn && _allowedSpecialEvents.Count > 0 && specialWaveManager != null)
            {
                SpecialWaveType eventType = _allowedSpecialEvents[Random.Range(0, _allowedSpecialEvents.Count)];
                Debug.Log($"[SmartSpawner] Triggering automated special event: {eventType}");
                specialWaveManager.TriggerSpecialWave(eventType);
            }

            // Wait 45 to 90 seconds before next event
            yield return new WaitForSeconds(Random.Range(45f, 90f));
        }
    }

    // ===================================================================
    //  PUBLIC API
    // ===================================================================

    /// <summary>
    /// Pause or resume spawning (e.g. during cutscenes).
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;
        if (active && !IsInvoking())
        {
            StartCoroutine(SpawnLoop());
            StartCoroutine(DespawnLoop());
            StartCoroutine(DifficultyScalingLoop());
            StartCoroutine(SpecialEventLoop());
        }
    }

    /// <summary>
    /// Get current difficulty multipliers (used by WaveSpawner for story waves).
    /// </summary>
    public float GetHpMultiplier() => _hpMultiplier;
    public float GetSpeedMultiplier() => _speedMultiplier;

    private void OnDestroy()
    {
        _isActive = false;
        if (Instance == this) Instance = null;
    }
}
