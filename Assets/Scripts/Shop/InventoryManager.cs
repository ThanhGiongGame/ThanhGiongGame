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
    }
    public int GetCurrency() => PlayerPrefs.GetInt("VinhDanhTotal", 100000000);

    public void AddCurrency(int amount)
    {
        PlayerPrefs.SetInt("VinhDanhTotal", GetCurrency() + amount);
        PlayerPrefs.Save();
    }

    public bool BuyItem(GameItemData item)
    {
        Debug.Log("IsOwned:" + item.IsOwned() + "\nVinhDanh:" + GetCurrency());
        if (item.IsOwned())
            return false;
        
        int money = GetCurrency();

        if (money < item.cost)
            return false;

        PlayerPrefs.SetInt(
            "VinhDanhTotal",
            money - item.cost
        );

        PlayerPrefs.SetInt(
            item.id + "_Owned",
            1
        );

        PlayerPrefs.Save();
        Debug.Log("Item is bought:" + item.IsOwned());
        return true;
    }
    public void ToggleEquip(GameItemData item)
    {
        if (!item.IsOwned())
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
