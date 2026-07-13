using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LegendSystemLeLoi : MonoBehaviour
{
    private int w1Level;
    private int w2Level;
    private int evoLevel;

    private PlayerController playerController;
    private bool isSubscribed = false;

    // W1: Flying Sword Waves
    private float swordDamage => 70f + (w1Level * 30f);
    private float swordTimer = 15f; // start ready
    private float swordCooldown = 15f;

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
                // Update turtle color to evo
                Renderer[] rends = turtle.GetComponentsInChildren<Renderer>();
                foreach(var r in rends) r.material.color = new Color(1f, 0.8f, 0f, 1f); 
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

        if (w1Level > 0)
        {
            W1Update();
        }
    }

    private void W1Update()
    {
        swordTimer += Time.deltaTime;
        
        float currentCooldown = (evoLevel > 0) ? 0.8f : swordCooldown;

        if (swordTimer >= currentCooldown)
        {
            swordTimer = 0f;
            StartCoroutine(ShootCrossCoroutine());
        }
    }

    private IEnumerator ShootCrossCoroutine()
    {
        bool isEvo = evoLevel > 0;
        
        // 1. Horizontal (Left and Right)
        Vector3 rightDir = transform.right;
        Vector3 leftDir = -transform.right;
        SpawnSwordWave(rightDir, isEvo);
        SpawnSwordWave(leftDir, isEvo);

        yield return new WaitForSeconds(0.3f);

        // 2. Vertical (Forward and Backward)
        Vector3 fwdDir = transform.forward;
        Vector3 backDir = -transform.forward;
        SpawnSwordWave(fwdDir, isEvo);
        SpawnSwordWave(backDir, isEvo);
    }

    private void SpawnSwordWave(Vector3 dir, bool isEvo)
    {
        GameObject wave = new GameObject(isEvo ? "LeLoi_EvoWave" : "LeLoi_SwordWave");
        wave.transform.position = transform.position + Vector3.up * 1.5f;
        wave.transform.rotation = Quaternion.LookRotation(dir);

        // Generate Crescent Mesh
        MeshFilter mf = wave.AddComponent<MeshFilter>();
        mf.mesh = CreateCrescentMesh(2.5f, 0.5f);
        
        MeshRenderer mr = wave.AddComponent<MeshRenderer>();
        // Find a suitable glowing material, Unlit or Additive
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        mat.color = isEvo ? new Color(1f, 0.5f, 0f, 0.8f) : new Color(1f, 0.8f, 0f, 0.8f);
        mr.material = mat;

        BoxCollider col = wave.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(5f, 1f, 2f);

        var logic = wave.AddComponent<LeLoiSwordWave>();
        logic.damage = isEvo ? 400f : swordDamage;
        logic.dir = dir;
        logic.isEvo = isEvo;
        
        Rigidbody rb = wave.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Destroy(wave, 1.5f);
    }

    private Mesh CreateCrescentMesh(float radius, float thickness)
    {
        Mesh mesh = new Mesh();
        int segments = 24;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];
        Vector2[] uv = new Vector2[(segments + 1) * 2];
        
        for (int i = 0; i <= segments; i++)
        {
            // Sweep from -90 to 90 degrees around Y axis (so it faces forward Z)
            float angle = -90f + (180f * i / segments);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            
            vertices[i * 2] = dir * (radius - thickness); // inner
            vertices[i * 2 + 1] = dir * radius;           // outer

            uv[i * 2] = new Vector2((float)i / segments, 0);
            uv[i * 2 + 1] = new Vector2((float)i / segments, 1);
            
            if (i < segments)
            {
                int start = i * 2;
                triangles[i * 6] = start;
                triangles[i * 6 + 1] = start + 1;
                triangles[i * 6 + 2] = start + 2;
                
                triangles[i * 6 + 3] = start + 1;
                triangles[i * 6 + 4] = start + 3;
                triangles[i * 6 + 5] = start + 2;
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void SpawnTurtle(bool isEvo)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Turtle");
        if (prefab != null)
        {
            turtle = Instantiate(prefab);
            turtle.name = isEvo ? "LeLoi_EvoTurtle" : "LeLoi_Turtle";
            turtle.transform.position = transform.position + Vector3.right * 2f + Vector3.up * 4.5f;

            // Add colliders and logic if not present
            Collider col = turtle.GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sc = turtle.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.25f;
            }
            else
            {
                col.isTrigger = true;
            }

            var logic = turtle.AddComponent<LeLoiTurtle>();
            logic.damage = isEvo ? 200f : turtleDamage;
            logic.player = transform;

            Rigidbody rb = turtle.GetComponent<Rigidbody>();
            if (rb == null) rb = turtle.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        else
        {
            Debug.LogError("Turtle prefab not found in Resources/Prefabs/");
        }
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
            
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.Heal(ph.maxHealth); 
                Debug.Log("LeLoi Turtle sacrificed to revive player!");
            }
            
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "LeLoi_ReviveFlash";
            flash.transform.position = transform.position;
            flash.transform.localScale = new Vector3(50f, 50f, 50f);
            Destroy(flash.GetComponent<Collider>());
            Renderer rend = flash.GetComponent<Renderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            rend.material = new Material(s);
            rend.material.color = new Color(1f, 0.8f, 0f, 0.5f);
            Destroy(flash, 0.5f);

            Collider[] enemies = Physics.OverlapSphere(transform.position, 25f);
            foreach(var e in enemies)
            {
                if (e.CompareTag("Enemy"))
                {
                    Enemy en = e.GetComponent<Enemy>();
                    if (en != null) en.TakeDamage(9999f);
                }
            }
            
            if (turtle != null) Destroy(turtle);
        }
    }
}

public class LeLoiSwordWave : MonoBehaviour
{
    public float damage;
    public Vector3 dir;
    public bool isEvo;
    
    private Renderer rend;
    private Color startColor;
    private float lifeTimer = 0f;
    private float maxLife = 1.5f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null && rend.material != null && rend.material.HasProperty("_Color")) 
        {
            startColor = rend.material.color;
        }
        else
        {
            startColor = new Color(1f, 0.8f, 0f, 0.8f);
        }
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        transform.position += dir * (isEvo ? 20f : 15f) * Time.deltaTime;
        
        if (rend != null && rend.material != null && rend.material.HasProperty("_Color"))
        {
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, lifeTimer / maxLife);
            rend.material.color = c;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) 
            {
                e.TakeDamage(damage);
                e.ApplyKnockbackStun(dir, 10f, 0.4f);
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

        // Smoothly rotate to face movement direction
        Vector3 moveDir = targetPos - transform.position;
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
        }

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
