using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LegendSystemLeLoi : MonoBehaviour
{
    private int w1Level;
    private int w2Level;
    private int evoLevel;

    // W1: Flying Swords
    private float swordTimer = 0f;
    private float swordRate => 1.2f - (w1Level * 0.15f);
    private float swordDamage => 70f + (w1Level * 20f);

    // W2: Golden Turtle
    private GameObject turtle;
    private float turtleDamage => 50f + (w2Level * 10f);
    private bool hasRevived = false;

    public void UpdateLevels(int w1, int w2, int evo)
    {
        w1Level = w1;
        w2Level = w2;
        evoLevel = evo;

        if (evoLevel > 0)
        {
            if (turtle == null) SpawnTurtle(true);
            else {
                // Update turtle to evo
                Renderer rend = turtle.GetComponent<Renderer>();
                rend.material.color = new Color(1f, 0.8f, 0f, 1f); // Super bright gold
                rend.transform.localScale = new Vector3(2f, 1f, 2f); // Big turtle
            }
            
            // Hook up death event if not already hooked
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null)
            {
                // We will hijack the TakeDamage or handle it from PlayerHealth.
                // We'll write a custom hook in PlayerHealth later, or just check HP in Update.
            }
        }
        else
        {
            if (w2Level > 0 && turtle == null) SpawnTurtle(false);
        }
    }

    private void Update()
    {
        if (evoLevel > 0 && !hasRevived)
        {
            // Revive check
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null && ph.CurrentHealth <= 0)
            {
                OnPlayerDeath();
            }
        }

        if (evoLevel > 0)
        {
            EvoUpdate();
        }
        else
        {
            if (w1Level > 0) W1Update();
        }
    }

    private void W1Update()
    {
        swordTimer += Time.deltaTime;
        if (swordTimer >= swordRate)
        {
            swordTimer = 0f;
            SpawnSword(false);
        }
    }

    private void EvoUpdate()
    {
        swordTimer += Time.deltaTime;
        if (swordTimer >= 1.5f) // Slower but massive
        {
            swordTimer = 0f;
            SpawnSword(true);
        }
    }

    private void SpawnSword(bool isEvo)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, 15f);
        Transform target = null;
        if (enemies.Length > 0)
        {
            foreach(var e in enemies) {
                if (e.CompareTag("Enemy")) { target = e.transform; break; }
            }
        }

        Vector3 dir = transform.forward;
        if (target != null) dir = (target.position - transform.position).normalized;

        string prefabName = isEvo ? "LeLoi_GiantSword" : "LeLoi_Sword";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        // travelDirection so the sword sprite tip points toward the enemy, spherical faces camera
        GameObject sword = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cube, new Color(0.8f, 0.9f, 1f, 0.8f), 3f, billboard: true, travelDirection: dir, spriteScale: isEvo ? 1.5f : 1f, spherical: true);
        sword.name = prefabName;
        sword.transform.position = transform.position + Vector3.up * 1.5f;
        // Only set scale/rotation for 3D fallback, Billboard handles sprite
        if (sword.GetComponent<SpriteRenderer>() == null)
        {
            sword.transform.localScale = isEvo ? new Vector3(0.5f, 0.1f, 2f) : new Vector3(0.1f, 0.05f, 0.75f);
            sword.transform.rotation = Quaternion.LookRotation(dir);
        }

        Collider col = sword.GetComponent<Collider>();
        if (col != null) { col.isTrigger = true; if (col is BoxCollider bc) bc.size = new Vector3(2f, 2f, 2f); }

        var logic = sword.AddComponent<LeLoiSword>();
        logic.damage = isEvo ? 400f : swordDamage;
        logic.dir = dir;
        logic.isEvo = isEvo;

        Rigidbody rb = sword.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        
        Destroy(sword, 5f);
    }

    private void SpawnTurtle(bool isEvo)
    {
        string prefabName = isEvo ? "LeLoi_EvoTurtle" : "LeLoi_Turtle";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        turtle = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Sphere, new Color(0.8f, 0.6f, 0f), 2f, billboard: true, spriteScale: isEvo ? 1f : 0.75f);
        turtle.name = prefabName;
        turtle.transform.position = transform.position + Vector3.right * 2f + Vector3.up * 4.5f; // Raised Y
        if (turtle.GetComponent<SpriteRenderer>() == null)
            turtle.transform.localScale = isEvo ? new Vector3(1f, 0.5f, 1f) : new Vector3(0.5f, 0.25f, 0.5f);

        Collider col = turtle.GetComponent<Collider>();
        if (col != null) { col.isTrigger = true; if (col is SphereCollider sc) sc.radius = 1.25f; }

        var logic = turtle.AddComponent<LeLoiTurtle>();
        logic.damage = isEvo ? 200f : turtleDamage;
        logic.player = transform;

        Rigidbody rb = turtle.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
    }

    public bool CanRevive()
    {
        return evoLevel > 0 && !hasRevived;
    }

    public void OnPlayerDeath()
    {
        if (CanRevive())
        {
            hasRevived = true;
            
            // Revive logic
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.Heal(ph.maxHealth); // Full heal
                // Important: Need to undo the GameOver if it was already triggered,
                // But typically GameController hooks into ph.OnDeath.
                // We'll reset HP immediately before GameController acts if possible, 
                // or just rely on healing to keep it alive. (Need to modify PlayerHealth.TakeDamage)
                
                Debug.Log("LeLoi Turtle sacrificed to revive player!");
            }
            
            // Screen wipe Visual
            GameObject flashPrefab = Resources.Load<GameObject>("Prefabs/LeLoi_ReviveFlash");
            GameObject flash = flashPrefab != null ? Instantiate(flashPrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "LeLoi_ReviveFlash";
            flash.transform.position = transform.position;
            flash.transform.localScale = new Vector3(50f, 50f, 50f);
            Destroy(flash.GetComponent<Collider>());
            Renderer rend = flash.GetComponent<Renderer>();
            rend.material.color = new Color(1f, 0.8f, 0f, 0.5f);
            Destroy(flash, 0.5f);

            // Screen wipe logic
            Collider[] enemies = Physics.OverlapSphere(transform.position, 25f);
            foreach(var e in enemies)
            {
                if (e.CompareTag("Enemy"))
                {
                    Enemy en = e.GetComponent<Enemy>();
                    if (en != null) en.TakeDamage(9999f);
                }
            }
            
            if (turtle != null) Destroy(turtle); // Turtle sacrificed
        }
    }
}

