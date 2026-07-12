using UnityEngine;

/// <summary>
/// Spawns a dramatic, multi-layered hit burst at a world position.
/// Three layers: a core flash, chunky main shards, and floating embers.
/// No prefab or external assets needed — all built at runtime.
/// </summary>
public class HitEffect : MonoBehaviour
{
    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    /// <summary>Spawn a hit burst at a world position.</summary>
    /// <param name="position">World-space hit position.</param>
    /// <param name="color">Base color. Red for player, yellow for enemy.</param>
    /// <param name="scale">Overall size multiplier (default 1 = full size).</param>
    public static void Spawn(Vector3 position, Color color, float scale = 1f)
    {
        GameObject go = new GameObject("HitEffect");
        go.transform.position = position;
        HitEffect he = go.AddComponent<HitEffect>();
        he.Play(color, scale);
    }

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private void Play(Color color, float scale)
    {
        float lifetime = 0.55f;

        // Layer 1 — Core flash: large, very short-lived, expands outward fast
        SpawnLayer(
            name:          "Flash",
            parent:        transform,
            color:         Color.Lerp(color, Color.white, 0.6f),
            count:         6,
            startSize:     new Vector2(0.5f, 1.1f),
            startSpeed:    new Vector2(10f, 18f),
            startLifetime: new Vector2(0.12f, 0.20f),
            gravity:       0f,
            sphereRadius:  0.05f,
            scale:         scale,
            scaleOverLife: BuildCurve(1f, 0f)
        );

        // Layer 2 — Main shards: chunky, fast, fly in all directions
        SpawnLayer(
            name:          "Shards",
            parent:        transform,
            color:         color,
            count:         20,
            startSize:     new Vector2(0.18f, 0.42f),
            startSpeed:    new Vector2(6f, 14f),
            startLifetime: new Vector2(0.30f, 0.55f),
            gravity:       1.2f,
            sphereRadius:  0.2f,
            scale:         scale,
            scaleOverLife: BuildCurve(1f, 0f)
        );

        // Layer 3 — Embers: small, slow, float upward and linger
        SpawnLayer(
            name:          "Embers",
            parent:        transform,
            color:         Color.Lerp(color, Color.white, 0.3f),
            count:         12,
            startSize:     new Vector2(0.06f, 0.14f),
            startSpeed:    new Vector2(1.5f, 4f),
            startLifetime: new Vector2(0.45f, 0.80f),
            gravity:       -0.3f,   // float up slightly
            sphereRadius:  0.3f,
            scale:         scale,
            scaleOverLife: BuildCurve(0.8f, 0f)
        );

        Destroy(gameObject, lifetime + 0.3f);
    }

    // -------------------------------------------------------
    // Layer builder
    // -------------------------------------------------------

    private void SpawnLayer(
        string name,
        Transform parent,
        Color color,
        int count,
        Vector2 startSize,
        Vector2 startSpeed,
        Vector2 startLifetime,
        float gravity,
        float sphereRadius,
        float scale,
        AnimationCurve scaleOverLife)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        // ---- Main ----
        var main = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(startLifetime.x, startLifetime.y);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(startSpeed.x * scale, startSpeed.y * scale);
        main.startSize       = new ParticleSystem.MinMaxCurve(startSize.x * scale, startSize.y * scale);
        main.startColor      = new ParticleSystem.MinMaxGradient(color, color * 0.75f);
        main.gravityModifier = gravity;
        main.maxParticles    = count * 2;

        // ---- Emission: single burst ----
        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        // ---- Shape ----
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = sphereRadius;

        // ---- Color over lifetime: fade to transparent ----
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.5f, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        // ---- Size over lifetime ----
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, scaleOverLife);

        // ---- Renderer — assign a real material to avoid the pink/purple fallback ----
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode   = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = 5;
        rend.material     = BuildMaterial(color);

        ps.Play();
    }

    // -------------------------------------------------------
    // Material helper — tries render pipelines in priority order
    // -------------------------------------------------------

    private static Material BuildMaterial(Color color)
    {
        // Priority: URP Additive (triggers bloom) → URP Unlit → Legacy
        string[] shaderNames = new[]
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Additive (Soft)",
            "Legacy Shaders/Particles/Additive",
            "Sprites/Default"
        };

        Shader shader = null;
        foreach (string sName in shaderNames)
        {
            shader = Shader.Find(sName);
            if (shader != null) break;
        }

        if (shader == null)
        {
            Debug.LogWarning("HitEffect: Could not find a particle shader. Particles may appear pink.");
            return new Material(Shader.Find("Standard"));
        }

        Material mat = new Material(shader);

        // Mild HDR — bright enough to glow without turning the scene orange
        Color hdrColor = color * 1.5f;
        hdrColor.a = color.a;

        if (mat.HasProperty("_Color"))      mat.SetColor("_Color",      hdrColor);
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  hdrColor);
        if (mat.HasProperty("_TintColor"))  mat.SetColor("_TintColor",  color); // legacy particles
        
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", hdrColor);
        }

        // Alpha blend — additive was turning the whole scene orange
        if (mat.HasProperty("_SrcBlend") && mat.HasProperty("_DstBlend"))
        {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",   0);
            mat.renderQueue = 3000;
        }
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   0f); // Alpha, not Additive

        return mat;
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private static AnimationCurve BuildCurve(float start, float end)
    {
        return new AnimationCurve(
            new Keyframe(0f, start, 0f, -2f),
            new Keyframe(1f, end,  -2f,  0f)
        );
    }
}
