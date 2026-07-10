using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Listens for XPManager.OnLevelUp, freezes time, and shows 3 upgrade cards.
/// The player picks one, game resumes.
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    // ---- Colors ----
    private static readonly Color OverlayColor  = new Color(0.02f, 0.02f, 0.03f, 0.92f); // Glassmorphism overlay
    private static readonly Color TitleColor    = new Color(0.92f, 0.84f, 0.64f, 1.00f); // Dong Son bronze gold
    private static readonly Color SubtitleColor = new Color(0.80f, 0.78f, 0.72f, 1.00f);

    // Stat card (Sleek Cyan Tech theme)
    private static readonly Color StatBg     = new Color(0.04f, 0.07f, 0.08f, 0.98f);
    private static readonly Color StatBorder = new Color(0.20f, 0.75f, 0.95f, 1.00f);
    private static readonly Color StatTitle  = new Color(0.40f, 0.85f, 1.00f, 1.00f);

    // Skill card (Fiery Golden Amber theme)
    private static readonly Color SkillBg     = new Color(0.12f, 0.07f, 0.05f, 0.98f);
    private static readonly Color SkillBorder = new Color(1.00f, 0.45f, 0.10f, 1.00f);
    private static readonly Color SkillTitle  = new Color(1.00f, 0.70f, 0.20f, 1.00f);

    // Legend card (Mythic Gold theme)
    private static readonly Color LegendBg       = new Color(0.10f, 0.08f, 0.04f, 0.98f);
    private static readonly Color LegendBorder   = new Color(1.00f, 0.85f, 0.20f, 1.00f);
    private static readonly Color LegendTitle    = new Color(1.00f, 0.90f, 0.40f, 1.00f);
    private static readonly Color LegendEvoBorder = new Color(0.90f, 0.20f, 0.90f, 1.00f);
    private static readonly Color LegendEvoTitle  = new Color(1.00f, 0.40f, 1.00f, 1.00f);

    private Canvas     _canvas;
    private GameObject _panel;
    private bool       _isShowing;

    private void Start()
    {
        XPManager.OnLevelUp += OnLevelUp;
        BuildCanvas();
    }

    private void OnDestroy()
    {
        XPManager.OnLevelUp -= OnLevelUp;
    }

    // ---- Event ----
    private void OnLevelUp()
    {
        if (_isShowing) return;
        _isShowing    = true;
        Time.timeScale = 0f;
        BuildPanel();
        _panel.SetActive(true);
    }

    // ---- Canvas ----
    // ---- Canvas ----
    private void BuildCanvas()
    {
        GameObject cgo = new GameObject("LevelUpCanvas");
        _canvas = cgo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        CanvasScaler cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        cgo.AddComponent<GraphicRaycaster>();

        // --- TỰ ĐỘNG KHỞI TẠO EVENT SYSTEM TƯƠNG THÍCH INPUT SYSTEM MỚI ---
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Sử dụng module tương thích với New Input System
            eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }    // ---- Panel ----
    private void BuildPanel()
    {
        if (_panel != null) Destroy(_panel);

        _panel = new GameObject("LevelUpPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        RectTransform pr = _panel.AddComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        _panel.AddComponent<Image>().color = OverlayColor;

        // Level badge
        int level = XPManager.Instance != null ? XPManager.Instance.CurrentLevel : 1;
        AddText(_panel.transform, $"✦  LEVEL {level}  ✦",
            new Vector2(0.1f, 0.80f), new Vector2(0.9f, 0.96f),
            84, FontStyle.Bold, TitleColor, TextAnchor.MiddleCenter);

        AddText(_panel.transform, "Choose an upgrade",
            new Vector2(0.1f, 0.71f), new Vector2(0.9f, 0.82f),
            38, FontStyle.Normal, SubtitleColor, TextAnchor.MiddleCenter);

        // Three cards
        List<UpgradeOption> opts = UpgradeManager.Instance.GetRandomThreeUpgrades();
        float cardW   = 0.25f;
        float gap     = 0.035f;
        float total   = cardW * 3 + gap * 2;
        float startX  = (1f - total) / 2f;

        for (int i = 0; i < Mathf.Min(opts.Count, 3); i++)
        {
            float x0 = startX + i * (cardW + gap);
            float x1 = x0 + cardW;
            BuildCard(_panel.transform, opts[i], x0, x1);
        }
    }

    // ---- Card ----
    private void BuildCard(Transform parent, UpgradeOption opt, float xMin, float xMax)
    {
        bool isSkill   = opt.type == UpgradeType.Skill1 || opt.type == UpgradeType.Skill2;
        bool isLegend  = opt.legendSystem != LegendSystemType.None;
        bool isEvo     = opt.isEvolution;

        Color bg       = isLegend ? LegendBg : (isSkill ? SkillBg     : StatBg);
        Color border   = isLegend ? (isEvo ? LegendEvoBorder : LegendBorder) : (isSkill ? SkillBorder : StatBorder);
        Color titleCol = isLegend ? (isEvo ? LegendEvoTitle : LegendTitle) : (isSkill ? SkillTitle  : StatTitle);

        // Outer border rectangle
        GameObject borderGO = new GameObject("Card_" + opt.type);
        borderGO.transform.SetParent(parent, false);
        RectTransform bRect = borderGO.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(xMin, 0.18f);
        bRect.anchorMax = new Vector2(xMax, 0.72f);
        bRect.offsetMin = bRect.offsetMax = Vector2.zero;
        Image bImg = borderGO.AddComponent<Image>();
        bImg.color = border;

        // Inner card
        GameObject cardGO = new GameObject("Inner");
        cardGO.transform.SetParent(borderGO.transform, false);
        RectTransform cRect = cardGO.AddComponent<RectTransform>();
        cRect.anchorMin = Vector2.zero;
        cRect.anchorMax = Vector2.one;
        cRect.offsetMin = new Vector2(4f, 4f);
        cRect.offsetMax = new Vector2(-4f, -4f);
        cardGO.AddComponent<Image>().color = bg;

        // Title
        AddText(cardGO.transform, opt.title,
            new Vector2(0f, 0.68f), new Vector2(1f, 1f),
            isSkill ? 34 : 36, FontStyle.Bold, titleCol,
            TextAnchor.UpperCenter, wrap: true, padding: 10f);

        // Stars (skills only)
        if (opt.maxLevel > 0)
        {
            string stars = "";
            for (int s = 0; s < opt.maxLevel; s++)
                stars += s < opt.currentLevel ? "★" : "☆";
            AddText(cardGO.transform, stars,
                new Vector2(0f, 0.57f), new Vector2(1f, 0.71f),
                28, FontStyle.Normal, new Color(1f, 0.85f, 0.1f),
                TextAnchor.MiddleCenter);
        }

        // Legend Subtitle
        float descTop = 0.57f;
        if (isLegend && !string.IsNullOrEmpty(opt.legendSubtitle))
        {
            AddText(cardGO.transform, opt.legendSubtitle,
                new Vector2(0f, 0.45f), new Vector2(1f, 0.53f),
                22, FontStyle.Italic, new Color(0.9f, 0.7f, 0.3f),
                TextAnchor.MiddleCenter);
            descTop = 0.45f;
        }

        // Icon
        if (opt.icon != null)
        {
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(cardGO.transform, false);
            RectTransform iRect = iconGO.AddComponent<RectTransform>();
            // Place icon on the left side of the description area
            iRect.anchorMin = new Vector2(0.05f, 0.05f);
            iRect.anchorMax = new Vector2(0.35f, descTop - 0.05f);
            iRect.offsetMin = iRect.offsetMax = Vector2.zero;
            Image img = iconGO.AddComponent<Image>();
            img.sprite = opt.icon;
            img.preserveAspect = true;

            // Description on the right side
            AddText(cardGO.transform, opt.description,
                new Vector2(0.38f, 0.03f), new Vector2(0.95f, descTop),
                22, FontStyle.Normal, new Color(0.72f, 0.72f, 0.82f),
                TextAnchor.MiddleLeft, wrap: true, padding: 0f);
        }
        else
        {
            // Full width description
            AddText(cardGO.transform, opt.description,
                new Vector2(0f, 0.03f), new Vector2(1f, descTop),
                26, FontStyle.Normal, new Color(0.72f, 0.72f, 0.82f),
                TextAnchor.UpperCenter, wrap: true, padding: 12f);
        }

        // Click button over the whole border
        Button btn = borderGO.AddComponent<Button>();
        var cols = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
        cols.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cols;
        btn.targetGraphic = bImg;

        UpgradeOption captured = opt;
        btn.onClick.AddListener(() => SelectUpgrade(captured));
    }

    private void SelectUpgrade(UpgradeOption opt)
    {
        UpgradeManager.Instance.ApplyUpgrade(opt);
        _panel.SetActive(false);
        Time.timeScale = 1f;
        _isShowing     = false;
        if (XPManager.Instance != null) XPManager.Instance.ResumeFromLevelUp();
    }

    // ---- Text helper ----
    private static Text AddText(
        Transform parent,
        string content,
        Vector2 anchorMin, Vector2 anchorMax,
        int fontSize,
        FontStyle style,
        Color color,
        TextAnchor alignment,
        bool wrap    = false,
        float padding = 0f)
    {
        GameObject go = new GameObject("Txt_" + content.Substring(0, Mathf.Min(8, content.Length)));
        go.transform.SetParent(parent, false);
        Text txt = go.AddComponent<Text>();
        txt.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize   = fontSize;
        txt.fontStyle  = style;
        txt.color      = color;
        txt.alignment  = alignment;
        txt.text       = content;
        if (wrap)
        {
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(padding, 0f);
        rt.offsetMax = new Vector2(-padding, 0f);
        return txt;
    }
}
