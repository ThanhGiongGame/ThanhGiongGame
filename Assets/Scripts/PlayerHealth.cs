using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    private float currentHealth;

    private CharacterController controller;

    private Vector3 knockbackVelocity;

    public float knockbackDamping = 5f;

    private void Start()
    {
        currentHealth = maxHealth;

        controller = GetComponent<CharacterController>();
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

        Debug.Log("Player took damage: " + damage);
        knockbackDirection.y = 0f;
        knockbackVelocity =
            knockbackDirection.normalized * knockbackForce;

        if (currentHealth <= 0f)
        {
            Debug.Log("Player died");
        }
    }
}