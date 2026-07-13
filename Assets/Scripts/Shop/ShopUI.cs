using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ShopUI : MonoBehaviour
{


    private enum CategoryFilter
    {
        Weapon,
        Mount,
        Character
    }

    [SerializeField] private PreviewManager previewManager;
    [SerializeField] private EquipmentPreviewManager equipmentPreviewManager;


    private CategoryFilter currentFilter = CategoryFilter.Character;
    private GameItemData selectedItem;
    private CameraManager cameraManager;

    private RectTransform root;
    private RectTransform listContent;
    private TMP_Text titleText;
    private TMP_Text hintText;
    private TMP_Text currencyText;
    private TMP_Text itemNameText;
    private TMP_Text itemMetaText;
    private TMP_Text itemPriceText;
    private TMP_Text itemDescriptionText;
    private TMP_Text itemStatusText;
    private TMP_Text actionButtonText;
    private Button actionButton;
    private Button equipmentTabButton;
    private Button mapTabButton;
    private Button weaponFilterButton;
    private Button mountFilterButton;
    private Button characterFilterButton;
    private Button escButton;
    private RectTransform escMenuPanel;
    private bool hasBuiltRuntimeUi;

    private void OnEnable()
    {
        hasBuiltRuntimeUi = false;
        currentFilter = CategoryFilter.Character;
        selectedItem = null;
        cameraManager = FindFirstObjectByType<CameraManager>(FindObjectsInactive.Include);
        previewManager ??= FindFirstObjectByType<PreviewManager>(FindObjectsInactive.Include);
        equipmentPreviewManager ??= FindFirstObjectByType<EquipmentPreviewManager>(FindObjectsInactive.Include);
        PersistentLevel.OnLevelChanged += UpdateCurrency;
        ShowEquipmentMode();
    }

    private void Update()
    {
        bool escapePressed = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif

#if ENABLE_INPUT_SYSTEM
        escapePressed = escapePressed || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
#endif

        if (escapePressed)
        {
            ToggleEscMenu();
        }
    }

    public void ShowEquipmentMode()
    {
        ResolveSceneRefs();

        BuildRuntimeUi();
        previewManager?.Clear();
        cameraManager?.ShowEquipment();
        titleText.text = "TRANG BỊ";
        hintText.text = "Chỉ hiện vật phẩm đã sở hữu. Chọn món để trang bị.";
        RebuildItemList();
        UpdateTabVisuals();
        equipmentPreviewManager?.SetPreviewVisible(true);
        equipmentPreviewManager?.Refresh();

    }



    private void OnEquipmentTabClicked()
    {
        ShowEquipmentMode();
    }

    private void BuildRuntimeUi()
    {
        UiSceneNormalizer.NormalizeScene("ShopCanvas");

        Canvas canvas = FindShopCanvas();
        if (canvas == null)
        {
            return;
        }

        HideLegacyUi();

        Transform existing = canvas.transform.Find("CleanShopRoot");
        if (!hasBuiltRuntimeUi)
        {
            while (existing != null)
            {
                DestroyImmediate(existing.gameObject);
                existing = canvas.transform.Find("CleanShopRoot");
            }
        }

        if (existing != null)
        {
            if (!IsRuntimeRootComplete(existing))
            {
                Destroy(existing.gameObject);
            }
            else
            {
            root = existing.GetComponent<RectTransform>();
            root.gameObject.SetActive(true);
            SetRuntimeRootVisible(root);
            root.SetAsLastSibling();
            CacheRuntimeRefs();
            EnsureRuntimeRefs();
            ApplyResponsiveLayout();
            UpdateCurrency();
            hasBuiltRuntimeUi = true;
            return;
            }
        }

        root = CreateRect(canvas.transform, "CleanShopRoot");
        root.gameObject.SetActive(true);
        Stretch(root, 0f, 0f, 0f, 0f);
        root.SetAsLastSibling();

        RectTransform leftPanel = CreatePanel(root, "CleanLeftPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -86f), new Vector2(560f, 700f), new Vector2(0f, 1f));
        RectTransform rightPanel = CreatePanel(root, "CleanRightPanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -86f), new Vector2(500f, 700f), new Vector2(1f, 1f));

        currencyText = CreateText(root, "CurrencyText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(38f, -28f), new Vector2(520f, 44f), new Vector2(0f, 1f), 28f, new Color(1f, 0.86f, 0.18f, 1f), FontStyles.Bold, TextAlignmentOptions.Left);
        escButton = CreateButton(root, "EscButton", "=", new Vector2(-34f, -28f), new Vector2(58f, 50f), ToggleEscMenu, new Color(0.05f, 0.045f, 0.025f, 0.96f), 34f, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        StyleIconButton(escButton);

        titleText = CreateText(leftPanel, "PanelTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(500f, 46f), new Vector2(0.5f, 1f), 34f, new Color(1f, 0.93f, 0.62f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
        hintText = CreateText(leftPanel, "PanelHint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(500f, 34f), new Vector2(0.5f, 1f), 18f, new Color(0.86f, 0.9f, 0.88f, 1f), FontStyles.Normal, TextAlignmentOptions.Center);

        RectTransform filterBar = CreateRect(leftPanel, "FilterBar");
        SetRect(filterBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(500f, 44f), new Vector2(0.5f, 1f));
        characterFilterButton = CreateSmallButton(filterBar, "CharacterFilter", "NHÂN VẬT", new Vector2(-166.5f, 0f), () => SelectFilter(CategoryFilter.Character));
        mountFilterButton = CreateSmallButton(filterBar, "MountFilter", "THÚ CƯỠI", Vector2.zero, () => SelectFilter(CategoryFilter.Mount));
        weaponFilterButton = CreateSmallButton(filterBar, "WeaponFilter", "VŨ KHÍ", new Vector2(166.5f, 0f), () => SelectFilter(CategoryFilter.Weapon));

        RectTransform listViewport = CreatePanel(leftPanel, "ListViewport", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Color(0f, 0f, 0f, 0.22f));
        Stretch(listViewport, 24f, 198f, 24f, 24f);
        Mask mask = listViewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;
        ScrollRect scroll = listViewport.gameObject.AddComponent<ScrollRect>();
        listContent = CreateRect(listViewport, "ListContent");
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        listContent.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = listContent;
        scroll.viewport = listViewport;
        scroll.horizontal = false;
        scroll.vertical = true;

        itemNameText = CreateText(rightPanel, "ItemName", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(440f, 54f), new Vector2(0.5f, 1f), 34f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        itemMetaText = CreateText(rightPanel, "ItemCategory", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, -128f), new Vector2(180f, 34f), new Vector2(0.5f, 1f), 20f, new Color(0.82f, 0.9f, 1f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
        itemPriceText = CreateText(rightPanel, "ItemPrice", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(110f, -128f), new Vector2(200f, 34f), new Vector2(0.5f, 1f), 20f, new Color(1f, 0.84f, 0.22f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
        itemDescriptionText = CreateText(rightPanel, "ItemDescription", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -194f), new Vector2(430f, 90f), new Vector2(0.5f, 1f), 22f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
        itemStatusText = CreateText(rightPanel, "ItemStatus", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -305f), new Vector2(430f, 56f), new Vector2(0.5f, 1f), 28f, new Color(0.38f, 1f, 0.42f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
        actionButton = CreateLargeButton(rightPanel, "ActionButton", "MUA", new Vector2(0f, 42f), PerformSelectedAction);
        actionButtonText = actionButton.GetComponentInChildren<TMP_Text>();

        RectTransform bottom = CreatePanel(root, "BottomNav", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1040f, 76f), new Vector2(0.5f, 0f), new Color(0.02f, 0.02f, 0.02f, 0.58f));
        equipmentTabButton = CreateNavButton(bottom, "EquipmentTab", "TRANG BỊ", new Vector2(-130f, 8f), OnEquipmentTabClicked);
        mapTabButton = CreateNavButton(bottom, "MapTab", "BẢN ĐỒ", new Vector2(130f, 8f), () => cameraManager?.OpenMapSelection());
        // CreateNavButton(bottom, "PlayTab", "VÀO TRẬN", new Vector2(260f, 8f), () => cameraManager?.ChangeGameplayScene());
        BuildEscMenu();

        CacheRuntimeRefs();
        EnsureRuntimeRefs();
        ApplyResponsiveLayout();
        UpdateCurrency();
        hasBuiltRuntimeUi = true;
    }

    private void ResolveSceneRefs()
    {
        cameraManager ??= FindFirstObjectByType<CameraManager>(FindObjectsInactive.Include);
        previewManager ??= FindFirstObjectByType<PreviewManager>(FindObjectsInactive.Include);
        equipmentPreviewManager ??= FindFirstObjectByType<EquipmentPreviewManager>(FindObjectsInactive.Include);
    }

    private static bool IsRuntimeRootComplete(Transform candidateRoot)
    {
        if (candidateRoot == null)
        {
            return false;
        }

        string[] requiredChildren =
        {
            "CleanLeftPanel",
            "CleanRightPanel",
            "BottomNav",
            "ListContent",
            "PanelTitle",
            "PanelHint",
            "CurrencyText",
            "ItemName",
            "ItemCategory",
            "ItemPrice",
            "ItemDescription",
            "ItemStatus",
            "ActionButton",
            "ShopTab",
            "EquipmentTab",
            "MapTab",
            "EscButton"
        };

        foreach (string childName in requiredChildren)
        {
            if (FindChild(candidateRoot, childName) == null)
            {
                return false;
            }
        }

        return true;
    }

    private static void SetRuntimeRootVisible(Transform candidateRoot)
    {
        if (candidateRoot == null)
        {
            return;
        }

        Transform[] children = candidateRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == candidateRoot)
            {
                continue;
            }

            if (child.name == "EscMenuPanel")
            {
                continue;
            }

            child.gameObject.SetActive(true);
        }
    }

    private void CacheRuntimeRefs()
    {
        if (root == null)
        {
            return;
        }

        listContent = FindRect(root, "ListContent");
        titleText = FindText(root, "PanelTitle");
        hintText = FindText(root, "PanelHint");
        currencyText = FindText(root, "CurrencyText");
        itemNameText = FindText(root, "ItemName");
        itemMetaText = FindText(root, "ItemCategory") ?? FindText(root, "ItemMeta");
        itemPriceText = FindText(root, "ItemPrice");
        itemDescriptionText = FindText(root, "ItemDescription") ?? FindText(root, "ItemDesc");
        itemStatusText = FindText(root, "ItemStatus");
        actionButton = FindButton(root, "ActionButton");
        actionButtonText = actionButton != null ? actionButton.GetComponentInChildren<TMP_Text>(true) : null;
        equipmentTabButton = FindButton(root, "EquipmentTab");
        mapTabButton = FindButton(root, "MapTab");
        weaponFilterButton = FindButton(root, "WeaponFilter");
        mountFilterButton = FindButton(root, "MountFilter");
        characterFilterButton = FindButton(root, "CharacterFilter");
        escButton = FindButton(root, "EscButton");
        escMenuPanel = FindRect(root, "EscMenuPanel");

        Button legacyAllFilter = FindButton(root, "AllFilter");
        if (legacyAllFilter != null)
        {
            legacyAllFilter.gameObject.SetActive(false);
        }
    }

    private void EnsureRuntimeRefs()
    {
        if (root == null)
        {
            return;
        }

        RectTransform leftPanel = FindRect(root, "CleanLeftPanel");
        RectTransform rightPanel = FindRect(root, "CleanRightPanel");

        currencyText ??= CreateText(root, "CurrencyText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(38f, -28f), new Vector2(520f, 44f), new Vector2(0f, 1f), 28f, new Color(1f, 0.86f, 0.18f, 1f), FontStyles.Bold, TextAlignmentOptions.Left);

        if (leftPanel != null)
        {
            titleText ??= CreateText(leftPanel, "PanelTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(500f, 46f), new Vector2(0.5f, 1f), 34f, new Color(1f, 0.93f, 0.62f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            hintText ??= CreateText(leftPanel, "PanelHint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(500f, 34f), new Vector2(0.5f, 1f), 18f, new Color(0.86f, 0.9f, 0.88f, 1f), FontStyles.Normal, TextAlignmentOptions.Center);
        }

        if (rightPanel != null)
        {
            itemNameText ??= CreateText(rightPanel, "ItemName", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(440f, 54f), new Vector2(0.5f, 1f), 34f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            itemMetaText ??= CreateText(rightPanel, "ItemCategory", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, -128f), new Vector2(180f, 34f), new Vector2(0.5f, 1f), 20f, new Color(0.82f, 0.9f, 1f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            itemPriceText ??= CreateText(rightPanel, "ItemPrice", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(110f, -128f), new Vector2(200f, 34f), new Vector2(0.5f, 1f), 20f, new Color(1f, 0.84f, 0.22f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            itemDescriptionText ??= CreateText(rightPanel, "ItemDescription", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -194f), new Vector2(430f, 90f), new Vector2(0.5f, 1f), 22f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
            itemStatusText ??= CreateText(rightPanel, "ItemStatus", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -305f), new Vector2(430f, 56f), new Vector2(0.5f, 1f), 28f, new Color(0.38f, 1f, 0.42f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);

            if (actionButton == null)
            {
                actionButton = CreateLargeButton(rightPanel, "ActionButton", "MUA", new Vector2(0f, 42f), PerformSelectedAction);
            }
        }

        actionButtonText ??= actionButton != null ? actionButton.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private void ApplyResponsiveLayout()
    {
        if (root == null)
        {
            return;
        }

        Stretch(root, 0f, 0f, 0f, 0f);

        RectTransform leftPanel = FindRect(root, "CleanLeftPanel");
        RectTransform rightPanel = FindRect(root, "CleanRightPanel");
        RectTransform filterBar = FindRect(root, "FilterBar");
        RectTransform listViewport = FindRect(root, "ListViewport");
        RectTransform bottomNav = FindRect(root, "BottomNav");

        SetAnchorBox(leftPanel, new Vector2(0.035f, 0.24f), new Vector2(0.305f, 0.82f), Vector2.zero, Vector2.zero);
        SetAnchorBox(rightPanel, new Vector2(0.695f, 0.24f), new Vector2(0.965f, 0.82f), Vector2.zero, Vector2.zero);
        SetAnchorBox(bottomNav, new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.14f), Vector2.zero, Vector2.zero);

        if (currencyText != null)
        {
            SetAnchorBox(currencyText.rectTransform, new Vector2(0.04f, 0.885f), new Vector2(0.33f, 0.955f), Vector2.zero, Vector2.zero);
            currencyText.fontSize = 24f;
            currencyText.alignment = TextAlignmentOptions.Left;
        }

        if (escButton != null)
        {
            SetAnchorBox(escButton.GetComponent<RectTransform>(), new Vector2(0.94f, 0.89f), new Vector2(0.985f, 0.975f), Vector2.zero, Vector2.zero);
            StyleIconButton(escButton);
        }

        if (titleText != null)
        {
            SetAnchorBox(titleText.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);
            titleText.fontSize = 27f;
        }

        if (hintText != null)
        {
            SetAnchorBox(hintText.rectTransform, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.85f), Vector2.zero, Vector2.zero);
            hintText.fontSize = 13f;
        }

        SetAnchorBox(filterBar, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.75f), Vector2.zero, Vector2.zero);
        LayoutFilterButton(characterFilterButton, 0);
        LayoutFilterButton(mountFilterButton, 1);
        LayoutFilterButton(weaponFilterButton, 2);

        SetAnchorBox(listViewport, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.66f), Vector2.zero, Vector2.zero);

        if (itemNameText != null)
        {
            SetAnchorBox(itemNameText.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
            itemNameText.fontSize = 28f;
        }

        if (itemMetaText != null)
        {
            SetAnchorBox(itemMetaText.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.42f, 0.80f), Vector2.zero, Vector2.zero);
            itemMetaText.enableAutoSizing = true;
            itemMetaText.fontSizeMin = 11f;
            itemMetaText.fontSizeMax = 17f;
            itemMetaText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (itemPriceText != null)
        {
            SetAnchorBox(itemPriceText.rectTransform, new Vector2(0.46f, 0.72f), new Vector2(0.92f, 0.80f), Vector2.zero, Vector2.zero);
            itemPriceText.enableAutoSizing = true;
            itemPriceText.fontSizeMin = 11f;
            itemPriceText.fontSizeMax = 17f;
            itemPriceText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (itemDescriptionText != null)
        {
            SetAnchorBox(itemDescriptionText.rectTransform, new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);
        }

        if (itemStatusText != null)
        {
            SetAnchorBox(itemStatusText.rectTransform, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.43f), Vector2.zero, Vector2.zero);
            itemStatusText.fontSize = 24f;
        }

        if (actionButton != null)
        {
            SetAnchorBox(actionButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);
        }

        LayoutBottomButton(equipmentTabButton, 0, 3);
        LayoutBottomButton(mapTabButton, 1, 3);
        LayoutBottomButton(FindButton(root, "PlayTab"), 2, 3);

        if (escMenuPanel != null)
        {
            SetRect(escMenuPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 330f), new Vector2(0.5f, 0.5f));
        }
    }

    private static void LayoutFilterButton(Button button, int index)
    {
        if (button == null)
        {
            return;
        }

        float min = index / 3f;
        float max = (index + 1f) / 3f;
        SetAnchorBox(button.GetComponent<RectTransform>(), new Vector2(min, 0f), new Vector2(max, 1f), new Vector2(3f, 4f), new Vector2(-3f, -4f));

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 10f;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private static void LayoutBottomButton(Button button, int index, int total = 4)
    {
        if (button == null)
        {
            return;
        }

        float min = index / (float)total;
        float max = (index + 1f) / (float)total;
        SetAnchorBox(button.GetComponent<RectTransform>(), new Vector2(min, 0f), new Vector2(max, 1f), new Vector2(14f, 9f), new Vector2(-14f, -9f));
    }

    private void HideLegacyUi()
    {
        string[] names =
        {
            "LeftPanel", "RightPannel", "Buy", "Scroll View", "BtnShop", "BtnEquipment", "BtnGame",
            "ItemName", "ItemCategory", "ItemPrice", "ItemDescription", "ItemStatus",
            "CategoryFilterBar", "CurrencyText", "ShopPanelTitle", "ShopPanelHint"
        };

        foreach (string objectName in names)
        {
            GameObject target = FindDirectOrGlobal(objectName);
            if (target != null && target.name != "CleanShopRoot" && !IsInsideCleanShopRoot(target.transform))
            {
                target.SetActive(false);
            }
        }
    }

    private static bool IsInsideCleanShopRoot(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (current.name == "CleanShopRoot")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void SelectFilter(CategoryFilter filter)
    {
        if (!HasItemsForFilter(filter))
        {
            return;
        }

        currentFilter = filter;
        RebuildItemList();
        UpdateFilterVisuals();
    }

    private void RebuildItemList()
    {
        if (listContent == null || InventoryManager.Instance == null)
        {
            return;
        }

        EnsureAvailableFilter();

        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        GameItemData first = null;
        foreach (GameItemData item in InventoryManager.Instance.allItems)
        {
            if (!MatchesFilter(item))
            {
                continue;
            }



            CreateItemRow(item);
            first ??= item;
        }

        if (first != null)
        {
            SelectItem(first);
        }
        else
        {
            ShowEmptyState();
        }

        UpdateCurrency();
        UpdateFilterVisuals();
    }

    private bool MatchesFilter(GameItemData item)
    {
        return (currentFilter == CategoryFilter.Weapon && item.category == InventoryManager.ItemCategory.Weapon)
            || (currentFilter == CategoryFilter.Mount && item.category == InventoryManager.ItemCategory.Mount)
            || (currentFilter == CategoryFilter.Character && item.category == InventoryManager.ItemCategory.Character);
    }

    private void CreateItemRow(GameItemData item)
    {
        Button row = CreateButton(listContent, "ItemRow_" + item.id, "", Vector2.zero, new Vector2(0f, 68f), () => SelectItem(item), new Color(0.88f, 0.78f, 0.52f, 0.98f));
        LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
        element.minHeight = 68f;
        element.preferredHeight = 68f;

        TMP_Text name = CreateText(row.transform, "Name", new Vector2(0f, 0f), new Vector2(0.68f, 1f), new Vector2(12f, 0f), Vector2.zero, new Vector2(0f, 0.5f), 20f, new Color(0.08f, 0.07f, 0.04f, 1f), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        name.text = item.displayName;
        SetAnchorBox(name.rectTransform, new Vector2(0f, 0f), new Vector2(0.62f, 1f), new Vector2(12f, 0f), Vector2.zero);
        name.enableAutoSizing = true;
        name.fontSizeMin = 12f;
        name.fontSizeMax = 18f;
        name.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text price = CreateText(row.transform, "Price", new Vector2(0.68f, 0f), Vector2.one, new Vector2(-12f, 0f), Vector2.zero, new Vector2(1f, 0.5f), 16f, new Color(0.08f, 0.07f, 0.04f, 1f), FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        price.text = item.IsUnlocked() ? "ĐÃ MỞ KHÓA" : "CẦN CẤP ĐỘ " + item.requiredLevel;
        SetAnchorBox(price.rectTransform, new Vector2(0.62f, 0f), Vector2.one, Vector2.zero, new Vector2(-12f, 0f));
        price.enableAutoSizing = true;
        price.fontSizeMin = 10f;
        price.fontSizeMax = 14f;
        price.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void SelectItem(GameItemData item)
    {
        if (!EnsureDetailRefsReady())
        {
            return;
        }

        selectedItem = item;
        Debug.Log("Item " + item.displayName);
        itemNameText.text = item.displayName;
        itemMetaText.text = GetCategoryLabel(item.category);
        itemPriceText.text = item.IsUnlocked() ? "ĐÃ MỞ KHÓA" : "CẦN CẤP ĐỘ " + item.requiredLevel;
        itemDescriptionText.text = item.description;
        itemStatusText.text = GetStatusText(item);

        previewManager?.Clear();

        UpdateActionButton();
    }

    private void ShowEmptyState()
    {
        if (!EnsureDetailRefsReady())
        {
            return;
        }

        selectedItem = null;
        itemNameText.text = "CHƯA CÓ ĐỒ";
        itemMetaText.text = "";
        itemPriceText.text = "";
        itemDescriptionText.text = "Không có vật phẩm trong danh mục này.";
        itemStatusText.text = "";
        UpdateActionButton();
    }

    private void PerformSelectedAction()
    {
        if (selectedItem == null || InventoryManager.Instance == null)
        {
            return;
        }

        if (selectedItem.IsUnlocked())
        {
            InventoryManager.Instance.ToggleEquip(selectedItem);
            SelectItem(selectedItem);
            RebuildItemList();
        }
    }

    private void UpdateActionButton()
    {
        if (actionButton == null || actionButtonText == null)
        {
            BuildRuntimeUi();
        }

        if (actionButton == null || actionButtonText == null)
        {
            return;
        }

        if (selectedItem == null)
        {
            actionButton.interactable = false;
            actionButtonText.text = "TRANG BỊ";
            return;
        }

        if (!selectedItem.IsUnlocked())
        {
            actionButtonText.text = "CẦN CẤP ĐỘ " + selectedItem.requiredLevel;
            actionButton.interactable = false;
            return;
        }

        actionButtonText.text = selectedItem.IsEquipped() ? "ĐANG DÙNG" : "TRANG BỊ";
        actionButton.interactable = !selectedItem.IsEquipped();
    }

    private bool EnsureDetailRefsReady()
    {
        if (
            itemNameText != null
            && itemMetaText != null
            && itemPriceText != null
            && itemDescriptionText != null
            && itemStatusText != null
        )
        {
            return true;
        }

        BuildRuntimeUi();
        return itemNameText != null
            && itemMetaText != null
            && itemPriceText != null
            && itemDescriptionText != null
            && itemStatusText != null;
    }

    private string GetStatusText(GameItemData item)
    {
        return item.IsUnlocked() ? (item.IsEquipped() ? "ĐANG TRANG BỊ" : "SẴN SÀNG") : "CHƯA MỞ KHÓA";
    }

    private void UpdateCurrency()
    {
        if (currencyText != null)
        {
            currencyText.text = "Cấp độ: " + PersistentLevel.Current;
            currencyText.gameObject.SetActive(true);
        }
    }

    private void BuildEscMenu()
    {
        escMenuPanel = CreatePanel(root, "EscMenuPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 480f), new Vector2(0.5f, 0.5f), new Color(0.02f, 0.02f, 0.02f, 0.94f));
        escMenuPanel.SetAsLastSibling();

        TMP_Text title = CreateText(escMenuPanel, "EscTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(360f, 50f), new Vector2(0.5f, 1f), 34f, new Color(1f, 0.93f, 0.62f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
        title.text = "TÙY CHỌN";

        CreateButton(escMenuPanel, "ResumeButton", "TIẾP TỤC", new Vector2(0f, 120f), new Vector2(310f, 58f), ToggleEscMenu, new Color(0.62f, 0.88f, 0.9f, 0.98f), 24f);
        CreateButton(escMenuPanel, "MainMenuButton", "VỀ MENU", new Vector2(0f, 40f), new Vector2(310f, 58f), BackToMainMenu, new Color(0.9f, 0.82f, 0.58f, 0.98f), 24f);
        CreateButton(escMenuPanel, "QuitButton", "THOÁT", new Vector2(0f, -40f), new Vector2(310f, 58f), QuitGame, new Color(0.75f, 0.28f, 0.24f, 0.98f), 24f);

        CreateButton(escMenuPanel, "DevLevelUpButton", "DEV: THÊM CẤP ĐỘ", new Vector2(0f, -120f), new Vector2(310f, 40f), () => { PersistentLevel.AddLevel(1); RebuildItemList(); }, new Color(0.3f, 0.8f, 0.3f, 0.98f), 18f);
        CreateButton(escMenuPanel, "DevLevelResetButton", "DEV: RESET CẤP ĐỘ", new Vector2(0f, -170f), new Vector2(310f, 40f), () => { PersistentLevel.ResetLevel(); RebuildItemList(); }, new Color(0.8f, 0.3f, 0.3f, 0.98f), 18f);

        escMenuPanel.gameObject.SetActive(false);
    }

    private void ToggleEscMenu()
    {
        if (escMenuPanel == null)
        {
            BuildRuntimeUi();
        }

        if (escMenuPanel != null)
        {
            escMenuPanel.gameObject.SetActive(!escMenuPanel.gameObject.activeSelf);
            escMenuPanel.SetAsLastSibling();
        }
    }

    private void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void UpdateTabVisuals()
    {
        SetButtonColor(equipmentTabButton, true, new Color(1f, 0.72f, 0.14f, 0.98f), new Color(0.62f, 0.88f, 0.9f, 0.98f));
        SetButtonColor(mapTabButton, false, new Color(1f, 0.72f, 0.14f, 0.98f), new Color(0.62f, 0.88f, 0.9f, 0.98f));
    }

    private void UpdateFilterVisuals()
    {
        SetFilterButtonState(characterFilterButton, HasItemsForFilter(CategoryFilter.Character), currentFilter == CategoryFilter.Character);
        SetFilterButtonState(mountFilterButton, HasItemsForFilter(CategoryFilter.Mount), currentFilter == CategoryFilter.Mount);
        SetFilterButtonState(weaponFilterButton, HasItemsForFilter(CategoryFilter.Weapon), currentFilter == CategoryFilter.Weapon);
    }

    private void EnsureAvailableFilter()
    {
        if (HasItemsForFilter(currentFilter))
        {
            return;
        }

        if (HasItemsForFilter(CategoryFilter.Character))
        {
            currentFilter = CategoryFilter.Character;
        }
        else if (HasItemsForFilter(CategoryFilter.Mount))
        {
            currentFilter = CategoryFilter.Mount;
        }
        else if (HasItemsForFilter(CategoryFilter.Weapon))
        {
            currentFilter = CategoryFilter.Weapon;
        }
    }

    private bool HasItemsForFilter(CategoryFilter filter)
    {
        if (InventoryManager.Instance == null)
        {
            return false;
        }

        foreach (GameItemData item in InventoryManager.Instance.allItems)
        {
            if (filter == CategoryFilter.Character && item.category == InventoryManager.ItemCategory.Character)
            {
                return true;
            }

            if (filter == CategoryFilter.Mount && item.category == InventoryManager.ItemCategory.Mount)
            {
                return true;
            }

            if (filter == CategoryFilter.Weapon && item.category == InventoryManager.ItemCategory.Weapon)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetCategoryLabel(InventoryManager.ItemCategory category)
    {
        return category switch
        {
            InventoryManager.ItemCategory.Character => "Nhân vật",
            InventoryManager.ItemCategory.Mount => "Thú cưỡi",
            InventoryManager.ItemCategory.Weapon => "Vũ khí",
            _ => category.ToString()
        };
    }

    private static void SetFilterButtonState(Button button, bool available, bool active)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = available;

        Color activeColor = new Color(1f, 0.72f, 0.14f, 0.98f);
        Color inactiveColor = new Color(0.9f, 0.82f, 0.58f, 0.98f);
        Color unavailableColor = new Color(0.38f, 0.35f, 0.26f, 0.72f);
        SetButtonColor(button, active, activeColor, available ? inactiveColor : unavailableColor);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = available ? new Color(0.08f, 0.07f, 0.04f, 1f) : new Color(0.72f, 0.68f, 0.56f, 0.9f);
        }
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        return CreatePanel(parent, name, anchorMin, anchorMax, position, size, pivot, new Color(0.03f, 0.035f, 0.035f, 0.93f));
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot, Color color)
    {
        RectTransform rect = CreateRect(parent, name);
        SetRect(rect, anchorMin, anchorMax, position, size, pivot);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.86f, 0.42f, 0.42f);
        outline.effectDistance = new Vector2(2f, -2f);
        return rect;
    }

    private static Button CreateSmallButton(Transform parent, string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        return CreateButton(parent, name, text, position, new Vector2(118f, 42f), onClick, new Color(0.9f, 0.82f, 0.58f, 0.98f), 12f);
    }

    private static Button CreateLargeButton(Transform parent, string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        return CreateButton(parent, name, text, position, new Vector2(400f, 66f), onClick, new Color(0.28f, 0.58f, 0.28f, 0.96f), 27f, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
    }

    private static Button CreateNavButton(Transform parent, string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        return CreateButton(parent, name, text, position, new Vector2(300f, 58f), onClick, new Color(0.62f, 0.88f, 0.9f, 0.98f), 22f, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick, Color color, float fontSize = 22f)
    {
        return CreateButton(parent, name, text, position, size, onClick, color, fontSize, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick, Color color, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        RectTransform rect = CreateRect(parent, name);
        SetRect(rect, anchorMin, anchorMax, position, size, pivot);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        TMP_Text label = CreateText(rect, "Label", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), fontSize, new Color(0.08f, 0.08f, 0.08f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
        label.text = text;
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot, float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(parent, name);
        SetRect(rect, anchorMin, anchorMax, position, size, pivot);
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.margin = Vector4.zero;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    private static void SetAnchorBox(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static void SetButtonColor(Button button, bool active, Color activeColor, Color inactiveColor)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = active ? activeColor : inactiveColor;
        }
    }

    private static void StyleIconButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.05f, 0.045f, 0.025f, 0.96f);
        }

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(1f, 0.86f, 0.26f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = new Color(1f, 0.92f, 0.36f, 1f);
            label.fontSize = 36f;
        }
    }

    private static GameObject FindDirectOrGlobal(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
        {
            if (transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static Canvas FindShopCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas fallback = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "ShopCanvas")
            {
                return canvas;
            }

            if (
                fallback == null
                && canvas.isRootCanvas
                && canvas.name != "GameplayPauseCanvas"
                && canvas.name != "MainMenuCanvas"
            )
            {
                fallback = canvas;
            }
        }

        return fallback;
    }

    private static RectTransform FindRect(Transform root, string objectName)
    {
        Transform found = FindChild(root, objectName);
        return found != null ? found.GetComponent<RectTransform>() : null;
    }

    private static TMP_Text FindText(Transform root, string objectName)
    {
        Transform found = FindChild(root, objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private static Button FindButton(Transform root, string objectName)
    {
        Transform found = FindChild(root, objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }
}
