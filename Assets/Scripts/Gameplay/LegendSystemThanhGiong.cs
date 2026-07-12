using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LegendSystemThanhGiong : MonoBehaviour
{
    private int w1Level;
    private int w2Level;
    private int evoLevel;

    // W1: Tre Ngà
    private float bambooTimer = 0f;
    private float bambooRate => 2.5f - (w1Level * 0.2f);
    private float bambooDamage => 20f + (w1Level * 10f);

    // W2: Ngựa Sắt Phun Lửa (Fire Trail)
    private float fireTimer = 0f;
    private float fireRate = 0.5f;
    private float fireDamage => 15f + (w2Level * 5f);
    private Vector3 lastFirePos;

    private PlayerController pc;
    private float originalMoveSpeed;

    public void UpdateLevels(int w1, int w2, int evo)
    {
        w1Level = w1;
        w2Level = w2;
        
        if (evoLevel == 0 && evo > 0)
        {
            // Just evolved
            if (pc == null) pc = GetComponent<PlayerController>();
            if (pc != null) {
                originalMoveSpeed = pc.moveSpeed;
                pc.moveSpeed += 5f; // massive speed boost
            }
        }
        evoLevel = evo;
    }

    private void Start()
    {
        pc = GetComponent<PlayerController>();
        lastFirePos = transform.position;
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
        bambooTimer += Time.deltaTime;
        if (bambooTimer >= bambooRate)
        {
            bambooTimer = 0f;
            SpawnBamboo(false);
        }
    }

    private void W2Update()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate && Vector3.Distance(transform.position, lastFirePos) > 1.5f)
        {
            fireTimer = 0f;
            lastFirePos = transform.position;
            SpawnFire(false);
        }
    }

    private void EvoUpdate()
    {
        bambooTimer += Time.deltaTime;
        if (bambooTimer >= 1.5f)
        {
            bambooTimer = 0f;
            SpawnBamboo(true);
        }

        fireTimer += Time.deltaTime;
        if (fireTimer >= 0.3f && Vector3.Distance(transform.position, lastFirePos) > 1.5f)
        {
            fireTimer = 0f;
            lastFirePos = transform.position;
            SpawnFire(true);
        }
    }

    private void SpawnBamboo(bool isEvo)
    {
        Vector2 rand = Random.insideUnitCircle * 5f;
        Vector3 pos = transform.position + new Vector3(rand.x, 0, rand.y);
        
        string prefabName = isEvo ? "ThanhGiong_EvoBamboo" : "ThanhGiong_Bamboo";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        // Bamboo stands vertically — no travelDirection needed (it stays still)
        GameObject bamboo = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cylinder, new Color(0.8f, 0.9f, 0.2f), 0f, billboard: true, spriteScale: isEvo ? 2f : 1.2f);
        bamboo.name = prefabName;
        bamboo.transform.position = pos + Vector3.up * 1f;
        if (bamboo.GetComponent<SpriteRenderer>() == null)
            bamboo.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);
        
        Collider col = bamboo.GetComponent<Collider>();
        if (col != null) { col.isTrigger = false; if (col is BoxCollider bc) bc.size = new Vector3(3f, 3f, 3f); } // Solid obstacle with big hitbox

        var logic = bamboo.AddComponent<ThanhGiongBamboo>();
        logic.damage = isEvo ? 80f : bambooDamage;
        logic.isEvo = isEvo;

        // Brown ground dust — bamboo stabs into earth
        LegendParticles.AddGroundDust(bamboo, rate: isEvo ? 18f : 10f);

        Rigidbody rb = bamboo.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        Destroy(bamboo, 10f);
    }

    private void SpawnFire(bool isEvo)
    {
        string prefabName = isEvo ? "ThanhGiong_EvoFire" : "ThanhGiong_Fire";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        // Fire trail is flat on the ground — no direction needed
        GameObject fire = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cube, isEvo ? new Color(1f, 0.5f, 0f, 0.8f) : new Color(1f, 0.2f, 0f, 0.6f), isEvo ? 3f : 1.5f, billboard: false, isFlat: true, spriteScale: isEvo ? 2f : 1.5f);
        fire.name = prefabName;
        fire.transform.position = transform.position + Vector3.up * 0.5f;
        if (fire.GetComponent<SpriteRenderer>() == null)
            fire.transform.localScale = isEvo ? new Vector3(1.5f, 3f, 1.5f) : new Vector3(1f, 1f, 1f);
        
        Collider col = fire.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        SphereCollider trigger = fire.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = isEvo ? 3f : 2f; // Increased hitbox
        
        var logic = fire.AddComponent<ThanhGiongFire>();
        logic.damage = isEvo ? 60f : fireDamage;

        Rigidbody rb = fire.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        
        Destroy(fire, 3f);
    }
}

public class ThanhGiongBamboo : MonoBehaviour
{
    public float damage;
    public bool isEvo;
    private Transform target;

    void Start()
    {
        if (isEvo)
        {
            // Find a target to home into
            Collider[] enemies = Physics.OverlapSphere(transform.position, 15f);
            float minD = float.MaxValue;
            foreach(var e in enemies) {
                if (e.CompareTag("Enemy")) {
                    float d = Vector3.Distance(transform.position, e.transform.position);
                    if (d < minD) { minD = d; target = e.transform; }
                }
            }
            if (target != null) {
                GetComponent<Collider>().isTrigger = true; // Become projectile
                transform.rotation = Quaternion.LookRotation(target.position - transform.position) * Quaternion.Euler(90f, 0, 0);
            }
        }
    }

    void Update()
    {
        if (isEvo && target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, 15f * Time.deltaTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy e = collision.gameObject.GetComponent<Enemy>();
            if (e != null) 
            {
                e.TakeDamage(damage);
                Vector3 push = (collision.transform.position - transform.position).normalized;
                e.ApplyKnockbackStun(push, 4f, 0.2f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isEvo && other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) 
            {
                e.TakeDamage(damage);
                Vector3 push = (other.transform.position - transform.position).normalized;
                e.ApplyKnockbackStun(push, 6f, 0.3f);
            }
            Destroy(gameObject);
        }
    }
}

public class ThanhGiongFire : MonoBehaviour
{
    public float damage;
    private float hitTimer;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            hitTimer += Time.deltaTime;
            if (hitTimer > 0.5f)
            {
                hitTimer = 0f;
                Enemy e = other.GetComponent<Enemy>();
                if (e != null) 
                {
                    e.TakeDamage(damage);
                    Vector3 push = (other.transform.position - transform.position).normalized;
                    e.ApplyKnockbackStun(push, 2f, 0.1f);
                }
            }
        }
    }
}
