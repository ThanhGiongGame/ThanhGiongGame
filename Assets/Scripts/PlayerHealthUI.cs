using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates and manages the player health bar UI entirely at runtime.
/// Attach this to any persistent GameObject in the scene (e.g. a "UIManager" empty object),
/// or let PlayerHealth create it automatically (see PlayerHealth.cs).
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the PlayerHealth component here, or leave null to auto-find.")]
    public PlayerHealth playerHealth;

    // -------------------------------------------------------
    // UI elements built at runtime
    // -------------------------------------------------------
    private Canvas     _canvas;
    private Image      _bgBar;
    private Image      _fillBar;
    private Image      _delayBar;   // ghost bar that drains slowly for juice
    private Text       _hpText;

    // Delay-bar smoothing
    private float _delayFill = 1f;
    private const float DelaySpeed = 1.5f;

    // Colors
    private static readonly Color ColorHigh    = new Color(0.18f, 0.85f, 0.30f);   // green
    private static readonly Color ColorMid     = new Color(0.95f, 0.75f, 0.10f);   // yellow
    private static readonly Color ColorLow     = new Color(0.90f, 0.18f, 0.18f);   // red
    private static readonly Color ColorDelay   = new Color(1f,    0.55f, 0.10f);   // orange ghost
    private static readonly Color ColorBg      = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    private static readonly Color ColorBorder  = new Color(0f,    0f,    0f,    0.9f);

    // -------------------------------------------------------
    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealthUI: No PlayerHealth found in scene.");
            return;
        }

        BuildUI();
    }

    private void Update()
    {
        if (playerHealth == null || _fillBar == null) return;

        float pct = playerHealth.CurrentHealthPercent;

        // Main fill
        _fillBar.fillAmount = pct;
        _fillBar.color = Color.Lerp(Color.Lerp(ColorLow, ColorMid, pct * 2f),
                                    Color.Lerp(ColorMid, ColorHigh, (pct - 0.5f) * 2f),
                                    Mathf.Clamp01(pct));

        // Ghost delay bar
        if (_delayFill > pct)
            _delayFill = Mathf.MoveTowards(_delayFill, pct, DelaySpeed * Time.deltaTime);
        else
            _delayFill = pct;

        _delayBar.fillAmount = _delayFill;

        // Label
        if (_hpText != null)
        {
            int cur = Mathf.CeilToInt(playerHealth.CurrentHealth);
            int max = Mathf.RoundToInt(playerHealth.MaxHealth);
            _hpText.text = $"{cur} / {max}";
        }
    }

    // -------------------------------------------------------
    // Runtime UI construction
    // -------------------------------------------------------
    private void BuildUI()
    {
        // ---- Canvas ----
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ---- Outer panel (border) ----
        RectTransform border = CreateRect("HPBorder", canvasGO.transform);
        Image borderImg = border.gameObject.AddComponent<Image>();
        borderImg.color = ColorBorder;
        // Position: bottom-left, 30px from edges
        border.anchorMin = new Vector2(0f, 0f);
        border.anchorMax = new Vector2(0f, 0f);
        border.pivot     = new Vector2(0f, 0f);
        border.anchoredPosition = new Vector2(30f, 30f);
        border.sizeDelta = new Vector2(364f, 44f);

        // ---- Background ----
        RectTransform bg = CreateRect("HPBackground", border);
        _bgBar = bg.gameObject.AddComponent<Image>();
        _bgBar.color = ColorBg;
        StretchFill(bg, 3f);  // 3px inset for border effect

        // ---- Delay / ghost bar ----
        RectTransform delayRect = CreateRect("HPDelay", border);
        _delayBar = delayRect.gameObject.AddComponent<Image>();
        _delayBar.color = ColorDelay;
        _delayBar.type  = Image.Type.Filled;
        _delayBar.fillMethod = Image.FillMethod.Horizontal;
        _delayBar.fillAmount = 1f;
        StretchFill(delayRect, 3f);

        // ---- Fill bar ----
        RectTransform fillRect = CreateRect("HPFill", border);
        _fillBar = fillRect.gameObject.AddComponent<Image>();
        _fillBar.color = ColorHigh;
        _fillBar.type  = Image.Type.Filled;
        _fillBar.fillMethod = Image.FillMethod.Horizontal;
        _fillBar.fillAmount = 1f;
        StretchFill(fillRect, 3f);

        // ---- Label ----
        GameObject labelGO = new GameObject("HPLabel");
        labelGO.transform.SetParent(border, false);
        _hpText = labelGO.AddComponent<Text>();
        _hpText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _hpText.fontSize  = 18;
        _hpText.fontStyle = FontStyle.Bold;
        _hpText.color     = Color.white;
        _hpText.alignment = TextAnchor.MiddleCenter;
        _hpText.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // ---- Heart icon label ----
        GameObject iconGO = new GameObject("HPIcon");
        iconGO.transform.SetParent(border, false);
        Text iconTxt = iconGO.AddComponent<Text>();
        iconTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconTxt.fontSize  = 22;
        iconTxt.color     = new Color(0.95f, 0.25f, 0.25f);
        iconTxt.text      = "♥";
        iconTxt.alignment = TextAnchor.MiddleLeft;

        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot     = new Vector2(0f, 0.5f);
        iconRect.offsetMin = new Vector2(8f,  0f);
        iconRect.offsetMax = new Vector2(36f, 0f);
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    /// <summary>Stretch to fill parent with an inset margin.</summary>
    private void StretchFill(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2( inset,  inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
