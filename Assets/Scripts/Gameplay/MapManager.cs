using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MapManager v3 — Realistic chunk-based prop spawning using real 3D models.
/// Loads FBX assets from Resources/MapProps/ at runtime.
/// Vietnamese-themed landscapes with natural colors.
/// Reduced fog for clear enemy visibility.
/// </summary>
public class MapManager : MonoBehaviour
{
    // --- Chunk settings ---
    private const float CHUNK_SIZE = 30f;
    private const int VIEW_RANGE = 3;
    private const int DESPAWN_RANGE = 5;
    private const int PROPS_PER_CHUNK = 10;
    private const int GROUND_COVER_PER_CHUNK = 14;

    // --- State ---
    private Transform _player;
    private Transform _propsRoot;
    private int _mapIndex;
    private Dictionary<Vector2Int, GameObject> _activeChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int _lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    private bool _cloudsSpawned;

    // --- Cached model prefabs (loaded from Resources) ---
    // Trees
    private GameObject[] _oakTrees;
    private GameObject[] _pineTrees;
    private GameObject[] _simpleTrees;
    private GameObject[] _willowTrees;
    private GameObject[] _brokenTrees;
    private GameObject[] _palmTrees;
    private GameObject[] _bigTrees;
    // Bushes
    private GameObject[] _bushes;
    private GameObject[] _grasses;
    // Rocks
    private GameObject[] _rocks;
    // Plants
    private GameObject[] _flowers;
    private GameObject[] _mushrooms;
    // Clouds
    private GameObject[] _clouds;

    // --- Cached fallback materials for procedural props ---
    private Material _matWoodPost, _matFireHolder, _matFireParticle;
    private Material _matStoneBrick;
    private Material _matWoodWall, _matWoodRoof;
    private Material _matTrunkBark;
    private Material _matMossGreen;
    private Material _matFireflyGlow;
    private Material _matCloudWhite;

