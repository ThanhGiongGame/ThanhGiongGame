using System;
using UnityEngine;

public class PlayerEquipmentLoader : MonoBehaviour
{
    public Transform characterRoot;
    public Transform horseRoot;
    public Transform weaponRoot;

    public GameObject characterTier1;
    public GameObject characterTier2;
    public GameObject characterTier3;
    public GameObject characterTier4;

    public GameObject horseTier1;
    public GameObject horseTier2;
    public GameObject horseTier3;
    public GameObject horseTier4;

    public GameObject weaponTier1;
    public GameObject weaponTier2;
    public GameObject weaponTier3;

    public float bonusHealth;
    public float bonusDamage;
    public float bonusSpeed;
    private void Awake()
    {
        LoadCharacter();
        LoadHorse();
        LoadWeapon();
    }

    void LoadCharacter()
    {
        string equipped =
            PlayerPrefs.GetString(
                "EquippedCharacter",
                "Character_Tier1"
            );

        GameObject prefab = characterTier1;

        switch (equipped)
        {
            case "Character_Tier1":
                prefab = characterTier1;
                bonusHealth += 0;
                bonusDamage += 0;
                break;
            case "Character_Tier2":
                prefab = characterTier2;
                bonusHealth += 40;
                bonusDamage += 10;
                break;
            case "Character_Tier3":
                prefab = characterTier3;
                bonusHealth += 100;
                bonusDamage += 30;
                break;
            case "Character_Tier4":
                prefab = characterTier4;
                bonusHealth += 300;
                bonusDamage += 50;
                break;

        }

        Instantiate(
            prefab,
            characterRoot
        );
    }

    void LoadHorse()
    {
        string equipped =
            PlayerPrefs.GetString(
                "EquippedHorse",
                "Horse_Tier1"
            );

        GameObject prefab = horseTier1;

        switch (equipped)
        {
            case "Horse_Tier1":
                prefab = horseTier1;
                bonusHealth += 0;
                bonusSpeed += 0;
                break;
            case "Horse_Tier2":
                prefab = horseTier2;
                bonusHealth -= 30;
                bonusSpeed += 10;
                break;
            case "Horse_Tier3":
                prefab = horseTier3;
                bonusHealth += 50;
                bonusSpeed += 5;

                break;
            case "Horse_Tier4":
                prefab = horseTier4;
                bonusHealth += 300;
                bonusSpeed += 8;
                break;

        }
        Debug.Log(
    "EquippedHorse = " +
    PlayerPrefs.GetString(
        "EquippedHorse"
    )
);
        Instantiate(
            prefab,
            horseRoot
        );
        if (equipped == "Horse_Tier2")
        {
            Vector3 pos = characterRoot.localPosition;
            pos.y = 1f;
            characterRoot.localPosition = pos;
            Vector3 posChild = characterRoot.GetChild(0).position;
            Debug.Log("posChild = " + posChild);
        }

    }

    void LoadWeapon()
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
        }

        Instantiate(
            prefab,
            weaponRoot
        );

    }
}