using System.Collections;
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

    [HideInInspector]
    public WaveSpawner waveSpawner;

    // Hit effect color: bright golden-yellow
    private static readonly Color HitColor = new Color(1f, 0.85f, 0.1f);

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
        Vector3 toPlayer =
            player.position - transform.position;

        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        Vector3 moveDirection = Vector3.zero;

        // Only move if outside attack range
        if (distance > attackRange * 0.9f)
        {
            moveDirection = toPlayer.normalized;
        }

        Vector3 separationForce =
            GetSeparationForce();

        Vector3 finalDirection =
            (moveDirection + separationForce).normalized;

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

    private Coroutine _knockbackCoroutine;
    private bool _isStunned;
    public void ApplyKnockbackStun(Vector3 direction, float force, float duration)
    {
        // If there's an active knockback coroutine, stop it first to prevent conflicts
        if (_knockbackCoroutine != null)
            StopCoroutine(_knockbackCoroutine);

        _knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction, force, duration));
    }
    private IEnumerator KnockbackRoutine(Vector3 direction, float force, float duration)
    {
        _isStunned = true;
        float elapsed = 0f;

        // Ensure direction is strictly horizontal if your game relies on flat XZ physics
        direction.y = 0f;
        direction.Normalize();

        // Optional: Disable enemy AI movement or NavMeshAgent here
        // var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        // if (agent != null) agent.isStopped = true;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            // Calculate diminishing force over time (linear decay)
            float currentForce = Mathf.Lerp(force, 0f, elapsed / duration);

            // Move the enemy. If using a CharacterController or Rigidbody, adapt this line:
            transform.Translate(direction * currentForce * dt, Space.World);

            yield return null;
        }

        // Optional: Re-enable enemy AI movement here
        // if (agent != null) agent.isStopped = false;

        _isStunned = false;
        _knockbackCoroutine = null;
    }


    private void AttackPlayer()
    {
        attackTimer -= Time.deltaTime;

        Vector3 offset = player.position - transform.position;
        offset.y = 0f;

        float sqrDistance = offset.sqrMagnitude;
        float attackRangeSqr = attackRange * attackRange;

        if (sqrDistance <= attackRangeSqr + 0.1f && attackTimer <= 0f)
        {
            PlayerHealth playerHealth =
                player.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                Vector3 knockbackDirection =
                    offset.normalized;

                playerHealth.TakeDamage(
                    damage,
                    knockbackDirection,
                    10f
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

        // Spawn a yellow hit burst at the enemy's centre
        HitEffect.Spawn(transform.position + Vector3.up * 0.8f, HitColor, 1.0f);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // Báo cho WaveSpawner biết đã kill enemy
        if (waveSpawner != null)
        {
            waveSpawner.OnEnemyKilled();
        }
        XPOrb.Spawn(transform.position, 10f);
        Destroy(gameObject);
    }
}