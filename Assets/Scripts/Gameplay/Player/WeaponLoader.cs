using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class WeaponLoader : MonoBehaviour
{
    [SerializeField]
    public GameObject weaponTier1;
    public GameObject weaponTier2;
    public GameObject weaponTier3;
    public GameObject weaponTier4;

    public float bonusHealth;
    public float bonusDamage;
    public float bonusSpeed;

    private GameObject currentWeapon;
    private void Start()
    {
        LoadWeapon();
    }
    public void LoadWeapon()
    {
        string equipped = PlayerPrefs.GetString("EquippedWeapon", "Weapon_Tier1");

        GameObject prefab = weaponTier1;
        float tierScaleMultiplier = 1.0f;

        switch (equipped)
        {
            case "Weapon_Tier1":
                prefab = weaponTier1;
                bonusDamage += 0;
                tierScaleMultiplier = 1.0f;
                break;
            case "Weapon_Tier2":
                prefab = weaponTier2;
                bonusDamage += 10;
                tierScaleMultiplier = 1.15f;
                break;
            case "Weapon_Tier3":
                prefab = weaponTier3;
                bonusDamage += 30;
                tierScaleMultiplier = 1.35f;
                break;
            case "Weapon_Tier4":
                prefab = weaponTier4;
                bonusDamage += 50;
                tierScaleMultiplier = 1.6f;
                break;
        }

        if (currentWeapon != null) Destroy(currentWeapon);
        currentWeapon = Instantiate(prefab, transform);
        
        // Increase the size of the weapon and its attack range collider based on tier
        currentWeapon.transform.localScale *= tierScaleMultiplier;
        var weaponDmg = currentWeapon.GetComponentInChildren<WeaponDamage>();
        var weaponTrl = currentWeapon.GetComponentInChildren<WeaponTrail>();
        PlayerController player = GetComponentInParent<PlayerController>();
        player.weaponScaleMultiplier = tierScaleMultiplier;
        player.weaponDamage = weaponDmg;
        player.weaponTrail = weaponTrl;
    }
}
