using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Map1Generator
{
    private static Transform root;
    private static Material grass, grassDark, grassLight, dirt, water, rock, rockDark, wood, roof, wall, crop, bamboo, pine, trunk, sand, black;
    private static System.Random rnd;

    [MenuItem("Tools/Generate/map 1")]
    public static void Generate()
    {
        var scenePath = "Assets/Scenes/map 1.unity";
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        var old = GameObject.Find("map1");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);

        var rootGo = new GameObject("map1");
        root = rootGo.transform;
        rnd = new System.Random(7);
        CreateMaterials();

        Cube("Ground_180x130", new Vector3(0, -0.08f, 0), new Vector3(180, 0.15f, 130), grass, root);
        Cube("Central_Village_Plateau", new Vector3(-8, 0.02f, -2), new Vector3(64, 0.18f, 46), grassLight, root);
        Cube("Western_Rice_Terraces", new Vector3(-65, 0.07f, 18), new Vector3(32, 0.2f, 28), grassDark, root);
        for (int i = 0; i < 6; i++) Cube("Rice_Terrace_Level_" + i, new Vector3(-68 + i * 2.2f, 0.18f + i * 0.05f, 6 + i * 4), new Vector3(30 - i * 3, 0.12f, 2.2f), dirt, root);

        Strip("Wide_Curving_River", new[] { new Vector3(30, 0.12f, 62), new Vector3(22, 0.12f, 40), new Vector3(27, 0.12f, 20), new Vector3(18, 0.12f, 2), new Vector3(22, 0.12f, -18), new Vector3(9, 0.12f, -44), new Vector3(2, 0.12f, -65) }, 6.0f, water);
        Strip("Main_Dirt_Road", new[] { new Vector3(-86, 0.19f, -45), new Vector3(-58, 0.19f, -25), new Vector3(-30, 0.19f, -8), new Vector3(-4, 0.19f, -2), new Vector3(25, 0.19f, 8), new Vector3(58, 0.19f, 25), new Vector3(86, 0.19f, 22) }, 3.3f, dirt);
        Strip("North_Dirt_Road", new[] { new Vector3(-16, 0.2f, -2), new Vector3(-25, 0.2f, 18), new Vector3(-10, 0.2f, 37), new Vector3(20, 0.2f, 44), new Vector3(53, 0.2f, 48) }, 2.6f, dirt);
        Strip("South_Dirt_Road", new[] { new Vector3(-12, 0.2f, -8), new Vector3(-4, 0.2f, -28), new Vector3(30, 0.2f, -43), new Vector3(78, 0.2f, -38) }, 2.8f, dirt);
        Strip("Village_Side_Path", new[] { new Vector3(-42, 0.21f, 3), new Vector3(-25, 0.21f, 1), new Vector3(-9, 0.21f, 6), new Vector3(8, 0.21f, 11) }, 2.0f, dirt);

        var lake = Cyl("Large_NorthEast_Lake", new Vector3(54, 0.13f, 50), new Vector3(28, 0.04f, 15), water, root);
        lake.transform.rotation = Quaternion.Euler(0, 18, 0);

        House("Village_Temple_Center", new Vector3(-6, 0.18f, 1), new Vector3(7, 1, 5), true);
        House("Long_Hall", new Vector3(-17, 0.18f, -10), new Vector3(11, 1, 3), false);
        House("Market_House_A", new Vector3(-28, 0.18f, -19), new Vector3(5, 1, 4), false);
        House("Market_House_B", new Vector3(-39, 0.18f, -27), new Vector3(6, 1, 4), false);
        House("Farm_House_East", new Vector3(19, 0.18f, -8), new Vector3(5, 1, 4), false);
        House("Lake_Hut", new Vector3(43, 0.18f, 32), new Vector3(4, 1, 3), false);
        House("South_Farm_Hut", new Vector3(60, 0.18f, -42), new Vector3(5, 1, 4), false);
        House("Terrace_Hut", new Vector3(-61, 0.18f, 0), new Vector3(5, 1, 4), false);
        House("Woodcutter_Hut", new Vector3(-63, 0.18f, -34), new Vector3(5, 1, 4), false);

        for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) Cube("Crop_Row", new Vector3(51 + c * 3, 0.25f, -31 - r * 4), new Vector3(1.2f, 0.18f, 2.8f), crop, root);
        for (int r = 0; r < 2; r++) for (int c = 0; c < 5; c++) Cube("Village_Crop_Row", new Vector3(8 + c * 2.2f, 0.25f, -15 - r * 3), new Vector3(1.0f, 0.18f, 2.2f), crop, root);
        for (int i = 0; i < 18; i++) Cube("Wood_Log", new Vector3(-39 + i % 6 * 1.1f, 0.35f, -13 + i / 6 * 1.2f), new Vector3(0.85f, 0.18f, 0.18f), wood, root);

        RockCluster("East_Mine", new Vector3(62, 0.2f, 2), 38, 15);
        RockCluster("South_Quarry", new Vector3(-2, 0.2f, -45), 30, 13);
        RockCluster("Village_Boulders", new Vector3(2, 0.2f, 10), 12, 8);
        for (int i = 0; i < 9; i++)
        {
            var m = Sphere("LowPoly_Hill", new Vector3(58 + i * 4, 0.2f, -2 + (i % 3) * 8), new Vector3(7 + i % 2 * 3, 3.2f, 6), rock, root);
            m.transform.rotation = Quaternion.Euler(0, i * 23, 0);
        }
        for (int i = 0; i < 5; i++)
        {
            var m = Sphere("North_Hill", new Vector3(-14 + i * 7, 0.2f, 44 + i % 2 * 5), new Vector3(8, 2.4f, 6), rock, root);
            m.transform.rotation = Quaternion.Euler(0, i * 31, 0);
        }
        Cube("West_Cave_Mouth", new Vector3(-78, 1.2f, -8), new Vector3(5, 2.4f, 2.2f), black, root);
        Sphere("West_Cave_Rock", new Vector3(-78, 1.1f, -7.2f), new Vector3(8, 3.2f, 4.5f), rockDark, root);

        var forest = new GameObject("Dense_Western_Pine_Forest").transform;
        forest.SetParent(root);
        for (int i = 0; i < 150; i++)
        {
            float x = -84 + (float)rnd.NextDouble() * 42;
            float z = -57 + (float)rnd.NextDouble() * 108;
            if (x > -50 && z > -5 && z < 30) continue;
            Tree(new Vector3(x, 0.18f, z), 3.5f + (float)rnd.NextDouble() * 2.4f, pine, forest);
        }
        var grove = new GameObject("Central_Bamboo_Grove").transform;
        grove.SetParent(root);
        for (int i = 0; i < 105; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2;
            float d = 8 + (float)rnd.NextDouble() * 21;
            BambooStem(new Vector3(-2 + Mathf.Cos(a) * d, 0.18f, 9 + Mathf.Sin(a) * d), 3.5f + (float)rnd.NextDouble() * 2.5f, grove);
        }

        Cube("River_Bridge", new Vector3(13, 0.55f, -27), new Vector3(8, 0.35f, 3.2f), wood, root);
        Cube("Bridge_Rail_A", new Vector3(13, 1.0f, -25.2f), new Vector3(8.4f, 0.25f, 0.25f), wood, root);
        Cube("Bridge_Rail_B", new Vector3(13, 1.0f, -28.8f), new Vector3(8.4f, 0.25f, 0.25f), wood, root);
        for (int i = 0; i < 10; i++) Cyl("Hay_Stack", new Vector3(37 + i % 5 * 2.2f, 0.42f, -33 - i / 5 * 2.3f), new Vector3(0.75f, 0.42f, 0.75f), sand, root);
        for (int i = 0; i < 12; i++) Cube("Fence_Post", new Vector3(-3 + i * 1.5f, 0.55f, -19), new Vector3(0.15f, 0.9f, 0.15f), wood, root);

        SetupCameraAndLight();
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.78f);
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.ambientLight = new Color(0.58f, 0.62f, 0.58f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void CreateMaterials()
    {
        grass = Mat("LP_Grass_Meadow", new Color(0.38f, 0.63f, 0.29f));
        grassDark = Mat("LP_Grass_Dark", new Color(0.24f, 0.48f, 0.23f));
        grassLight = Mat("LP_Grass_Light", new Color(0.53f, 0.72f, 0.34f));
        dirt = Mat("LP_Dirt_Road", new Color(0.67f, 0.53f, 0.34f));
        water = Mat("LP_Water_River", new Color(0.42f, 0.72f, 0.78f), 0.55f);
        rock = Mat("LP_Rock", new Color(0.47f, 0.45f, 0.39f));
        rockDark = Mat("LP_Rock_Dark", new Color(0.32f, 0.32f, 0.29f));
        wood = Mat("LP_Wood", new Color(0.42f, 0.25f, 0.13f));
        roof = Mat("LP_Roof_Terracotta", new Color(0.55f, 0.22f, 0.12f));
        wall = Mat("LP_Wall_Warm", new Color(0.74f, 0.61f, 0.42f));
        crop = Mat("LP_Crops", new Color(0.34f, 0.58f, 0.23f));
        bamboo = Mat("LP_Bamboo", new Color(0.48f, 0.57f, 0.20f));
        pine = Mat("LP_Pine", new Color(0.16f, 0.32f, 0.20f));
        trunk = Mat("LP_Trunk", new Color(0.31f, 0.20f, 0.12f));
        sand = Mat("LP_Sand", new Color(0.73f, 0.65f, 0.47f));
        black = Mat("LP_Cave_Dark", new Color(0.08f, 0.08f, 0.075f));
    }

    private static Material Mat(string name, Color color, float smooth = 0.15f)
    {
        if (!AssetDatabase.IsValidFolder("Assets/GeneratedMap")) AssetDatabase.CreateFolder("Assets", "GeneratedMap");
        if (!AssetDatabase.IsValidFolder("Assets/GeneratedMap/Materials")) AssetDatabase.CreateFolder("Assets/GeneratedMap", "Materials");
        var path = "Assets/GeneratedMap/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static GameObject Cube(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject Cyl(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject Sphere(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject Strip(string name, Vector3[] centers, float width, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        var mesh = new Mesh();
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < centers.Length; i++)
        {
            Vector3 dir = i == 0 ? centers[1] - centers[0] : (i == centers.Length - 1 ? centers[i] - centers[i - 1] : centers[i + 1] - centers[i - 1]);
            var perp = new Vector3(-dir.z, 0, dir.x).normalized * width * 0.5f;
            verts.Add(centers[i] + perp);
            verts.Add(centers[i] - perp);
        }
        for (int i = 0; i < centers.Length - 1; i++)
        {
            int a = i * 2;
            tris.Add(a); tris.Add(a + 2); tris.Add(a + 1);
            tris.Add(a + 1); tris.Add(a + 2); tris.Add(a + 3);
        }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static void Tree(Vector3 p, float h, Material leaf, Transform parent)
    {
        Cyl("Tree_Trunk", p + Vector3.up * h * 0.22f, new Vector3(0.22f, h * 0.45f, 0.22f), trunk, parent);
        var c1 = Cyl("Tree_Crown", p + Vector3.up * h * 0.62f, new Vector3(1.1f, h * 0.34f, 1.1f), leaf, parent);
        c1.transform.rotation = Quaternion.Euler(0, 45, 0);
        var c2 = Cyl("Tree_Crown_Top", p + Vector3.up * h * 0.88f, new Vector3(0.75f, h * 0.25f, 0.75f), leaf, parent);
        c2.transform.rotation = Quaternion.Euler(0, 45, 0);
    }

    private static void BambooStem(Vector3 p, float h, Transform parent)
    {
        Cyl("Bamboo_Stem", p + Vector3.up * h * 0.5f, new Vector3(0.08f, h * 0.5f, 0.08f), bamboo, parent);
        Sphere("Bamboo_Top", p + Vector3.up * h, new Vector3(0.8f, 0.35f, 0.8f), bamboo, parent);
    }

    private static void House(string name, Vector3 p, Vector3 s, bool temple)
    {
        var g = new GameObject(name);
        g.transform.SetParent(root);
        Cube("Base", p + new Vector3(0, 0.55f, 0), new Vector3(s.x, 1.1f, s.z), wall, g.transform);
        var r = Cube("Roof", p + new Vector3(0, 1.35f, 0), new Vector3(s.x * 1.25f, 0.55f, s.z * 1.2f), temple ? roof : wood, g.transform);
        r.transform.rotation = Quaternion.Euler(0, 45, 0);
        Cube("Door", p + new Vector3(0, 0.45f, -s.z * 0.51f), new Vector3(s.x * 0.22f, 0.65f, 0.06f), wood, g.transform);
        if (temple)
        {
            Cube("Upper_Floor", p + new Vector3(0, 2.05f, 0), new Vector3(s.x * 0.65f, 0.65f, s.z * 0.62f), wall, g.transform);
            var rr = Cube("Upper_Roof", p + new Vector3(0, 2.55f, 0), new Vector3(s.x * 0.9f, 0.42f, s.z * 0.85f), roof, g.transform);
            rr.transform.rotation = Quaternion.Euler(0, 45, 0);
        }
    }

    private static void RockCluster(string label, Vector3 center, int count, float radius)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2;
            float d = (float)rnd.NextDouble() * radius;
            var p = center + new Vector3(Mathf.Cos(a) * d, 0.25f, Mathf.Sin(a) * d);
            var s = 0.8f + (float)rnd.NextDouble() * 1.8f;
            var ro = Sphere(label + "_Rock", p, new Vector3(s, s * 0.65f, s), i % 3 == 0 ? rockDark : rock, root);
            ro.transform.rotation = Quaternion.Euler(rnd.Next(0, 40), rnd.Next(0, 180), rnd.Next(0, 40));
        }
    }

    private static void SetupCameraAndLight()
    {
        var camObj = GameObject.Find("Main Camera");
        if (camObj == null)
        {
            camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        var cam = camObj.GetComponent<Camera>();
        cam.transform.position = new Vector3(0, 72, -82);
        cam.transform.rotation = Quaternion.Euler(55, 0, 0);
        cam.fieldOfView = 55;
        cam.farClipPlane = 500;

        var lightObj = GameObject.Find("Directional Light");
        if (lightObj == null)
        {
            lightObj = new GameObject("Directional Light");
            lightObj.AddComponent<Light>().type = LightType.Directional;
        }
        var light = lightObj.GetComponent<Light>();
        light.transform.rotation = Quaternion.Euler(50, -35, 0);
        light.intensity = 1.25f;
    }
}
