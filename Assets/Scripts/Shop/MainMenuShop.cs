using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using System;

public class MainMenuShop : MonoBehaviour
{
    private Canvas _canvas;
    private Text _txtCurrency;
    private Transform _contentRoot;

    private enum PageType { Shop, Equipment }
    private PageType _currentPage = PageType.Shop;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetupEventSystem();
        BuildUI();
        RefreshPage();
    }

    private void SetupEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("ShopCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Nền Khói Tre Đậm cổ kính
        GameObject bg = CreateUIObject("Background", _canvas.transform);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.09f, 1f);

        // TopBar
        GameObject topBar = CreateUIObject("TopBar", bg.transform);
        RectTransform topRt = topBar.AddComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0f, 1f); topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f); topRt.sizeDelta = new Vector2(0f, 110f);
        topRt.anchoredPosition = Vector2.zero;
        topBar.AddComponent<Image>().color = new Color(0.09f, 0.12f, 0.14f, 1f);

        CreateText(topBar.transform, "❖  KHO TRANG BỊ ĐÔNG SƠN  ❖", new Vector2(-400f, 0f), 34, FontStyle.Bold, new Color(0.92f, 0.84f, 0.64f));

        _txtCurrency = CreateText(topBar.transform, "", new Vector2(600f, 0f), 26, FontStyle.Bold, new Color(0.92f, 0.84f, 0.64f));
        _txtCurrency.alignment = TextAnchor.MiddleRight;
        RefreshCurrency();

        // Left Menu Panel
        GameObject leftPanel = CreatePanel(bg.transform, new Vector2(200f, -55f), new Vector2(300f, 970f), new Color(0.07f, 0.09f, 0.11f));
        leftPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
        leftPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);

        CreateButton(leftPanel.transform, "🏯  MUA SẮM", new Vector2(0f, 300f), new Vector2(240f, 75f), new Color(0.18f, 0.24f, 0.22f), () => {
            _currentPage = PageType.Shop; RefreshPage();
        });

        CreateButton(leftPanel.transform, "🎒  BÊN TRONG", new Vector2(0f, 190f), new Vector2(240f, 75f), new Color(0.18f, 0.24f, 0.22f), () => {
            _currentPage = PageType.Equipment; RefreshPage();
        });

        CreateButton(leftPanel.transform, "⚔  XUẤT TRẬN", new Vector2(0f, -280f), new Vector2(240f, 85f), new Color(0.55f, 0.2f, 0.2f), () => {
            SceneManager.LoadScene("SampleScene");
        });

        CreateButton(leftPanel.transform, "↩  LUI VỀ", new Vector2(0f, -390f), new Vector2(240f, 65f), new Color(0.2f, 0.22f, 0.25f), () => {
            SceneManager.LoadScene("MainMenuScene");
        });

        // Content Root
        GameObject content = CreateUIObject("ContentRoot", bg.transform);
        RectTransform cRt = content.AddComponent<RectTransform>();
        cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
        cRt.offsetMin = new Vector2(380f, 40f); cRt.offsetMax = new Vector2(-60f, -140f);
        _contentRoot = content.transform;
    }

    private void RefreshPage()
    {
        foreach (Transform child in _contentRoot) Destroy(child.gameObject);

        // Kiểm tra an toàn xem Bộ não quản lý đã được load chưa
        if (InventoryManager.Instance == null) return;

        if (_currentPage == PageType.Shop) BuildShopPage();
        else BuildEquipmentPage();
    }

    private void BuildShopPage()
    {
        CreateText(_contentRoot, "― CỔ VẬT THƯƠNG PHƯỜNG ―", new Vector2(0f, 400f), 30, FontStyle.Bold, new Color(0.7f, 0.75f, 0.7f));

        var items = InventoryManager.Instance.allItems;
        for (int i = 0; i < items.Count; i++)
        {
            CreateShopItemRow(items[i], new Vector2(0f, 260f - (i * 145f)));
        }
    }

    private void CreateShopItemRow(GameItemData item, Vector2 pos)
    {
        GameObject row =
            CreatePanel(
                _contentRoot,
                pos,
                new Vector2(1200f, 120f),
                new Color(0.1f, 0.13f, 0.15f)
            );

        GameObject textGroup =
            CreateUIObject(
                "TextGroup",
                row.transform
            );

        RectTransform groupRt =
            textGroup.AddComponent<RectTransform>();

        groupRt.anchorMin =
            new Vector2(0f, 0.5f);

        groupRt.anchorMax =
            new Vector2(0f, 0.5f);

        groupRt.pivot =
            new Vector2(0f, 0.5f);

        groupRt.anchoredPosition =
            new Vector2(40f, 0f);

        groupRt.sizeDelta =
            new Vector2(800f, 110f);

        Text catText =
            CreateText(
                textGroup.transform,
                $"[{item.category}]",
                new Vector2(0f, 30f),
                18,
                FontStyle.Normal,
                new Color(0.6f, 0.7f, 0.6f)
            );

        catText.alignment =
            TextAnchor.MiddleLeft;

        Text nameText =
            CreateText(
                textGroup.transform,
                item.displayName,
                new Vector2(0f, 0f),
                26,
                FontStyle.Bold,
                Color.white
            );

        nameText.alignment =
            TextAnchor.MiddleLeft;

        Text descText =
            CreateText(
                textGroup.transform,
                item.description,
                new Vector2(0f, -35f),
                18,
                FontStyle.Normal,
                new Color(0.8f, 0.8f, 0.8f)
            );

        descText.alignment =
            TextAnchor.MiddleLeft;

        Button btn =
            CreateButton(
                row.transform,
                "",
                new Vector2(440f, 0f),
                new Vector2(260f, 65f),
                Color.gray,
                null
            );

        Text btnText =
            btn.GetComponentInChildren<Text>();

        Image btnImg =
            btn.GetComponent<Image>();

        if (item.IsOwned())
        {
            btnText.text = "ĐÃ SỞ HỮU";
            btnImg.color = new Color(0.2f, 0.4f, 0.25f);
            btn.interactable = false;
        }
        else
        {
            btnText.text = $"MUA {item.cost} VD";
            btnImg.color = new Color(0.45f, 0.35f, 0.15f);

            btn.onClick.AddListener(() =>
            {
                if (InventoryManager.Instance.BuyItem(item))
                {
                    RefreshCurrency();
                    RefreshPage();
                }
            });
        }
    }
    private void BuildEquipmentPage()
    {
        CreateText(_contentRoot, "― HÀNH TRANG THỰC TRẬN ―", new Vector2(0f, 400f), 30, FontStyle.Bold, new Color(0.7f, 0.75f, 0.7f));

        var items = InventoryManager.Instance.allItems;
        bool hasAnyItem = false;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].IsOwned())
            {
                CreateEquipmentItemRow(
                    items[i],
                    new Vector2(0f, 260f - (i * 145f))
                );

                hasAnyItem = true;
            }
        }

        if (!hasAnyItem)
        {
            CreateText(_contentRoot, "Hành trang trống rỗng. Hãy tới Thương Phường chế tạo vũ khí!", new Vector2(0f, 0f), 24, FontStyle.Normal, Color.gray);
        }
    }

