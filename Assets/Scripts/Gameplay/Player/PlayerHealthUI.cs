using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the PlayerHealth component here, or leave null to auto-find.")]
    public PlayerHealth playerHealth;

    private Canvas _canvas;
    private RectTransform _fillRect;
    private RectTransform _delayRect;
    private Text _hpText;

    private float _delayFill = 1f;
    private const float DelaySpeed = 1.7f;

    private static readonly Color ColorHigh = new Color(0.18f, 0.85f, 0.30f, 1f);
    private static readonly Color ColorMid = new Color(0.95f, 0.75f, 0.10f, 1f);
    private static readonly Color ColorLow = new Color(0.90f, 0.18f, 0.18f, 1f);
    private static readonly Color ColorDelay = new Color(1f, 0.55f, 0.10f, 0.9f);
    private static readonly Color ColorBg = new Color(0.04f, 0.045f, 0.055f, 0.88f);
    private static readonly Color ColorBorder = new Color(0f, 0f, 0f, 0.95f);

    private void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealthUI: No PlayerHealth found in scene.");
            return;
        }

        BuildUI();
        Update();
    }

    private void Update()
    {
        if (playerHealth == null || _fillRect == null || _delayRect == null)
        {
            return;
        }

        float pct = playerHealth.CurrentHealthPercent;
        SetFill(_fillRect, pct);

        Image fillImage = _fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = GetHealthColor(pct);
        }

        if (_delayFill > pct)
        {
            _delayFill = Mathf.MoveTowards(_delayFill, pct, DelaySpeed * Time.deltaTime);
        }
        else
        {
            _delayFill = pct;
        }

        SetFill(_delayRect, _delayFill);

        if (_hpText != null)
        {
            int cur = Mathf.CeilToInt(playerHealth.CurrentHealth);
            int max = Mathf.RoundToInt(playerHealth.MaxHealth);
            _hpText.text = $"{cur} / {max}";
        }
    }

    private void BuildUI()
    {
        _canvas = GetOrCreateHudCanvas();
        DestroyExisting("HPBorder");

        RectTransform border = CreateRect("HPBorder", _canvas.transform);
        Image borderImg = border.gameObject.AddComponent<Image>();
        borderImg.color = ColorBorder;
        SetTopLeft(border, new Vector2(30f, -56f), new Vector2(420f, 54f));

        RectTransform bg = CreateRect("HPBackground", border);
        Image bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.color = ColorBg;
        StretchFill(bg, 3f);

        _delayRect = CreateRect("HPDelay", border);
        Image delayImage = _delayRect.gameObject.AddComponent<Image>();
        delayImage.color = ColorDelay;
        StretchFill(_delayRect, 3f);

        _fillRect = CreateRect("HPFill", border);
        Image fillImage = _fillRect.gameObject.AddComponent<Image>();
        fillImage.color = ColorHigh;
        StretchFill(_fillRect, 3f);

        GameObject labelGO = new GameObject("HPLabel", typeof(RectTransform));
        labelGO.transform.SetParent(border, false);
        _hpText = labelGO.AddComponent<Text>();
        _hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _hpText.fontSize = 28;
        _hpText.fontStyle = FontStyle.Bold;
        _hpText.color = Color.white;
        _hpText.alignment = TextAnchor.MiddleCenter;

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        StretchFill(labelRect, 0f);

        GameObject iconGO = new GameObject("HPIcon", typeof(RectTransform));
        iconGO.transform.SetParent(border, false);
        Text iconTxt = iconGO.AddComponent<Text>();
        iconTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconTxt.fontSize = 19;
        iconTxt.color = new Color(1f, 0.28f, 0.28f, 1f);
        iconTxt.text = "♥";
        iconTxt.alignment = TextAnchor.MiddleLeft;

        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.offsetMin = new Vector2(10f, 0f);
        iconRect.offsetMax = new Vector2(38f, 0f);
    }

    private static Canvas GetOrCreateHudCanvas()
    {
        GameObject canvasGO = GameObject.Find("GameplayHUDCanvas") ?? GameObject.Find("HealthBarCanvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("GameplayHUDCanvas");
        }
        else
        {
            canvasGO.name = "GameplayHUDCanvas";
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasGO.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasGO.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasGO.GetComponent<GraphicRaycaster>() == null)
        {
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        return canvas;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void DestroyExisting(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Destroy(existing);
        }
    }

    private static void SetTopLeft(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void StretchFill(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void SetFill(RectTransform rt, float percent)
    {
        percent = Mathf.Clamp01(percent);
        rt.gameObject.SetActive(percent > 0.001f);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(percent, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(3f, 3f);
        rt.offsetMax = new Vector2(-3f, -3f);
    }

    private static Color GetHealthColor(float pct)
    {
        if (pct < 0.5f)
        {
            return Color.Lerp(ColorLow, ColorMid, pct * 2f);
        }

        return Color.Lerp(ColorMid, ColorHigh, (pct - 0.5f) * 2f);
    }
}
