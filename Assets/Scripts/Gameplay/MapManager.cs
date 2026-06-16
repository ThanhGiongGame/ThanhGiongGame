using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MapManager v2 — Dynamic chunk-based prop spawning.
/// Props are generated in square "chunks" around the player.
/// As the player moves, new chunks are spawned and distant ones are despawned.
/// This ensures the world always looks populated no matter how far the player travels.
/// All props are purely visual — colliders are destroyed immediately.
/// </summary>
public class MapManager : MonoBehaviour
{
    // --- Chunk settings ---
    private const float CHUNK_SIZE = 30f;          // Each chunk covers 30x30 meters
    private const int   VIEW_RANGE = 3;            // Spawn chunks in a 7x7 grid around player (3 in each direction)
    private const int   DESPAWN_RANGE = 5;         // Despawn chunks further than 5 chunks away
    private const int   PROPS_PER_CHUNK = 12;      // Number of decorative props per chunk
    private const int   GRASS_PER_CHUNK = 18;      // Number of grass tufts per chunk

    // --- State ---
    private Transform _player;
    private Transform _propsRoot;
    private int _mapIndex;
    private Dictionary<Vector2Int, GameObject> _activeChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int _lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);

    // --- Cached materials (avoid creating hundreds of duplicate materials) ---
    private Material _matBambooStalk, _matBambooLeaf, _matBambooNode;
    private Material _matGrassBlade, _matGrassDark;
    private Material _matStoneBase, _matStoneBody;
    private Material _matWoodPost, _matFireHolder;
    private Material _matCrystalBody;
    private Material _matMysticStalk, _matMysticLeaf;
    private Material _matRock;
    private Material _matFireParticle, _matMysticParticle;

    private void Start()
    {
        _mapIndex = PlayerPrefs.GetInt("SelectedMap", 0);
        ConfigureEnvironment(_mapIndex);
        InitMaterials(_mapIndex);

        // Tạo root container
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
        // Tìm Player nếu chưa có
        if (_player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO == null)
            {
                // Fallback: tìm theo tên hoặc component
                var pc = FindObjectOfType<PlayerController>();
                if (pc != null) playerGO = pc.gameObject;
            }
            if (playerGO != null) _player = playerGO.transform;
            else return;
        }

        Vector2Int currentChunk = WorldToChunk(_player.position);
        if (currentChunk == _lastPlayerChunk) return;
        _lastPlayerChunk = currentChunk;

        // Sinh ô map mới 7x7 quanh người chơi
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

        // Dọn dẹp các ô quá xa
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
    // Tự sinh bản đồ động
    private GameObject GenerateChunk(Vector2Int chunkCoord)
    {
        GameObject chunk = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunk.transform.SetParent(_propsRoot);

        Vector3 chunkOrigin = new Vector3(chunkCoord.x * CHUNK_SIZE, 0f, chunkCoord.y * CHUNK_SIZE);

        // Dùng seed cố định theo chunk coord để mỗi lần qua lại vị trí cây giống nhau
        Random.State savedState = Random.state;
        Random.InitState(chunkCoord.x * 73856093 ^ chunkCoord.y * 19349663);

        // --- Spawn decorative props ---
        for (int i = 0; i < PROPS_PER_CHUNK; i++)
        {
            Vector3 pos = chunkOrigin + new Vector3(
                Random.Range(1f, CHUNK_SIZE - 1f),
                0f,
                Random.Range(1f, CHUNK_SIZE - 1f)
            );

            // Tránh spawn quá gần tâm (0,0) nơi Player xuất hiện
            if (pos.magnitude < 8f) continue;

            SpawnProp(pos, chunk.transform);
        }

        // --- Spawn ground cover (grass/rocks) ---
        for (int i = 0; i < GRASS_PER_CHUNK; i++)
        {
            Vector3 pos = chunkOrigin + new Vector3(
                Random.Range(0.5f, CHUNK_SIZE - 0.5f),
                0f,
                Random.Range(0.5f, CHUNK_SIZE - 0.5f)
            );
            SpawnGroundCover(pos, chunk.transform);
        }

        Random.state = savedState;
        return chunk;
    }

    private void SpawnProp(Vector3 pos, Transform parent)
    {
        switch (_mapIndex)
        {
            case 0: // Ải Thạch Thất
                if (Random.value < 0.3f)
                    SpawnBambooCluster(pos, parent);
                else
                    SpawnDetailedBamboo(pos, parent);
                break;
            case 1: // Ải Trâu Sơn
                if (Random.value < 0.25f)
                    SpawnFlamingBeacon(pos, parent);
                else if (Random.value < 0.5f)
                    SpawnDetailedStonePillar(pos, parent);
                else
                    SpawnLayeredRock(pos, parent);
                break;
            case 2: // Rừng U Minh
                if (Random.value < 0.2f)
                    SpawnGlowingCrystal(pos, parent);
                else
                    SpawnMysticBambooCluster(pos, parent);
                break;
        }
    }

    private void SpawnGroundCover(Vector3 pos, Transform parent)
    {
        switch (_mapIndex)
        {
            case 0:
                SpawnGrassTuft(pos, parent, new Color(0.15f, 0.42f, 0.18f), new Color(0.22f, 0.55f, 0.25f));
                break;
            case 1:
                if (Random.value < 0.5f)
                    SpawnSmallRock(pos, parent);
                else
                    SpawnGrassTuft(pos, parent, new Color(0.35f, 0.28f, 0.15f), new Color(0.42f, 0.32f, 0.18f));
                break;
            case 2:
                if (Random.value < 0.4f)
                    SpawnGrassTuft(pos, parent, new Color(0.04f, 0.18f, 0.22f), new Color(0.06f, 0.25f, 0.32f));
                else
                    SpawnSmallMushroom(pos, parent);
                break;
        }
    }

    // ================================================================
    //  PROP GENERATORS — Detailed multi-part models
    // ================================================================

    /// <summary> Cây tre chi tiết với nhiều đốt tre và cành lá </summary>
    private void SpawnDetailedBamboo(Vector3 pos, Transform parent)
    {
        GameObject bamboo = new GameObject("Bamboo");
        bamboo.transform.position = pos;
        bamboo.transform.SetParent(parent);

        float totalHeight = Random.Range(5f, 9f);
        float width = Random.Range(0.12f, 0.22f);
        int segments = Random.Range(3, 6);
        float segHeight = totalHeight / segments;

        for (int s = 0; s < segments; s++)
        {
            float yBase = s * segHeight;

            // Đốt tre (Cylinder)
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(seg.GetComponent<Collider>());
            seg.transform.SetParent(bamboo.transform);
            seg.transform.localPosition = new Vector3(0f, yBase + segHeight / 2f, 0f);
            seg.transform.localScale = new Vector3(width, segHeight / 2f, width);
            seg.GetComponent<Renderer>().sharedMaterial = _matBambooStalk;

            // Mấu đốt tre (Ring)
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(node.GetComponent<Collider>());
            node.transform.SetParent(bamboo.transform);
            node.transform.localPosition = new Vector3(0f, yBase + segHeight, 0f);
            node.transform.localScale = new Vector3(width * 1.6f, 0.04f, width * 1.6f);
            node.GetComponent<Renderer>().sharedMaterial = _matBambooNode;
        }

        // Tán lá (2-3 cụm)
        int leafClusters = Random.Range(2, 4);
        for (int l = 0; l < leafClusters; l++)
        {
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(leaves.GetComponent<Collider>());
            leaves.transform.SetParent(bamboo.transform);
            float leafY = totalHeight + Random.Range(-0.5f, 0.8f);
            float leafX = Random.Range(-0.6f, 0.6f);
            float leafZ = Random.Range(-0.6f, 0.6f);
            leaves.transform.localPosition = new Vector3(leafX, leafY, leafZ);
            float leafSize = Random.Range(0.7f, 1.3f);
            leaves.transform.localScale = new Vector3(leafSize * 1.2f, leafSize * 0.6f, leafSize);
            leaves.GetComponent<Renderer>().sharedMaterial = _matBambooLeaf;
        }
    }

    /// <summary> Cụm 2-4 cây tre mọc gần nhau </summary>
    private void SpawnBambooCluster(Vector3 pos, Transform parent)
    {
        int count = Random.Range(2, 5);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f));
            SpawnDetailedBamboo(pos + offset, parent);
        }
    }

    /// <summary> Trụ đá cổ chi tiết với nhiều tầng </summary>
    private void SpawnDetailedStonePillar(Vector3 pos, Transform parent)
    {
        GameObject pillar = new GameObject("StonePillar");
        pillar.transform.position = pos;
        pillar.transform.SetParent(parent);

        float height = Random.Range(2.5f, 5f);
        float width = Random.Range(0.4f, 0.8f);

        // Đế đá rộng
        GameObject baseStone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(baseStone.GetComponent<Collider>());
        baseStone.transform.SetParent(pillar.transform);
        baseStone.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        baseStone.transform.localScale = new Vector3(width * 1.8f, 0.4f, width * 1.8f);
        baseStone.GetComponent<Renderer>().sharedMaterial = _matStoneBase;

        // Thân trụ
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(pillar.transform);
        body.transform.localPosition = new Vector3(0f, 0.4f + height / 2f, 0f);
        body.transform.localScale = new Vector3(width, height / 2f, width);
        body.GetComponent<Renderer>().sharedMaterial = _matStoneBody;

        // Đỉnh trụ (trang trí)
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(top.GetComponent<Collider>());
        top.transform.SetParent(pillar.transform);
        top.transform.localPosition = new Vector3(0f, 0.4f + height + 0.15f, 0f);
        top.transform.localScale = new Vector3(width * 1.3f, 0.3f, width * 1.3f);
        top.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        top.GetComponent<Renderer>().sharedMaterial = _matStoneBase;
    }

    /// <summary> Tảng đá phân tầng tự nhiên </summary>
    private void SpawnLayeredRock(Vector3 pos, Transform parent)
    {
        GameObject rock = new GameObject("LayeredRock");
        rock.transform.position = pos;
        rock.transform.SetParent(parent);

        int layers = Random.Range(2, 4);
        float yOffset = 0f;
        for (int l = 0; l < layers; l++)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(slab.GetComponent<Collider>());
            slab.transform.SetParent(rock.transform);
            float w = Random.Range(0.6f, 1.5f) * (1f - l * 0.25f);
            float h = Random.Range(0.2f, 0.5f);
            float d = Random.Range(0.5f, 1.2f) * (1f - l * 0.25f);
            slab.transform.localPosition = new Vector3(Random.Range(-0.15f, 0.15f), yOffset + h / 2f, Random.Range(-0.15f, 0.15f));
            slab.transform.localScale = new Vector3(w, h, d);
            slab.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 90f), Random.Range(-5f, 5f));
            slab.GetComponent<Renderer>().sharedMaterial = _matRock != null ? _matRock : _matStoneBody;
            yOffset += h;
        }
    }

    /// <summary> Ngọn đuốc cháy (Ải Trâu Sơn) </summary>
    private void SpawnFlamingBeacon(Vector3 pos, Transform parent)
    {
        GameObject beacon = new GameObject("FlamingBeacon");
        beacon.transform.position = pos;
        beacon.transform.SetParent(parent);

        // Trụ gỗ
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(post.GetComponent<Collider>());
        post.transform.SetParent(beacon.transform);
        post.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        post.transform.localScale = new Vector3(0.14f, 1.2f, 0.14f);
        post.GetComponent<Renderer>().sharedMaterial = _matWoodPost;

        // Thanh ngang chữ thập
        GameObject cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cross.GetComponent<Collider>());
        cross.transform.SetParent(beacon.transform);
        cross.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        cross.transform.localScale = new Vector3(0.8f, 0.08f, 0.08f);
        cross.GetComponent<Renderer>().sharedMaterial = _matWoodPost;

        // Bát lửa
        GameObject holder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(holder.GetComponent<Collider>());
        holder.transform.SetParent(beacon.transform);
        holder.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        holder.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
        holder.GetComponent<Renderer>().sharedMaterial = _matFireHolder;

        // Hệ thống hạt lửa
        GameObject fireGO = new GameObject("BeaconFire");
        fireGO.transform.position = pos + Vector3.up * 2.55f;
        fireGO.transform.SetParent(beacon.transform);
        var ps = fireGO.AddComponent<ParticleSystem>();
        ConfigureFireParticles(ps);

        // Ánh sáng đuốc
        var lightGO = new GameObject("TorchLight");
        lightGO.transform.SetParent(beacon.transform);
        lightGO.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.1f);
        light.range = 8f;
        light.intensity = 1.5f;
    }

    /// <summary> Pha lê phát sáng (Rừng U Minh) </summary>
    private void SpawnGlowingCrystal(Vector3 pos, Transform parent)
    {
        GameObject crystal = new GameObject("GlowingCrystal");
        crystal.transform.position = pos;
        crystal.transform.SetParent(parent);

        // Nhiều mảnh pha lê xếp chồng
        int shards = Random.Range(2, 4);
        for (int s = 0; s < shards; s++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(shard.GetComponent<Collider>());
            shard.transform.SetParent(crystal.transform);
            float h = Random.Range(0.5f, 1.3f);
            float w = Random.Range(0.15f, 0.3f);
            shard.transform.localPosition = new Vector3(
                Random.Range(-0.25f, 0.25f),
                h / 2f + s * 0.2f,
                Random.Range(-0.25f, 0.25f)
            );
            shard.transform.localRotation = Quaternion.Euler(
                Random.Range(5f, 35f),
                Random.Range(0f, 360f),
                Random.Range(5f, 25f)
            );
            shard.transform.localScale = new Vector3(w, h, w);
            shard.GetComponent<Renderer>().sharedMaterial = _matCrystalBody;
        }

        // Nguồn sáng
        var lightGO = new GameObject("CrystalLight");
        lightGO.transform.SetParent(crystal.transform);
        lightGO.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.15f, 0.85f, 1f);
        light.range = 7f;
        light.intensity = 2.0f;

        // Hạt sáng ma mị
        var sparks = new GameObject("MysticSparks");
        sparks.transform.position = pos + Vector3.up * 0.6f;
        sparks.transform.SetParent(crystal.transform);
        var ps = sparks.AddComponent<ParticleSystem>();
        ConfigureMysticParticles(ps, new Color(0.15f, 0.85f, 1f));
    }

    /// <summary> Cụm tre U Linh (Rừng U Minh) </summary>
    private void SpawnMysticBambooCluster(Vector3 pos, Transform parent)
    {
        int count = Random.Range(1, 4);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
            SpawnMysticBamboo(pos + offset, parent);
        }
    }

    private void SpawnMysticBamboo(Vector3 pos, Transform parent)
    {
        GameObject bamboo = new GameObject("MysticBamboo");
        bamboo.transform.position = pos;
        bamboo.transform.SetParent(parent);

        float totalHeight = Random.Range(5f, 9f);
        float width = Random.Range(0.1f, 0.2f);
        int segments = Random.Range(3, 5);
        float segHeight = totalHeight / segments;

        for (int s = 0; s < segments; s++)
        {
            float yBase = s * segHeight;
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(seg.GetComponent<Collider>());
            seg.transform.SetParent(bamboo.transform);
            seg.transform.localPosition = new Vector3(0f, yBase + segHeight / 2f, 0f);
            seg.transform.localScale = new Vector3(width, segHeight / 2f, width);
            seg.GetComponent<Renderer>().sharedMaterial = _matMysticStalk;
        }

        // Lá
        int leafCount = Random.Range(2, 4);
        for (int l = 0; l < leafCount; l++)
        {
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(leaves.GetComponent<Collider>());
            leaves.transform.SetParent(bamboo.transform);
            leaves.transform.localPosition = new Vector3(
                Random.Range(-0.5f, 0.5f),
                totalHeight + Random.Range(-0.3f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            );
            float sz = Random.Range(0.5f, 1.0f);
            leaves.transform.localScale = new Vector3(sz * 1.1f, sz * 0.5f, sz);
            leaves.GetComponent<Renderer>().sharedMaterial = _matMysticLeaf;
        }
    }

    // ================================================================
    //  GROUND COVER — Cỏ, đá nhỏ, nấm
    // ================================================================

    /// <summary> Bụi cỏ nhiều lớp </summary>
    private void SpawnGrassTuft(Vector3 pos, Transform parent, Color colorLight, Color colorDark)
    {
        GameObject tuft = new GameObject("Grass");
        tuft.transform.position = pos;
        tuft.transform.SetParent(parent);

        int blades = Random.Range(3, 7);
        for (int b = 0; b < blades; b++)
        {
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(blade.GetComponent<Collider>());
            blade.transform.SetParent(tuft.transform);
            float h = Random.Range(0.25f, 0.7f);
            float w = Random.Range(0.04f, 0.1f);
            blade.transform.localPosition = new Vector3(
                Random.Range(-0.3f, 0.3f),
                h / 2f,
                Random.Range(-0.3f, 0.3f)
            );
            blade.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
            blade.transform.localScale = new Vector3(w, h, w * 0.6f);
            blade.GetComponent<Renderer>().sharedMaterial = Random.value < 0.5f ? _matGrassBlade : _matGrassDark;
        }
    }

    /// <summary> Đá nhỏ rải rác </summary>
    private void SpawnSmallRock(Vector3 pos, Transform parent)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(rock.GetComponent<Collider>());
        rock.transform.position = pos;
        rock.transform.SetParent(parent);
        float sz = Random.Range(0.15f, 0.45f);
        rock.transform.localScale = new Vector3(sz * Random.Range(0.8f, 1.5f), sz * 0.6f, sz * Random.Range(0.8f, 1.3f));
        rock.transform.localRotation = Quaternion.Euler(Random.Range(0f, 20f), Random.Range(0f, 360f), Random.Range(0f, 15f));
        rock.GetComponent<Renderer>().sharedMaterial = _matRock != null ? _matRock : _matStoneBody;
    }

    /// <summary> Nấm phát sáng (Rừng U Minh) </summary>
    private void SpawnSmallMushroom(Vector3 pos, Transform parent)
    {
        GameObject mush = new GameObject("Mushroom");
        mush.transform.position = pos;
        mush.transform.SetParent(parent);

        // Thân nấm
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(stem.GetComponent<Collider>());
        stem.transform.SetParent(mush.transform);
        stem.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        stem.transform.localScale = new Vector3(0.06f, 0.1f, 0.06f);
        stem.GetComponent<Renderer>().sharedMaterial = _matMysticStalk;

        // Mũ nấm phát sáng
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(cap.GetComponent<Collider>());
        cap.transform.SetParent(mush.transform);
        float capSize = Random.Range(0.12f, 0.25f);
        cap.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        cap.transform.localScale = new Vector3(capSize, capSize * 0.5f, capSize);
        cap.GetComponent<Renderer>().sharedMaterial = _matCrystalBody;
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
            case 1:
                groundColor = new Color(0.48f, 0.22f, 0.15f);
                sunColor = new Color(0.95f, 0.45f, 0.15f);
                sunIntensity = 0.8f;
                fogColor = new Color(0.4f, 0.18f, 0.1f);
                fogDensity = 0.025f;
                ambientColor = new Color(0.2f, 0.1f, 0.08f);
                break;
            case 2:
                groundColor = new Color(0.05f, 0.10f, 0.15f);
                sunColor = new Color(0.15f, 0.25f, 0.45f);
                sunIntensity = 0.4f;
                fogColor = new Color(0.02f, 0.05f, 0.1f);
                fogDensity = 0.035f;
                ambientColor = new Color(0.03f, 0.05f, 0.08f);
                break;
            default:
                groundColor = new Color(0.18f, 0.45f, 0.25f);
                sunColor = new Color(1.0f, 0.95f, 0.85f);
                sunIntensity = 1.2f;
                fogColor = new Color(0.6f, 0.75f, 0.65f);
                fogDensity = 0.008f;
                ambientColor = new Color(0.25f, 0.28f, 0.26f);
                break;
        }

        // Màu mặt đất
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

        // Ánh sáng
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

        // Sương mù
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
    }

    // ================================================================
    //  MATERIALS — Cached and reused across all chunks
    // ================================================================

    private void InitMaterials(int mapIndex)
    {
        // Common
        _matGrassBlade = BuildMaterial(new Color(0.15f, 0.42f, 0.18f));
        _matGrassDark = BuildMaterial(new Color(0.1f, 0.3f, 0.12f));
        _matRock = BuildMaterial(new Color(0.35f, 0.33f, 0.3f));

        switch (mapIndex)
        {
            case 0: // Ải Thạch Thất
                _matBambooStalk = BuildMaterial(new Color(0.12f, 0.38f, 0.18f));
                _matBambooNode = BuildMaterial(new Color(0.08f, 0.28f, 0.12f));
                _matBambooLeaf = BuildMaterial(new Color(0.22f, 0.55f, 0.30f));
                break;
            case 1: // Ải Trâu Sơn
                _matStoneBase = BuildMaterial(new Color(0.38f, 0.35f, 0.32f));
                _matStoneBody = BuildMaterial(new Color(0.28f, 0.26f, 0.25f));
                _matWoodPost = BuildMaterial(new Color(0.25f, 0.16f, 0.1f));
                _matFireHolder = BuildMaterial(new Color(0.1f, 0.1f, 0.1f));
                _matFireParticle = BuildMaterial(new Color(1f, 0.4f, 0.05f));
                _matGrassBlade = BuildMaterial(new Color(0.35f, 0.28f, 0.15f));
                _matGrassDark = BuildMaterial(new Color(0.28f, 0.22f, 0.12f));
                _matRock = BuildMaterial(new Color(0.4f, 0.3f, 0.22f));
                break;
            case 2: // Rừng U Minh
                _matMysticStalk = BuildMaterial(new Color(0.06f, 0.12f, 0.22f));
                _matMysticLeaf = BuildMaterial(new Color(0.08f, 0.32f, 0.42f));
                _matCrystalBody = BuildMaterial(new Color(0.15f, 0.85f, 1f), true);
                _matMysticParticle = BuildMaterial(new Color(0.15f, 0.85f, 1f));
                _matGrassBlade = BuildMaterial(new Color(0.04f, 0.18f, 0.22f));
                _matGrassDark = BuildMaterial(new Color(0.03f, 0.12f, 0.16f));
                break;
        }
    }

    // ================================================================
    //  PARTICLE SYSTEMS
    // ================================================================

    private void ConfigureFireParticles(ParticleSystem ps)
    {
        Color fireOrange = new Color(1f, 0.4f, 0.05f);
        Color fireYellow = new Color(1f, 0.8f, 0.10f);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(fireOrange, fireYellow);
        main.gravityModifier = -0.3f;
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(fireOrange, 0f), new GradientColorKey(new Color(0.25f, 0.08f, 0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material = _matFireParticle != null ? _matFireParticle : BuildMaterial(fireOrange);
    }

    private void ConfigureMysticParticles(ParticleSystem ps, Color color)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
        main.gravityModifier = -0.05f;
        main.maxParticles = 15;

        var emission = ps.emission;
        emission.rateOverTime = 5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material = _matMysticParticle != null ? _matMysticParticle : BuildMaterial(color);
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
            mat.SetColor("_EmissionColor", color * 2.5f);
        }

        return mat;
    }
}
