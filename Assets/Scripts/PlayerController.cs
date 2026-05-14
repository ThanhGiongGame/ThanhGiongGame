using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Attack")]
    public GameObject slashPrefab;
    public Transform attackSpawnPoint;
    public float attackInterval = 1.5f;

    // ---- Private references ----
    private CharacterController controller;
    private PlayerHealth         playerHealth;
    private Camera               mainCamera;

    // ---- Attack timer ----
    private float attackTimer;

    // Top-down game: use a small constant downward push to stay grounded
    private const float GroundStick = -2f;

    // ---- Unused legacy field (kept to avoid prefab warnings) ----
    private Transform enemyTransform;

    // -------------------------------------------------------

    void Start()
    {
        controller   = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        mainCamera   = Camera.main;
        attackTimer  = attackInterval;
    }

    void Update()
    {
        HandleRotation();
        HandleAttack();
        HandleMovement();
    }

    // -------------------------------------------------------
    // Movement — SINGLE controller.Move() call per frame
    // -------------------------------------------------------

    private void HandleMovement()
    {
        Vector3 moveVelocity = Vector3.zero;

        bool isKnockedBack = playerHealth != null && playerHealth.IsKnockedBack;

        if (!isKnockedBack && Keyboard.current != null)
        {
            float horizontal = 0f;
            float vertical   = 0f;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  vertical   -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    vertical   += 1f;

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
            Attack();
            attackTimer = attackInterval;
        }
    }

    private void Attack()
    {
        if (slashPrefab == null)
        {
            Debug.LogWarning("Slash Prefab is not assigned in PlayerController!");
            return;
        }

        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;

        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 targetPoint   = ray.GetPoint(rayDistance);
            Vector3 lookDirection = targetPoint - transform.position;
            lookDirection.y = 0f;

            GameObject slash = Instantiate(slashPrefab, transform.position, Quaternion.identity);
            SlashProjectile slashScript = slash.GetComponent<SlashProjectile>();
            if (slashScript != null)
                slashScript.Initialize(transform, lookDirection);
        }
    }
}
