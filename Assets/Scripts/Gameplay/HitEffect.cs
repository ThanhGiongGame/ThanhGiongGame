using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns a dramatic, multi-layered hit burst at a world position.
/// Now fully optimized with an Object Pool and Shader caching.
/// </summary>
public class HitEffect : MonoBehaviour
{
    private static Queue<HitEffect> _pool = new Queue<HitEffect>();
    private static Shader _cachedShader;

    private ParticleSystem psFlash;
    private ParticleSystem psShards;
    private ParticleSystem psEmbers;

    private ParticleSystemRenderer rendFlash;
    private ParticleSystemRenderer rendShards;
    private ParticleSystemRenderer rendEmbers;

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    public static void Spawn(Vector3 position, Color color, float scale = 1f)
    {
        HitEffect he = GetFromPool();
        he.transform.position = position;
        he.gameObject.SetActive(true);
        he.Play(color, scale);
    }

    private static HitEffect GetFromPool()
    {
        while (_pool.Count > 0)
        {
            HitEffect he = _pool.Dequeue();
            if (he != null) return he;
        }

        // Create new if pool is empty
        GameObject go = new GameObject("HitEffect_Pooled");
        HitEffect newHe = go.AddComponent<HitEffect>();
        newHe.InitializeLayers();
        return newHe;
    }

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private void InitializeLayers()
    {
        // Create the 3 layers once
        psFlash = CreateLayer("Flash", this.transform, 6, new Vector2(0.5f, 1.1f), new Vector2(10f, 18f), new Vector2(0.12f, 0.20f), 0f, 0.05f, BuildCurve(1f, 0f), out rendFlash);
        psShards = CreateLayer("Shards", this.transform, 20, new Vector2(0.18f, 0.42f), new Vector2(6f, 14f), new Vector2(0.30f, 0.55f), 1.2f, 0.2f, BuildCurve(1f, 0f), out rendShards);
        psEmbers = CreateLayer("Embers", this.transform, 12, new Vector2(0.06f, 0.14f), new Vector2(1.5f, 4f), new Vector2(0.45f, 0.80f), -0.3f, 0.3f, BuildCurve(0.8f, 0f), out rendEmbers);
    }

    private void Play(Color color, float scale)
    {
        // Update properties
        Color flashColor = Color.Lerp(color, Color.white, 0.6f);
        Color emberColor = Color.Lerp(color, Color.white, 0.3f);

        UpdateLayer(psFlash, rendFlash, flashColor, scale, new Vector2(0.5f, 1.1f), new Vector2(10f, 18f));
        UpdateLayer(psShards, rendShards, color, scale, new Vector2(0.18f, 0.42f), new Vector2(6f, 14f));
        UpdateLayer(psEmbers, rendEmbers, emberColor, scale, new Vector2(0.06f, 0.14f), new Vector2(1.5f, 4f));

        psFlash.Play();
        psShards.Play();
        psEmbers.Play();

        Invoke(nameof(ReturnToPool), 1f);
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false);
        _pool.Enqueue(this);
    }

    // -------------------------------------------------------
    // Layer builder
    // -------------------------------------------------------

    private ParticleSystem CreateLayer(
        string layerName, Transform parent, int count,
        Vector2 startSize, Vector2 startSpeed, Vector2 startLifetime,
        float gravity, float sphereRadius, AnimationCurve scaleOverLife,
        out ParticleSystemRenderer rendOut)
    {
        GameObject go = new GameObject(layerName);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(startLifetime.x, startLifetime.y);
        main.gravityModifier = gravity;
        main.maxParticles = count * 2;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = sphereRadius;

        var col = ps.colorOverLifetime;
        col.enabled = true;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, scaleOverLife);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = 5;
        
        rendOut = rend;
        return ps;
    }

    private void UpdateLayer(ParticleSystem ps, ParticleSystemRenderer rend, Color color, float scale, Vector2 startSize, Vector2 startSpeed)
    {
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed.x * scale, startSpeed.y * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize.x * scale, startSize.y * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(color, color * 0.75f);

        var col = ps.colorOverLifetime;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.5f, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        rend.material = GetOrCreateMaterial(color);
    }

    // -------------------------------------------------------
    // Material caching
    // -------------------------------------------------------
    private static Dictionary<Color, Material> _materialCache = new Dictionary<Color, Material>();

    private static Material GetOrCreateMaterial(Color color)
    {
        if (_materialCache.TryGetValue(color, out Material cached))
        {
            return cached;
        }

        if (_cachedShader == null)
        {
            string[] shaderNames = new[]
            {
                "Universal Render Pipeline/Particles/Unlit",
                "Particles/Standard Unlit",
                "Legacy Shaders/Particles/Additive (Soft)",
                "Legacy Shaders/Particles/Additive",
                "Sprites/Default"
            };

            foreach (string sName in shaderNames)
            {
                _cachedShader = Shader.Find(sName);
                if (_cachedShader != null) break;
            }

            if (_cachedShader == null)
            {
                _cachedShader = Shader.Find("Standard");
            }
        }

        Material mat = new Material(_cachedShader);
        Color hdrColor = color * 1.5f;
        hdrColor.a = color.a;

        if (mat.HasProperty("_Color"))      mat.SetColor("_Color",      hdrColor);
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  hdrColor);
        if (mat.HasProperty("_TintColor"))  mat.SetColor("_TintColor",  color);
        
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", hdrColor);
        }

        if (mat.HasProperty("_SrcBlend") && mat.HasProperty("_DstBlend"))
        {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",   0);
            mat.renderQueue = 3000;
        }
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   0f);

        _materialCache[color] = mat;
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
