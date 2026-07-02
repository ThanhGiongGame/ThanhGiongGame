using System.Collections.Generic;
using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    public float damage = 10f;

    private bool canDamage;

    private readonly HashSet<Enemy> hitEnemies = new();

    public void BeginAttack()
    {
        canDamage = true;
        hitEnemies.Clear();
    }

    public void EndAttack()
    {
        canDamage = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage)
            return;

        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
            return;

        if (hitEnemies.Contains(enemy))
            return;

        hitEnemies.Add(enemy);
        Debug.Log(enemy);
        enemy.TakeDamage(damage);
    }
}