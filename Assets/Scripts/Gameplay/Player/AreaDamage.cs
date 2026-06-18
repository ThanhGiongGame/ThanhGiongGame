using System.Collections.Generic;
using UnityEngine;

public class AreaDamage : MonoBehaviour
{
    [Header("Damage")]
    public float damagePerTick = 5f;
    public float tickInterval = 0.5f;
    public float duration = 5f;

    private readonly List<Enemy> enemiesInArea = new();

    private float tickTimer;

    private void Start()
    {
    }

    private void Update()
    {
        tickTimer -= Time.deltaTime;

        if (tickTimer <= 0f)
        {
            DamageEnemies();
            tickTimer = tickInterval;
        }
    }

    private void DamageEnemies()
    {
        Debug.Log("Enemy in Area" + enemiesInArea.Count);

        foreach (Enemy enemy in enemiesInArea)
        {
                if (enemy != null)
                {
                    enemy.TakeDamage(damagePerTick);
                }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();

            if (enemy != null && !enemiesInArea.Contains(enemy))
            {
                enemiesInArea.Add(enemy);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();

            if (enemy != null)
            {
                enemiesInArea.Remove(enemy);
            }
        }
    }
}