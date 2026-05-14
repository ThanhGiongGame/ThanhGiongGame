using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    private float currentHealth;

    private CharacterController controller;

    private Vector3 knockbackVelocity;

    public bool IsKnockedBack => knockbackVelocity.magnitude > 0.1f;

    public float knockbackDamping = 5f;

    // ---- Properties exposed for UI ----
    public float CurrentHealth        => currentHealth;
    public float MaxHealth            => maxHealth;
    public float CurrentHealthPercent => Mathf.Clamp01(currentHealth / maxHealth);

    // ---- Hit effect color for player (red) ----
    private static readonly Color HitColor = new Color(1f, 0.15f, 0.15f);

    private void Start()
    {
        currentHealth = maxHealth;
        controller    = GetComponent<CharacterController>();

        // Auto-create the health bar UI if it doesn't exist yet
        if (FindObjectOfType<PlayerHealthUI>() == null)
        {
            GameObject uiGO = new GameObject("PlayerHealthUI");
            PlayerHealthUI ui = uiGO.AddComponent<PlayerHealthUI>();
            ui.playerHealth = this;
        }
    }

    private void Update()
    {
        if (knockbackVelocity.magnitude > 0.1f)
        {
            controller.Move(knockbackVelocity * Time.deltaTime);

            knockbackVelocity = Vector3.Lerp(
                knockbackVelocity,
                Vector3.zero,
                knockbackDamping * Time.deltaTime
            );
        }
    }

    public void TakeDamage(float damage, Vector3 knockbackDirection, float knockbackForce)
    {
        currentHealth -= damage;
        currentHealth  = Mathf.Max(currentHealth, 0f);

        Debug.Log("Player took damage: " + damage);

        // Spawn red hit burst at the player's chest height
        HitEffect.Spawn(transform.position + Vector3.up * 1f, HitColor, 1.4f);

        knockbackDirection.y = 0f;
        knockbackVelocity    = knockbackDirection.normalized * knockbackForce;

        if (currentHealth <= 0f)
        {
            Debug.LogError("Player died");
        }
    }
}