using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelUI : MonoBehaviour
{
    private RectTransform _fillRect;
    private Text _levelText;
    private Text _xpText;

    private static readonly Color ColorBg = new Color(0.04f, 0.045f, 0.065f, 0.88f);
    private static readonly Color ColorFill = new Color(0.20f, 0.75f, 0.95f, 1f);
    private static readonly Color ColorBorder = new Color(0f, 0f, 0f, 0.95f);
    private static readonly Color ColorText = new Color(0.92f, 0.95f, 1f, 1f);

    private void Start()
    {
        XPManager.OnXPChanged += UpdateUI;
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

        if (XPManager.Instance != null)
        {
            UpdateUI(XPManager.Instance.CurrentXP, XPManager.Instance.XPForNextLevel, XPManager.Instance.CurrentLevel);
        }
    }

    private void UpdateUI(float currentXP, float xpNeeded, int currentLevel)
    {
        if (_fillRect == null)
        {
            return;
        }

        float pct = xpNeeded <= 0f ? 0f : Mathf.Clamp01(currentXP / xpNeeded);
        SetFill(_fillRect, pct);

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
        Canvas canvas = GetOrCreateHudCanvas();
        DestroyExisting("XPBorder");

        RectTransform border = CreateRect("XPBorder", canvas.transform);
        Image borderImg = border.gameObject.AddComponent<Image>();
        borderImg.color = ColorBorder;
        SetTopLeft(border, new Vector2(30f, -116f), new Vector2(420f, 26f));

        RectTransform bg = CreateRect("XPBackground", border);
        Image bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.color = ColorBg;
        StretchFill(bg, 2f);

        _fillRect = CreateRect("XPFill", border);
        Image fillImage = _fillRect.gameObject.AddComponent<Image>();
        fillImage.color = ColorFill;
        StretchFill(_fillRect, 2f);

        GameObject lvGO = new GameObject("XPLevelLabel", typeof(RectTransform));
        lvGO.transform.SetParent(border, false);
        _levelText = lvGO.AddComponent<Text>();
        _levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _levelText.fontSize = 18;
        _levelText.fontStyle = FontStyle.Bold;
        _levelText.color = ColorText;
        _levelText.alignment = TextAnchor.MiddleLeft;

        RectTransform lvRect = lvGO.GetComponent<RectTransform>();
        StretchFill(lvRect, 0f);
        lvRect.offsetMin = new Vector2(12f, 0f);

        GameObject numericGO = new GameObject("XPNumericLabel", typeof(RectTransform));
        numericGO.transform.SetParent(border, false);
        _xpText = numericGO.AddComponent<Text>();
        _xpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _xpText.fontSize = 16;
        _xpText.fontStyle = FontStyle.Bold;
        _xpText.color = ColorText;
        _xpText.alignment = TextAnchor.MiddleRight;

        RectTransform numRect = numericGO.GetComponent<RectTransform>();
        StretchFill(numRect, 0f);
        numRect.offsetMax = new Vector2(-12f, 0f);
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
        float inset = 2f;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(percent, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
