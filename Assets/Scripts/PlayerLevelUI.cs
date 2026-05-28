using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates and manages the player XP/Level bar UI entirely at runtime.
/// Automatically places itself directly above the Health Bar HUD.
/// </summary>
public class PlayerLevelUI : MonoBehaviour
{
    private Image _bgBar;
    private Image _fillBar;
    private Text _levelText;
    private Text _xpText;

    // Cosmic theme colors to differentiate from HP
    private static readonly Color ColorBg = new Color(0.06f, 0.05f, 0.10f, 0.85f);
    private static readonly Color ColorFill = new Color(0.20f, 0.75f, 0.95f, 1.00f); // Cyan/Teal glow
    private static readonly Color ColorBorder = new Color(0.0f, 0.0f, 0.0f, 0.90f);
    private static readonly Color ColorText = new Color(0.90f, 0.85f, 1.00f, 1.00f); // Soft violet white

    private void Start()
    {
        // Listen to XP updates
        XPManager.OnXPChanged += UpdateUI;

        // Give the UI generation a brief frame delay to ensure the HealthBarCanvas is initialized
        StartCoroutine(DelayedBuild());
    }

    private void OnDestroy()
    {
        XPManager.OnXPChanged -= UpdateUI;
    }

    private System.Collections.IEnumerator DelayedBuild()
    {
        yield return null;
        BuildUI();

        // Push initial values manually right after building
        if (XPManager.Instance != null)
        {
            UpdateUI(XPManager.Instance.CurrentXP, XPManager.Instance.XPForNextLevel, XPManager.Instance.CurrentLevel);
        }
    }

    private void UpdateUI(float currentXP, float xpNeeded, int currentLevel)
    {
        if (_fillBar == null) return;

        float pct = Mathf.Clamp01(currentXP / xpNeeded);
        _fillBar.fillAmount = pct;

        if (_levelText != null)
        {
            _levelText.text = $"LV {currentLevel}";
        }

        if (_xpText != null)
        {
            _xpText.text = $"{Mathf.FloorToInt(currentXP)} / {Mathf.FloorToInt(xpNeeded)} XP";
        }
    }

    private void BuildUI()
    {
        // Locate the Canvas generated dynamically by PlayerHealthUI
        GameObject canvasGO = GameObject.Find("HealthBarCanvas");

        if (canvasGO == null)
        {
            Debug.LogWarning("PlayerLevelUI: Could not find 'HealthBarCanvas' in scene. Ensure PlayerHealthUI runs first.");
            return;
        }

        // ---- Outer panel (border) ----
        RectTransform border = CreateRect("XPBorder", canvasGO.transform);
        Image borderImg = border.gameObject.AddComponent<Image>();
        borderImg.color = ColorBorder;

        // Position: Anchored bottom-left, placed exactly ABOVE the HP bar (HP is at Y:30 with H:100, so we start at Y:140)
        border.anchorMin = new Vector2(0f, 0f);
        border.anchorMax = new Vector2(0f, 0f);
        border.pivot = new Vector2(0f, 0f);
        border.anchoredPosition = new Vector2(30f, 140f);
        border.sizeDelta = new Vector2(900f, 40f); // Sleeker, thinner design profile than the HP bar

        // ---- Background ----
        RectTransform bg = CreateRect("XPBackground", border);
        _bgBar = bg.gameObject.AddComponent<Image>();
        _bgBar.color = ColorBg;
        StretchFill(bg, 2f); // 2px inset

        // ---- Fill bar ----
        RectTransform fillRect = CreateRect("XPFill", border);
        _fillBar = fillRect.gameObject.AddComponent<Image>();
        _fillBar.color = ColorFill;
        _fillBar.type = Image.Type.Filled;
        _fillBar.fillMethod = Image.FillMethod.Horizontal;
        _fillBar.fillAmount = 0f;
        StretchFill(fillRect, 2f);

        // ---- Level Text Label (Left side) ----
        GameObject lvGO = new GameObject("XPLevelLabel");
        lvGO.transform.SetParent(border, false);
        _levelText = lvGO.AddComponent<Text>();
        _levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _levelText.fontSize = 26;
        _levelText.fontStyle = FontStyle.Bold;
        _levelText.color = ColorText;
        _levelText.alignment = TextAnchor.MiddleLeft;
        _levelText.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform lvRect = lvGO.GetComponent<RectTransform>();
        lvRect.anchorMin = Vector2.zero;
        lvRect.anchorMax = Vector2.one;
        lvRect.offsetMin = new Vector2(15f, 0f); // Small padding offset from left wall
        lvRect.offsetMax = Vector2.zero;

        // ---- Numeric XP Counter (Right side) ----
        GameObject numericGO = new GameObject("XPNumericLabel");
        numericGO.transform.SetParent(border, false);
        _xpText = numericGO.AddComponent<Text>();
        _xpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _xpText.fontSize = 22;
        _xpText.fontStyle = FontStyle.Normal;
        _xpText.color = ColorText;
        _xpText.alignment = TextAnchor.MiddleRight;
        _xpText.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform numRect = numericGO.GetComponent<RectTransform>();
        numRect.anchorMin = Vector2.zero;
        numRect.anchorMax = Vector2.one;
        numRect.offsetMin = Vector2.zero;
        numRect.offsetMax = new Vector2(-15f, 0f); // Small padding offset from right wall
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    private void StretchFill(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}