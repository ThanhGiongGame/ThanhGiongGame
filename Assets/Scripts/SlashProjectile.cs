using System.Collections.Generic;
using UnityEngine;

public class SlashProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float sweepAngle = 180f; // Half circle
    public float lifetime = 1.0f; // How long the sweep takes (matches swing duration)
    public float radius = 2f; // Distance from player
    public float damage = 10f;

    private Transform playerTransform;
    private float currentLerpTime = 0f;
    private float startAngle;
    private float endAngle;

    private readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    public void Initialize(Transform player, Vector3 targetDirection)
    {
        playerTransform = player;
        
        // Calculate the base angle in degrees based on the target direction
        float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
        
        // Sweep from -halfAngle to +halfAngle around the target direction
        startAngle = targetAngle - (sweepAngle / 2f);
        endAngle = targetAngle + (sweepAngle / 2f);

        UpdatePositionAndRotation(0f);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (playerTransform == null) return;

        currentLerpTime += Time.deltaTime;
        float percentComplete = currentLerpTime / lifetime;
        
        UpdatePositionAndRotation(percentComplete);
    }

    private void UpdatePositionAndRotation(float percentComplete)
    {
        float currentAngle = Mathf.Lerp(startAngle, endAngle, percentComplete);
        
        // Convert angle back to direction
        Vector3 direction = new Vector3(Mathf.Sin(currentAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(currentAngle * Mathf.Deg2Rad));

        // Update position relative to player
        float slashHeight = 1f;

        transform.position =
            playerTransform.position +
            direction * radius +
            Vector3.up * slashHeight;
        // Rotate the slash to face outwards
        transform.rotation = Quaternion.LookRotation(direction);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();

            if (enemy != null && !hitEnemies.Contains(enemy))
            {
                hitEnemies.Add(enemy);
                enemy.TakeDamage(damage);
            }
        }
    }
}
