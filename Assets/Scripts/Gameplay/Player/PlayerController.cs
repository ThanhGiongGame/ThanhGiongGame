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
    public float sprintMultiplier = 1.8f;
    [HideInInspector]
    public float slashDamageMultiplier = 1.0f; // Defaults to 100% damage base
    [Header("Attack")]
    public GameObject slashPrefab;
    [Range(0.001f, 0.1f)] public float attackArcMouseSensitivity = 0.015f;
    [Min(1f)] public float attackArcMoveSpeed = 4f;
    [Header("Horse Rotation")]
    public float turnSpeed = 300f;
    public float mountedTurnSpeed = 150f;
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
    private bool isAttacking;
    private float attackArcPosition;
    private float attackArcTarget;
    private AttackDirection mountedAttackDirection = AttackDirection.North;

    // ---- Attack timer ----
    private float attackTimer;

    private Vector3 currentAttackDirVector = Vector3.forward;
    private bool isArcAttacking = false;
    private System.Collections.Generic.HashSet<Enemy> hitEnemiesThisAttack = new System.Collections.Generic.HashSet<Enemy>();
    private Collider[] _attackHits = new Collider[50];


    // Top-down game: use a small constant downward push to stay grounded
    private const float GroundStick = -2f;

    // ---- Unused legacy field (kept to avoid prefab warnings) ----
    private Transform enemyTransform;
    
    [HideInInspector]
    public float weaponScaleMultiplier = 1.0f;

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
        float baseDamage = slashDamageMultiplier > 0f
            ? slashDamageMultiplier * 20f
            : 20f;
        float baseMaxHealth = playerHealth != null ? playerHealth.maxHealth : 100f;
        float baseSpeed = moveSpeed;

        // Controls are global, while each map keeps its own combat/stat values.
        sprintMultiplier = 1.8f;
        turnSpeed = 300f;
        mountedTurnSpeed = 150f;
        attackArcMouseSensitivity = 0.015f;
        attackArcMoveSpeed = 4f;
        attackInterval = 1.2f;
        attackWindUp = 0.2f;


        // --- Đọc dữ liệu Loadout (Buff từ menu) ---
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

        // --- Đọc dữ liệu Equipment (Tier 1 -> Tier 4) ---
        string eqChar = PlayerPrefs.GetString("EquippedCharacter", "Character_Tier1");
        if (eqChar == "Character_Tier2") baseMaxHealth += 50f;
        else if (eqChar == "Character_Tier3") baseMaxHealth += 150f;
        else if (eqChar == "Character_Tier4") baseMaxHealth += 300f;

        string eqHorse = PlayerPrefs.GetString("EquippedHorse", "Horse_Tier1");
        if (eqHorse == "Horse_Tier2") baseSpeed += 1f;
        else if (eqHorse == "Horse_Tier3") baseSpeed += 2f;
        else if (eqHorse == "Horse_Tier4") baseSpeed += 3.5f;

        string eqWeapon = PlayerPrefs.GetString("EquippedWeapon", "Weapon_Tier1");
        if (eqWeapon == "Weapon_Tier2") baseDamage += 10f;
        else if (eqWeapon == "Weapon_Tier3") baseDamage += 20f;
        else if (eqWeapon == "Weapon_Tier4") baseDamage += 40f;

        moveSpeed = baseSpeed;
        playerHealth.maxHealth = baseMaxHealth;
        playerHealth.currentHealth = baseMaxHealth;
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
        if (GetComponent<AttackArcIndicator>() == null)
        {
            gameObject.AddComponent<AttackArcIndicator>();
        }
    }

    private IEnumerator EnsureSharedGameplayUiAfterStartup()
    {
        // Give scene-owned components one frame to build first. Map 1 has an
        // older setup, so we verify the actual UI objects instead of assuming
        // a component reference means its canvas exists.
        yield return null;

        GameObject systems = GameObject.Find("GameplayRuntimeSystems");
        if (systems == null)
        {
            systems = new GameObject("GameplayRuntimeSystems");
        }

        if (FindFirstObjectByType<XPManager>() == null)
        {
            systems.AddComponent<XPManager>();
        }

        if (FindFirstObjectByType<UpgradeManager>() == null)
        {
            systems.AddComponent<UpgradeManager>();
        }

        if (FindFirstObjectByType<LevelUpUI>() == null)
        {
            systems.AddComponent<LevelUpUI>();
        }

        if (FindFirstObjectByType<GameOverManager>() == null)
        {
            systems.AddComponent<GameOverManager>();
        }

        if (GameObject.Find("HPBorder") == null)
        {
            PlayerHealthUI healthUi = systems.GetComponent<PlayerHealthUI>();
            if (healthUi == null)
            {
                healthUi = systems.AddComponent<PlayerHealthUI>();
            }

            healthUi.playerHealth = playerHealth;
        }

        if (GameObject.Find("XPBorder") == null && systems.GetComponent<PlayerLevelUI>() == null)
        {
            systems.AddComponent<PlayerLevelUI>();
        }

        if (GameObject.Find("SkillHotbarCanvas") == null && systems.GetComponent<SkillCooldownUI>() == null)
        {
            systems.AddComponent<SkillCooldownUI>();
        }

        if (GameObject.Find("GameplayPauseCanvas") == null
            && FindFirstObjectByType<GameplayPauseMenu>(FindObjectsInactive.Include) == null)
        {
            systems.AddComponent<GameplayPauseMenu>();
        }
    }

    void Update()
    {
        ExpireStuckInvulnerability();

        // Dev Cheat: Instantly finish map
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F8))
        {
            GameObject mapDrop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mapDrop.name = "MapUnlockDrop";
            mapDrop.transform.position = transform.position + Vector3.up * 1f;
            mapDrop.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            mapDrop.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Collider oldCol = mapDrop.GetComponent<Collider>();
            if (oldCol != null) Destroy(oldCol);

            SphereCollider trigger = mapDrop.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 3f;

            mapDrop.AddComponent<MapUnlockItem>();
            Debug.Log("Forced Map Finish with F8!");
        }

        if (!IsPerformingSkill)
        {
            UpdateAttackArcInput();
            HandleAttack();
        }
        HandleMovement();

        if (isPhase2BuffActive)
        {
            HandlePhase2Buffs();
        }

        if (isArcAttacking)
        {
            HandleArcAttackDamage();
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
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 6f, _attackHits);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _attackHits[i];
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
        Enemy.GlobalFreeze = true; // Freeze all enemies
        
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
        
        if (horseLoader != null) horseLoader.LoadHorse();
        
        RiderLoader riderLoader = GetComponent<RiderLoader>();
        if (riderLoader != null) riderLoader.LoadRider();
        
        WeaponLoader weaponLoader = GetComponent<WeaponLoader>();
        if (weaponLoader != null) weaponLoader.LoadWeapon();
        
        if (playerHealth != null)
        {
            playerHealth.currentHealth = playerHealth.maxHealth;
        }
        
        yield return new WaitForSeconds(0.1f);

        // 3. Custom Animation: Fly up, scale up, hover, slam down
        CameraController cam = Camera.main.GetComponent<CameraController>();
        if (cam != null)
        {
            cam.target = transform;
            cam.SetCinematicView(new Vector3(0, 10f, -20f), 20f);
        }

        Vector3 startPos = transform.position;
        Vector3 highPos = startPos + Vector3.up * 15f;
        Vector3 startScale = Vector3.one;
        Vector3 giantScale = new Vector3(3f, 3f, 3f);

        // Ascend & Grow
        float t = 0;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            float frac = t / 1.5f;
            transform.position = Vector3.Lerp(startPos, highPos, frac * frac); // Ease in
            transform.localScale = Vector3.Lerp(startScale, giantScale, frac);
            HitEffect.Spawn(transform.position + UnityEngine.Random.insideUnitSphere * 2f, Color.yellow, 0.5f);
            yield return null;
        }

        // Hover briefly
        yield return new WaitForSeconds(0.5f);

        // Slam down
        t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(highPos, startPos, t / 0.2f);
            yield return null;
        }
        transform.position = startPos;

        // Ground Hit - Shake & Damage
        if (cam != null) cam.Shake(3f, 1.5f);
        HitEffect.Spawn(transform.position + Vector3.up * 2f, Color.red, 5f);
        HitEffect.Spawn(transform.position + Vector3.up * 2f, new Color(1f, 0.5f, 0f), 5f);
        
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

        // Return to normal
        yield return new WaitForSeconds(1f);
        transform.localScale = Vector3.one;
        if (cam != null) cam.ResetView();

        Enemy.GlobalFreeze = false;
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
        bool wantsSprint = false;

        if (!isKnockedBack && Keyboard.current != null)
        {
            float horizontal = 0f;
            float vertical = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;

            wantsSprint = vertical > 0f
                && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            float moveMultiplier = wantsSprint ? sprintMultiplier : 1f;

            CameraController cameraController = mainCamera != null
                ? mainCamera.GetComponent<CameraController>()
                : null;
            bool mountedThirdPerson = UsesMountedGameplayControls;
            Vector3 moveDir;

            if (mountedThirdPerson)
            {
                bool alignToCamera = Mouse.current != null && Mouse.current.rightButton.isPressed;
                if (alignToCamera && cameraController != null)
                {
                    RotateTowards(cameraController.GetPlanarForward());
                }
                else if (Mathf.Abs(horizontal) > 0.01f)
                {
                    transform.Rotate(Vector3.up, horizontal * mountedTurnSpeed * Time.deltaTime, Space.World);
                }

                // Mounted controls: W/S move along the horse's own heading; A/D steer it.
                moveDir = transform.forward * vertical;
                moveDir.y = 0f;
                moveDir.Normalize();
            }
            else
            {
                Vector3 camForward = mainCamera.transform.forward;
                Vector3 camRight = mainCamera.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                moveDir = (camForward * vertical + camRight * horizontal).normalized;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    RotateTowards(moveDir);
                }
            }

            moveVelocity =
                moveDir
                * moveSpeed
                * moveMultiplier;
        }
        if (isKnockedBack)
        {
            if (horseAnimator != null) 
            {
                horseAnimator.SetBool("isWalking", false);
                horseAnimator.SetBool("isRunning", false);
            }
            if (riderAnimator != null) 
            {
                riderAnimator.SetBool("isWalking", false);
                riderAnimator.SetBool("isRunning", false);
            }
            if (horseAnimator == null && riderAnimator == null)
            {
                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null) 
                {
                    anim.SetBool("isWalking", false);
                    anim.SetBool("isRunning", false);
                }
            }
        }
        else
        {
            bool hasHorizontalMovement = new Vector3(moveVelocity.x, 0f, moveVelocity.z).sqrMagnitude > 0.01f;
            bool isRunning = hasHorizontalMovement && wantsSprint;
            bool isWalking = hasHorizontalMovement && !wantsSprint;

            if (horseAnimator != null) 
            {
                horseAnimator.SetBool("isWalking", isWalking);
                horseAnimator.SetBool("isRunning", isRunning);
            }
            if (riderAnimator != null) 
            {
                riderAnimator.SetBool("isWalking", isWalking);
                riderAnimator.SetBool("isRunning", isRunning);
            }
            if (horseAnimator == null && riderAnimator == null)
            {
                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null) 
                {
                    // For characters with only one animation (like the baby),
                    // any movement should trigger the walking/running state.
                    anim.SetBool("isWalking", hasHorizontalMovement);
                    
                    // Only try to set isRunning if the parameter actually exists to avoid warnings,
                    // but for now we just rely on hasHorizontalMovement for the single boolean.
                }
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

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);

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

    public AttackDirection GetMountedAttackDirection()
    {
        return mountedAttackDirection;
    }

    public bool UsesMountedGameplayControls => IsGameplayScene();

    public float MountedAttackArcPosition => attackArcPosition;

    public string GetMountedAttackDirectionLabel()
    {
        return GetMountedAttackDirection() switch
        {
            AttackDirection.West => "TRÁI",
            AttackDirection.East => "PHẢI",
            _ => "GIỮA"
        };
    }

    private void UpdateAttackArcInput()
    {
        if (!UsesMountedGameplayControls || Mouse.current == null)
        {
            return;
        }

        float horizontalMouseDelta = Mouse.current.delta.ReadValue().x;
        if (Mathf.Abs(horizontalMouseDelta) > 0.001f)
        {
            attackArcTarget = Mathf.Clamp(
                attackArcTarget + horizontalMouseDelta * attackArcMouseSensitivity,
                -1f,
                1f);
        }

        attackArcPosition = Mathf.MoveTowards(
            attackArcPosition,
            attackArcTarget,
            attackArcMoveSpeed * Time.unscaledDeltaTime);
        UpdateMountedAttackDirection();
    }

    private void UpdateMountedAttackDirection()
    {
        // Hysteresis stops the attack lane from flickering at a boundary.
        switch (mountedAttackDirection)
        {
            case AttackDirection.West:
                if (attackArcPosition > -0.2f)
                {
                    mountedAttackDirection = AttackDirection.North;
                }
                break;
            case AttackDirection.East:
                if (attackArcPosition < 0.2f)
                {
                    mountedAttackDirection = AttackDirection.North;
                }
                break;
            default:
                if (attackArcPosition <= -0.45f)
                {
                    mountedAttackDirection = AttackDirection.West;
                }
                else if (attackArcPosition >= 0.45f)
                {
                    mountedAttackDirection = AttackDirection.East;
                }
                break;
        }
    }
    public bool canAttack = true;

    private void HandleAttack()
    {
        if (!canAttack) return;

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
            Vector3 targetPoint;
            AttackDirection attackDir;
            if (UsesMountedGameplayControls)
            {
                attackDir = GetMountedAttackDirection();
                float attackAngle = attackDir switch
                {
                    AttackDirection.West => -45f,
                    AttackDirection.East => 45f,
                    _ => 0f
                };
                currentAttackDirVector = Quaternion.Euler(0f, attackAngle, 0f) * transform.forward;
                targetPoint = transform.position + currentAttackDirVector * 6f;
            }
            else
            {
                TryGetMouseGroundPoint(out targetPoint);
                attackDir = GetAttackDirection();
                currentAttackDirVector = (targetPoint - transform.position).normalized;
                currentAttackDirVector.y = 0;
                if (currentAttackDirVector.sqrMagnitude == 0) currentAttackDirVector = transform.forward;
            }

            if (riderAnimator != null)
            {
                if (horseAnimator != null) 
                {
                    riderAnimator.SetInteger("AttackDirection", (int)attackDir);
                }
                riderAnimator.SetTrigger("Attack");
            }
            
            if (slashSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(slashSound);
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TutorialScene")
            {
                StartCoroutine(SimulateAnimationEvents());
            }

            // Chờ animation kết thúc
            yield return new WaitForSeconds(attackInterval);
        }
        finally
        {
            isAttacking = false;
        }
    }

    private IEnumerator SimulateAnimationEvents()
    {
        yield return new WaitForSeconds(attackWindUp);
        EnableWeaponDamage();
        yield return new WaitForSeconds(0.25f);
        DisableWeaponDamage();
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    private bool IsGameplayScene()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return scene == "map 1" || scene == "map2" || scene == "map3" || scene == "SampleScene" || scene == "TutorialScene";
    }

    public bool IsTutorialScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TutorialScene";

    public void EnableWeaponDamage()
    {
        if (weaponDamage != null) weaponDamage.EndAttack();
        if (weaponTrail != null) weaponTrail.EndTrail();

        isArcAttacking = true;
        hitEnemiesThisAttack.Clear();

        StartCoroutine(SpawnCrescentSlashVisual(currentAttackDirVector));
    }

    public void DisableWeaponDamage()
    {
        isArcAttacking = false;
    }

    private void HandleArcAttackDamage()
    {
        // Scale the attack radius based on the weapon tier! This gives higher tier weapons actual range.
        float radius = IsTutorialScene ? 2.2f : (4.5f * weaponScaleMultiplier);
        
        // Use NonAlloc to prevent massive GC allocations every frame!
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, _attackHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _attackHits[i];
            if (hit.CompareTag("Enemy"))
            {
                Enemy e = hit.GetComponent<Enemy>();
                if (e != null && !hitEnemiesThisAttack.Contains(e))
                {
                    Vector3 dirToEnemy = (e.transform.position - transform.position);
                    dirToEnemy.y = 0;
                    if (dirToEnemy.sqrMagnitude > 0) dirToEnemy.Normalize();
                    
                    if (Vector3.Angle(currentAttackDirVector, dirToEnemy) <= 90f)
                    {
                        hitEnemiesThisAttack.Add(e);
                        e.TakeDamage(slashDamageMultiplier * 20f);
                        e.ApplyKnockbackStun(currentAttackDirVector, 8f, 0.2f);
                        HitEffect.Spawn(e.transform.position + Vector3.up, Color.red, 0.5f);
                    }
                }
            }
        }
    }

    private IEnumerator SpawnCrescentSlashVisual(Vector3 direction)
    {
        float scale = IsTutorialScene ? 0.5f : 1f;
        GameObject slashPivot = new GameObject("CrescentSlashPivot");
        slashPivot.transform.position = transform.position + Vector3.up * (1.5f * scale);
        
        GameObject slashTip = new GameObject("SlashTip");
        slashTip.transform.SetParent(slashPivot.transform);
        slashTip.transform.localPosition = new Vector3(0, 0, 4f * scale); 
        
        TrailRenderer tr = slashTip.AddComponent<TrailRenderer>();
        tr.time = 0.25f;
        tr.startWidth = 0.8f * scale;
        tr.endWidth = 0.1f * scale;
        tr.material = new Material(Shader.Find("Sprites/Default"));
        tr.startColor = new Color(1f, 0.9f, 0.4f, 0.9f);
        tr.endColor = new Color(1f, 0.5f, 0f, 0f);
        
        float duration = 0.2f; 
        float elapsed = 0f;
        
        Quaternion baseRot = Quaternion.LookRotation(direction);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.Lerp(-90f, 90f, t);
            
            slashPivot.transform.rotation = baseRot * Quaternion.Euler(0, angle, 0);
            slashPivot.transform.position = transform.position + Vector3.up * 1.5f;
            
            yield return null;
        }
        
        Destroy(slashPivot, tr.time + 0.1f);
    }

    private void SpawnBasicSlash(Vector3 targetPoint)
    {
        Vector3 dir = (targetPoint - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f) dir.Normalize();
        else dir = transform.forward;
        
        // Spawn a transparent hitbox instead of a white cube
        GameObject slash = new GameObject("BasicSlash");
        
        // Scale the hitbox and forward reach based on weapon tier scale
        float depth = 3.5f * weaponScaleMultiplier; // Longer base depth to match spear
        float forwardOffset = depth / 2f; // Offset by half depth so it starts exactly at the player's center
        
        // Always place the hitbox relative to the player's body to avoid inner blind spots!
        slash.transform.position = transform.position + Vector3.up * 1.2f + dir * forwardOffset;
        slash.transform.rotation = Quaternion.LookRotation(dir);
        
        // Visual effect can still use attackSpawnPoint if available
        Vector3 fxPos = attackSpawnPoint != null ? attackSpawnPoint.position : slash.transform.position;
        HitEffect.Spawn(fxPos, new Color(1f, 0.8f, 0.2f), 0.5f * weaponScaleMultiplier);
        
        BoxCollider col = slash.AddComponent<BoxCollider>();
        // Base size is 3 wide, 4.0 high (to hit everything from the floor up), and depth based on weapon
        col.size = new Vector3(3f * weaponScaleMultiplier, 4.0f, depth);
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
        // 1. CLEANUP ALL ENEMIES, ABILITIES, BGM
        IsPerformingSkill = true; // disable control
        canAttack = false;
        isPhase2BuffActive = false; // turn off damage aura so it's peaceful
        
        // Kill all remaining enemies
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (var e in allEnemies)
        {
            if (e != null && e.gameObject != null && e.currentHealth > 0)
            {
                e.TakeDamage(99999f);
            }
        }

        // Stop BGM
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (var src in audioSources)
        {
            if (src.isPlaying && src.loop)
            {
                src.Stop();
            }
        }
        
        if (riderAnimator != null)
        {
            riderAnimator.enabled = false; // Disable all attacking animations
        }

        if (horseAnimator != null)
        {
            horseAnimator.SetBool("isWalking", true);
        }
        
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Cinematic Camera Setup
        CameraController cam = Camera.main.GetComponent<CameraController>();
        if (cam != null)
        {
            cam.SetCinematicView(new Vector3(0, 2f, -15f), 10f); // Cinematic angle looking up
        }

        // Setup Credits Canvas
        GameObject canvasObj = new GameObject("CreditsCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        // Background - Fades to black
        GameObject bgObj = new GameObject("CreditsBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0, 0, 0, 0f); // Start fully transparent
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Credits Text
        GameObject textObj = new GameObject("CreditsText");
        textObj.transform.SetParent(canvasObj.transform, false);
        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "<size=64><b>THÁNH GIÓNG</b></size>\n\n\n\n<size=48><b>Special Thanks To</b></size>\n\n<size=36><b>Mentor:</b>\nLại Đức Hùng</size>\n\n\n\n<size=36><b>Team Members:</b>\nSE192321 Nguyễn Thành Kiên\nSE192024 Dương Quốc Thái\nSE192047 Dương Hồng Quang\nSE180374 Bùi Quang Hiếu</size>";
        text.fontSize = 36;
        text.color = new Color(1f, 0.9f, 0.6f, 1f); // Golden yellow
        text.alignment = TMPro.TextAlignmentOptions.Center;
        
        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 1f); // Pivot at top, anchor at bottom
        rt.sizeDelta = new Vector2(1200, 1500);
        rt.anchoredPosition = new Vector2(0, -100); // Start completely below the screen
        
        // Add shadow for better text readability
        var shadow = textObj.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(3f, -3f);

        // Lyrics Text
        GameObject lyricsObj = new GameObject("LyricsText");
        lyricsObj.transform.SetParent(canvasObj.transform, false);
        var lyricsText = lyricsObj.AddComponent<TMPro.TextMeshProUGUI>();
        lyricsText.text = "";
        lyricsText.fontSize = 42;
        lyricsText.color = Color.white;
        lyricsText.alignment = TMPro.TextAlignmentOptions.Center;

        RectTransform lyricsRT = lyricsText.GetComponent<RectTransform>();
        lyricsRT.anchorMin = new Vector2(0, 0);
        lyricsRT.anchorMax = new Vector2(1, 0);
        lyricsRT.pivot = new Vector2(0.5f, 0);
        lyricsRT.sizeDelta = new Vector2(0, 100);
        lyricsRT.anchoredPosition = new Vector2(0, 80);

        var lyricsShadow = lyricsObj.AddComponent<UnityEngine.UI.Shadow>();
        lyricsShadow.effectColor = Color.black;
        lyricsShadow.effectDistance = new Vector2(2f, -2f);

        float t = 0;
        float effectTimer = 0;
        float totalDuration = 30f;
        float scrollSpeed = 2500f / totalDuration; // Scroll 2500 units over 30s

        while (t < totalDuration)
        {
            t += Time.deltaTime;
            effectTimer += Time.deltaTime;
            
            // Fade background to black over 30s
            bg.color = new Color(0, 0, 0, t / totalDuration);
            
            // Lyrics Logic
            if (t >= 27f) lyricsText.text = "Thiên vương.... về trời";
            else if (t >= 25f) lyricsText.text = "Thiên vương....";
            else if (t >= 17f) lyricsText.text = "Rạng danh non nước! Rạng danh non nước!";
            else if (t >= 14f) lyricsText.text = "Hào khí Thiên Vương, Hào khí Thiên Vương";
            else if (t >= 11f) lyricsText.text = "Sáng soi nòi giống! Sáng soi nòi giống!";
            else if (t >= 7f) lyricsText.text = "Hào khí vĩnh hằng, Hào khí vĩnh hằng";
            else if (t >= 4f) lyricsText.text = "Sử xanh còn ghi, chiến công lẫy lừng!";
            else if (t >= 1f) lyricsText.text = "Hào khí cha ông, lưu truyền muôn kiếp!";
            
            // Spawn golden glow particles very frequently
            if (effectTimer > 0.05f)
            {
                effectTimer = 0f;
                Vector3 randomOffset = new Vector3(
                    UnityEngine.Random.Range(-3f, 3f),
                    UnityEngine.Random.Range(-1f, 4f),
                    UnityEngine.Random.Range(-3f, 3f)
                );
                HitEffect.Spawn(transform.position + randomOffset, new Color(1f, 0.85f, 0.2f, 0.8f), 0.8f);
            }
            
            // Slow majestic flight upward and forward
            transform.position += (transform.forward * 4f + Vector3.up * 2f) * Time.deltaTime;

            // Optional: Rotate the camera slowly around the player
            if (cam != null)
            {
                cam.transform.RotateAround(transform.position, Vector3.up, 25f * Time.deltaTime);
            }
            
            // Scroll credits text upwards
            rt.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        if (cam != null) cam.ResetView();
        
        // Wait in the black screen for a few seconds before returning to Main Menu
        yield return new WaitForSeconds(6f);
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

public class AttackArcIndicator : MonoBehaviour
{
    private const int ArcSegments = 25;
    private const float ArcHalfAngle = 55f;

    private float ArcRadius => playerController != null && playerController.IsTutorialScene ? 1.7f : 3.4f;

    private PlayerController playerController;
    private LineRenderer arcLine;
    private LineRenderer markerLine;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        arcLine = CreateLine("AttackArc", 0.07f, new Color(1f, 0.68f, 0.12f, 0.42f));
        markerLine = CreateLine("AttackArcMarker", 0.14f, new Color(1f, 0.92f, 0.28f, 0.95f));
        arcLine.positionCount = ArcSegments;
        markerLine.positionCount = 2;
    }

    private void Update()
    {
        bool visible = playerController != null && playerController.UsesMountedGameplayControls && playerController.canAttack;

        if (arcLine != null) arcLine.enabled = visible;
        if (markerLine != null) markerLine.enabled = visible;
        if (!visible)
        {
            return;
        }

        Vector3 origin = transform.position + Vector3.up * 0.08f;
        float currentRadius = ArcRadius;
        for (int i = 0; i < ArcSegments; i++)
        {
            float t = i / (float)(ArcSegments - 1);
            float angle = Mathf.Lerp(-ArcHalfAngle, ArcHalfAngle, t);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            arcLine.SetPosition(i, origin + direction * currentRadius);
        }

        float markerAngle = playerController.MountedAttackArcPosition * ArcHalfAngle;
        Vector3 markerDirection = Quaternion.Euler(0f, markerAngle, 0f) * transform.forward;
        markerLine.SetPosition(0, origin + markerDirection * 0.8f);
        markerLine.SetPosition(1, origin + markerDirection * currentRadius);
    }

    private LineRenderer CreateLine(string objectName, float width, Color color)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = color;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 3;
        return line;
    }
}
