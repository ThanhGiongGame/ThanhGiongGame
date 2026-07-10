using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LegendSystemSonTinh : MonoBehaviour
{
    private int w1Level;
    private int w2Level;
    private int evoLevel;

    // W1: Mountain Drop
    private float rockTimer = 0f;
    private float rockRate => 3f - (w1Level * 0.3f);
    private float rockRadius => 4f + (w1Level * 0.5f);
    private float rockDamage => 50f + (w1Level * 20f);

    // W2: Water Wave
    private float waveTimer = 0f;
    private float waveRate => 5f - (w2Level * 0.4f);
    private float waveDamage => 40f + (w2Level * 15f);

    public void UpdateLevels(int w1, int w2, int evo)
    {
        w1Level = w1;
        w2Level = w2;
        evoLevel = evo;
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
        rockTimer += Time.deltaTime;
        if (rockTimer >= rockRate)
        {
            rockTimer = 0f;
            DropRock(false);
        }
    }

    private void W2Update()
    {
        waveTimer += Time.deltaTime;
        if (waveTimer >= waveRate)
        {
            waveTimer = 0f;
            SpawnWave(false);
        }
    }

    private void EvoUpdate()
    {
        rockTimer += Time.deltaTime;
        if (rockTimer >= 2f)
        {
            rockTimer = 0f;
            DropRock(true);
        }

        waveTimer += Time.deltaTime;
        if (waveTimer >= 4f)
        {
            waveTimer = 0f;
            SpawnWave(true);
        }
    }

    private void DropRock(bool isEvo)
    {
        // Find a random enemy to drop on
        Collider[] enemies = Physics.OverlapSphere(transform.position, 15f);
        Transform target = null;
        if (enemies.Length > 0)
        {
            foreach(var e in enemies) {
                if (e.CompareTag("Enemy")) { target = e.transform; break; }
            }
        }

        if (target != null)
        {
            string prefabName = isEvo ? "SonTinh_EvoRock" : "SonTinh_Rock";
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            GameObject rock = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = prefabName;
            rock.transform.position = target.position + Vector3.up * 10f; // Drop from sky
            float radius = isEvo ? 8f : rockRadius;
            rock.transform.localScale = new Vector3(radius, radius, radius);
            
            Renderer rend = rock.GetComponent<Renderer>();
            rend.material.color = new Color(0.3f, 0.3f, 0.3f);
            
            Collider col = rock.GetComponent<Collider>();
            col.isTrigger = true;

            var logic = rock.AddComponent<SonTinhRock>();
            logic.damage = isEvo ? 150f : rockDamage;
            logic.isEvo = isEvo;
            logic.radius = radius;
            logic.groundY = 0f;
        }
    }

    private void SpawnWave(bool isEvo)
    {
        string prefabName = isEvo ? "SonTinh_EvoWave" : "SonTinh_Wave";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        GameObject wave = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wave.name = prefabName;
        
        Destroy(wave.GetComponent<Collider>());
        
        Renderer rend = wave.GetComponent<Renderer>();
        rend.material.color = new Color(0f, 0.5f, 1f, 0.5f);
        rend.material.SetFloat("_Mode", 3);
        rend.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        rend.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        rend.material.SetInt("_ZWrite", 0);
        rend.material.DisableKeyword("_ALPHATEST_ON");
        rend.material.EnableKeyword("_ALPHABLEND_ON");
        rend.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        rend.material.renderQueue = 3000;

        var logic = wave.AddComponent<SonTinhWave>();
        logic.damage = isEvo ? 120f : waveDamage;
        logic.isEvo = isEvo;
        logic.maxRadius = isEvo ? 20f : 12f;
    }
}

public class SonTinhRock : MonoBehaviour
{
    public float damage;
    public bool isEvo;
    public float radius;
    public float groundY;

    void Update()
    {
        transform.position += Vector3.down * 20f * Time.deltaTime;
        
        if (transform.position.y <= groundY)
        {
            // Hit ground
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach(var h in hits)
            {
                if (h.CompareTag("Enemy"))
                {
                    Enemy e = h.GetComponent<Enemy>();
                    if (e != null) e.TakeDamage(damage);
                }
            }

            if (isEvo)
            {
                // Spawn mud puddle
                GameObject mudPrefab = Resources.Load<GameObject>("Prefabs/SonTinh_Mud");
                GameObject mud = mudPrefab != null ? Instantiate(mudPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mud.name = "SonTinh_Mud";
                mud.transform.position = new Vector3(transform.position.x, groundY + 0.05f, transform.position.z);
                mud.transform.localScale = new Vector3(radius, 0.05f, radius);
                Destroy(mud.GetComponent<Collider>());
                Renderer rend = mud.GetComponent<Renderer>();
                rend.material.color = new Color(0.4f, 0.2f, 0f, 0.8f);
                
                var mudLogic = mud.AddComponent<SonTinhMud>();
                mudLogic.radius = radius;
                Destroy(mud, 5f);
            }

            Destroy(gameObject);
        }
    }
}

public class SonTinhMud : MonoBehaviour
{
    public float radius;
    void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius/2f);
        foreach(var h in hits)
        {
            if (h.CompareTag("Enemy"))
            {
                // Slow effect by clamping position change or directly reducing speed
                Enemy e = h.GetComponent<Enemy>();
                if (e != null && e.moveSpeed > 1f) {
                    e.moveSpeed -= Time.deltaTime * 5f; 
                    if (e.moveSpeed < 1f) e.moveSpeed = 1f; // Clamp to min speed
                }
            }
        }
    }
}

public class SonTinhWave : MonoBehaviour
{
    public float damage;
    public bool isEvo;
    public float maxRadius;
    private float currentRadius = 1f;
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

    void Update()
    {
        currentRadius += 10f * Time.deltaTime;
        transform.localScale = new Vector3(currentRadius, 0.1f, currentRadius);

        Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius/2f);
        foreach(var h in hits)
        {
            if (h.CompareTag("Enemy") && !hitEnemies.Contains(h.gameObject))
            {
                hitEnemies.Add(h.gameObject);
                Enemy e = h.GetComponent<Enemy>();
                if (e != null) 
                {
                    float dmg = damage;
                    // Move speed check ensures double damage if they are slowed by mud
                    if (isEvo && e.moveSpeed <= 1.5f) 
                    {
                        dmg *= 2f;
                    }
                    e.TakeDamage(dmg);
                }
            }
        }

        if (currentRadius >= maxRadius)
        {
            Destroy(gameObject);
        }
    }
}
