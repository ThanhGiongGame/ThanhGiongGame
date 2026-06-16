using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSelectionUI : MonoBehaviour
{
    private GameObject _panelInstance;
    private Action _onCloseCallback;
    private TMP_FontAsset _customFont;

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
        _panelInstance = CreatePanel(canvasTransform, Vector2.zero, new Vector2(1000f, 620f), new Color(0.04f, 0.06f, 0.07f, 0.98f));
        _panelInstance.name = "MapSelectionOverlayPanel";

        // Thêm viền vàng Đông Sơn cổ kính
        GameObject outline = CreatePanel(_panelInstance.transform, Vector2.zero, new Vector2(1000f, 620f), new Color(0.92f, 0.82f, 0.55f, 0.08f));
        outline.transform.SetAsFirstSibling();
        outline.name = "GoldOutlineBorder";

        // 2. Tiêu đề
        CreateText(_panelInstance.transform, "― CỔ KÍNH ẢI MÀN CHƠI ―", new Vector2(0f, 240f), 30, FontStyle.Bold, new Color(0.92f, 0.82f, 0.55f));

        // 3. Nội dung 3 bản đồ
        RefreshPage();

        // 4. Nút Đóng / Quay Lại
        CreateButton(_panelInstance.transform, "↩  LUI VỀ", new Vector2(0f, -240f), new Vector2(240f, 55f), new Color(0.2f, 0.22f, 0.25f), () =>
        {
            Hide();
            _onCloseCallback?.Invoke();
        });
    }

    private void RefreshPage()
    {
        // Xóa các dòng map cũ nếu có (ngoại trừ tiêu đề, viền và nút đóng)
        foreach (Transform child in _panelInstance.transform)
        {
            if (child.name.StartsWith("MapRow_"))
            {
                Destroy(child.gameObject);
            }
        }

        string[] mapNames = { "● Ải Thạch Thất (Mặc Định)", "● Ải Trâu Sơn (Chiến Trường Lửa)", "● Rừng U Minh (Đêm Trúc Linh)" };
        string[] mapDescs = {
            "Vùng thung lũng xanh mướt rợp bóng tre ngà thanh bình.\nNơi bắt đầu cuộc hành trình dẹp giặc Ân cứu nước của Thánh Gióng.",
            "Chiến trường đất đỏ khô cằn phủ đầy sương khói hoàng hôn cam cháy.\nCác ngọn đuốc cháy rực thiêu rụi và đẩy lùi quân thù.",
            "Rừng tre cổ kính huyền bí chìm sâu trong đêm tối cô quạnh.\nÁnh trăng huyền ảo cùng các cột pha lê phát sáng linh thiêng."
        };
        int selectedMap = PlayerPrefs.GetInt("SelectedMap", 0);

        // Sinh 3 dòng tương ứng 3 ải bản đồ
        for (int i = 0; i < 3; i++)
        {
            CreateMapRow(i, mapNames[i], mapDescs[i], selectedMap == i, new Vector2(0f, 120f - (i * 120f)));
        }
    }

    private void CreateMapRow(int index, string mapName, string mapDesc, bool isSelected, Vector2 pos)
    {
        GameObject row = CreatePanel(_panelInstance.transform, pos, new Vector2(920f, 105f), new Color(0.08f, 0.10f, 0.11f, 1f));
        row.name = $"MapRow_{index}";

        // Viền của dòng
        GameObject rowOutline = CreatePanel(row.transform, Vector2.zero, new Vector2(920f, 105f), new Color(0.92f, 0.82f, 0.55f, 0.05f));
        rowOutline.transform.SetAsFirstSibling();

        // Text Group chứa tên và mô tả
        GameObject textGroup = new GameObject("TextGroup");
        textGroup.transform.SetParent(row.transform, false);
        RectTransform groupRt = textGroup.AddComponent<RectTransform>();
        groupRt.anchorMin = new Vector2(0f, 0.5f);
        groupRt.anchorMax = new Vector2(0f, 0.5f);
        groupRt.pivot = new Vector2(0f, 0.5f);
        groupRt.anchoredPosition = new Vector2(25f, 0f);
        groupRt.sizeDelta = new Vector2(620f, 90f);

        // Tên bản đồ
        TMP_Text titleText = CreateText(textGroup.transform, mapName, new Vector2(0f, 18f), 20, FontStyle.Bold, new Color(0.92f, 0.82f, 0.55f));
        titleText.alignment = TextAlignmentOptions.Left;
        RectTransform titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0.5f); titleRt.anchorMax = new Vector2(1f, 0.5f);
        titleRt.pivot = new Vector2(0f, 0.5f); titleRt.sizeDelta = new Vector2(0f, 30f);

        // Mô tả bản đồ
        TMP_Text descText = CreateText(textGroup.transform, mapDesc, new Vector2(0f, -18f), 14, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
        descText.alignment = TextAlignmentOptions.Left;
        RectTransform descRt = descText.rectTransform;
        descRt.anchorMin = new Vector2(0f, 0.5f); descRt.anchorMax = new Vector2(1f, 0.5f);
        descRt.pivot = new Vector2(0f, 0.5f); descRt.sizeDelta = new Vector2(0f, 40f);

        // Nút bấm chọn bản đồ
        string btnLabel = isSelected ? "⚡ ĐÃ CHỌN" : "CHỌN ẢI";
        Button btn = CreateButton(row.transform, btnLabel, new Vector2(340f, 0f), new Vector2(180f, 50f), Color.gray, null);
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
            TMP_Text txt = CreateText(go.transform, label, Vector2.zero, 24, FontStyle.Bold, Color.white);
            RectTransform txtRt = txt.rectTransform;
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
        }

        return btn;
    }
}
