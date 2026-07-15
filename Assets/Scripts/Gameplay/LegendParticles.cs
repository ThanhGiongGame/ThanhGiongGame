using UnityEngine;

/// <summary>
/// Static helpers for spawning runtime particle systems on legend weapons.
/// All particles build themselves at runtime — no prefabs or assets needed.
/// </summary>
public static class LegendParticles
{
    // -------------------------------------------------------
    // Ground Dust — brown earth particles (rocks, bamboo)
    // -------------------------------------------------------
    public static void AddGroundDust(GameObject parent, float rate = 12f)
    {
        GameObject go = new GameObject("FX_GroundDust");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop             = true;
        main.playOnAwake      = true;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier  = 0.4f;
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.34f, 0.12f, 0.95f),
            new Color(0.72f, 0.55f, 0.28f, 0.65f)
        );
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.25f;

        // Fade out over lifetime
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.6f, 0.38f, 0.15f), 0f),
                    new GradientColorKey(new Color(0.75f, 0.62f, 0.44f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode   = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = 2;
        rend.material     = ParticleMat(new Color(0.6f, 0.38f, 0.15f));

        ps.Play();
    }

    // -------------------------------------------------------
    // Water Splash — blue water particles (waves, water attacks)
    // -------------------------------------------------------
    public static void AddWaterSplash(GameObject parent, float rate = 20f, float radius = 0.8f)
    {
        GameObject go = new GameObject("FX_WaterSplash");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop             = true;
        main.playOnAwake      = true;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(1.5f, 5f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier  = -0.15f; // drift upward slightly
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(0.10f, 0.50f, 1.0f, 0.95f),
            new Color(0.35f, 0.85f, 1.0f, 0.75f)
        );
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        // Emit from a ring (circle) to look like a ripple wave
        var shape = ps.shape;
        shape.enabled       = true;
        shape.shapeType     = ParticleSystemShapeType.Circle;
        shape.radius        = radius;
        shape.radiusThickness = 0f;  // emit from edge only

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.2f, 0.6f, 1f), 0f),
                    new GradientColorKey(new Color(0.7f, 0.93f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode   = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = 2;
        rend.material     = ParticleMat(new Color(0.2f, 0.6f, 1f));

        ps.Play();
    }

    // -------------------------------------------------------
    // Rising Water Particles — for whirlpools
    // -------------------------------------------------------
    public static void AddRisingWaterParticles(GameObject parent, float rate = 30f, float radius = 2f)
    {
        GameObject go = new GameObject("FX_RisingWater");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop             = true;
        main.playOnAwake      = true;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(2.0f, 4.0f); // Move upward
        main.startSize        = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier  = -0.2f; // Rise up
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(0.10f, 0.50f, 1.0f, 0.8f),
            new Color(0.35f, 0.85f, 1.0f, 0.6f)
        );
        main.maxParticles = 150;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled       = true;
        shape.shapeType     = ParticleSystemShapeType.Circle;
        shape.radius        = radius;
        // Rotate shape so it points upward (assuming default is emitting along Z, circle usually is XY plane, emit along Z)
        // Actually, circle in Unity Particle System shape emits along Z. If we want it to emit UP, we rotate -90 on X.
        shape.rotation      = new Vector3(-90f, 0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.2f, 0.6f, 1f), 0f),
                    new GradientColorKey(new Color(0.7f, 0.93f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.0f, 0f), new GradientAlphaKey(0.8f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode   = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = 2;
        rend.material     = ParticleMat(new Color(0.2f, 0.6f, 1f));

        ps.Play();
    }

    // -------------------------------------------------------
    // One-shot burst on impact (dust or water)
    // -------------------------------------------------------
    public static void BurstAt(Vector3 position, Color color, int count = 30, float speed = 6f)
    {
        GameObject go = new GameObject("FX_Burst");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop             = false;
        main.playOnAwake      = false;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startColor       = new ParticleSystem.MinMaxGradient(color, color * 0.7f);
        main.gravityModifier  = 0.8f;
        main.maxParticles     = count * 2;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.15f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material   = ParticleMat(color);

        ps.Play();
        Object.Destroy(go, 2f);
    }

    // -------------------------------------------------------
    // Blob shadow — a soft dark oval on the ground beneath a sprite
    // -------------------------------------------------------
    public static void AddBlobShadow(GameObject parent, float size = 0.8f, float yOffset = -0.45f)
    {
        GameObject shadow = new GameObject("BlobShadow");
        shadow.transform.SetParent(parent.transform, false);
        shadow.transform.localPosition = new Vector3(0f, yOffset, 0f);
        shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // flat on ground
        shadow.transform.localScale    = new Vector3(size, size, 1f);

        SpriteRenderer sr = shadow.AddComponent<SpriteRenderer>();
        sr.sprite       = CircleSprite();
        sr.color        = new Color(0f, 0f, 0f, 0.45f);
        sr.sortingOrder = -5;
        // Don't let the shadow be affected by Billboard; it's a child and always flat
    }

    // -------------------------------------------------------
    // Material helper
    // -------------------------------------------------------
    private static Material ParticleMat(Color color)
    {
        string[] candidates = {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Additive (Soft)",
            "Sprites/Default"
        };
        Shader shader = null;
        foreach (string s in candidates) { shader = Shader.Find(s); if (shader != null) break; }

        Material mat = new Material(shader ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);

        // SrcAlpha / One-Minus for natural alpha blending
        if (mat.HasProperty("_SrcBlend") && mat.HasProperty("_DstBlend"))
        {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",   0);
            mat.renderQueue = 3000;
        }
        return mat;
    }

    // Procedurally generated soft circle sprite for blob shadow
    private static Sprite _circleSprite;
    private static Sprite CircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;

        const int S = 64;
        Texture2D tex    = new Texture2D(S, S, TextureFormat.RGBA32, false);
        Color[]   pixels = new Color[S * S];
        Vector2   center = new Vector2(S * 0.5f, S * 0.5f);
        float     r      = S * 0.5f - 2f;

        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d     = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - d / r), 1.8f); // soft falloff
                pixels[y * S + x] = new Color(0f, 0f, 0f, alpha);
            }

        tex.SetPixels(pixels);
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        return _circleSprite;
    }
}
