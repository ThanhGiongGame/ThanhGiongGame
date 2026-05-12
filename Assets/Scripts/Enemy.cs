using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 30f;
    public float currentHealth;

    public float moveSpeed = 3f;
    public float damage = 10f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Knockback")]
    public float selfKnockbackForce = 5f;
    public float selfKnockbackDuration = 0.2f;

    [Header("Movement")]
    public float personalSpaceRadius = 2f;
    public float separationRadius = 1.5f;
    public float separationStrength = 2f;

    private Transform player;

    private float attackTimer;

    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    private void Start()
    {
        currentHealth = maxHealth;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (knockbackTimer > 0f)
        {
            HandleKnockback();
        }
        else
        {
            MoveTowardPlayer();
            AttackPlayer();
        }

        // Prevent flying
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;
    }

    private void MoveTowardPlayer()
    {
        Vector3 offset =
            (transform.position - player.position).normalized
            * personalSpaceRadius;

        Vector3 targetPosition =
            player.position + offset;

        Vector3 directionToPlayer =
            (targetPosition - transform.position).normalized;

        directionToPlayer.y = 0f;

        Vector3 separationForce = GetSeparationForce();

        Vector3 finalDirection =
            (directionToPlayer + separationForce).normalized;

        finalDirection.y = 0f;

        transform.position +=
            finalDirection * moveSpeed * Time.deltaTime;

        if (finalDirection != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(finalDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
        }
    }

    private Vector3 GetSeparationForce()
    {
        Collider[] nearbyEnemies =
            Physics.OverlapSphere(transform.position, separationRadius);

        Vector3 separationForce = Vector3.zero;

        foreach (Collider collider in nearbyEnemies)
        {
            if (collider.gameObject == gameObject)
                continue;

            if (collider.CompareTag("Enemy"))
            {
                Vector3 away =
                    transform.position - collider.transform.position;

                away.y = 0f;

                float distance = away.magnitude;

                if (distance > 0f)
                {
                    separationForce +=
                        away.normalized / distance;
                }
            }
        }

        return separationForce * separationStrength;
    }

    private void AttackPlayer()
    {
        attackTimer -= Time.deltaTime;

        float distance =
            Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange && attackTimer <= 0f)
        {
            PlayerHealth playerHealth =
                player.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                Vector3 knockbackDirection =
                    player.position - transform.position;

                knockbackDirection.y = 0f;

                playerHealth.TakeDamage(
                    damage,
                    knockbackDirection,
                    5f
                );
            }

            ApplySelfKnockback();

            attackTimer = attackCooldown;
        }
    }

    private void ApplySelfKnockback()
    {
        knockbackTimer = selfKnockbackDuration;

        Vector3 awayFromPlayer =
            (transform.position - player.position).normalized;

        awayFromPlayer.y = 0f;

        knockbackVelocity =
            awayFromPlayer * selfKnockbackForce;
    }

    private void HandleKnockback()
    {
        knockbackTimer -= Time.deltaTime;

        knockbackVelocity.y = 0f;

        transform.position +=
            knockbackVelocity * Time.deltaTime;

        knockbackVelocity = Vector3.Lerp(
            knockbackVelocity,
            Vector3.zero,
            10f * Time.deltaTime
        );
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        Debug.Log(gameObject.name + " took damage: " + damageAmount);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}