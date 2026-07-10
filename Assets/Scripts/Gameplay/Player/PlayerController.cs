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
            }
            else
            {
                bool hasHorizontalMovement = new Vector3(moveVelocity.x, 0f, moveVelocity.z).sqrMagnitude > 0.01f;
                if (hasHorizontalMovement) {
                    walkTimer += Time.deltaTime;

                }
                else
                {
                    walkTimer = 0f;
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
        point = Vector3.zero;

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
            if (!TryGetMouseGroundPoint(out Vector3 targetPoint))
                yield break;

            AttackDirection attackDir = GetAttackDirection();

            if (riderAnimator != null)
            {
                riderAnimator.SetInteger("AttackDirection", (int)attackDir);
                riderAnimator.SetTrigger("Attack");
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
}
