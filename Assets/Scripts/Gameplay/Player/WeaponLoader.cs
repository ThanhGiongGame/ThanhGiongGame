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
        string equipped =
    PlayerPrefs.GetString(
        "EquippedWeapon",
        "Weapon_Tier1"
    );

        GameObject prefab = weaponTier1;

        switch (equipped)
        {
            case "Weapon_Tier1":
                prefab = weaponTier1;
                bonusDamage += 0;
                break;
            case "Weapon_Tier2":
                prefab = weaponTier2;
                bonusDamage += 10;
                break;
            case "Weapon_Tier3":
                prefab = weaponTier3;
                bonusDamage += 30;
                break;
            case "Weapon_Tier4":
                prefab = weaponTier4;
                bonusDamage += 50;

                break;
        }


        var weapon = Instantiate(
            prefab,
            transform
        );
        var weaponDmg = weapon.GetComponentInChildren<WeaponDamage>();
        var weaponTrl = weapon.GetComponentInChildren<WeaponTrail>();
        PlayerController player =
    GetComponentInParent<PlayerController>();
        player.weaponDamage = weaponDmg;
        player.weaponTrail = weaponTrl;
    }
}