private void CreateEquipmentItemRow(
    GameItemData item,
    Vector2 pos
)
{
    GameObject row =
        CreatePanel(
            _contentRoot,
            pos,
            new Vector2(1200f, 120f),
            new Color(0.1f, 0.13f, 0.15f)
        );

    GameObject textGroup =
        CreateUIObject(
            "TextGroup",
            row.transform
        );

    RectTransform groupRt =
        textGroup.AddComponent<RectTransform>();

    groupRt.anchorMin =
        new Vector2(0f, 0.5f);

    groupRt.anchorMax =
        new Vector2(0f, 0.5f);

    groupRt.pivot =
        new Vector2(0f, 0.5f);

    groupRt.anchoredPosition =
        new Vector2(40f, 0f);

    groupRt.sizeDelta =
        new Vector2(800f, 110f);

    Text catText =
        CreateText(
            textGroup.transform,
            $"[{item.category}]",
            new Vector2(0f, 30f),
            18,
            FontStyle.Normal,
            Color.gray
        );

    catText.alignment =
        TextAnchor.MiddleLeft;

    Text nameText =
        CreateText(
            textGroup.transform,
            item.displayName,
            new Vector2(0f, 0f),
            26,
            FontStyle.Bold,
            Color.white
        );

    nameText.alignment =
        TextAnchor.MiddleLeft;

    Text descText =
        CreateText(
            textGroup.transform,
            item.description,
            new Vector2(0f, -35f),
            18,
            FontStyle.Normal,
            new Color(0.8f, 0.8f, 0.8f)
        );

    descText.alignment =
        TextAnchor.MiddleLeft;

    Button btn =
        CreateButton(
            row.transform,
            "",
            new Vector2(440f, 0f),
            new Vector2(260f, 65f),
            Color.gray,
            null
        );

    Text btnText =
        btn.GetComponentInChildren<Text>();

    Image btnImg =
        btn.GetComponent<Image>();

    if (item.IsEquipped())
    {
        btnText.text = "⚡ ĐANG MANG";
        btnImg.color =
            new Color(
                0.15f,
                0.5f,
                0.35f
            );
    }
    else
    {
        btnText.text = "TRANG BỊ";
        btnImg.color =
            new Color(
                0.22f,
                0.25f,
                0.28f
            );
    }

    btn.onClick.AddListener(() =>
    {
        Debug.Log("CLICK EQUIP: " + item.id);
        InventoryManager.Instance.ToggleEquip(item);
        RefreshPage();
    });
}    private void RefreshCurrency()
    {
        if (InventoryManager.Instance != null)
        {
            _txtCurrency.text = "🧧 TÍCH LŨY: " + InventoryManager.Instance.GetCurrency() + " VINH DANH";
        }
    }

    private GameObject CreateUIObject(string name, Transform parent) => new GameObject(name) { transform = { parent = parent, localPosition = Vector3.zero } };

    private GameObject CreatePanel(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject panel = CreateUIObject("Panel", parent);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        panel.AddComponent<Image>().color = color;
        return panel;
    }

    private Text CreateText(Transform parent, string content, Vector2 pos, int fontSize, FontStyle style, Color color)
    {
        GameObject go = CreateUIObject("Text", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(950f, 85f);
        Text txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content; txt.fontSize = fontSize; txt.fontStyle = style; txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;
        return txt;
    }

    private Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color, Action onClick)
    {
        GameObject go = CreateUIObject("Button", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        Button btn = go.AddComponent<Button>();
        Navigation nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
        if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());
        CreateText(go.transform, label, Vector2.zero, 22, FontStyle.Bold, Color.white);
        return btn;
    }
}