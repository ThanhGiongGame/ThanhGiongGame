using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [HideInInspector]
    public float slashDamageMultiplier = 1.0f; // Defaults to 100% damage base
    [Header("Attack")]
    public GameObject slashPrefab;
    public Transform attackSpawnPoint;
    public float attackInterval = 1.2f;   // 0.2s wind-up + 1.0s swing
    public float attackWindUp = 0.2f;   // delay before slash spawns

    // ---- Private references ----
    private CharacterController controller;
    private PlayerHealth playerHealth;
    private Camera mainCamera;
    private UltimateController ultimateController;
    private Animator horseAnimator;
    public PlayerEquipmentLoader equipmentLoader;
    private float walkTimer = 0f;

    // ---- Attack timer ----
    private float attackTimer;

    // Top-down game: use a small constant downward push to stay grounded
    private const float GroundStick = -2f;

    // ---- Unused legacy field (kept to avoid prefab warnings) ----
    private Transform enemyTransform;

    // ================================================================
    // Properties used by Skills (Fixes compilation errors)
    // ================================================================
    /// <summary>
    /// Tracks if the player is currently executing a manual movement skill (like Flame Dash)
    /// to block regular WASD movement inputs and automated primary attacks.
    /// </summary>
    public bool IsPerformingSkill { get; set; }

    /// <summary>
    /// Provides access to the player's vulnerability state.
    /// </summary>
    public bool IsInvulnerable
    {
        get => _isInvulnerable;
        set => _isInvulnerable = value;
    }
    private bool _isInvulnerable;
    // -------------------------------------------------------

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        ultimateController = GetComponent<UltimateController>();

        // Always find the camera first so combat functions even if visuals are missing
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("PlayerController: No Camera tagged 'MainCamera' found in the scene!");
        }

        // --- Safe Initialization for the Horse Animator ---
        if ( equipmentLoader == null)
        {
            equipmentLoader = GetComponent<PlayerEquipmentLoader>();
            if (equipmentLoader == null)
            {
                Debug.LogWarning("PlayerController: No PlayerEquipmentLoader found on the player! Horse animations will not play.");
            }
            else if (equipmentLoader.horseRoot == null)
            {
                Debug.LogWarning("PlayerController: PlayerEquipmentLoader found but horseRoot is not assigned! Horse animations will not play.");
            }

        }
        attackTimer = attackInterval;
        float baseDamage = 20f;
        float baseMaxHealth = 100f;
        float baseSpeed = 8f;

        // --- Đọc dữ liệu Loadout từ Shop Menu ---
        if (PlayerPrefs.GetInt("Item_DamageBuff", 0) == 2)
        {
            baseDamage += 15f;
            Debug.Log("Đã kích hoạt: +15 Sát thương từ trang bị Hotbar!");
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
        // Don't rotate or attack automatically if aiming or performing a skill
        if (!IsPerformingSkill)
        {
            HandleRotation();
            HandleAttack();
        }
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryUseUltimate();
        }
        HandleMovement();
    }

    // -------------------------------------------------------
    // Movement — SINGLE controller.Move() call per frame
    // -------------------------------------------------------
    private void TryUseUltimate()
    {
        string equippedUltimate =
            PlayerPrefs.GetString(
                "EquippedUltimate",
                ""
            );
        Debug.Log("Using Ultimate: " + equippedUltimate);
        switch (equippedUltimate)
        {
            case "Ultimate_Tier1":

                if (ultimateController != null)
                {
                    ultimateController.TryUseUltimate();
                }

                break;
        }
    }
    private void HandleMovement()
    {
        horseAnimator = equipmentLoader.horseRoot.GetComponentInChildren<Animator>();
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
                moveMutiplier = 1.5f;
            }
            else moveMutiplier = 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;

            moveVelocity = new Vector3(horizontal, 0f, vertical).normalized * moveSpeed * moveMutiplier;
        }
        if (horseAnimator != null)
        {
            Debug.Log("Updating horse animation. Knockback: " + isKnockedBack + ", Move Velocity: " + moveVelocity + ", Object" + horseAnimator);
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
                    Debug.Log("Player is moving. Walk timer: " + walkTimer);
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

    // -------------------------------------------------------
    // Rotation — face mouse cursor
    // -------------------------------------------------------

    private void HandleRotation()
    {
        if (Mouse.current == null) return;
        if (mainCamera == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;

        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            Vector3 lookDirection = point - transform.position;
            lookDirection.y = 0f;

            if (lookDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    // -------------------------------------------------------
    // Attack
    // -------------------------------------------------------

    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            StartCoroutine(AttackCoroutine());
            attackTimer = attackInterval;   // reset immediately so interval is consistent
        }
    }

    // Wind-up (0.2 s) → spawn slash → swing for 1 s
    private IEnumerator AttackCoroutine()
    {
        if (slashPrefab == null)
        {
            Debug.LogWarning("Slash Prefab is not assigned in PlayerController!");
            yield break;
        }

        if (Mouse.current == null) yield break;

        // --- Capture direction at the moment the attack is triggered ---
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;

        if (!groundPlane.Raycast(ray, out rayDistance)) yield break;

        Vector3 targetPoint = ray.GetPoint(rayDistance);
        Vector3 lookDirection = targetPoint - transform.position;
        lookDirection.y = 0f;

        // --- Wind-up pause ---
        yield return new WaitForSeconds(attackWindUp);

        // --- Spawn & launch the slash ---
        GameObject slash = Instantiate(slashPrefab, transform.position, Quaternion.identity);
        SlashProjectile slashScript = slash.GetComponent<SlashProjectile>();

        if (slashScript != null)
        {
            slashScript.Initialize(transform, lookDirection);

            // Multiply the projectile's native damage by the player's stat multiplier
            // (Change 'damage' to whatever variable name your projectile script uses)
            slashScript.damage *= slashDamageMultiplier;
        }
    }
}