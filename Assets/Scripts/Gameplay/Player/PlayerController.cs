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
        mainCamera = Camera.main;
        attackTimer = attackInterval;
        float baseDamage = 20f;
        float baseMaxHealth = 100f;
        float baseSpeed = 8f;

        // --- Đọc dữ liệu Loadout từ Shop Menu ---
        if (PlayerPrefs.GetInt("Item_DamageBuff", 0) == 2)
        {
            baseDamage += 15f; // Cộng thêm 15 sát thương nếu lắp Kiếm
            Debug.Log("Đã kích hoạt: +15 Sát thương từ trang bị Hotbar!");
        }

        if (PlayerPrefs.GetInt("Item_HealthBuff", 0) == 2)
        {
            baseMaxHealth += 50f; // Cộng thêm 50 Máu tối đa nếu lắp Giáp
        }

        if (PlayerPrefs.GetInt("Item_SpeedBuff", 0) == 2)
        {
            baseSpeed += 3f; // Cộng tốc độ chạy nếu đi giày
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

        HandleMovement();
    }

    // -------------------------------------------------------
    // Movement — SINGLE controller.Move() call per frame
    // -------------------------------------------------------

    private void HandleMovement()
    {
        // If a skill like Flame Dash is currently driving CharacterController.Move(),
        // we completely bypass the standard movement calculation loop.
        if (IsPerformingSkill) return;

        Vector3 moveVelocity = Vector3.zero;

        bool isKnockedBack = playerHealth != null && playerHealth.IsKnockedBack;

        if (!isKnockedBack && Keyboard.current != null)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;

            moveVelocity = new Vector3(horizontal, 0f, vertical).normalized * moveSpeed;
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