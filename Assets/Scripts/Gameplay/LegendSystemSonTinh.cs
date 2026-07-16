using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LegendSystemSonTinh : MonoBehaviour
{
    private int w1Level;
    private int w2Level;
    private int evoLevel;

    // W1: Arc Rock
    private float rockTimer = 0f;
    private float rockRate => 12.0f - (w1Level * 0.2f); // Slower frequency
    private float rockRadius => 10.0f + (w1Level * 0.8f); 
    private float rockDamage => 40f + (w1Level * 15f); // Reduced damage heavily

    // W2: Whirlpool
    private float poolTimer = 0f;
    private float poolRate => 15f - (w2Level * 0.5f);
    private float poolRadius => 10f + (w2Level * 0.5f);
    private float poolDamage => 10f + (w2Level * 5f); // Reduced damage heavily

    // Evo
    private float evoTimer = 0f;
    private float evoRate => 15f; // Slower evo rate

    // Audio Cooldowns
    private float lastRockAudioTime = -999f;
    private float lastPoolAudioTime = -999f;

    public void UpdateLevels(int w1, int w2, int evo)
    {
        w1Level = w1;
        w2Level = w2;
        evoLevel = evo;
    }

    private void Update()
    {
        if (Enemy.GlobalFreeze) return;

        if (evoLevel > 0)
        {
            EvoUpdate();
        }
        else
        {
            if (w1Level > 0) W1Update();
            if (w2Level > 0) W2Update();
        }
    }

    private void W1Update()
    {
        rockTimer += Time.deltaTime;
        if (rockTimer >= rockRate)
        {
            rockTimer = 0f;
            ThrowRock(GetRandomEnemy(), false);
        }
    }

    private void W2Update()
    {
        poolTimer += Time.deltaTime;
        if (poolTimer >= poolRate)
        {
            poolTimer = 0f;
            SpawnWhirlpool(GetRandomEnemy(), false);
        }
    }

    private void EvoUpdate()
    {
        evoTimer += Time.deltaTime;
        if (evoTimer >= evoRate)
        {
            evoTimer = 0f;
            StartCoroutine(EvoSequence(GetRandomEnemy()));
        }
    }

    private Transform GetRandomEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, 25f);
        List<Transform> validEnemies = new List<Transform>();
        foreach (var e in enemies)
        {
            if (e.CompareTag("Enemy")) validEnemies.Add(e.transform);
        }
        if (validEnemies.Count > 0) return validEnemies[Random.Range(0, validEnemies.Count)];
        return null;
    }

    private void ThrowRock(Transform target, bool isEvo)
    {
        Vector3 targetPos = target != null ? target.position : transform.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(5f, 15f));
        targetPos.y = 0f; // ground level

        // Compute start pos far behind camera
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        
        Vector3 startPos;
        if (cam != null)
        {
            startPos = cam.transform.position - cam.transform.forward * 40f + Vector3.up * 25f;
        }
        else
        {
            startPos = transform.position - transform.forward * 40f + Vector3.up * 25f;
        }

        string prefabName = isEvo ? "SonTinh_EvoRock" : "SonTinh_Rock";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        
        GameObject rock = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cube, new Color(0.3f, 0.3f, 0.3f), 0f, billboard: true, travelDirection: Vector3.down, spriteScale: isEvo ? 2.5f : 1.5f, spherical: true);
        rock.name = prefabName;
        
        float radius = isEvo ? 10f : rockRadius;
        if (rock.GetComponent<SpriteRenderer>() == null)
            rock.transform.localScale = new Vector3(radius, radius, radius);
        
        Collider col = rock.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var logic = rock.AddComponent<SonTinhArcRock>();
        logic.damage = isEvo ? 150f : rockDamage;
        logic.radius = radius;
        logic.startPos = startPos;
        logic.targetPos = targetPos;
        logic.duration = 1.0f; // travel time
        logic.arcHeight = 15f;

        if (Time.time - lastRockAudioTime > 1.5f)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audios/Son_Tinh_Attack");
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, startPos);
                lastRockAudioTime = Time.time;
            }
        }
    }

    private ThuyTinhWhirlpool SpawnWhirlpool(Transform target, bool isEvo)
    {
        Vector3 spawnPos = target != null ? target.position : transform.position + transform.forward * 5f;
        spawnPos.y = 0.05f; // slightly above ground

        string prefabName = isEvo ? "SonTinh_EvoWhirlpool" : "SonTinh_Whirlpool";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        
        // Use a container for the logic, and a child for the visual so we can rotate only the mesh
        GameObject pool = new GameObject(prefabName);
        pool.transform.position = spawnPos;

        GameObject visual;
        if (prefab != null)
        {
            visual = Instantiate(prefab, pool.transform);
        }
        else
        {
            visual = LegendVisualHelper.CreateVisual(prefabName + "_Vis", PrimitiveType.Cylinder, new Color(0.1f, 0.4f, 1f, 0.25f), 0f, billboard: false, isFlat: true);
            visual.transform.SetParent(pool.transform, false);
            visual.transform.localPosition = Vector3.zero;
        }

        float radius = isEvo ? 6f : poolRadius;
        visual.transform.localScale = new Vector3(radius, 0.05f, radius);
        
        var logic = pool.AddComponent<ThuyTinhWhirlpool>();
        logic.damagePerSec = isEvo ? 30f : poolDamage;
        logic.radius = radius;
        logic.visualTransform = visual.transform;
        logic.duration = isEvo ? 4f : 5f; // Evo is shorter before boom
        logic.pullSpeed = isEvo ? 6f : 3f;

        LegendParticles.AddRisingWaterParticles(pool, rate: isEvo ? 30f : 15f, radius: radius);

        if (Time.time - lastPoolAudioTime > 2.0f)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audios/Thuy_Tinh_Attack");
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, spawnPos);
                lastPoolAudioTime = Time.time;
            }
        }

        return logic;
    }

    private IEnumerator EvoSequence(Transform target)
    {
        if (target == null) yield break;

        // 1. Spawn empowered whirlpool
        var pool = SpawnWhirlpool(target, true);

        // 2. Wait for whirlpool to do its thing and gather enemies
        yield return new WaitForSeconds(pool.duration - 0.5f);

        // 3. Just before whirlpool ends, throw the massive rock into it
        // We throw it at the pool's position
        if (pool != null) {
            GameObject tempTarget = new GameObject("TempTarget");
            tempTarget.transform.position = pool.transform.position;
            ThrowRock(tempTarget.transform, true);
            Destroy(tempTarget, 2f);
        }
    }
}