    private void Start()
    {
        _mapIndex = PlayerPrefs.GetInt("SelectedMap", 0);
        ConfigureEnvironment(_mapIndex);
        LoadModelPrefabs(_mapIndex);
        InitFallbackMaterials();

        var rootGO = new GameObject("MapProps_Dynamic");
        _propsRoot = rootGO.transform;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "SampleScene")
        {
            if (FindObjectOfType<MapManager>() == null)
            {
                GameObject mapGO = new GameObject("MapManager");
                mapGO.AddComponent<MapManager>();
            }
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO == null)
            {
                var pc = FindObjectOfType<PlayerController>();
                if (pc != null) playerGO = pc.gameObject;
            }
            if (playerGO != null) _player = playerGO.transform;
            else return;
        }

        Vector2Int currentChunk = WorldToChunk(_player.position);
        if (currentChunk == _lastPlayerChunk) return;
        _lastPlayerChunk = currentChunk;

        for (int x = currentChunk.x - VIEW_RANGE; x <= currentChunk.x + VIEW_RANGE; x++)
        {
            for (int z = currentChunk.y - VIEW_RANGE; z <= currentChunk.y + VIEW_RANGE; z++)
            {
                Vector2Int key = new Vector2Int(x, z);
                if (!_activeChunks.ContainsKey(key))
                {
                    _activeChunks[key] = GenerateChunk(key);
                }
            }
        }

        if (!_cloudsSpawned)
        {
            SpawnCloudLayer();
            _cloudsSpawned = true;
        }

        var toRemove = new List<Vector2Int>();
        foreach (var kvp in _activeChunks)
        {
            int dist = Mathf.Max(Mathf.Abs(kvp.Key.x - currentChunk.x), Mathf.Abs(kvp.Key.y - currentChunk.y));
            if (dist > DESPAWN_RANGE)
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove) _activeChunks.Remove(key);
    }

    private Vector2Int WorldToChunk(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / CHUNK_SIZE),
            Mathf.FloorToInt(worldPos.z / CHUNK_SIZE)
        );
    }

    private GameObject GenerateChunk(Vector2Int chunkCoord)
    {
        GameObject chunk = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunk.transform.SetParent(_propsRoot);

        Vector3 chunkOrigin = new Vector3(chunkCoord.x * CHUNK_SIZE, 0f, chunkCoord.y * CHUNK_SIZE);

        Random.State savedState = Random.state;
        Random.InitState(chunkCoord.x * 73856093 ^ chunkCoord.y * 19349663);

        for (int i = 0; i < PROPS_PER_CHUNK; i++)
        {
            Vector3 pos = chunkOrigin + new Vector3(
                Random.Range(1f, CHUNK_SIZE - 1f), 0f, Random.Range(1f, CHUNK_SIZE - 1f)
            );
            if (pos.magnitude < 8f) continue;
            SpawnProp(pos, chunk.transform);
        }

        for (int i = 0; i < GROUND_COVER_PER_CHUNK; i++)
        {
            Vector3 pos = chunkOrigin + new Vector3(
                Random.Range(0.5f, CHUNK_SIZE - 0.5f), 0f, Random.Range(0.5f, CHUNK_SIZE - 0.5f)
            );
            SpawnGroundCover(pos, chunk.transform);
        }

        Random.state = savedState;
        return chunk;
    }

    // ================================================================
    //  LOAD MODELS FROM RESOURCES
    // ================================================================

    private void LoadModelPrefabs(int mapIndex)
    {
        // Trees - load season variants based on map
        string treeSeason;
        switch (mapIndex)
        {
            case 1: treeSeason = "Dead"; break;    // Ải Trâu Sơn — cây chết
            case 2: treeSeason = "Summer"; break;  // Rừng U Minh — rậm rạp
            default: treeSeason = "Spring"; break;  // Ải Thạch Thất — xuân tươi
        }

        _oakTrees = LoadModelArray("MapProps/Trees", $"Oak_Tree_{treeSeason}");
        _pineTrees = LoadModelArray("MapProps/Trees", "Pine_Tree_1_" + treeSeason, "Pine_Tree_2_" + treeSeason);
        _willowTrees = LoadModelArray("MapProps/Trees", $"Willow_Tree_{treeSeason}");
        _brokenTrees = LoadModelArray("MapProps/Trees", $"Broken_Tree_{treeSeason}");
        _palmTrees = LoadModelArray("MapProps/Trees", $"Palm_Tree_{treeSeason}");
        _bigTrees = LoadModelArray("MapProps/Trees", $"Tree_1_{treeSeason}", $"Tree_2_{treeSeason}");

        // Simple trees (không có mùa, chỉ có normal + winter)
        _simpleTrees = LoadModelArray("MapProps/Trees",
            "Simple_Tree_1", "Simple_Tree_2", "Simple_Tree_3", "Simple_Tree_4",
            "Simple_Tree_5", "Simple_Tree_6", "Simple_Tree_7", "Simple_Tree_8");

        // Bushes
        string bushSuffix = mapIndex == 1 ? "_Snow" : ""; // Map 1 dùng Snow variant cho vẻ khô cằn
        // Load non-snow first, fallback
        _bushes = LoadModelArray("MapProps/Bushes",
            "Bush_1" + bushSuffix, "Bush_2" + bushSuffix, "Bush_3" + bushSuffix,
            "Bush_4" + bushSuffix, "Bush_5" + bushSuffix, "Bush_6" + bushSuffix);

        // Nếu không load được snow variant, fallback sang normal
        if (_bushes.Length == 0 && bushSuffix != "")
        {
            _bushes = LoadModelArray("MapProps/Bushes", "Bush_1", "Bush_2", "Bush_3", "Bush_4", "Bush_5", "Bush_6");
        }

        _grasses = LoadModelArray("MapProps/Bushes", "Grass_1", "Grass_2");

        // Rocks
        _rocks = LoadModelArray("MapProps/Rocks", "Rock_1", "Rock_2", "Rock_3", "Rock_4", "Rock_5");

        // Plants
        _flowers = LoadModelArray("MapProps/PlantsAndFlowers", "Flower_1", "Flower_2", "Flower_3", "Flower_4", "Flower_5");
        _mushrooms = LoadModelArray("MapProps/PlantsAndFlowers", "Mushroom_1", "Mushroom_2", "Mushroom_3", "Mushroom_4");

        // Clouds
        _clouds = LoadModelArray("MapProps/Clouds", "Clouds_1", "Clouds_2", "Clouds_3", "Clouds_4");
    }

    /// <summary> Load multiple models from Resources by name patterns </summary>
    private GameObject[] LoadModelArray(string folder, params string[] names)
    {
        var loaded = new List<GameObject>();
        foreach (string name in names)
        {
            string path = $"{folder}/{name}";
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                loaded.Add(prefab);
            }
        }
        return loaded.ToArray();
    }

    /// <summary> Pick a random model from an array, or null if empty </summary>
    private GameObject PickRandom(GameObject[] models)
    {
        if (models == null || models.Length == 0) return null;
        return models[Random.Range(0, models.Length)];
    }

    /// <summary> Instantiate a model at position with random Y rotation and scale </summary>
    private GameObject SpawnModel(GameObject prefab, Vector3 pos, Transform parent, float minScale = 0.8f, float maxScale = 1.2f)
    {
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
        float scale = Random.Range(minScale, maxScale);
        instance.transform.localScale = Vector3.one * scale;

        // Xóa tất cả Collider để tránh cản trở gameplay
        RemoveAllColliders(instance);

        return instance;
    }

    private void RemoveAllColliders(GameObject go)
    {
        var colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            Destroy(col);
        }
    }

    // ================================================================
    //  PROP ROUTING — Dùng model thật
    // ================================================================

    private void SpawnProp(Vector3 pos, Transform parent)
    {
        float r = Random.value;
        switch (_mapIndex)
        {
            case 0: // Ải Thạch Thất — Đồng quê Việt Nam
                if (r < 0.15f)
                    SpawnFromArray(_oakTrees, pos, parent, 1.0f, 2.0f);
                else if (r < 0.28f)
                    SpawnFromArray(_pineTrees, pos, parent, 1.0f, 1.8f);
                else if (r < 0.40f)
                    SpawnFromArray(_simpleTrees, pos, parent, 0.8f, 1.5f);
                else if (r < 0.52f)
                    SpawnFromArray(_willowTrees, pos, parent, 1.0f, 1.8f);
                else if (r < 0.62f)
                    SpawnFromArray(_palmTrees, pos, parent, 0.8f, 1.5f);
                else if (r < 0.72f)
                    SpawnBushGroup(pos, parent);
                else if (r < 0.82f)
                    SpawnRockGroup(pos, parent);
                else if (r < 0.90f)
                    SpawnWoodenHouse(pos, parent);
                else
                    SpawnFromArray(_bigTrees, pos, parent, 1.0f, 2.0f);
                break;

            case 1: // Ải Trâu Sơn — Chiến trường hoang vắng
                if (r < 0.22f)
                    SpawnFromArray(_brokenTrees, pos, parent, 1.0f, 1.8f);
                else if (r < 0.35f)
                    SpawnFlamingBeacon(pos, parent);
                else if (r < 0.48f)
                    SpawnRockGroup(pos, parent);
                else if (r < 0.60f)
                    SpawnStoneRuin(pos, parent);
                else if (r < 0.72f)
                    SpawnFromArray(_pineTrees, pos, parent, 0.6f, 1.2f);
                else if (r < 0.85f)
                    SpawnBushGroup(pos, parent);
                else
                    SpawnFromArray(_simpleTrees, pos, parent, 0.5f, 1.0f);
                break;

            case 2: // Rừng U Minh — Rừng nhiệt đới rậm rạp
                if (r < 0.18f)
                    SpawnFromArray(_oakTrees, pos, parent, 1.2f, 2.5f);
                else if (r < 0.32f)
                    SpawnFromArray(_bigTrees, pos, parent, 1.5f, 2.8f);
                else if (r < 0.44f)
                    SpawnFromArray(_willowTrees, pos, parent, 1.2f, 2.2f);
                else if (r < 0.55f)
                    SpawnFromArray(_simpleTrees, pos, parent, 1.0f, 2.0f);
                else if (r < 0.65f)
                    SpawnBushGroup(pos, parent);
                else if (r < 0.75f)
                    SpawnMossyRockGroup(pos, parent);
                else if (r < 0.85f)
                    SpawnFromArray(_palmTrees, pos, parent, 1.0f, 1.8f);
                else
                    SpawnFireflyCluster(pos, parent);
                break;
        }
    }

    private void SpawnGroundCover(Vector3 pos, Transform parent)
    {
        float r = Random.value;
        switch (_mapIndex)
        {
            case 0: // Đồng quê
                if (r < 0.30f)
                    SpawnFromArray(_grasses, pos, parent, 0.5f, 1.2f);
                else if (r < 0.50f)
                    SpawnFromArray(_flowers, pos, parent, 0.5f, 1.0f);
                else if (r < 0.70f)
                    SpawnFromArray(_rocks, pos, parent, 0.2f, 0.5f);
                else
                    SpawnFromArray(_bushes, pos, parent, 0.3f, 0.6f);
                break;

            case 1: // Chiến trường
                if (r < 0.35f)
                    SpawnFromArray(_grasses, pos, parent, 0.3f, 0.8f);
                else if (r < 0.65f)
                    SpawnFromArray(_rocks, pos, parent, 0.3f, 0.7f);
                else
                    SpawnFromArray(_bushes, pos, parent, 0.2f, 0.5f);
                break;

            case 2: // Rừng rậm
                if (r < 0.25f)
                    SpawnFromArray(_grasses, pos, parent, 0.6f, 1.3f);
                else if (r < 0.45f)
                    SpawnFromArray(_mushrooms, pos, parent, 0.4f, 1.0f);
                else if (r < 0.65f)
                    SpawnFromArray(_flowers, pos, parent, 0.4f, 0.9f);
                else if (r < 0.80f)
                    SpawnFromArray(_bushes, pos, parent, 0.4f, 0.8f);
                else
                    SpawnFromArray(_rocks, pos, parent, 0.3f, 0.6f);
                break;
        }
    }

    // ================================================================
    //  SPAWN HELPERS
    // ================================================================

    private void SpawnFromArray(GameObject[] models, Vector3 pos, Transform parent, float minScale, float maxScale)
    {
        GameObject prefab = PickRandom(models);
        if (prefab != null)
        {
            SpawnModel(prefab, pos, parent, minScale, maxScale);
        }
    }

    private void SpawnBushGroup(Vector3 pos, Transform parent)
    {
        int count = Random.Range(1, 4);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            SpawnFromArray(_bushes, pos + offset, parent, 0.5f, 1.2f);
        }
    }

    private void SpawnRockGroup(Vector3 pos, Transform parent)
    {
        int count = Random.Range(2, 5);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            SpawnFromArray(_rocks, pos + offset, parent, 0.3f, 1.0f);
        }
    }

    private void SpawnMossyRockGroup(Vector3 pos, Transform parent)
    {
        int count = Random.Range(1, 3);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            GameObject rock = PickRandom(_rocks);
            if (rock != null)
            {
                GameObject instance = SpawnModel(rock, pos + offset, parent, 0.5f, 1.2f);
                if (instance != null)
                {
                    // Thêm sphere rêu phủ trên
                    GameObject moss = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(moss.GetComponent<Collider>());
                    moss.transform.SetParent(instance.transform);
                    moss.transform.localPosition = Vector3.up * 0.3f;
                    moss.transform.localScale = new Vector3(0.8f, 0.25f, 0.7f);
                    moss.GetComponent<Renderer>().sharedMaterial = _matMossGreen;
                }
            }
        }
    }

    // ================================================================
    //  PROCEDURAL STRUCTURES (không có model .fbx)
    // ================================================================

    /// <summary> Nhà gỗ Việt Nam </summary>
    private void SpawnWoodenHouse(Vector3 pos, Transform parent)
    {
        GameObject house = new GameObject("WoodenHouse");
        house.transform.position = pos;
        house.transform.SetParent(parent);
        house.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        float w = Random.Range(2.5f, 4f);
        float h = Random.Range(2f, 3f);
        float d = Random.Range(2f, 3.5f);

        // Tường
        GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(walls.GetComponent<Collider>());
        walls.transform.SetParent(house.transform);
        walls.transform.localPosition = new Vector3(0f, h / 2f, 0f);
        walls.transform.localScale = new Vector3(w, h, d);
        walls.GetComponent<Renderer>().sharedMaterial = _matWoodWall;

        // Mái nhà
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(roof.GetComponent<Collider>());
        roof.transform.SetParent(house.transform);
        roof.transform.localPosition = new Vector3(0f, h + 0.4f, 0f);
        roof.transform.localScale = new Vector3(w * 1.3f, 0.15f, d * 1.4f);
        roof.GetComponent<Renderer>().sharedMaterial = _matWoodRoof;

        // Mái nhọn
        GameObject roofTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(roofTop.GetComponent<Collider>());
        roofTop.transform.SetParent(house.transform);
        roofTop.transform.localPosition = new Vector3(0f, h + 0.8f, 0f);
        roofTop.transform.localScale = new Vector3(w * 0.8f, 0.12f, d * 1.1f);
        roofTop.GetComponent<Renderer>().sharedMaterial = _matWoodRoof;

        // 4 cột góc
        float postH = h + 0.5f;
        Vector3[] corners = {
            new Vector3(-w/2f, 0f, -d/2f), new Vector3(w/2f, 0f, -d/2f),
            new Vector3(-w/2f, 0f, d/2f), new Vector3(w/2f, 0f, d/2f)
        };
        foreach (var corner in corners)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(post.GetComponent<Collider>());
            post.transform.SetParent(house.transform);
            post.transform.localPosition = corner + new Vector3(0f, postH / 2f, 0f);
            post.transform.localScale = new Vector3(0.12f, postH / 2f, 0.12f);
            post.GetComponent<Renderer>().sharedMaterial = _matTrunkBark;
        }
    }

    /// <summary> Tàn tích đá (Map 1) </summary>
    private void SpawnStoneRuin(Vector3 pos, Transform parent)
    {
        GameObject ruin = new GameObject("StoneRuin");
        ruin.transform.position = pos;
        ruin.transform.SetParent(parent);
        ruin.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Tường đổ
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(wall.GetComponent<Collider>());
        wall.transform.SetParent(ruin.transform);
        float wallH = Random.Range(1.5f, 3f);
        float wallW = Random.Range(2f, 4f);
        wall.transform.localPosition = new Vector3(0f, wallH / 2f, 0f);
        wall.transform.localScale = new Vector3(wallW, wallH, 0.35f);
        wall.transform.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), 0f, Random.Range(-8f, 8f));
        wall.GetComponent<Renderer>().sharedMaterial = _matStoneBrick;

        // Đống đá rơi
        int debris = Random.Range(2, 4);
        for (int d = 0; d < debris; d++)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(block.GetComponent<Collider>());
            block.transform.SetParent(ruin.transform);
            float bSz = Random.Range(0.3f, 0.7f);
            block.transform.localPosition = new Vector3(
                Random.Range(-wallW / 2f, wallW / 2f), bSz / 2f, Random.Range(0.3f, 1.5f)
            );
            block.transform.localScale = new Vector3(bSz, bSz * 0.7f, bSz * 0.8f);
            block.transform.localRotation = Quaternion.Euler(
                Random.Range(-15f, 15f), Random.Range(0f, 90f), Random.Range(-10f, 10f)
            );
            block.GetComponent<Renderer>().sharedMaterial = _matStoneBrick;
        }
    }

    /// <summary> Ngọn đuốc (Map 1) </summary>
    private void SpawnFlamingBeacon(Vector3 pos, Transform parent)
    {
        GameObject beacon = new GameObject("FlamingBeacon");
        beacon.transform.position = pos;
        beacon.transform.SetParent(parent);

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(post.GetComponent<Collider>());
        post.transform.SetParent(beacon.transform);
        post.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        post.transform.localScale = new Vector3(0.14f, 1.2f, 0.14f);
        post.GetComponent<Renderer>().sharedMaterial = _matWoodPost;

        GameObject cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cross.GetComponent<Collider>());
        cross.transform.SetParent(beacon.transform);
        cross.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        cross.transform.localScale = new Vector3(0.8f, 0.08f, 0.08f);
        cross.GetComponent<Renderer>().sharedMaterial = _matWoodPost;

        GameObject holder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(holder.GetComponent<Collider>());
        holder.transform.SetParent(beacon.transform);
        holder.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        holder.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
        holder.GetComponent<Renderer>().sharedMaterial = _matFireHolder;

        // Particle lửa
        GameObject fireGO = new GameObject("BeaconFire");
        fireGO.transform.position = pos + Vector3.up * 2.55f;
        fireGO.transform.SetParent(beacon.transform);
        var ps = fireGO.AddComponent<ParticleSystem>();
        ConfigureFireParticles(ps);

        // Ánh sáng
        var lightGO = new GameObject("TorchLight");
        lightGO.transform.SetParent(beacon.transform);
        lightGO.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.2f);
        light.range = 6f;
        light.intensity = 1.2f;
    }

    // ================================================================
    //  SPECIAL EFFECTS
    // ================================================================

    /// <summary> Đom đóm (Map 2) </summary>
    private void SpawnFireflyCluster(Vector3 pos, Transform parent)
    {
        GameObject cluster = new GameObject("Fireflies");
        cluster.transform.position = pos;
        cluster.transform.SetParent(parent);

        var sparks = new GameObject("FireflySparks");
        sparks.transform.position = pos + Vector3.up * 1.2f;
        sparks.transform.SetParent(cluster.transform);
        var ps = sparks.AddComponent<ParticleSystem>();
        ConfigureFireflyParticles(ps);

        var lightGO = new GameObject("FireflyLight");
        lightGO.transform.SetParent(cluster.transform);
        lightGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.7f, 0.95f, 0.4f);
        light.range = 4f;
        light.intensity = 0.6f;
    }

    /// <summary> Lớp mây nền </summary>
    private void SpawnCloudLayer()
    {
        GameObject cloudRoot = new GameObject("Clouds");
        cloudRoot.transform.SetParent(_propsRoot);

        int cloudCount = Random.Range(8, 15);
        for (int i = 0; i < cloudCount; i++)
        {
            Vector3 cloudPos = new Vector3(
                Random.Range(-80f, 80f),
                Random.Range(25f, 40f),
                Random.Range(-80f, 80f)
            );

            GameObject cloudPrefab = PickRandom(_clouds);
            if (cloudPrefab != null)
            {
                GameObject instance = SpawnModel(cloudPrefab, cloudPos, cloudRoot.transform, 2f, 5f);
                if (instance != null)
                {
                    // Clouds nhìn từ dưới lên
                    instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
            }
            else
            {
                // Fallback: mây procedural
                SpawnProceduralCloud(cloudPos, cloudRoot.transform);
            }
        }
    }

    private void SpawnProceduralCloud(Vector3 pos, Transform parent)
    {
        GameObject cloud = new GameObject("Cloud");
        cloud.transform.position = pos;
        cloud.transform.SetParent(parent);

        int puffs = Random.Range(3, 6);
        for (int p = 0; p < puffs; p++)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(puff.GetComponent<Collider>());
            puff.transform.SetParent(cloud.transform);
            float sz = Random.Range(3f, 7f);
            puff.transform.localPosition = new Vector3(
                Random.Range(-4f, 4f), Random.Range(-0.5f, 0.5f), Random.Range(-3f, 3f)
            );
            puff.transform.localScale = new Vector3(sz, sz * 0.4f, sz * 0.8f);
            puff.GetComponent<Renderer>().sharedMaterial = _matCloudWhite;
        }
    }

    // ================================================================
    //  ENVIRONMENT CONFIGURATION
    // ================================================================

    private void ConfigureEnvironment(int mapIndex)
    {
        Color groundColor, sunColor, fogColor, ambientColor;
        float sunIntensity, fogDensity;

        switch (mapIndex)
        {
            case 1: // Ải Trâu Sơn
                groundColor = new Color(0.55f, 0.38f, 0.25f);
                sunColor = new Color(1.0f, 0.78f, 0.55f);
                sunIntensity = 0.95f;
                fogColor = new Color(0.55f, 0.40f, 0.30f);
                fogDensity = 0.006f;
                ambientColor = new Color(0.30f, 0.22f, 0.18f);
                break;

            case 2: // Rừng U Minh
                groundColor = new Color(0.10f, 0.18f, 0.12f);
                sunColor = new Color(0.55f, 0.62f, 0.75f);
                sunIntensity = 0.55f;
                fogColor = new Color(0.08f, 0.12f, 0.10f);
                fogDensity = 0.010f;
                ambientColor = new Color(0.08f, 0.12f, 0.10f);
                break;

            default: // Ải Thạch Thất
                groundColor = new Color(0.28f, 0.48f, 0.22f);
                sunColor = new Color(1.0f, 0.95f, 0.88f);
                sunIntensity = 1.1f;
                fogColor = new Color(0.65f, 0.72f, 0.60f);
                fogDensity = 0.004f;
                ambientColor = new Color(0.30f, 0.32f, 0.28f);
                break;
        }

        var plane = GameObject.Find("Plane");
        if (plane != null)
        {
            var renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(renderer.sharedMaterial);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", groundColor);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", groundColor);
                renderer.material = mat;
            }
        }

        var dirLightGO = GameObject.Find("Directional Light");
        if (dirLightGO == null)
        {
            var lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
                if (l.type == LightType.Directional) { dirLightGO = l.gameObject; break; }
        }
        if (dirLightGO != null)
        {
            var light = dirLightGO.GetComponent<Light>();
            if (light != null) { light.color = sunColor; light.intensity = sunIntensity; }
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
    }

    // ================================================================
    //  FALLBACK MATERIALS
    // ================================================================

    private void InitFallbackMaterials()
    {
        _matTrunkBark = BuildMaterial(new Color(0.35f, 0.25f, 0.15f));
        _matWoodWall = BuildMaterial(new Color(0.50f, 0.38f, 0.22f));
        _matWoodRoof = BuildMaterial(new Color(0.30f, 0.18f, 0.10f));
        _matStoneBrick = BuildMaterial(new Color(0.42f, 0.38f, 0.35f));
        _matWoodPost = BuildMaterial(new Color(0.28f, 0.18f, 0.10f));
        _matFireHolder = BuildMaterial(new Color(0.12f, 0.10f, 0.08f));
        _matFireParticle = BuildMaterial(new Color(1f, 0.5f, 0.1f));
        _matMossGreen = BuildMaterial(new Color(0.18f, 0.38f, 0.15f));
        _matFireflyGlow = BuildMaterial(new Color(0.7f, 0.95f, 0.4f), true);
        _matCloudWhite = BuildMaterial(new Color(0.92f, 0.92f, 0.90f));
    }

    // ================================================================
    //  PARTICLE SYSTEMS
    // ================================================================

    private void ConfigureFireParticles(ParticleSystem ps)
    {
        Color fireOrange = new Color(1f, 0.5f, 0.1f);
        Color fireYellow = new Color(1f, 0.8f, 0.15f);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(fireOrange, fireYellow);
        main.gravityModifier = -0.3f;
        main.maxParticles = 25;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(fireOrange, 0f), new GradientColorKey(new Color(0.3f, 0.1f, 0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material = _matFireParticle;
    }

    private void ConfigureFireflyParticles(ParticleSystem ps)
    {
        Color fireflyColor = new Color(0.7f, 0.95f, 0.4f);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(fireflyColor, new Color(0.5f, 0.8f, 0.3f));
        main.gravityModifier = -0.02f;
        main.maxParticles = 12;

        var emission = ps.emission;
        emission.rateOverTime = 3f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(fireflyColor, 0f), new GradientColorKey(fireflyColor, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material = _matFireflyGlow;
    }

    // ================================================================
    //  MATERIAL BUILDER
    // ================================================================

    private static Material BuildMaterial(Color color, bool isEmissive = false)
    {
        string[] shaderNames = new[]
        {
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
            "Unlit/Color",
            "Standard"
        };

        Shader shader = null;
        foreach (string sName in shaderNames)
        {
            shader = Shader.Find(sName);
            if (shader != null) break;
        }

        Material mat = new Material(shader ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

        if (isEmissive && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.5f);
        }

        return mat;
    }
}
