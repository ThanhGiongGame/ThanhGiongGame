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

    public int cost;

    public GameObject prefab;

    public bool IsOwned()
    {
        return PlayerPrefs.GetInt(id + "_Owned", 0) == 1;
    }

    public bool IsEquipped()
    {
        switch (category)
        {
            case ItemCategory.Character:
                return PlayerPrefs.GetString("EquippedCharacter") == id;

            case ItemCategory.Mount:
                return PlayerPrefs.GetString("EquippedHorse") == id;

            case ItemCategory.Weapon:
                return PlayerPrefs.GetString("EquippedWeapon") == id;
        }

        return false;
    }
}