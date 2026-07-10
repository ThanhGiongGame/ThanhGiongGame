using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private PlayerController _pc;
    public float currentHealth;

    // Knockback is now OWNED by PlayerController via the public getter.
    // PlayerHealth only tracks the vector and decays it — it never calls controller.Move() itself.
    private Vector3 _knockbackVelocity;

    public float knockbackDamping = 5f;

    // ---- Properties exposed for PlayerController & UI ----
    public float   CurrentHealth        => currentHealth;
    public float   MaxHealth            => maxHealth;
    public float   CurrentHealthPercent => Mathf.Clamp01(currentHealth / maxHealth);
    public bool    IsKnockedBack        => _knockbackVelocity.sqrMagnitude > 0.01f; // 0.1^2
    public Vector3 KnockbackVelocity    => _knockbackVelocity;

    // ---- Hit effect color ----
    private static readonly Color HitColor = new Color(1f, 0.15f, 0.15f);

    private void Start()
    {
        currentHealth = maxHealth;
        _pc = GetComponent<PlayerController>();
        // Auto-create UI if not already in scene
        if (FindObjectOfType<PlayerHealthUI>() == null)
        {
            GameObject uiGO = new GameObject("PlayerHealthUI");
            PlayerHealthUI ui = uiGO.AddComponent<PlayerHealthUI>();
            ui.playerHealth = this;
        }
    }

    private void Update()
    {
        // Decay knockback over time — no controller.Move() here.
        // PlayerController reads KnockbackVelocity and applies it in its own Move() call.
        if (_knockbackVelocity.sqrMagnitude > 0.0001f)
        {
            _knockbackVelocity = Vector3.Lerp(
                _knockbackVelocity,
                Vector3.zero,
                knockbackDamping * Time.deltaTime
            );

            // Snap to zero when small enough to stop jitter
            if (_knockbackVelocity.sqrMagnitude < 0.01f)
                _knockbackVelocity = Vector3.zero;
        }
    }
    public void AddMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount; // Heal them for the amount gained so their bar fills up
        Debug.Log($"Max HP upgraded! New Max HP: {maxHealth}");
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
    public void TakeDamage(float damage, Vector3 knockbackDirection, float knockbackForce)
    {
        if (_pc != null && _pc.IsInvulnerable)
        {
            Debug.Log("Player đang bất tử, né hoàn toàn sát thương!");
            return; // Thoát hàm luôn, không trừ máu nữa
        }
        else
        {
            currentHealth = Mathf.Max(currentHealth - damage, 0f);

            Debug.Log("Player took damage: " + damage);

            // Overwrite knockback — enemy hit always re-applies full force
            knockbackDirection.y = 0f;
            _knockbackVelocity = knockbackDirection.normalized * knockbackForce;

            // Spawn red hit burst at chest height
            HitEffect.Spawn(transform.position + Vector3.up * 1f, HitColor, 1.4f);
        }
        if (currentHealth <= 0f)
        {
            var leLoi = GetComponent<LegendSystemLeLoi>();
            if (leLoi != null && leLoi.CanRevive())
            {
                leLoi.OnPlayerDeath();
                return; // Survived!
            }
            
            GameOverManager.Instance.OnPlayerDeath();
        }
    }
}