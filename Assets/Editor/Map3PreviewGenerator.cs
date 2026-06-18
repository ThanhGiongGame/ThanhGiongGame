using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class Map3PreviewGenerator : EditorWindow
{
    private const string RootName = "Map3_Preview";
    private const float ChunkSize = 30f;

    private int radius = 1;
    private int propsPerChunk = 4;
    private int groundCoverPerChunk = 6;
    private int seed = 2303;
    private bool includeClouds = true;

    private GameObject[] willowTrees;
    private GameObject[] simpleTrees;
    private GameObject[] bigTrees;
    private GameObject[] bushes;
    private GameObject[] grasses;
    private GameObject[] rocks;
    private GameObject[] flowers;
    private GameObject[] bamboos;
    private GameObject[] clouds;

    [MenuItem("Thanh Giong/Map Preview/Map 3 Preview Window")]
    public static void ShowWindow()
    {
        GetWindow<Map3PreviewGenerator>("Map 3 Preview");
    }

    [MenuItem("Thanh Giong/Map Preview/Generate Map 3")]
    public static void GenerateDefault()
    {
        Map3PreviewGenerator window = CreateInstance<Map3PreviewGenerator>();
        window.GeneratePreview();
        DestroyImmediate(window);
    }

    [MenuItem("Thanh Giong/Map Preview/Clear Preview")]
    public static void ClearPreviewMenu()
    {
        ClearPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Map 3 Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Generate a Scene View preview of map 3 without entering Play Mode. Runtime map 3 now uses 4 props and 6 ground cover objects per chunk by default; raising these values quickly becomes expensive because bamboo/tree assets are heavy.", MessageType.Info);

        radius = EditorGUILayout.IntSlider("Chunk Radius", radius, 0, 3);
        propsPerChunk = EditorGUILayout.IntSlider("Props / Chunk", propsPerChunk, 0, 10);
        groundCoverPerChunk = EditorGUILayout.IntSlider("Ground Cover / Chunk", groundCoverPerChunk, 0, 38);
        seed = EditorGUILayout.IntField("Seed", seed);
        includeClouds = EditorGUILayout.Toggle("Include Clouds", includeClouds);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Map 3 Preview", GUILayout.Height(34f)))
            {
                GeneratePreview();
            }

            if (GUILayout.Button("Clear Preview", GUILayout.Height(34f)))
            {
                ClearPreview();
            }
        }
    }

    private void GeneratePreview()
    {
        LoadPrefabs();
        ClearPreview();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Map 3 Preview");

        Random.State savedState = Random.state;
        Random.InitState(seed);

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                GenerateChunk(new Vector2Int(x, z), root.transform);
            }
        }

        if (includeClouds)
        {
            GenerateClouds(root.transform);
        }

        Random.state = savedState;
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void ClearPreview()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing == null) return;

        Undo.DestroyObjectImmediate(existing);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void LoadPrefabs()
    {
        willowTrees = LoadModelArray("MapProps/Trees", "Willow_Tree_Summer");
        simpleTrees = LoadModelArray("MapProps/Trees",
            "Simple_Tree_1", "Simple_Tree_2", "Simple_Tree_3", "Simple_Tree_4",
            "Simple_Tree_5", "Simple_Tree_6", "Simple_Tree_7", "Simple_Tree_8");
        bigTrees = LoadModelArray("MapProps/Trees", "Tree_1_Summer", "Tree_2_Summer");
        bushes = LoadModelArray("MapProps/Bushes", "Bush_1", "Bush_2", "Bush_3", "Bush_4", "Bush_5", "Bush_6");
        grasses = LoadModelArray("MapProps/Bushes", "Grass_1", "Grass_2");
        rocks = LoadModelArray("MapProps/Rocks", "Rock_1", "Rock_2", "Rock_3", "Rock_4", "Rock_5");
        flowers = LoadModelArray("MapProps/PlantsAndFlowers", "Flower_1", "Flower_2", "Flower_3", "Flower_4", "Flower_5");
        bamboos = LoadModelArray("MapProps", "bamboo");
        clouds = LoadModelArray("MapProps/Clouds", "Clouds_1", "Clouds_2", "Clouds_3", "Clouds_4");
    }

    private void GenerateChunk(Vector2Int chunkCoord, Transform parent)
    {
        GameObject chunk = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        Undo.RegisterCreatedObjectUndo(chunk, "Generate Map 3 Preview Chunk");
        chunk.transform.SetParent(parent);

        Vector3 chunkOrigin = new Vector3(chunkCoord.x * ChunkSize, 0f, chunkCoord.y * ChunkSize);
        List<Vector3> occupied = new List<Vector3>();

        for (int i = 0; i < propsPerChunk; i++)
        {
            if (TryFindPosition(chunkOrigin, occupied, 6f, out Vector3 pos))
            {
                occupied.Add(pos);
                SpawnMap3Prop(pos, chunk.transform);
            }
        }

        List<Vector3> coverPositions = new List<Vector3>();
        for (int i = 0; i < groundCoverPerChunk; i++)
        {
            if (TryFindPosition(chunkOrigin, coverPositions, 1f, out Vector3 pos))
            {
                coverPositions.Add(pos);
                SpawnMap3GroundCover(pos, chunk.transform);
            }
        }
    }

    private static bool TryFindPosition(Vector3 chunkOrigin, List<Vector3> occupied, float minDistance, out Vector3 position)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 candidate = chunkOrigin + new Vector3(
                Random.Range(1f, ChunkSize - 1f),
                0f,
                Random.Range(1f, ChunkSize - 1f));

            if (candidate.magnitude < 8f) continue;

            bool tooClose = false;
            foreach (Vector3 existing in occupied)
            {
                if (Vector3.Distance(candidate, existing) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                position = candidate;
                return true;
            }
        }

        position = default;
        return false;
    }

    private void SpawnMap3Prop(Vector3 pos, Transform parent)
    {
        float r = Random.value;
        if (r < 0.55f)
        {
            SpawnBambooGroup(pos, parent);
        }
        else if (r < 0.73f)
        {
            SpawnFromArray(Random.value < 0.5f ? willowTrees : simpleTrees, pos, parent, 0.9f, 1.5f);
        }
        else if (r < 0.88f)
        {
            SpawnBushGroup(pos, parent);
        }
        else if (r < 0.94f)
        {
            SpawnRockGroup(pos, parent);
        }
        else
        {
            SpawnFromArray(bigTrees, pos, parent, 1f, 1.6f);
        }
    }

    private void SpawnMap3GroundCover(Vector3 pos, Transform parent)
    {
        float r = Random.value;
        if (r < 0.45f)
        {
            SpawnFromArray(grasses, pos, parent, 0.8f, 1.5f);
        }
        else if (r < 0.60f)
        {
            SpawnFromArray(flowers, pos, parent, 0.5f, 1f);
        }
        else if (r < 0.70f)
        {
            SpawnFromArray(rocks, pos, parent, 0.2f, 0.5f);
        }
        else
        {
            SpawnFromArray(bushes, pos, parent, 0.5f, 1f);
        }
    }

    private void SpawnBambooGroup(Vector3 pos, Transform parent)
    {
        int count = Random.Range(2, 5);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));
            SpawnFromArray(bamboos, pos + offset, parent, 0.7f, 1.4f);
        }
    }

    private void SpawnBushGroup(Vector3 pos, Transform parent)
    {
        int count = Random.Range(1, 4);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            SpawnFromArray(bushes, pos + offset, parent, 0.5f, 1.2f);
        }
    }

    private void SpawnRockGroup(Vector3 pos, Transform parent)
    {
        int count = Random.Range(2, 5);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            SpawnFromArray(rocks, pos + offset, parent, 0.3f, 1f);
        }
    }

    private void GenerateClouds(Transform parent)
    {
        GameObject cloudRoot = new GameObject("Clouds");
        Undo.RegisterCreatedObjectUndo(cloudRoot, "Generate Map 3 Clouds");
        cloudRoot.transform.SetParent(parent);

        for (int i = 0; i < 8; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-80f, 80f), Random.Range(25f, 40f), Random.Range(-80f, 80f));
            SpawnFromArray(clouds, pos, cloudRoot.transform, 2f, 5f);
        }
    }

    private void SpawnFromArray(GameObject[] models, Vector3 pos, Transform parent, float minScale, float maxScale)
    {
        GameObject prefab = PickRandom(models);
        if (prefab == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null)
        {
            instance = Instantiate(prefab);
        }

        Undo.RegisterCreatedObjectUndo(instance, "Generate Map 3 Preview Prop");
        instance.transform.SetParent(parent);
        instance.transform.position = pos;
        instance.transform.rotation = prefab.transform.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        instance.transform.localScale = Vector3.one * Random.Range(minScale, maxScale) * GetScaleMultiplier(prefab.name);
        RemoveAllColliders(instance);
        ConvertErrorMaterials(instance);
    }

    private static GameObject PickRandom(GameObject[] models)
    {
        if (models == null || models.Length == 0) return null;
        return models[Random.Range(0, models.Length)];
    }

    private static GameObject[] LoadModelArray(string folder, params string[] names)
    {
        List<GameObject> loaded = new List<GameObject>();
        foreach (string name in names)
        {
            GameObject prefab = Resources.Load<GameObject>($"{folder}/{name}");
            if (prefab != null)
            {
                loaded.Add(prefab);
            }
        }

        return loaded.ToArray();
    }

    private static float GetScaleMultiplier(string prefabName)
    {
        string lower = prefabName.ToLowerInvariant();
        if (lower.Contains("bamboo")) return 0.5f;
        if (lower.Contains("tree")) return 45f;
        if (lower.Contains("bush")) return 45f;
        if (lower.Contains("rock")) return 250f;
        if (lower.Contains("flower") || lower.Contains("mushroom") || lower.Contains("grass")) return 50f;
        return 1f;
    }

    private static void RemoveAllColliders(GameObject go)
    {
        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            DestroyImmediate(collider);
        }
    }

    private static void ConvertErrorMaterials(GameObject go)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            Material source = renderer.sharedMaterial;
            if (source == null) continue;

            Shader shader = source.shader;
            if (shader != null && !shader.name.Contains("Error") && !shader.name.Contains("glTF") && !shader.name.Contains("Standard"))
            {
                continue;
            }

            Material material = new Material(urpLit);
            if (source.HasProperty("_BaseColor")) material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            else if (source.HasProperty("_Color")) material.SetColor("_BaseColor", source.GetColor("_Color"));

            if (source.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            else if (source.HasProperty("_MainTex")) material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));

            renderer.sharedMaterial = material;
        }
    }
}
