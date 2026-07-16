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
    private float bambooRate => 3.0f - (w1Level * 0.2f);
    private float bambooDamage => 15f + (w1Level * 5f);

    // W2: Ngựa Sắt Phun Lửa (Fire Trail)
    private float fireTimer = 0f;
    private float fireRate = 0.6f;
    private float fireDamage => 10f + (w2Level * 5f);
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
        if (bambooTimer >= 2.0f)
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
        
        // Force creating own mesh instead of using prefab
        GameObject bamboo = Create3DBamboo(prefabName, isEvo);
        
        float height = isEvo ? 8f : 6f; // Since localScale.y is 4f and 3f
        float halfHeight = height / 2f;
        
        Vector3 targetRisePos = pos + Vector3.up * halfHeight;
        Vector3 startPos = pos + Vector3.down * halfHeight;
        
        bamboo.transform.position = startPos;
        
        Collider existingCol = bamboo.GetComponent<Collider>();
        if (existingCol != null) Destroy(existingCol);

        BoxCollider bc = bamboo.AddComponent<BoxCollider>();
        bc.isTrigger = true; 
        bc.size = new Vector3(1.5f, 1.5f, 1.5f); // Solid obstacle with big hitbox

        var logic = bamboo.AddComponent<ThanhGiongBamboo>();
        logic.damage = isEvo ? 40f : bambooDamage;
        logic.isEvo = isEvo;
        logic.targetRisePos = targetRisePos;

        // Brown ground dust — bamboo stabs into earth
        LegendParticles.AddGroundDust(bamboo, rate: isEvo ? 10f : 5f);

        Rigidbody rb = bamboo.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        Destroy(bamboo, 10f);
    }

    private GameObject Create3DBamboo(string prefabName, bool isEvo)
    {
        GameObject bamboo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bamboo.name = prefabName;
        bamboo.transform.localScale = isEvo ? new Vector3(0.5f, 4f, 0.5f) : new Vector3(0.4f, 3f, 0.4f);
        
        Renderer rend = bamboo.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader stdShader = Shader.Find("Standard");
            Shader shaderToUse = urpShader != null ? urpShader : stdShader;
            
            Material mat = new Material(shaderToUse);
            Color bambooColor = new Color(0.2f, 0.8f, 0.2f); // Solid green for bamboo
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", bambooColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", bambooColor);
            
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            rend.receiveShadows = true;
        }
        
        return bamboo;
    }

    private void SpawnFire(bool isEvo)
    {
        string prefabName = isEvo ? "ThanhGiong_EvoFire" : "ThanhGiong_Fire";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        
        int count = isEvo ? 3 + w2Level : 1 + (w2Level / 2);
        float spreadRadius = 0.5f + (w2Level * 0.4f);

        for (int i = 0; i < count; i++)
        {
            // Fire trail is flat on the ground — no direction needed
            // Use Color.white and 0f emission to prevent the orange tinting effect
            GameObject fire = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cube, new Color(1f, 1f, 1f, 0.5f), 0f, billboard: false, isFlat: true, spriteScale: isEvo ? 1f : 0.75f);
            
            fire.name = prefabName;
            Vector2 rand = Random.insideUnitCircle * spreadRadius;
            if (i == 0) rand = Vector2.zero; // Always drop one directly on the path
            
            fire.transform.position = transform.position + new Vector3(rand.x, 0.5f, rand.y);
            
            if (fire.GetComponent<SpriteRenderer>() == null)
                fire.transform.localScale = isEvo ? new Vector3(0.75f, 1.5f, 0.75f) : new Vector3(0.5f, 0.5f, 0.5f);
            
            Collider col = fire.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            SphereCollider trigger = fire.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = isEvo ? 1.5f : 1f; // Hitbox stays consistent, since we are spawning more instances
            
            var logic = fire.AddComponent<ThanhGiongFire>();
            logic.damage = isEvo ? 30f : fireDamage;

            Rigidbody rb = fire.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            
            Destroy(fire, 3f);
        }
    }
}

public class ThanhGiongBamboo : MonoBehaviour
{
    public float damage;
    public bool isEvo;
    public Vector3 targetRisePos;
    private Transform target;
    private bool isRising = true;

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
                // We'll set rotation after it finishes rising
            }
        }
    }

    void Update()
    {
        if (isRising)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetRisePos, 15f * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetRisePos) < 0.1f)
            {
                transform.position = targetRisePos;
                isRising = false;
                
                if (isEvo && target != null) {
                    transform.rotation = Quaternion.LookRotation(target.position - transform.position) * Quaternion.Euler(90f, 0, 0);
                }
            }
        }
        else if (isEvo && target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, 30f * Time.deltaTime);
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
                Vector3 push = (other.transform.position - transform.position).normalized;
                e.ApplyKnockbackStun(push, isEvo ? 6f : 4f, isEvo ? 0.3f : 0.2f);
            }
            if (isEvo) Destroy(gameObject);
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
