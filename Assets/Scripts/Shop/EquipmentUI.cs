using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentUI : MonoBehaviour
{
    [Header("Left")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ShopItemUI itemPrefab;

    [Header("Right")]
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemCategory;
    [SerializeField] private TMP_Text itemDescription;

    [Header("Bottom")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonText;

    [Header("Preview")]
    [SerializeField] private EquipmentPreviewManager previewManager;
    private InventoryManager.ItemCategory currentCategory =
    InventoryManager.ItemCategory.Weapon;
    private GameItemData selectedItem;

    [Header("Button")]
    [SerializeField] private Button weaponButton;
    [SerializeField] private Button mountButton;
    [SerializeField] private Button characterButton;
    private void OnEnable()
    {
        if (FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include) != null)
        {
            enabled = false;
            return;
        }

        if (InventoryManager.Instance == null)
            return;
        BuildEquipment();
        previewManager?.Refresh();
    }
    private void UpdateTabVisual()
    {
        if (!enabled)
            return;

        SetTab(
            weaponButton,
            currentCategory == InventoryManager.ItemCategory.Weapon
        );

        SetTab(
            mountButton,
            currentCategory == InventoryManager.ItemCategory.Mount
        );

        SetTab(
            characterButton,
            currentCategory == InventoryManager.ItemCategory.Character
        );
    }

    private void SetTab(Button btn, bool selected)
    {
        if (btn == null)
            return;

        btn.transform.localScale =
            selected
                ? Vector3.one * 1.1f
                : Vector3.one;

        ColorBlock colors = btn.colors;

        colors.normalColor =
            selected
                ? new Color(1f, 0.85f, 0.3f)
                : Color.white;

        btn.colors = colors;
    }
    public void ShowWeapons()
    {
        if (!enabled)
            return;

        currentCategory =
            InventoryManager.ItemCategory.Weapon;
        UpdateTabVisual();
        BuildEquipment();
    }

    public void ShowMounts()
    {
        if (!enabled)
            return;

        currentCategory =
            InventoryManager.ItemCategory.Mount;
        UpdateTabVisual();
        BuildEquipment();
    }

    public void ShowUltimates()
    {
        if (!enabled)
            return;

        currentCategory =
            InventoryManager.ItemCategory.Character;
        UpdateTabVisual();
        BuildEquipment();
    }
    private void BuildEquipment()
    {
        if (contentRoot == null || itemPrefab == null || InventoryManager.Instance == null)
            return;

        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in InventoryManager.Instance.allItems)
        {
            if (!item.IsOwned())
                continue;
            if (item.category != currentCategory)
                continue;

            ShopItemUI entry =
                Instantiate(
                    itemPrefab,
                    contentRoot
                );

            entry.Setup(
                item,
                OnItemSelected
            );
        }
    }

    private void OnItemSelected(GameItemData item)
    {
        if (!enabled)
            return;

        Debug.Log("SELECTED: " + item.displayName);

        selectedItem = item;

        itemName.text =
            item.displayName;

        itemCategory.text =
            item.category.ToString();

        itemDescription.text =
            item.description;

        equipButtonText.text =
            item.IsEquipped()
                ? "ĐANG TRANG BỊ"
                : "TRANG BỊ";

        equipButton.interactable =
            !item.IsEquipped();
    }

    public void EquipSelectedItem()
    {
        if (!enabled)
            return;

        if (selectedItem == null)
            return;

        InventoryManager.Instance
            .ToggleEquip(selectedItem);

        previewManager?.Refresh();

        OnItemSelected(selectedItem);
    }
}
