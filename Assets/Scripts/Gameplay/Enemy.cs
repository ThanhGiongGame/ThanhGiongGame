using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [HideInInspector] public float maxHealth = 30f;
    [HideInInspector] public float currentHealth;
    [HideInInspector] public float moveSpeed = 3f;
    [HideInInspector] public float damage = 10f;
    [HideInInspector] public float attackRange = 1.5f;
    [HideInInspector] public float attackCooldown = 1f;
    [HideInInspector] public bool isRanged = false;
    [HideInInspector] public float projectileSpeed = 12f;
    [HideInInspector] public float selfKnockbackForce = 5f;
    [HideInInspector] public float selfKnockbackDuration = 0.2f;
    [HideInInspector] public float personalSpaceRadius = 2f;
    [HideInInspector] public float separationRadius = 1.5f;
    [HideInInspector] public float separationStrength = 2f;

    [HideInInspector]
    public WaveSpawner waveSpawner;

    // Hit effect color: bright golden-yellow
    private static readonly Color HitColor = new Color(1f, 0.85f, 0.1f);
    private static Material _javelinMaterial;

    private Transform player;

    private float attackTimer;

    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    private float initialRotationX;
    private float initialRotationZ;

    public static bool GlobalFreeze = false;
    
    [HideInInspector] public bool isStampeding = false;
    [HideInInspector] public Vector3 stampedeTarget;
    public bool isSpecialWaveEnemy = false;

    [HideInInspector]
    public bool isBoss = false;

    // Pool support: track last combat time for despawn protection
    [HideInInspector]
    public float lastCombatTime = -999f;
    private float lastHitTime = -999f;

    // Original stats (for pool reset)
    private float _baseMoveSpeed;
    private float _baseMaxHealth;
    private float _baseDamage;
    private bool _baseStatsRecorded = false;

    private void Start()
    {
        gameObject.tag = "Enemy";
        currentHealth = maxHealth;
        initialRotationX = transform.eulerAngles.x;
        initialRotationZ = transform.eulerAngles.z;

        // Record base stats for pool reset (only once per prefab instance)
        if (!_baseStatsRecorded)
        {
            _baseMoveSpeed = moveSpeed;
            _baseMaxHealth = maxHealth;
            _baseDamage = damage;
            _baseStatsRecorded = true;
        }

        FindPlayer();

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

    private void OnEnable()
    {
        // Re-find player when activated from pool
        FindPlayer();
        currentHealth = maxHealth;
        initialRotationX = transform.eulerAngles.x;
        initialRotationZ = transform.eulerAngles.z;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    /// <summary>
    /// Reset all state when retrieved from object pool.
    /// </summary>
    public void ResetForPool()
    {
        // Restore base stats
        if (_baseStatsRecorded)
        {
            moveSpeed = _baseMoveSpeed;
            maxHealth = _baseMaxHealth;
            damage = _baseDamage;
        }
        currentHealth = maxHealth;

        // Reset combat state
        attackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        lastCombatTime = -999f;
        lastHitTime = -999f;
        isStampeding = false;
        stampedeTarget = Vector3.zero;
        isBoss = false;
        _isStunned = false;

        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = null;
        }

        // Re-enable this component
        enabled = true;

        // Re-find player
        FindPlayer();
    }

    private void Update()
    {
        if (GlobalFreeze)
            return;
        if (player == null) return;

        if (isBoss) return; // Let Boss.cs handle AI

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
        Vector3 targetPos = isStampeding ? stampedeTarget : player.position;
        Vector3 toTarget = targetPos - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        Vector3 moveDirection = Vector3.zero;

        if (isStampeding)
        {
            moveDirection = toTarget.normalized;
            if (distance < 1f)
            {
                Die(); // Despawn gracefully when reaching the end of the line
            }
        }
        // Only move if outside attack range
        else if (distance > attackRange * 0.9f)
        {
            moveDirection = toTarget.normalized;
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
    }

    // ResolveOverlaps removed for massive performance gains

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
            lastCombatTime = Time.time; // Track combat for despawn protection

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
            if (_javelinMaterial == null)
            {
                _javelinMaterial = new Material(Shader.Find("Standard"));
                _javelinMaterial.color = new Color(0.8f, 0.5f, 0.2f);
            }
            rend.sharedMaterial = _javelinMaterial;
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
        if (isBoss)
        {
            if (Time.time < lastHitTime + 0.1f) return;
            
            Boss bossScript = GetComponent<Boss>();
            if (bossScript != null && bossScript.IsStunned) return; // Invulnerable during phase transition
        }

        lastHitTime = Time.time;
        lastCombatTime = Time.time; // Track combat for despawn protection

        Debug.Log(gameObject.name + " took damage: " + damageAmount);

        // Spawn a red hit burst at the enemy's centre
        HitEffect.Spawn(transform.position + Vector3.up * 0.8f, Color.red, 1.0f);

        if (isBoss)
        {
            Boss bossScript = GetComponent<Boss>();
            if (bossScript != null)
            {
                if (!bossScript.Phase2Active)
                {
                    // Health Gating in Phase 1
                    if (currentHealth - damageAmount <= maxHealth * 0.5f)
                    {
                        currentHealth = maxHealth * 0.5f;
                        bossScript.TriggerPhase2();
                        return;
                    }
                }
                
                currentHealth -= damageAmount;
                
                if (currentHealth <= 0f)
                {
                    currentHealth = 0f;
                    bossScript.TriggerBossDeathCinematic();
                    return;
                }
            }
            else
            {
                currentHealth -= damageAmount;
                if (currentHealth <= 0f) Die();
            }
        }
        else
        {
            currentHealth -= damageAmount;
            if (currentHealth <= 0f)
            {
                Die();
            }
        }
    }

    private void Die()
    {
        // Báo cho WaveSpawner biết đã kill enemy
        if (waveSpawner != null && !isSpecialWaveEnemy)
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

        // Return to pool instead of destroying
        if (EnemyPool.Instance != null)
        {
            EnemyPool.Instance.ReturnEnemy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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