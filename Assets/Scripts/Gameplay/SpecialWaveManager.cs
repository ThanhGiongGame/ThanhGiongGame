using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialWaveManager : MonoBehaviour
{
    [HideInInspector]
    public WaveSpawner waveSpawner;

    private Transform player;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    public void TriggerSpecialWave(SpecialWaveType waveType)
    {
        switch (waveType)
        {
            case SpecialWaveType.ChickenStampede:
                StartCoroutine(ChickenStampedeRoutine());
                break;
            case SpecialWaveType.ChickenBomb:
                StartCoroutine(ChickenBombRoutine());
                break;
            case SpecialWaveType.Surround:
                StartCoroutine(SurroundRoutine());
                break;
            case SpecialWaveType.MassiveLinh2:
                StartCoroutine(MassiveLinh2Routine());
                break;
            case SpecialWaveType.FinalBoss:
                SpawnFinalBoss();
                break;
        }
    }

    private IEnumerator ChickenStampedeRoutine()
    {
        if (player == null) yield break;

        // Determine direction
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir.y = 0;
        randomDir.Normalize();

        Vector3 startPos = player.position - randomDir * 20f;
        Vector3 endPos = player.position + randomDir * 20f;

        // Warning line
        SkillIndicator indicator = SkillIndicator.CreateLineAndCircle(Color.red, Color.clear);
        indicator.UpdateLineAndCircle(startPos, endPos, 0f);

        // UI Warning !
        ShowWarning(player.position + Vector3.up * 2f);

        yield return new WaitForSeconds(2f);
        if (indicator != null) Destroy(indicator.gameObject);

        // Spawn chickens via pool
        for (int i = 0; i < 8; i++)
        {
            Vector3 offset = Vector3.Cross(randomDir, Vector3.up) * Random.Range(-3f, 3f);
            Vector3 spawnPos = startPos + offset;
            
            GameObject chicken = waveSpawner.SpawnEnemy(
                EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("chicken") : Resources.Load<GameObject>("Prefabs/chicken"),
                1f, 1.5f, spawnPos);
            if (chicken != null)
            {
                Enemy enemyScript = chicken.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.stampedeTarget = endPos + offset; 
                    enemyScript.isStampeding = true;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator ChickenBombRoutine()
    {
        if (player == null) yield break;

        Vector3 targetPos = player.position;

        // Warning circle
        SkillIndicator indicator = SkillIndicator.CreateRing(Color.red);
        indicator.UpdateRing(targetPos, 4f);

        ShowWarning(targetPos + Vector3.up * 2f);

        yield return new WaitForSeconds(1.5f);
        if (indicator != null) Destroy(indicator.gameObject);

        GameObject chickenPrefab = EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("chicken") : Resources.Load<GameObject>("Prefabs/chicken");
        if (chickenPrefab != null)
        {
            GameObject chicken = waveSpawner.SpawnEnemy(chickenPrefab, 1f, 0f, targetPos + Vector3.up * 10f);
            if (chicken != null)
            {
                ChickenBomb bomb = chicken.AddComponent<ChickenBomb>();
                bomb.targetPos = targetPos;
            }
        }
    }

    private IEnumerator SurroundRoutine()
    {
        if (player == null) yield break;
        
        GameObject linh1Prefab = EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("linh-1") : Resources.Load<GameObject>("Prefabs/linh-1");
        if (linh1Prefab == null) yield break;

        int count = 12;
        float radius = 15f;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 pos = player.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            waveSpawner.SpawnEnemy(linh1Prefab, 1f, 0.5f, pos);
        }
    }

    private IEnumerator MassiveLinh2Routine()
    {
        if (player == null) yield break;

        GameObject linh2Prefab = EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("linh-2") : Resources.Load<GameObject>("Prefabs/linh-2");
        GameObject linh1Prefab = EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("linh-1") : Resources.Load<GameObject>("Prefabs/linh-1");

        if (linh2Prefab != null)
        {
            SpawnMassiveEnemy(linh2Prefab, 5f, 0.8f);
        }

        yield return new WaitForSeconds(1f);

        if (linh1Prefab != null)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized * 5f;
                Vector3 pos = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                waveSpawner.SpawnEnemy(linh1Prefab, 1f, 1f, pos);
            }
        }
    }

    public void SpawnMassiveEnemy(GameObject prefab, float hpMult, float spdMult)
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * waveSpawner.spawnRadius;
        Vector3 pos = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        GameObject massiveObj = waveSpawner.SpawnEnemy(prefab, hpMult, spdMult, pos);
        if (massiveObj != null)
        {
            massiveObj.transform.localScale *= 2.5f; // visually massive
        }
    }

    private void SpawnFinalBoss()
    {
        GameObject bossPrefab = EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("boss") : Resources.Load<GameObject>("Prefabs/boss");
        if (bossPrefab != null)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * waveSpawner.spawnRadius;
            Vector3 pos = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Boss uses direct Instantiate (not pooled — only one boss)
            GameObject bossObj = Instantiate(bossPrefab, pos, Quaternion.identity);
            Boss bossScript = bossObj.GetComponent<Boss>();
            if (bossScript == null) bossScript = bossObj.AddComponent<Boss>();
            
            bossScript.waveSpawner = waveSpawner;
        }
    }

    private void ShowWarning(Vector3 pos)
    {
        // Placeholder for a warning UI or particle. We can use HitEffect temporarily
        HitEffect.Spawn(pos, Color.red, 1f);
    }
}

public class ChickenBomb : MonoBehaviour
{
    public Vector3 targetPos;
    public float fallSpeed = 15f;
    public float explosionRadius = 4f;
    public float explosionDamage = 30f;

    private Enemy enemyScript;

    private void Start()
    {
        enemyScript = GetComponent<Enemy>();
        if (enemyScript != null) enemyScript.enabled = false; // Disable normal AI while falling
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, fallSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPos) < 0.2f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        HitEffect.Spawn(transform.position, Color.red, 3f);
        
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector3 dir = (hit.transform.position - transform.position).normalized;
                    ph.TakeDamage(explosionDamage, dir, 5f);
                }
            }
        }
        
        // Chicken dies in explosion
        if (enemyScript != null)
        {
            enemyScript.TakeDamage(9999f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
