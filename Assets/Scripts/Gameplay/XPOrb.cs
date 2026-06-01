using UnityEngine;

/// <summary>
/// A floating XP orb that spawns on enemy death and flies toward the player.
/// Built entirely at runtime — no prefab required.
/// </summary>
public class XPOrb : MonoBehaviour
{
    [Header("Settings")]
    public float xpAmount    = 10f;
    public float magnetRange = 8f;
    public float magnetSpeed = 14f;

    private Transform _player;
    private float     _floatOffset;
    private float     _spawnY;
    private bool      _attracted;

    // ---- Factory ----
    public static void Spawn(Vector3 position, float xp)
    {
        GameObject go = new GameObject("XPOrb");
        go.transform.position = position + Vector3.up * 0.5f;
        XPOrb orb = go.AddComponent<XPOrb>();
        orb.xpAmount = xp;
        orb.Initialize();
    }

    private void Initialize()
    {
        _spawnY      = transform.position.y;
        _floatOffset = Random.Range(0f, Mathf.PI * 2f);

        // ---- Visual: small glowing sphere ----
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * 0.30f;
        Destroy(sphere.GetComponent<Collider>());
        sphere.GetComponent<Renderer>().material = BuildMaterial(new Color(0.15f, 1f, 0.45f));

        // ---- Trigger collider for collection ----
        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.radius    = 0.6f;
        col.isTrigger = true;

        // ---- Glow particles ----
        SpawnGlowParticle();

        // ---- Find player ----
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= magnetRange || _attracted)
        {
            _attracted = true;
            // Accelerate as it gets closer
            float speed  = magnetSpeed * Mathf.Lerp(1f, 3f, 1f - Mathf.Clamp01(dist / magnetRange));
            Vector3 dir  = (_player.position + Vector3.up * 0.5f) - transform.position;
            transform.position += dir.normalized * speed * Time.deltaTime;
        }
        else
        {
            // Gentle bob
            Vector3 pos = transform.position;
            pos.y = _spawnY + Mathf.Sin(Time.time * 2.5f + _floatOffset) * 0.12f;
            transform.position = pos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (XPManager.Instance != null)
                XPManager.Instance.AddXP(xpAmount);
            Destroy(gameObject);
        }
    }

    // ---- Helpers ----
    private static Material BuildMaterial(Color color)
    {
        string[] candidates = {
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
            "Standard"
        };
        Shader shader = null;
        foreach (string s in candidates)
        {
            shader = Shader.Find(s);
            if (shader != null) break;
        }
        Material mat = new Material(shader ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.5f);
        }
        return mat;
    }

    private void SpawnGlowParticle()
    {
        GameObject psGO = new GameObject("OrbGlow");
        psGO.transform.SetParent(transform, false);
        ParticleSystem ps = psGO.AddComponent<ParticleSystem>();

        Color orbColor = new Color(0.15f, 1f, 0.45f);

        var main = ps.main;
        main.loop            = true;
        main.playOnAwake     = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor      = new ParticleSystem.MinMaxGradient(orbColor, Color.white);
        main.maxParticles    = 30;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.15f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(orbColor, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f),     new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        ps.GetComponent<ParticleSystemRenderer>().material = BuildMaterial(orbColor);
        ps.Play();
    }
}
