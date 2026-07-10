using System;
using UnityEngine;
using static InventoryManager;

[Serializable]
public class GameItemData
{
    public string id;

    public string displayName;

    [TextArea]
    public string description;

    public ItemCategory category;

    public int requiredLevel = 1;

    public GameObject prefab;

    public bool IsUnlocked()
    {
        return PersistentLevel.Current >= requiredLevel;
    }

    public bool IsEquipped()
    {
        switch (category)
        {
            case ItemCategory.Character:
                return PlayerPrefs.GetString("EquippedCharacter", "Character_Tier1") == id;

            case ItemCategory.Mount:
                return PlayerPrefs.GetString("EquippedHorse", "Horse_Tier1") == id;

            case ItemCategory.Weapon:
                return PlayerPrefs.GetString("EquippedWeapon", "Weapon_Tier1") == id;
        }

        return false;
    }
}