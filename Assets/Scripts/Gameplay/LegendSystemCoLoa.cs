using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LegendSystemCoLoa : MonoBehaviour
{
    private int w1Level;
    private int w2Level;
    private int evoLevel;

    // W1: Nỏ Liên Châu
    private float fireTimer = 0f;
    private float fireRate => 3.0f - (w1Level * 0.2f);
    
    // W2: Mai Rùa Vàng
    private List<GameObject> shields = new List<GameObject>();
    private float shieldRadius = 6f;
    private float shieldRotSpeed = 90f;

    public void UpdateLevels(int w1, int w2, int evo)
    {
        w1Level = w1;
        w2Level = w2;
        evoLevel = evo;

        if (evoLevel > 0)
        {
            ClearShields();
            SpawnEvoShields();
        }
        else
        {
            if (w2Level > 0) UpdateShields();
        }
    }

    private void Update()
    {
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
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            fireTimer = 0f;
            FireCrossbow(false);
        }
    }

    private void W2Update()
    {
        RotateShields();
    }

    private void EvoUpdate()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= 1.5f) // Faster fire rate for evo
        {
            fireTimer = 0f;
            FireCrossbow(true); // Piercing and exploding
        }
        RotateShields();
    }

    private void FireCrossbow(bool isEvo)
    {
        // Find nearest enemy
        Collider[] enemies = Physics.OverlapSphere(transform.position, 15f);
        Transform target = null;
        float closestDist = float.MaxValue;
        foreach(var col in enemies)
        {
            if (col.CompareTag("Enemy"))
            {
                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < closestDist) { closestDist = d; target = col.transform; }
            }
        }

        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0;
            
            int arrows = isEvo ? 3 : (1 + w1Level / 2);
            for(int i=0; i<arrows; i++)
            {
                Vector3 spreadDir = Quaternion.Euler(0, (i - arrows/2f)*15f, 0) * dir;
                SpawnArrow(spreadDir, isEvo);
            }
        }
    }

    private void SpawnArrow(Vector3 dir, bool isEvo)
    {
        string prefabName = isEvo ? "CoLoa_EvoArrow" : "CoLoa_Arrow";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        // spherical=true: arrow faces camera fully, then rotates in screen space so tip → enemy
        GameObject arrow = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cylinder, isEvo ? Color.red : new Color(0.8f, 0.4f, 0.1f), 2.5f, billboard: true, travelDirection: dir, spriteScale: 2.5f, spherical: true);
        arrow.name = prefabName;
        arrow.transform.position = transform.position + Vector3.up * 1f;
        // Don't override rotation for sprite — Billboard handles it. For 3D fallback keep the look rotation:
        if (arrow.GetComponent<SpriteRenderer>() == null)
            arrow.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0, 0);
        arrow.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f); // big enough to see
        
        Collider col = arrow.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
        Rigidbody rb = arrow.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        var proj = arrow.AddComponent<CoLoaArrow>();
        proj.dir = dir;
        proj.speed = 40f;
        proj.damage = isEvo ? 30f : 10f + (w1Level * 5f);
        proj.isEvo = isEvo;
    }

    private void UpdateShields()
    {
        int count = w2Level;
        ClearShields();
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/CoLoa_Shield");
            GameObject shield = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual("CoLoa_Shield", PrimitiveType.Sphere, new Color(1f, 0.9f, 0f, 0.25f), 1.5f, billboard: true);
            shield.name = "CoLoa_Shield";
            shield.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            
            Collider col = shield.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            
            var logic = shield.AddComponent<CoLoaShield>();
            logic.damage = 10f + (w2Level * 5f);
            logic.parentSystem = this;

            shields.Add(shield);
        }
    }

    private void SpawnEvoShields()
    {
        ClearShields();
        for (int i = 0; i < 4; i++) // 4 big shields
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/CoLoa_EvoShield");
            GameObject shield = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual("CoLoa_EvoShield", PrimitiveType.Sphere, new Color(1f, 0.5f, 0f, 0.3f), 2.5f, billboard: true);
            shield.name = "CoLoa_EvoShield";
            shield.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); // Increased hitbox
            
            Collider col = shield.GetComponent<Collider>();
            if (col != null) { col.isTrigger = true; ((SphereCollider)col).radius = 0.8f; }
            
            var logic = shield.AddComponent<CoLoaShield>();
            logic.damage = 40f;
            logic.parentSystem = this;
            logic.isEvo = true;
            logic.hp = 10; // Extra durability for evo

            shields.Add(shield);
        }
    }

    private void RotateShields()
    {
        if (shields.Count == 0) return;
        float angleStep = 360f / shields.Count;
        for (int i = 0; i < shields.Count; i++)
        {
            if (shields[i] == null) continue;
            float angle = (Time.time * shieldRotSpeed) + (i * angleStep);
            float radius = evoLevel > 0 ? shieldRadius + 3f : shieldRadius;
            Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0f, Mathf.Cos(angle * Mathf.Deg2Rad)) * radius;
            shields[i].transform.position = transform.position + Vector3.up * 1f + offset;
        }
    }

    private void ClearShields()
    {
        foreach (var s in shields) if (s != null) Destroy(s);
        shields.Clear();
    }

    public void OnShieldBroken(GameObject shield)
    {
        if (evoLevel > 0)
        {
            // Evo Shockwave Visual
            GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shockwave.transform.position = transform.position;
            shockwave.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Destroy(shockwave.GetComponent<Collider>());
            Renderer rend = shockwave.GetComponent<Renderer>();
            rend.material.color = new Color(1f, 0.5f, 0f, 0.25f);
            StartCoroutine(ShockwaveAnim(shockwave));

            // Evo Shockwave Logic
            Collider[] enemies = Physics.OverlapSphere(transform.position, 8f); // Increased explosion radius
            foreach(var e in enemies)
            {
                if (e.CompareTag("Enemy"))
                {
                    Enemy en = e.GetComponent<Enemy>();
                    if (en != null) 
                    {
                        en.TakeDamage(30f);
                        Vector3 push = (e.transform.position - transform.position).normalized;
                        en.ApplyKnockbackStun(push, 15f, 0.8f);
                    }
                }
            }
            // Heal 10%
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null) ph.Heal(ph.maxHealth * 0.1f);
        }
        
        shields.Remove(shield);
        Destroy(shield);
        
        // Respawn shield after a delay
        StartCoroutine(RespawnShieldRoutine(evoLevel > 0));
    }

    private IEnumerator ShockwaveAnim(GameObject wave)
    {
        float t = 0;
        while(t < 0.3f)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(0.1f, 12f, t / 0.3f);
            wave.transform.localScale = new Vector3(scale, 0.1f, scale);
            yield return null;
        }
        Destroy(wave);
    }

    private IEnumerator RespawnShieldRoutine(bool isEvo)
    {
        yield return new WaitForSeconds(5f);
        if (isEvo) SpawnEvoShields();
        else UpdateShields();
    }
}