public class SonTinhArcRock : MonoBehaviour
{
    public float damage;
    public float radius;
    public Vector3 startPos;
    public Vector3 targetPos;
    public float duration;
    public float arcHeight;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        if (t >= 1f)
        {
            // Reached destination
            transform.position = targetPos;
            StartCoroutine(ImpactSequence());
            this.enabled = false; // stop updating
        }
        else
        {
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = currentPos;

            // Optional: spin rock
            transform.Rotate(Vector3.right * 360f * Time.deltaTime);
        }
    }

    private IEnumerator ImpactSequence()
    {
        // 0.1s delay before explosion for anticipation
        yield return new WaitForSeconds(0.1f);

        // Explosion Visuals - reduced particles
        LegendParticles.BurstAt(transform.position, new Color(0.55f, 0.34f, 0.12f, 0.5f), count: 20, speed: 5f);

        // Hit logic
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach(var h in hits)
        {
            if (h.CompareTag("Enemy"))
            {
                Enemy e = h.GetComponent<Enemy>();
                if (e != null) 
                {
                    e.TakeDamage(damage);
                    Vector3 push = (h.transform.position - transform.position).normalized;
                    e.ApplyKnockbackStun(push, 15f, 0.8f); // longer stun
                }
            }
        }
        
        Destroy(gameObject);
    }
}

public class ThuyTinhWhirlpool : MonoBehaviour
{
    public float damagePerSec;
    public float radius;
    public Transform visualTransform;
    public float duration;
    public float pullSpeed;

    private float lifeTimer = 0f;

    void Update()
    {
        lifeTimer += Time.deltaTime;

        // Rotate visual - reduced speed
        if (visualTransform != null)
        {
            visualTransform.Rotate(Vector3.up * -90f * Time.deltaTime, Space.Self);
        }

        // Apply pull and slow
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach(var h in hits)
        {
            if (h.CompareTag("Enemy"))
            {
                Enemy e = h.GetComponent<Enemy>();
                if (e != null) 
                {
                    // Damage over time
                    e.TakeDamage(damagePerSec * Time.deltaTime);

                    // Pull towards center
                    Vector3 toCenter = transform.position - h.transform.position;
                    toCenter.y = 0; // Don't pull underground
                    if (toCenter.magnitude > 0.5f)
                    {
                        // Use transform translate or character controller if they have one
                        e.transform.position += toCenter.normalized * pullSpeed * Time.deltaTime;
                    }

                    // Slow
                    if (e.moveSpeed > 1f) {
                        e.moveSpeed -= Time.deltaTime * 5f; 
                        if (e.moveSpeed < 1f) e.moveSpeed = 1f;
                    }
                }
            }
        }

        if (lifeTimer >= duration)
        {
            // End of whirlpool -> Apply a short stun to anyone still inside
            foreach(var h in hits)
            {
                if (h.CompareTag("Enemy"))
                {
                    Enemy e = h.GetComponent<Enemy>();
                    if (e != null) e.ApplyKnockbackStun(Vector3.zero, 0f, 1.0f); // stun in place
                }
            }
            Destroy(gameObject);
        }
    }
}
