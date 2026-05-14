using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    private CharacterController controller;
    private PlayerHealth playerHealth;

    [Header("Attack")]
    public GameObject slashPrefab;
    public Transform attackSpawnPoint;
    public float attackInterval = 1.5f;
    private float attackTimer;
    private Transform enemyTransform;

    private Camera mainCamera;

    void Start()
    {
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
        if(enemy != null)
        {
            enemyTransform = enemy.transform;
        }
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        mainCamera = Camera.main;
        attackTimer = attackInterval;
        Debug.Log($"Button {Keyboard.current.anyKey.wasPressedThisFrame} pressed!");

    }

    void Update()
    {
        HandleRotation();
        HandleAttack();
        bool isKnockedBack = playerHealth != null && playerHealth.IsKnockedBack;
        if (Keyboard.current != null && !isKnockedBack)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;
        Debug.Log($"Button {Keyboard.current.anyKey.wasPressedThisFrame} pressed!");

        if (Keyboard.current != null)
        {

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                vertical += 1f;
            }
        }

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);
    }

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
            lookDirection.y = 0f; // Keep rotation horizontal

            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

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
        if (slashPrefab != null)
        {
            // Get the direction towards the mouse
            if (Mouse.current == null) return;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float rayDistance;
            
            if (groundPlane.Raycast(ray, out rayDistance))
            {
                Vector3 targetPoint = ray.GetPoint(rayDistance);
                Vector3 lookDirection = targetPoint - transform.position;
                lookDirection.y = 0f;
                
                GameObject slash = Instantiate(slashPrefab, transform.position, Quaternion.identity);
                SlashProjectile slashScript = slash.GetComponent<SlashProjectile>();
                if (slashScript != null)
                {
                    slashScript.Initialize(transform, lookDirection);
                }
            }
        }
        else
        {
            Debug.LogWarning("Slash Prefab is not assigned in PlayerController!");
        }
    }
}
