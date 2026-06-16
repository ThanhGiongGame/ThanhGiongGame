using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Left Panel")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ShopItemUI itemPrefab;

    [Header("Right Panel")]
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemCategory;
    [SerializeField] private TMP_Text itemPrice;
    [SerializeField] private TMP_Text itemDescription;
    [SerializeField] private TMP_Text itemStatus;

    [Header("Bottom")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    [SerializeField]
    private PreviewManager previewManager;
    private GameItemData selectedItem;

    private void Start()
    {
        NormalizeStaticText();
        BuildShop();
    }

    private void BuildShop()
    {
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in InventoryManager.Instance.allItems)
        {
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

        if (InventoryManager.Instance.allItems.Count > 0)
        {
            OnItemSelected(
                InventoryManager.Instance.allItems[0]
            );
        }
    }

    private void OnItemSelected(GameItemData item)
    {
        Debug.Log("Selected: " + item.displayName + item.prefab);

        selectedItem = item;

        itemName.text =
            item.displayName;

        itemCategory.text =
            item.category.ToString();

        itemPrice.text =
            item.cost + " Vinh Danh";

        itemDescription.text =
            item.description;

        itemStatus.text =
            item.IsOwned()
                ? "Da so huu"
                : "Chua so huu";
        previewManager.Show(item);
        UpdateActionButton();
    }

    private void UpdateActionButton()
    {
        if (selectedItem == null)
            return;

        if (selectedItem.IsOwned())
        {
            actionButtonText.text =
                "DA SO HUU";

            actionButton.interactable =
                false;
        }
        else
        {
            actionButtonText.text =
                "MUA";

            actionButton.interactable =
                true;
        }
    }

    public void BuySelectedItem()
    {
        if (selectedItem == null)
            return;

        if (
            InventoryManager.Instance
                .BuyItem(selectedItem)
        )
        {
            OnItemSelected(
                selectedItem
            );
        }
    }

    private void NormalizeStaticText()
    {
        ConfigureLabel(itemName, 38f, TextAlignmentOptions.Center);
        ConfigureLabel(itemCategory, 22f, TextAlignmentOptions.Center);
        ConfigureLabel(itemPrice, 20f, TextAlignmentOptions.Center);
        ConfigureLabel(itemDescription, 20f, TextAlignmentOptions.Center);
        ConfigureLabel(itemStatus, 34f, TextAlignmentOptions.Center);
        ConfigureLabel(actionButtonText, 30f, TextAlignmentOptions.Center);

        if (itemStatus != null)
        {
            itemStatus.color = new Color(0.2f, 1f, 0.25f, 1f);
        }
    }

    private static void ConfigureLabel(TMP_Text label, float fontSize, TextAlignmentOptions alignment)
    {
        if (label == null)
        {
            return;
        }

        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.alignment = alignment;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
        label.rectTransform.localScale = Vector3.one;
    }
}
