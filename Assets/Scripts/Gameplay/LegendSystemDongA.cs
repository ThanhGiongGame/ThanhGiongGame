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
            GameObject ribbon = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            ribbon.name = prefabName;
            ribbon.transform.localScale = new Vector3(0.2f, 2f, 0.2f); // like a vertical banner
            
            Collider col = ribbon.GetComponent<Collider>();
            col.isTrigger = true;
            
            Renderer rend = ribbon.GetComponent<Renderer>();
            rend.material.color = isEvo ? new Color(1f, 0.2f, 0f, 0.8f) : new Color(0.9f, 0.9f, 0.9f, 0.8f);
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", isEvo ? new Color(1f, 0.1f, 0f) : new Color(0.2f, 0.2f, 0.2f));
            rend.material.SetFloat("_Mode", 3);
            rend.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            rend.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            rend.material.SetInt("_ZWrite", 0);
            rend.material.DisableKeyword("_ALPHATEST_ON");
            rend.material.EnableKeyword("_ALPHABLEND_ON");
            rend.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            rend.material.renderQueue = 3000;

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
        float currentRadius = isEvo ? 4f : ribbonRadius;
        float angleStep = 360f / ribbons.Count;
        for (int i = 0; i < ribbons.Count; i++)
        {
            if (ribbons[i] == null) continue;
            float angle = (Time.time * ribbonSpeed) + (i * angleStep);
            Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0f, Mathf.Cos(angle * Mathf.Deg2Rad)) * currentRadius;
            ribbons[i].transform.position = transform.position + Vector3.up * 1f + offset;
            ribbons[i].transform.rotation = Quaternion.LookRotation(-offset) * Quaternion.Euler(0, 0, 45f); // Angle it a bit
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
        GameObject stake = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stake.name = prefabName;
        stake.transform.position = transform.position + Vector3.up * 0.5f;
        stake.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        Collider col = stake.GetComponent<Collider>();
        col.isTrigger = true;
        
        Renderer rend = stake.GetComponent<Renderer>();
        rend.material.color = new Color(0.4f, 0.2f, 0.1f);
        if (isEvo)
        {
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", new Color(0.5f, 0f, 0.5f)); // Dark energy
        }

        var logic = stake.AddComponent<DongAStake>();
        logic.damage = isEvo ? 150f : stakeDamage;
        logic.isEvo = isEvo;

        Rigidbody rb = stake.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        // Visual spike emerging
        StartCoroutine(StakeRise(stake.transform));
        
        Destroy(stake, isEvo ? 5f : 10f); // Lives for 10s
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
                if (e != null) e.TakeDamage(damage);
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
            if (e != null) e.TakeDamage(damage);
        }
    }
}
