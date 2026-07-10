using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour
{
    public enum ItemCategory
    {
        Weapon,
        Mount,
        Character
    }
    public static InventoryManager Instance { get; private set; }
    public static event Action OnEquipmentChanged;


    [Header("Kho Dữ Liệu Vật Phẩm Hệ Thống")]
    public List<GameItemData> allItems = new List<GameItemData>();

    private void Awake()
    {
        // Khởi tạo Singleton để script khác dễ dàng gọi tới thông qua InventoryManager.Instance
        if (Instance == null)
        {
            Instance = this;
            InitDatabase();
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Thiết lập toàn bộ dữ liệu vật phẩm tại đây (Dễ dàng thêm mới món thứ 5, thứ 6)
    private void InitDatabase()
    {
        foreach (var item in allItems)
        {
            if (item.id.Contains("Tier1")) item.requiredLevel = 1;
            else if (item.id.Contains("Tier2")) item.requiredLevel = 3;
            else if (item.id.Contains("Tier3"))
            {
                if (item.id.Contains("Character")) item.requiredLevel = 8;
                else item.requiredLevel = 5;
            }
            else if (item.id.Contains("Tier4")) item.requiredLevel = 8;
            else item.requiredLevel = 1;
        }
    }
    // Removed Currency and Buy logic as Shop uses Level-based unlocks now.
    public void ToggleEquip(GameItemData item)
    {
        if (!item.IsUnlocked())
            return;

        switch (item.category)
        {
            case ItemCategory.Character:

                PlayerPrefs.SetString(
                    "EquippedCharacter",
                    item.id
                );
                break;

            case ItemCategory.Mount:

                PlayerPrefs.SetString(
                    "EquippedHorse",
                    item.id
                );
                break;

            case ItemCategory.Weapon:

                PlayerPrefs.SetString(
                    "EquippedWeapon",
                    item.id
                );
                break;
        }

        PlayerPrefs.Save();
        OnEquipmentChanged?.Invoke();

    }
}
