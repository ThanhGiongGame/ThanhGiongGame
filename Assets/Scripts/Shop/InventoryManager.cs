using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public enum ItemCategory
    {
        Weapon,
        Mount,
        Ultimate
    }
    public static InventoryManager Instance { get; private set; }

    [Header("Kho Dữ Liệu Vật Phẩm Hệ Thống")]
    public List<GameItemData> allItems = new List<GameItemData>();

    private void Awake()
    {
        // Khởi tạo Singleton để script khác dễ dàng gọi tới thông qua InventoryManager.Instance
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Thiết lập toàn bộ dữ liệu vật phẩm tại đây (Dễ dàng thêm mới món thứ 5, thứ 6)
    private void InitDatabase()
    {
        //allItems.Clear();

        //allItems.Add(new GameItemData
        //{
        //    id = "Character_Tier1",
        //    displayName = "Kiếm Trúc",

        //    description =
        //        "+10 Sát thương",

        //    category =
        //        ItemCategory.Weapon,

        //    cost = 100,

        //});

        //allItems.Add(new GameItemData
        //{
        //    id = "Character_Tier2",
        //    displayName = "Đoản Đao Sắt",

        //    description =
        //        "+25 Sát thương",

        //    category =
        //        ItemCategory.Weapon,

        //    cost = 300,

        //});
        //allItems.Add(new GameItemData
        //{
        //    id = "Horse_Tier1",
        //    displayName = "Ngựa Sắt",

        //    description =
        //"+25 Sát thương",

        //    category =
        //ItemCategory.Mount,

        //    cost = 300,

        //});
        //allItems.Add(new GameItemData
        //{
        //    id = "Horse_Tier2",
        //    displayName = "Ngựa Bạc",

        //    description =
        //"+50 Sát thương",
        //    category =
        //ItemCategory.Mount,

        //    cost = 300,

        //});
        //allItems.Add(new GameItemData
        //{
        //    id = "Ultimate_Tier1",
        //    displayName = "Cú đá sấm sét",
        //    description =
        //    "Nhất kích tất sát thương",
        //    category = ItemCategory.Ultimate,
        //    cost = 500,
        //});
        }

    // ---- CÁC HÀM XỬ LÝ LOGIC CORE ----

    public int GetCurrency() => PlayerPrefs.GetInt("VinhDanhTotal", 100000);

    public void AddCurrency(int amount)
    {
        PlayerPrefs.SetInt("VinhDanhTotal", GetCurrency() + amount);
        PlayerPrefs.Save();
    }

    public bool BuyItem(GameItemData item)
    {
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

        return true;
    }
    public void ToggleEquip(GameItemData item)
    {
        if (!item.IsOwned())
            return;

        switch (item.category)
        {
            case ItemCategory.Weapon:

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

            case ItemCategory.Ultimate:

                PlayerPrefs.SetString(
                    "EquippedUltimate",
                    item.id
                );
                break;
        }

        PlayerPrefs.Save();
    }
}