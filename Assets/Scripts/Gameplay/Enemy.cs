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

    [Header("Ranged Attack")]
    public bool isRanged = false;
    public float projectileSpeed = 12f;

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

    private float initialRotationX;
    private float initialRotationZ;

    public static bool GlobalFreeze = false;
    private void Start()
    {
        currentHealth = maxHealth;
        initialRotationX = transform.eulerAngles.x;
        initialRotationZ = transform.eulerAngles.z;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        // Configure EnemyC as ranged javelin thrower
        //if (gameObject.name.Contains("EnemyC"))
        //{
        //    isRanged = true;
        //    attackRange = 8f;
        //    attackCooldown = 2.5f;
        //    moveSpeed = 2f;

        //    // Fix visual for EnemyC to use EnemyB's soldier mesh
        //    WaveSpawner spawner = FindObjectOfType<WaveSpawner>();
        //    if (spawner != null)
        //    {
        //        GameObject enemyBPrefab = spawner.FindEnemyBPrefab();
        //        if (enemyBPrefab != null)
        //        {
        //            Transform cylinder = transform.Find("Cylinder");
        //            if (cylinder != null)
        //            {
        //                cylinder.gameObject.SetActive(false);
        //            }

        //            Transform bCylinder = enemyBPrefab.transform.Find("Cylinder");
        //            if (bCylinder != null)
        //            {
        //                GameObject visual = Instantiate(bCylinder.gameObject, transform);
        //                visual.name = "Visual";
        //                visual.transform.localPosition = Vector3.zero;
        //                visual.transform.localRotation = Quaternion.identity;
        //                visual.transform.localScale = new Vector3(3f, 3f, 3f);

        //                foreach (Collider col in visual.GetComponentsInChildren<Collider>())
        //                {
        //                    col.enabled = false;
        //                }
        //            }
        //        }
        //    }
        //}

        //// Map 2 Armored Horse swap
        //if (PlayerPrefs.GetInt("SelectedMap", 0) == 1 && gameObject.name.Contains("EnemyA"))
        //{
        //    PlayerEquipmentLoader loader = FindObjectOfType<PlayerEquipmentLoader>();
        //    if (loader != null && loader.horseTier2 != null)
        //    {
        //        Transform cylinder = transform.Find("Cylinder");
        //        if (cylinder != null)
        //        {
        //            cylinder.gameObject.SetActive(false);
        //        }

        //        GameObject ironHorse = Instantiate(loader.horseTier2, transform);
        //        ironHorse.transform.localPosition = Vector3.zero;
        //        ironHorse.transform.localRotation = Quaternion.identity;
        //        ironHorse.transform.localScale = new Vector3(6f, 6f, 6f);

        //        foreach (Collider col in ironHorse.GetComponentsInChildren<Collider>())
        //        {
        //            col.enabled = false;
        //        }
        //    }
        //}

        //// Auto-align visuals to stand on the ground based on CapsuleCollider
        //CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        //if (capsule != null && !gameObject.name.Contains("EnemyA"))
        //{
        //    Transform visuals = transform.Find("Visuals") ?? transform.Find("Cylinder") ?? transform.Find("Visual");
        //    if (visuals != null && visuals.transform.localPosition.y == 0f)
        //    {
        //        if (capsule.center.y > 0f)
        //        {
        //            visuals.transform.localPosition = new Vector3(
        //                visuals.transform.localPosition.x,
        //                capsule.center.y,
        //                visuals.transform.localPosition.z
        //            );
        //        }
        //    }
        //}
    }



    private void Update()
    {
        if (GlobalFreeze)
            return;
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

        Vector3 finalDirection = moveDirection.normalized;

        finalDirection.y = 0f;

        transform.position +=
            finalDirection * moveSpeed * Time.deltaTime;

        if (finalDirection != Vector3.zero)
        {
            Vector3 flat = new Vector3(finalDirection.x, 0f, finalDirection.z); 
            Quaternion targetRotation =
                Quaternion.LookRotation(flat);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

            Vector3 targetEuler = transform.rotation.eulerAngles;
            targetEuler.x = initialRotationX;
            targetEuler.z = initialRotationZ;
            transform.rotation = Quaternion.Euler(targetEuler);
        }

        ResolveOverlaps();
    }

    private void ResolveOverlaps()
    {
        Collider myCol = GetComponent<Collider>();
        if (myCol == null) return;

        Collider[] nearby = Physics.OverlapBox(myCol.bounds.center, myCol.bounds.extents, Quaternion.identity);
        foreach (Collider other in nearby)
        {
            if (other == myCol || other.isTrigger || !other.CompareTag("Enemy")) continue;

            if (Physics.ComputePenetration(myCol, transform.position, transform.rotation,
                                           other, other.transform.position, other.transform.rotation,
                                           out Vector3 dir, out float dist))
            {
                transform.position += dir * (dist * 0.5f);
                other.transform.position -= dir * (dist * 0.5f);
            }
        }
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
            if (isRanged)
            {
                ThrowProjectile(offset.normalized);
            }
            else
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
            }

            attackTimer = attackCooldown;
        }
    }

    private void ThrowProjectile(Vector3 dir)
    {
        GameObject javelin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        javelin.name = "EnemyJavelin";

        Collider oldCol = javelin.GetComponent<Collider>();
        if (oldCol != null) Destroy(oldCol);

        CapsuleCollider cap = javelin.AddComponent<CapsuleCollider>();
        cap.isTrigger = true;
        cap.radius = 0.15f;
        cap.height = 1.8f;
        cap.direction = 1;

        javelin.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
        javelin.transform.position = transform.position + Vector3.up * 1f;
        javelin.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);

        Renderer rend = javelin.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(0.8f, 0.5f, 0.2f);
        }

        EnemyJavelin script = javelin.AddComponent<EnemyJavelin>();
        script.damage = damage;
        script.direction = dir;
        script.speed = projectileSpeed;
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
        HitEffect.Spawn(transform.position + Vector3.up * 0.8f, HitColor, 3.0f);

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
            waveSpawner.OnEnemyKilled(transform.position);
        }

        // XP Drop Chance logic
        float xpAmount = 10f;
        if (UpgradeManager.Instance != null)
        {
            float doubleXpChance = UpgradeManager.Instance.XpDropChanceLevel * 0.15f;
            if (Random.value < doubleXpChance)
            {
                xpAmount *= 2f;
                // Quick yellow glow effect for critical double XP
                HitEffect.Spawn(transform.position, Color.yellow, 1.5f);
            }
        }
        XPOrb.Spawn(transform.position, xpAmount);

        // Health Pack Drop Chance logic
        if (UpgradeManager.Instance != null)
        {
            float healthPackChance = UpgradeManager.Instance.HealthPackDropChanceLevel * 0.08f;
            if (Random.value < healthPackChance)
            {
                HealthPack.Spawn(transform.position, 25f);
            }
        }

        Destroy(gameObject);
    }
}

public class EnemyJavelin : MonoBehaviour
{
    public float damage;
    public Vector3 direction;
    public float speed;
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, direction, 8f);
            }
            Destroy(gameObject);
        }
    }
}