using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private int attackId = 0;
    [Header("Movement")]
    public float moveSpeed = 5f;
    [HideInInspector]
    public float slashDamageMultiplier = 1.0f; // Defaults to 100% damage base
    [Header("Attack")]
    public GameObject slashPrefab;
    [Header("Horse Rotation")]
    public float turnSpeed = 300f;
    public Transform attackSpawnPoint;
    public float attackInterval = 1.2f;   // 0.2s wind-up + 1.0s swing
    public float attackWindUp = 0.2f;   // delay before slash spawns
    public WeaponDamage weaponDamage;
    public WeaponTrail weaponTrail;
    public Boolean Tutorial = false;
    public Animator riderAnimator;
    public Animator horseAnimator;
    public HorseLoader horseLoader;

    [Header("Audio")]
    public AudioClip slashSound;
    public AudioClip footstepSound;
    public float footstepInterval = 0.5f;
    private AudioSource audioSource;
    private float footstepTimer;

    // ---- Private references ----
    private CharacterController controller;
    private PlayerHealth playerHealth;
    private Camera mainCamera;
    private float walkTimer = 0f;
    private bool isAttacking;

    // ---- Attack timer ----
    private float attackTimer;

    // Top-down game: use a small constant downward push to stay grounded
    private const float GroundStick = -2f;

    // ---- Unused legacy field (kept to avoid prefab warnings) ----
    private Transform enemyTransform;

    public bool IsPerformingSkill { get; set; }
    public bool isPhase2BuffActive = false;
    private float phase2AuraTimer = 0f;
    public bool IsInvulnerable
    {
        get => _isInvulnerable;
        set
        {
            _isInvulnerable = value;
            _invulnerableExpiresAt = value ? Time.time + MaxInvulnerableSeconds : 0f;
        }
    }
    private bool _isInvulnerable;
    private float _invulnerableExpiresAt;
    private const float MaxInvulnerableSeconds = 5f;
    // -------------------------------------------------------

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        horseLoader = GetComponent<HorseLoader>();
        horseLoader.LoadHorse();
        weaponDamage = GetComponent<WeaponDamage>();
        IsInvulnerable = false;
        float baseDamage = 20f;
        float baseMaxHealth = 100f;
        float baseSpeed = 5f;
        moveSpeed = baseSpeed;
        playerHealth.maxHealth = baseMaxHealth;
        slashDamageMultiplier = baseDamage / 20f;

        gameObject.AddComponent<LegendaryUpgradeSystem>();
        new GameObject("LegendHUD").AddComponent<LegendHUD>();

        // Always find the camera first so combat functions even if visuals are missing
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("PlayerController: No Camera tagged 'MainCamera' found in the scene!");
        }

        attackTimer = attackInterval;

        // --- Đọc dữ liệu Loadout từ Shop Menu ---
        if (PlayerPrefs.GetInt("Item_DamageBuff", 0) == 2)
        {
            baseDamage += 15f;
        }

        if (PlayerPrefs.GetInt("Item_HealthBuff", 0) == 2)
        {
            baseMaxHealth += 50f;
        }

        if (PlayerPrefs.GetInt("Item_SpeedBuff", 0) == 2)
        {
            baseSpeed += 3f;
        }
    }
    void Update()
    {
        ExpireStuckInvulnerability();

        if (!IsPerformingSkill)
        {
            //HandleRotation();
            HandleAttack();
        }
        HandleMovement();

        if (isPhase2BuffActive)
        {
            HandlePhase2Buffs();
        }
    }

    private void HandlePhase2Buffs()
    {
        // Massive health regeneration
        if (playerHealth != null)
        {
            playerHealth.Heal(50f * Time.deltaTime);
        }

        // Circular aura fire damage
        phase2AuraTimer -= Time.deltaTime;
        if (phase2AuraTimer <= 0f)
        {
            phase2AuraTimer = 0.5f;
            Collider[] hits = Physics.OverlapSphere(transform.position, 6f);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    Enemy e = hit.GetComponent<Enemy>();
                    if (e != null)
                    {
                        e.TakeDamage(100f);
                    }
                }
            }
        }
    }

    public void StartFinalMoveCinematic()
    {
        StartCoroutine(FinalMoveCinematicCoroutine());
    }

    private IEnumerator FinalMoveCinematicCoroutine()
    {
        IsPerformingSkill = true;
        
        // 1. Knockback all enemies and glowing particle
        HitEffect.Spawn(transform.position + Vector3.up, Color.yellow, 5f);
        Collider[] hits = Physics.OverlapSphere(transform.position, 15f);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy e = hit.GetComponent<Enemy>();
                if (e != null)
                {
                    Vector3 dir = (e.transform.position - transform.position).normalized;
                    dir.y = 0;
                    e.ApplyKnockbackStun(dir, 30f, 2f);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 2. Change to Tier 4
        PlayerPrefs.SetString("EquippedHorse", "Horse_Tier4");
        PlayerPrefs.SetString("EquippedCharacter", "Character_Tier4");
        PlayerPrefs.SetString("EquippedWeapon", "Weapon_Tier4");
        
        if (horseLoader != null)
        {
            horseLoader.LoadHorse();
        }
        
        yield return new WaitForSeconds(0.1f);

        // 3. Trigger Final Move Animation and Camera Zoom
        if (riderAnimator != null)
        {
            riderAnimator.Play("Final");
            riderAnimator.SetTrigger("Final Move");
        }
        
        CameraController cam = Camera.main.GetComponent<CameraController>();
        if (cam != null)
        {
            cam.target = transform;
            cam.SetCinematicView(new Vector3(0, 5f, -6f), 35f);
        }

        // Timeline:
        // + 0:10 enlarge himself
        yield return new WaitForSeconds(10f);
        transform.localScale = new Vector3(3f, 3f, 3f);
        if (cam != null) cam.ResetView();

        // + 0:30 prepare to attack
        yield return new WaitForSeconds(20f);
        // Add any prepare visuals here if needed
        HitEffect.Spawn(transform.position + Vector3.up * 2f, Color.red, 3f);

        // + 1:00 attack, shake, damage all enemies
        yield return new WaitForSeconds(30f);
        if (cam != null) cam.Shake(2f, 1f);
        
        hits = Physics.OverlapSphere(transform.position, 100f);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy e = hit.GetComponent<Enemy>();
                if (e != null && !e.isBoss)
                {
                    e.TakeDamage(999999f);
                }
            }
        }

        // + 1:10 return to normal
        yield return new WaitForSeconds(10f);
        transform.localScale = Vector3.one;

        // End cinematic, start phase 2 buffs
        IsPerformingSkill = false;
        
        // Massive health buff
        if (playerHealth != null)
        {
            playerHealth.maxHealth = 9999f;
            playerHealth.currentHealth = 9999f;
        }
        
        // Golden Trail
        TrailRenderer tr = gameObject.AddComponent<TrailRenderer>();
        tr.startWidth = 2f;
        tr.endWidth = 0f;
        tr.time = 2f;
        tr.material = new Material(Shader.Find("Sprites/Default"));
        tr.material.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        tr.startColor = new Color(1f, 0.8f, 0.2f, 0.5f);
        tr.endColor = new Color(1f, 0.8f, 0.2f, 0f);
        
        isPhase2BuffActive = true;
    }

    private void HandleMovement()
    {
        //if (equipmentLoader != null && equipmentLoader.horseRoot != null)
        //{
        //    horseAnimator = equipmentLoader.horseRoot.GetComponentInChildren<Animator>();
        //}

        // If a skill like Flame Dash is currently driving CharacterController.Move(),
        // we completely bypass the standard movement calculation loop.
        if (IsPerformingSkill) return;

        Vector3 moveVelocity = Vector3.zero;

        bool isKnockedBack = playerHealth != null && playerHealth.IsKnockedBack;

        if (!isKnockedBack && Keyboard.current != null)
        {
            float horizontal = 0f;
            float vertical = 0f;
            float moveMutiplier = 1f;
            if (walkTimer > 3)
            {
                moveMutiplier = 1.8f;
            }
            else moveMutiplier = 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;

            Vector3 moveDir =
                new Vector3(horizontal, 0f, vertical);

            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(moveDir.normalized);

                transform.rotation =
                    Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        turnSpeed * Time.deltaTime
                    );
            }

            moveVelocity =
                moveDir.normalized
                * moveSpeed
                * moveMutiplier;
        }
        if (horseAnimator != null)
        {
            if (isKnockedBack)
            {
                horseAnimator.SetBool("isWalking", false);
                walkTimer = 0f;
                
                if (footstepSound != null && audioSource != null && audioSource.clip == footstepSound && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
            else
            {
                bool hasHorizontalMovement = new Vector3(moveVelocity.x, 0f, moveVelocity.z).sqrMagnitude > 0.01f;
                if (hasHorizontalMovement) {
                    walkTimer += Time.deltaTime;
                    
                    if (footstepSound != null && audioSource != null)
                    {
                        if (audioSource.clip != footstepSound)
                        {
                            audioSource.clip = footstepSound;
                            audioSource.loop = true;
                        }
                        if (!audioSource.isPlaying)
                        {
                            audioSource.Play();
                        }
                    }
                }
                else
                {
                    walkTimer = 0f;
                    if (footstepSound != null && audioSource != null && audioSource.clip == footstepSound && audioSource.isPlaying)
                    {
                        audioSource.Stop();
                    }
                }
                horseAnimator.SetBool("isWalking", hasHorizontalMovement);
            }
        }

        // --- Knockback (from PlayerHealth) ---
        if (playerHealth != null && isKnockedBack)
        {
            Vector3 kb = playerHealth.KnockbackVelocity;
            moveVelocity.x = kb.x;
            moveVelocity.z = kb.z;
        }
        // --- Small downward push keeps CharacterController grounded ---
        moveVelocity.y = GroundStick;

        // --- ONE Move() call for everything ---
        controller.Move(moveVelocity * Time.deltaTime);

        // --- Pin Y to ground (same as enemies) ---
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;
    }

    private void ExpireStuckInvulnerability()
    {
        if (_isInvulnerable && _invulnerableExpiresAt > 0f && Time.time >= _invulnerableExpiresAt)
        {
            _isInvulnerable = false;
            _invulnerableExpiresAt = 0f;
            Debug.LogWarning("PlayerController: invulnerability auto-expired to avoid a stuck immortal state.");
        }
    }

    // Mouse Detection
    private float GetMouseRelativeAngle()
    {
        if (!TryGetMouseGroundPoint(out Vector3 mousePoint))
            return 0f;

        Vector3 mouseDir =
            mousePoint - transform.position;

        mouseDir.y = 0;

        float angle =
            Vector3.SignedAngle(
                transform.forward,
                mouseDir,
                Vector3.up
            );

        if (angle < 0)
            angle += 360;

        return angle;
    }
    private bool TryGetMouseGroundPoint(out Vector3 point)
    {
        point = transform.position + transform.forward * 5f;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return false;

        if (Mouse.current == null)
            return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float distance))
            return false;

        point = ray.GetPoint(distance);
        return true;
    }
    // -------------------------------------------------------
    // Attack
    // -------------------------------------------------------
    public enum AttackDirection
    {
        West = 0,
        North = 1,
        East = 2
    }
    private AttackDirection GetAttackDirection()
    {
        float angle = GetMouseRelativeAngle();

        // vùng trước mặt
        if (angle <= 45f || angle >= 315f)
        {
            return AttackDirection.North;
        }

        // nửa phải
        if (angle > 45f && angle < 180f)
        {
            return AttackDirection.East;
        }

        // nửa trái
        return AttackDirection.West;
    }
    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f && !isAttacking)
        {
            StartCoroutine(AttackCoroutine());

            attackTimer = attackInterval;
        }
    }
    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;

        try
        {
            TryGetMouseGroundPoint(out Vector3 targetPoint);

            AttackDirection attackDir = GetAttackDirection();

            if (riderAnimator != null)
            {
                riderAnimator.SetInteger("AttackDirection", (int)attackDir);
                riderAnimator.SetTrigger("Attack");
            }
            
            if (slashSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(slashSound);
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TutorialScene")
            {
                SpawnBasicSlash(targetPoint);
            }

            // Chờ animation kết thúc
            yield return new WaitForSeconds(attackInterval);
        }
        finally
        {
            isAttacking = false;
        }
    }
    public void EnableWeaponDamage()
    {
        weaponDamage.BeginAttack();
        weaponTrail.BeginTrail();
    }

    public void DisableWeaponDamage()
    {
        weaponDamage.EndAttack();
        weaponTrail.EndTrail();
    }

    private void SpawnBasicSlash(Vector3 targetPoint)
    {
        Vector3 dir = (targetPoint - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f) dir.Normalize();
        else dir = transform.forward;
        
        // Spawn a transparent hitbox instead of a white cube
        GameObject slash = new GameObject("BasicSlash");
        slash.transform.position = attackSpawnPoint != null ? attackSpawnPoint.position : transform.position + Vector3.up * 1.2f + dir * 1.5f;
        slash.transform.rotation = Quaternion.LookRotation(dir);
        
        // Quick visual effect
        HitEffect.Spawn(slash.transform.position, new Color(1f, 0.8f, 0.2f), 1.5f);
        
        BoxCollider col = slash.AddComponent<BoxCollider>();
        col.size = new Vector3(3f, 0.5f, 0.5f);
        col.isTrigger = true;
        
        var logic = slash.AddComponent<BasicSlashProjectile>();
        logic.damage = slashDamageMultiplier * 20f;
        logic.dir = dir;
        
        Rigidbody rb = slash.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true; // No physics movement
        
        Destroy(slash, 0.25f); // Short duration melee swipe
    }

    public void AscendToSky()
    {
        StartCoroutine(AscendToSkyCoroutine());
    }
    
    private IEnumerator AscendToSkyCoroutine()
    {
        IsPerformingSkill = true; // disable control
        isPhase2BuffActive = false; // turn off damage aura so it's peaceful
        
        if (riderAnimator != null)
        {
            riderAnimator.enabled = false;
        }

        if (horseAnimator != null)
        {
            horseAnimator.SetBool("isWalking", true);
        }
        
        float t = 0;
        float effectTimer = 0;
        while (t < 6f)
        {
            t += Time.deltaTime;
            effectTimer += Time.deltaTime;
            
            // Spawn glow effect periodically
            if (effectTimer > 0.15f)
            {
                effectTimer = 0f;
                HitEffect.Spawn(transform.position, new Color(1f, 0.9f, 0.2f, 0.5f), 1.5f);
            }
            
            // Move up and slightly forward into the sky
            transform.position += (transform.forward * 2f + Vector3.up * 3f) * Time.deltaTime;
            
            yield return null;
        }
        
        // Load Win Screen or Main Menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }
}

public class BasicSlashProjectile : MonoBehaviour
{
    public float damage;
    public Vector3 dir;

    void Update()
    {
        // Stationary melee slash, no movement
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null)
            {
                e.TakeDamage(damage);
                e.ApplyKnockbackStun(dir, 8f, 0.2f);
            }
        }
    }
}
