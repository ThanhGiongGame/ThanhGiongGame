using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LegendSystemSonTinh : MonoBehaviour
{
    private int w1Level;
    private int w2Level;
    private int evoLevel;

    // W1: Falling Rock
    private float rockTimer = 0f;
    private float rockRate => 1.8f - (w1Level * 0.15f);
    private float rockRadius => 3f + (w1Level * 0.5f); // Smaller radius
    private float rockDamage => 100f + (w1Level * 40f);

    // W2: Water Wave (Crosses right to left)
    private float waveTimer = 0f;
    private float waveRate => 15f;
    private float waveDamage => 200f + (w2Level * 100f);

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
        if (rockTimer >= 1f)
        {
            rockTimer = 0f;
            DropRock(true);
        }

        waveTimer += Time.deltaTime;
        if (waveTimer >= 2f)
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
            // spherical=true: rock faces camera, travelDirection=down so bottom of sprite points at ground
            GameObject rock = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cube, new Color(0.3f, 0.3f, 0.3f), 0f, billboard: true, travelDirection: Vector3.down, spriteScale: isEvo ? 1.25f : 0.75f, spherical: true);
            rock.name = prefabName;
            rock.transform.position = target.position + Vector3.up * 10f; // Drop from sky
            float radius = isEvo ? 4f : rockRadius;
            if (rock.GetComponent<SpriteRenderer>() == null)
                rock.transform.localScale = new Vector3(radius, radius, radius);
            
            Collider col = rock.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var logic = rock.AddComponent<SonTinhRock>();
            logic.damage = isEvo ? 300f : rockDamage;
            logic.isEvo = isEvo;
            logic.radius = radius;
            logic.groundY = 0f;
        }
    }

    private void SpawnWave(bool isEvo)
    {
        string prefabName = isEvo ? "SonTinh_EvoWave" : "SonTinh_Wave";
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        
        // Spawn wave far to the right and slightly behind the player
        Vector3 spawnPos = transform.position + new Vector3(35f, 2.5f, 5f);
        
        // Spherical = false (cylindrical) so it stands upright. travelDirection = Vector3.left so its tip points left
        GameObject wave = prefab != null ? Instantiate(prefab) : LegendVisualHelper.CreateVisual(prefabName, PrimitiveType.Cube, new Color(0.2f, 0.5f, 1f, 0.35f), 0f, billboard: true, travelDirection: Vector3.left, spriteScale: isEvo ? 4f : 2.5f, spherical: false);
        wave.name = prefabName;
        wave.transform.position = spawnPos;

        // If fallback primitive, make it a wide wall
        if (wave.GetComponent<SpriteRenderer>() == null)
            wave.transform.localScale = new Vector3(1f, 2.5f, 15f);
        
        Collider col = wave.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
        var logic = wave.AddComponent<SonTinhWave>();
        logic.damage = isEvo ? waveDamage * 2f : waveDamage;
        logic.isEvo = isEvo;
        
        // Particles trailing the wave
        LegendParticles.AddWaterSplash(wave, rate: isEvo ? 50f : 30f, radius: 2f);
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
        transform.position += Vector3.down * 40f * Time.deltaTime;
        
        if (transform.position.y <= groundY)
        {
            // Dust impact burst when rock hits ground
            LegendParticles.BurstAt(transform.position,
                isEvo ? new Color(0.2f, 0.5f, 1f) : new Color(0.55f, 0.34f, 0.12f),
                count: isEvo ? 50 : 30, speed: isEvo ? 8f : 5f);

            // Hit ground
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
                        e.ApplyKnockbackStun(push, 10f, 0.4f);
                    }
                }
            }

            if (isEvo)
            {
                // Spawn mud puddle
                GameObject mudPrefab = Resources.Load<GameObject>("Prefabs/SonTinh_Mud");
                GameObject mud = mudPrefab != null ? Instantiate(mudPrefab) : LegendVisualHelper.CreateVisual("SonTinh_Mud", PrimitiveType.Cylinder, new Color(0.4f, 0.2f, 0f, 0.8f), 0f, billboard: false, isFlat: true);
                mud.name = "SonTinh_Mud";
                mud.transform.position = new Vector3(transform.position.x, groundY + 0.05f, transform.position.z);
                mud.transform.localScale = new Vector3(radius, 0.05f, radius);
                
                Collider col = mud.GetComponent<Collider>();
                if (col != null) Destroy(col); // mud is custom overlap-sphere scanned
                
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
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
    private float lifeTime = 0f;

    void Update()
    {
        // Move horizontally from right to left
        float speed = isEvo ? 40f : 24f;
        transform.position += Vector3.left * speed * Time.deltaTime;
        lifeTime += Time.deltaTime;

        // Hit box: wide in Z, narrow in X
        Vector3 boxSize = new Vector3(1f, 2.5f, 30f);
        Collider[] hits = Physics.OverlapBox(transform.position, boxSize / 2f, Quaternion.identity);
        foreach(var h in hits)
        {
            if (h.CompareTag("Enemy") && !hitEnemies.Contains(h.gameObject))
            {
                hitEnemies.Add(h.gameObject);
                Enemy e = h.GetComponent<Enemy>();
                if (e != null) 
                {
                    e.TakeDamage(damage);
                    StartCoroutine(StunEnemy(e, isEvo ? 4f : 2f));
                    // Also gently push left along with the wave
                    e.ApplyKnockbackStun(Vector3.left, 5f, 0.3f);
                }
            }
        }

        // Destroy after it has crossed the screen (approx 5-6 seconds)
        if (lifeTime >= 6f)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator StunEnemy(Enemy e, float duration)
    {
        if (e != null)
        {
            float originalSpeed = e.moveSpeed;
            e.moveSpeed = 0f; // Stun
            yield return new WaitForSeconds(duration);
            if (e != null) e.moveSpeed = originalSpeed; // Restore
        }
    }
}
