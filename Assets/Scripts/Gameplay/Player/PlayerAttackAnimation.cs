using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    public PlayerController player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        Debug.Log($"Relay on {gameObject.name}");
        Debug.Log($"Found PlayerController: {player}");
    }

    public void EnableWeaponDamage()
    {
        player.EnableWeaponDamage();
    }

    public void DisableWeaponDamage()
    {
        player.DisableWeaponDamage();
    }
}