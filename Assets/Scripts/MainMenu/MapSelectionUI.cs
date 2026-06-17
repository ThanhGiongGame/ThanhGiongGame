using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSelectionUI : MonoBehaviour
{
    private GameObject _panelInstance;
    private Action _onCloseCallback;
    private TMP_FontAsset _customFont;

    // Unlock conditions
    private static readonly int[] REQUIRED_VINHDAN = { 0, 500, 1500 };
    private static readonly int[] REQUIRED_KILLS = { 0, 50, 150 };

    public void Initialize(Action onClose)
    {
        _onCloseCallback = onClose;

        // Cố gắng lấy font từ các text hiện có để đồng bộ giao diện
        var existingText = FindObjectOfType<TMP_Text>();
        if (existingText != null)
        {
            _customFont = existingText.font;
        }

        BuildPanel();
    }

    public void Show()
    {
        if (_panelInstance != null)
        {
            _panelInstance.SetActive(true);
            RefreshPage();
        }
    }

    public void Hide()
    {
        if (_panelInstance != null)
        {
            _panelInstance.SetActive(false);
        }
    }

    private void BuildPanel()
    {
        Transform canvasTransform = transform;

        // 1. Tạo Panel chính (Modal Container)
        _panelInstance = CreatePanel(canvasTransform, Vector2.zero, new Vector2(1050f, 680f), new Color(0.06f, 0.08f, 0.10f, 0.98f));
        _panelInstance.name = "MapSelectionOverlayPanel";

        // Viền vàng cổ kính
        GameObject outline = CreatePanel(_panelInstance.transform, Vector2.zero, new Vector2(1050f, 680f), new Color(0.85f, 0.72f, 0.42f, 0.06f));
        outline.transform.SetAsFirstSibling();
        outline.name = "GoldOutlineBorder";

        // 2. Tiêu đề
        CreateText(_panelInstance.transform, "― CHỌN BẢN ĐỒ ―", new Vector2(0f, 270f), 32, FontStyle.Bold, new Color(0.90f, 0.80f, 0.50f));

        // 3. Nội dung 3 bản đồ
        RefreshPage();

        // 4. Nút Đóng / Quay Lại
        CreateButton(_panelInstance.transform, "↩  QUAY LẠI", new Vector2(0f, -280f), new Vector2(250f, 55f), new Color(0.22f, 0.24f, 0.28f), () =>
        {
            Hide();
            _onCloseCallback?.Invoke();
        });
    }

    private void RefreshPage()
    {
        // Xóa các dòng map cũ nếu có
        foreach (Transform child in _panelInstance.transform)
        {
            if (child.name.StartsWith("MapRow_"))
            {
                Destroy(child.gameObject);
            }
        }

        string[] mapNames = {
            "● Trong Làng (Nhà Lá)",
            "● Đồng Bằng (Chiến Trường)",
            "● Rừng Tre (Bamboo Forest)"
        };
        string[] mapDescs = {
            "Đồng quê Việt Nam mộc mạc với những mái nhà lá đơn sơ, cây cối xanh mát và ruộng vườn thanh bình.",
            "Nơi lính ngoại xâm kéo vào đánh phá làng quê đồng bằng hiểm nguy, đầy vách đá dựng đứng, cây cối và đuốc cháy.",
            "Rừng tre sâu thẳm rậm rạp rợp bóng tre xanh mát, phủ đầy cỏ dại rừng già cùng đom đóm huyền ảo dưới trăng."
        };
        int selectedMap = PlayerPrefs.GetInt("SelectedMap", 0);
        int playerVinhDanh = PlayerPrefs.GetInt("VinhDanhTotal", 0);
        int playerTotalKills = PlayerPrefs.GetInt("TotalEnemiesKilled", 0);

        for (int i = 0; i < 3; i++)
        {
            bool unlocked = IsMapUnlocked(i, playerVinhDanh, playerTotalKills);
            string unlockInfo = GetUnlockText(i, playerVinhDanh, playerTotalKills);
            CreateMapRow(i, mapNames[i], mapDescs[i], selectedMap == i, unlocked, unlockInfo,
                new Vector2(0f, 140f - (i * 135f)));
        }
    }

    private bool IsMapUnlocked(int mapIndex, int vinhDanh, int totalKills)
    {
        if (mapIndex == 0) return true; // Map mặc định
        return vinhDanh >= REQUIRED_VINHDAN[mapIndex] || totalKills >= REQUIRED_KILLS[mapIndex];
    }

    private string GetUnlockText(int mapIndex, int vinhDanh, int totalKills)
    {
        if (mapIndex == 0) return "";
        if (IsMapUnlocked(mapIndex, vinhDanh, totalKills)) return "✅ ĐÃ MỞ KHÓA";

        int reqVD = REQUIRED_VINHDAN[mapIndex];
        int reqKill = REQUIRED_KILLS[mapIndex];
        return $"🔒 Cần {vinhDanh}/{reqVD} Vinh Danh  hoặc  {totalKills}/{reqKill} Quái";
    }

    private void CreateMapRow(int index, string mapName, string mapDesc, bool isSelected, bool isUnlocked, string unlockInfo, Vector2 pos)
    {
        // Màu nền dòng
        Color rowBg = isUnlocked
            ? new Color(0.08f, 0.10f, 0.12f, 1f)
            : new Color(0.06f, 0.06f, 0.07f, 1f);

        GameObject row = CreatePanel(_panelInstance.transform, pos, new Vector2(960f, 120f), rowBg);
        row.name = $"MapRow_{index}";

        // Viền nhẹ
        GameObject rowOutline = CreatePanel(row.transform, Vector2.zero, new Vector2(960f, 120f), new Color(0.85f, 0.72f, 0.42f, 0.04f));
        rowOutline.transform.SetAsFirstSibling();

        // Text Group chứa tên và mô tả
        GameObject textGroup = new GameObject("TextGroup");
        textGroup.transform.SetParent(row.transform, false);
        RectTransform groupRt = textGroup.AddComponent<RectTransform>();
        groupRt.anchorMin = new Vector2(0f, 0.5f);
        groupRt.anchorMax = new Vector2(0f, 0.5f);
        groupRt.pivot = new Vector2(0f, 0.5f);
        groupRt.anchoredPosition = new Vector2(25f, 5f);
        groupRt.sizeDelta = new Vector2(620f, 100f);

        // Tên bản đồ
        Color titleColor = isUnlocked
            ? new Color(0.90f, 0.80f, 0.50f)
            : new Color(0.50f, 0.45f, 0.35f);

        TMP_Text titleText = CreateText(textGroup.transform, mapName, new Vector2(0f, 22f), 20, FontStyle.Bold, titleColor);
        titleText.alignment = TextAlignmentOptions.Left;
        RectTransform titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0.5f); titleRt.anchorMax = new Vector2(1f, 0.5f);
        titleRt.pivot = new Vector2(0f, 0.5f); titleRt.sizeDelta = new Vector2(0f, 30f);

        // Mô tả bản đồ
        Color descColor = isUnlocked
            ? new Color(0.75f, 0.75f, 0.75f)
            : new Color(0.40f, 0.40f, 0.40f);

        TMP_Text descText = CreateText(textGroup.transform, mapDesc, new Vector2(0f, -12f), 13, FontStyle.Normal, descColor);
        descText.alignment = TextAlignmentOptions.Left;
        RectTransform descRt = descText.rectTransform;
        descRt.anchorMin = new Vector2(0f, 0.5f); descRt.anchorMax = new Vector2(1f, 0.5f);
        descRt.pivot = new Vector2(0f, 0.5f); descRt.sizeDelta = new Vector2(0f, 40f);

        // Thông tin mở khóa (nếu có)
        if (!string.IsNullOrEmpty(unlockInfo))
        {
            Color unlockColor = isUnlocked
                ? new Color(0.3f, 0.75f, 0.4f)
                : new Color(0.85f, 0.45f, 0.25f);
            TMP_Text unlockText = CreateText(textGroup.transform, unlockInfo, new Vector2(0f, -38f), 12, FontStyle.Normal, unlockColor);
            unlockText.alignment = TextAlignmentOptions.Left;
            RectTransform unlockRt = unlockText.rectTransform;
            unlockRt.anchorMin = new Vector2(0f, 0.5f); unlockRt.anchorMax = new Vector2(1f, 0.5f);
            unlockRt.pivot = new Vector2(0f, 0.5f); unlockRt.sizeDelta = new Vector2(0f, 22f);
        }

        // Nút bấm
        if (isUnlocked)
        {
            string btnLabel = isSelected ? "⚡ ĐÃ CHỌN" : "CHỌN ẢI";
            Button btn = CreateButton(row.transform, btnLabel, new Vector2(370f, 0f), new Vector2(180f, 50f), Color.gray, null);
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
            Image btnImg = btn.GetComponent<Image>();

            if (isSelected)
            {
                if (btnText != null) btnText.color = Color.white;
                btnImg.color = new Color(0.15f, 0.50f, 0.35f, 1f);
            }
            else
            {
                if (btnText != null) btnText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                btnImg.color = new Color(0.22f, 0.25f, 0.28f, 1f);
            }

            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[MapSelectionUI] Chọn bản đồ: {index}");
                PlayerPrefs.SetInt("SelectedMap", index);
                PlayerPrefs.Save();
                RefreshPage();
            });
        }
        else
        {
            // Map bị khóa
            Button btn = CreateButton(row.transform, "🔒 CHƯA MỞ", new Vector2(370f, 0f), new Vector2(180f, 50f), Color.gray, null);
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
            Image btnImg = btn.GetComponent<Image>();
            if (btnText != null) btnText.color = new Color(0.50f, 0.45f, 0.40f, 1f);
            btnImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            btn.interactable = false;
        }
    }

    private GameObject CreatePanel(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
    }

    private TMP_Text CreateText(Transform parent, string content, Vector2 pos, int fontSize, FontStyle style, Color color)
    {
        GameObject go = new GameObject("TextTMP");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(800f, 80f);

        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        if (_customFont != null)
        {
            txt.font = _customFont;
        }
        txt.text = content;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAlignmentOptions.Center;

        if (style == FontStyle.Bold)
        {
            txt.fontStyle = FontStyles.Bold;
        }
        else if (style == FontStyle.Italic)
        {
            txt.fontStyle = FontStyles.Italic;
        }

        txt.raycastTarget = false;
        return txt;
    }

    private Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color, Action onClick)
    {
        GameObject go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = color;

        Button btn = go.AddComponent<Button>();
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;

        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick.Invoke());
        }

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text txt = CreateText(go.transform, label, Vector2.zero, 22, FontStyle.Bold, Color.white);
            RectTransform txtRt = txt.rectTransform;
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
        }

        return btn;
    }
}