public class LeLoiSword : MonoBehaviour
{
    public float damage;
    public Vector3 dir;
    public bool isEvo;
    private int pierceCount = 0;

    void Update()
    {
        transform.position += dir * (isEvo ? 30f : 50f) * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) 
            {
                e.TakeDamage(damage);
                e.ApplyKnockbackStun(dir, 8f, 0.3f);
            }

            if (!isEvo && pierceCount >= 2)
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

public class LeLoiTurtle : MonoBehaviour
{
    public float damage;
    public Transform player;
    private float hitTimer = 0f;
    private Transform targetEnemy;

    void Update()
    {
        if (player == null) return;

        if (targetEnemy == null)
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, 10f);
            float minD = float.MaxValue;
            foreach(var e in enemies)
            {
                if (e.CompareTag("Enemy"))
                {
                    float d = Vector3.Distance(transform.position, e.transform.position);
                    if (d < minD) { minD = d; targetEnemy = e.transform; }
                }
            }
        }

        Vector3 targetPos = player.position + Vector3.right * 2f;
        if (targetEnemy != null)
        {
            targetPos = targetEnemy.position;
            if (Vector3.Distance(player.position, targetPos) > 15f)
            {
                targetEnemy = null;
            }
        }
        
        targetPos.y += 1.5f; // Raise Y so turtle hovers

        transform.position = Vector3.MoveTowards(transform.position, targetPos, 16f * Time.deltaTime);
    }

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
                    e.ApplyKnockbackStun(push, 6f, 0.2f);
                }
            }
        }
    }
}