public class CoLoaArrow : MonoBehaviour
{
    public Vector3 dir;
    public float speed;
    public float damage;
    public bool isEvo;
    private int pierceCount = 0;
    private float lifeTime = 0f;

    void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
        lifeTime += Time.deltaTime;
        if (lifeTime > 5f) Destroy(gameObject); // Cleanup
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) 
            {
                e.TakeDamage(damage);
                e.ApplyKnockbackStun(dir, 5f, 0.2f);
            }

            if (isEvo)
            {
                // Explosion Visual
                GameObject explo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                explo.transform.position = transform.position;
                explo.transform.localScale = new Vector3(3f, 3f, 3f);
                Destroy(explo.GetComponent<Collider>());
                Renderer rend = explo.GetComponent<Renderer>();
                rend.material.color = new Color(1f, 0f, 0f, 0.25f);
                Destroy(explo, 0.2f); // quick flash

                // Explosion Logic
                Collider[] splash = Physics.OverlapSphere(transform.position, 2.5f);
                foreach(var s in splash)
                {
                    if (s.CompareTag("Enemy") && s.gameObject != other.gameObject)
                    {
                        Enemy en = s.GetComponent<Enemy>();
                        if (en != null) en.TakeDamage(damage * 0.5f);
                    }
                }
            }

            if (!isEvo || pierceCount >= 3)
            {
                Destroy(gameObject);
            }
            else
            {
                pierceCount++;
            }
        }
    }
}

public class CoLoaShield : MonoBehaviour
{
    public float damage;
    public LegendSystemCoLoa parentSystem;
    public bool isEvo;
    public int hp = 3;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) 
            {
                e.TakeDamage(damage);
                Vector3 push = (other.transform.position - transform.position).normalized;
                e.ApplyKnockbackStun(push, 8f, 0.3f);
            }
            
            hp--;
            if (hp <= 0)
            {
                parentSystem.OnShieldBroken(gameObject);
            }
        }
    }
}
