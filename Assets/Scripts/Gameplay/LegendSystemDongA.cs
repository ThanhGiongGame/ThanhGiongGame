using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LegendSystemDongA : MonoBehaviour
{
    private int w1Level;
    private int w2Level;
    private int evoLevel;

    // W1: Hịch Tướng Sĩ
    private List<GameObject> ribbons = new List<GameObject>();
    private float ribbonRadius => 3f + (w1Level * 0.5f);
    private float ribbonSpeed = 120f;
    private float ribbonDamage => 10f + (w1Level * 5f);
    
    // W2: Cọc Bạch Đằng
    private float stakeTimer = 0f;
    private float stakeInterval => 2f - (w2Level * 0.2f);
    private float stakeDamage => 30f + (w2Level * 10f);

    public void UpdateLevels(int w1, int w2, int evo)
    {
        w1Level = w1;
        w2Level = w2;
        evoLevel = evo;

        if (evoLevel > 0)
        {
            UpdateRibbons(true);
        }
        else
        {
            if (w1Level > 0) UpdateRibbons(false);
        }
    }

    private void Update()
    {
        if (evoLevel > 0)
        {
            RotateRibbons(true);
            StakeUpdate(true);
        }
        else
        {
            if (w1Level > 0) RotateRibbons(false);
            if (w2Level > 0) StakeUpdate(false);
        }
    }

    private void UpdateRibbons(bool isEvo)
    {
        foreach(var r in ribbons) if (r != null) Destroy(r);
        ribbons.Clear();

        int count = isEvo ? 6 : (2 + w1Level);
        for(int i=0; i<count; i++)
        {
            string prefabName = isEvo ? "DongA_FireRibbon" : "DongA_Ribbon";
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            // NO billboard here — RotateRibbons handles camera-facing each frame
            Color col = isEvo ? new Color(1f, 0.2f, 0f, 0.8f) : new Color(0.9f, 0.9f, 0.9f, 0.8f);
            GameObject ribbon = prefab != null ? Instantiate(prefab) :
                LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cube, col,
                    isEvo ? 2f : 1f, billboard: true, spriteScale: isEvo ? 2.5f : 1.8f);
            ribbon.name = prefabName;
            if (ribbon.GetComponent<SpriteRenderer>() == null)
                ribbon.transform.localScale = new Vector3(0.2f, 2f, 0.2f);
            
            Collider col2 = ribbon.GetComponent<Collider>();
            if (col2 != null) { col2.isTrigger = true; ((BoxCollider)col2).size = new Vector3(3f, 3f, 3f); } // Huge hitbox for ribbon
            
            var logic = ribbon.AddComponent<DongARibbon>();
            logic.damage = isEvo ? 80f : ribbonDamage;
            logic.isEvo = isEvo;
            
            Rigidbody rb = ribbon.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            
            ribbons.Add(ribbon);
        }
    }

    private void RotateRibbons(bool isEvo)
    {
        if (ribbons.Count == 0) return;
        Camera cam = Camera.main;
        float currentRadius = isEvo ? 4f : ribbonRadius;
        float angleStep = 360f / ribbons.Count;
        for (int i = 0; i < ribbons.Count; i++)
        {
            if (ribbons[i] == null) continue;
            float angle = (Time.time * ribbonSpeed) + (i * angleStep);
            Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0f, Mathf.Cos(angle * Mathf.Deg2Rad)) * currentRadius;
            ribbons[i].transform.position = transform.position + Vector3.up * 1f + offset;
        }
    }

    private void StakeUpdate(bool isEvo)
    {
        float currentInterval = isEvo ? 1.0f : stakeInterval;
        stakeTimer += Time.deltaTime;
        if (stakeTimer >= currentInterval)
        {
            stakeTimer = 0f;
            SpawnStake(isEvo);
        }
    }

    private void SpawnStake(bool isEvo)
    {
        string prefabName = isEvo ? "DongA_EvoStake" : "DongA_Stake";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        // Stakes rise from ground — sprite tip points upward (Vector3.up)
        GameObject stake = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cylinder, new Color(0.4f, 0.2f, 0.1f), isEvo ? 2f : 0f, billboard: true, travelDirection: Vector3.up, spriteScale: isEvo ? 2f : 1.2f);
        stake.name = prefabName;
        stake.transform.position = transform.position + Vector3.up * 0.5f;
        if (stake.GetComponent<SpriteRenderer>() == null)
            stake.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        Collider col = stake.GetComponent<Collider>();
        if (col != null) { col.isTrigger = true; if (col is BoxCollider bc) bc.size = new Vector3(4f, 4f, 4f); } // Huge hitbox
        
        var logic = stake.AddComponent<DongAStake>();
        logic.damage = isEvo ? 150f : stakeDamage;
        logic.isEvo = isEvo;

        Rigidbody rb = stake.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        // Ground dust as stake erupts from earth
        LegendParticles.AddGroundDust(stake, rate: isEvo ? 20f : 12f);
        LegendParticles.BurstAt(stake.transform.position,
            isEvo ? new Color(0.3f, 0.0f, 0.5f) : new Color(0.45f, 0.25f, 0.1f),
            count: isEvo ? 40 : 20, speed: 4f);

        // Visual spike emerging
        StartCoroutine(StakeRise(stake.transform));
        
        Destroy(stake, isEvo ? 5f : 10f);
    }

    private IEnumerator StakeRise(Transform t)
    {
        float time = 0;
        Vector3 start = t.position - Vector3.up * 1f;
        Vector3 end = t.position;
        while(time < 0.2f)
        {
            if (t == null) yield break;
            time += Time.deltaTime;
            t.position = Vector3.Lerp(start, end, time / 0.2f);
            yield return null;
        }
    }
}

public class DongARibbon : MonoBehaviour
{
    public float damage;
    public bool isEvo;
    private float hitTimer = 0f;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            hitTimer += Time.deltaTime;
            if (hitTimer > (isEvo ? 0.2f : 0.5f))
            {
                hitTimer = 0f;
                Enemy e = other.GetComponent<Enemy>();
                if (e != null) 
                {
                    e.TakeDamage(damage);
                    Vector3 push = (other.transform.position - transform.position).normalized;
                    e.ApplyKnockbackStun(push, 4f, 0.2f);
                }
            }
        }
    }
}

public class DongAStake : MonoBehaviour
{
    public float damage;
    public bool isEvo;
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

    void Update()
    {
        if (isEvo)
        {
            // Black hole attract
            Collider[] enemies = Physics.OverlapSphere(transform.position, 6f);
            foreach(var e in enemies)
            {
                if (e.CompareTag("Enemy"))
                {
                    Vector3 pull = (transform.position - e.transform.position).normalized;
                    pull.y = 0;
                    e.transform.position += pull * 2f * Time.deltaTime;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && !hitEnemies.Contains(other.gameObject))
        {
            hitEnemies.Add(other.gameObject);
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) 
            {
                e.TakeDamage(damage);
                Vector3 push = (other.transform.position - transform.position).normalized;
                e.ApplyKnockbackStun(push, 6f, 0.4f);
            }
        }
    }
}
