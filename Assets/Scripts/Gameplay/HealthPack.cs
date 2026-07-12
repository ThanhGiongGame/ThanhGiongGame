using UnityEngine;

/// <summary>
/// A floating Health Pack that spawns on enemy death based on player upgrades and flies toward the player.
/// Built entirely at runtime — no prefab required.
/// </summary>
public class HealthPack : MonoBehaviour
{
    [Header("Settings")]
    public float healAmount  = 25f;
    public float magnetRange = 8f;
    public float magnetSpeed = 14f;

    private Transform _player;
    private float     _floatOffset;
    private float     _spawnY;
    private bool      _attracted;

    // ---- Factory ----
    public static void Spawn(Vector3 position, float heal)
    {
        GameObject go = new GameObject("HealthPack");
        go.transform.position = position + Vector3.up * 0.5f;
        HealthPack pack = go.AddComponent<HealthPack>();
        pack.healAmount = heal;
        pack.Initialize();
    }

    private void Initialize()
    {
        _spawnY      = transform.position.y;
        _floatOffset = Random.Range(0f, Mathf.PI * 2f);

        // ---- Visual: Red glowing heart/cross (represented by a red cube with a white cross or sphere) ----
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
        core.transform.SetParent(transform, false);
        core.transform.localScale = Vector3.one * 0.35f;
        Destroy(core.GetComponent<Collider>());
        core.GetComponent<Renderer>().material = BuildMaterial(new Color(1f, 0.15f, 0.15f)); // Red

        // White cross visual
        GameObject crossH = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossH.transform.SetParent(core.transform, false);
        crossH.transform.localScale = new Vector3(1.2f, 0.3f, 0.3f);
        Destroy(crossH.GetComponent<Collider>());
        crossH.GetComponent<Renderer>().material = BuildMaterial(Color.white);

        GameObject crossV = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossV.transform.SetParent(core.transform, false);
        crossV.transform.localScale = new Vector3(0.3f, 1.2f, 0.3f);
        Destroy(crossV.GetComponent<Collider>());
        crossV.GetComponent<Renderer>().material = BuildMaterial(Color.white);

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
            float speed  = magnetSpeed * Mathf.Lerp(1f, 3f, 1f - Mathf.Clamp01(dist / magnetRange));
            Vector3 dir  = (_player.position + Vector3.up * 0.5f) - transform.position;
            transform.position += dir.normalized * speed * Time.deltaTime;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = _spawnY + Mathf.Sin(Time.time * 2.5f + _floatOffset) * 0.12f;
            transform.position = pos;
            transform.Rotate(Vector3.up, 45f * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.Heal(healAmount);
                // Quick green burst effect
                HitEffect.Spawn(transform.position, new Color(0.15f, 1f, 0.15f), 2.0f);
            }
            Destroy(gameObject);
        }
    }

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
            mat.SetColor("_EmissionColor", color * 2.0f);
        }
        return mat;
    }

    private void SpawnGlowParticle()
    {
        GameObject psGO = new GameObject("HealthGlow");
        psGO.transform.SetParent(transform, false);
        ParticleSystem ps = psGO.AddComponent<ParticleSystem>();

        Color packColor = new Color(1f, 0.2f, 0.2f);

        var main = ps.main;
        main.loop            = true;
        main.playOnAwake     = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor      = new ParticleSystem.MinMaxGradient(packColor, Color.white);
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
            new[] { new GradientColorKey(packColor, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f),     new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        ps.GetComponent<ParticleSystemRenderer>().material = BuildMaterial(packColor);
        ps.Play();
    }
}
