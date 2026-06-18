using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("Stats")]
    [HideInInspector] public int enemiesKilled = 25;
    [HideInInspector] public float timeSurvived = 120f;

    private Canvas _canvas;

    private void Awake()
    {
        Instance = this;
    }

    // =========================================================
    // PLAYER DIE
    // =========================================================
    public void OnPlayerDeath()
    {
        // Tránh gọi nhiều lần
        if (_canvas != null)
            return;

        // Pause game
        Time.timeScale = 0f;

        // Tính điểm
        int vinhDanhEarned =
            (enemiesKilled * 10)
            + Mathf.RoundToInt(timeSurvived * 0.5f);

        // Save tổng điểm
        int currentTotal = PlayerPrefs.GetInt("VinhDanhTotal", 0);

        PlayerPrefs.SetInt(
            "VinhDanhTotal",
            currentTotal + vinhDanhEarned
        );

        // Tích lũy tổng số quái đã giết qua tất cả các ván chơi
        int totalKills = PlayerPrefs.GetInt("TotalEnemiesKilled", 0);
        PlayerPrefs.SetInt("TotalEnemiesKilled", totalKills + enemiesKilled);

        PlayerPrefs.Save();

        // Build UI
        BuildGameOverUI(vinhDanhEarned);

        // Unlock chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =========================================================
    // UI
    // =========================================================
    private void BuildGameOverUI(int scoreEarned)
    {
        SetupEventSystem();

        // =========================
        // CANVAS
        // =========================
        GameObject canvasGO = new GameObject("GameOverCanvas");

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler =
            canvasGO.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920, 1080);

        // =========================
        // BACKGROUND
        // =========================
        GameObject bg = CreateUIObject(
            "Background",
            _canvas.transform
        );

        RectTransform bgRt =
            bg.AddComponent<RectTransform>();

        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        Image bgImg = bg.AddComponent<Image>();

        bgImg.color =
            new Color(0f, 0f, 0f, 0.92f);

        // =========================
        // MAIN PANEL
        // =========================
        GameObject panel = CreatePanel(
            bg.transform,
            Vector2.zero,
            new Vector2(950f, 700f),
            new Color(0.1f, 0.11f, 0.14f)
        );

        // =========================
        // TITLE
        // =========================
        CreateText(
            panel.transform,
            "THẤT BẠI",
            new Vector2(0f, 250f),
            56,
            FontStyle.Bold,
            new Color(1f, 0.25f, 0.25f)
        );

        // =========================
        // SUBTITLE
        // =========================
        CreateText(
            panel.transform,
            "Bạn đã bị hạ gục trên chiến trường",
            new Vector2(0f, 190f),
            24,
            FontStyle.Italic,
            new Color(0.75f, 0.75f, 0.75f)
        );

        // =========================
        // STATS PANEL
        // =========================
        GameObject statPanel = CreatePanel(
            panel.transform,
            new Vector2(0f, 30f),
            new Vector2(780f, 260f),
            new Color(0.14f, 0.15f, 0.19f)
        );

        // Enemy Killed
        CreateStatRow(
            statPanel.transform,
            "Quái đã tiêu diệt",
            enemiesKilled.ToString(),
            new Vector2(0f, 70f)
        );

        // Time survived
        CreateStatRow(
            statPanel.transform,
            "Thời gian sống sót",
            timeSurvived.ToString("F0") + " giây",
            new Vector2(0f, 0f)
        );

        // Score
        CreateStatRow(
            statPanel.transform,
            "Vinh Danh nhận được",
            "+" + scoreEarned,
            new Vector2(0f, -70f),
            new Color(1f, 0.85f, 0.2f)
        );

        // =========================
        // TOTAL POINTS
        // =========================
        int total =
            PlayerPrefs.GetInt("VinhDanhTotal", 0);

        CreateText(
            panel.transform,
            "Tổng Vinh Danh: " + total,
            new Vector2(0f, -150f),
            28,
            FontStyle.Bold,
            new Color(1f, 0.9f, 0.3f)
        );

        // =========================
        // BUTTONS
        // =========================

        // Retry
        CreateButton(
            panel.transform,
            "CHƠI LẠI",
            new Vector2(-170f, -260f),
            new Vector2(280f, 75f),
            new Color(0.2f, 0.55f, 0.95f),
            () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(
                    SceneManager.GetActiveScene().name
                );
            }
        );

        // Shop
        CreateButton(
            panel.transform,
            "SHOP",
            new Vector2(170f, -260f),
            new Vector2(280f, 75f),
            new Color(0.18f, 0.7f, 0.45f),
            () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("GameShopScene");
            }
        );

        // Menu
        CreateButton(
            panel.transform,
            "MENU",
            new Vector2(0f, -360f),
            new Vector2(220f, 60f),
            new Color(0.3f, 0.3f, 0.35f),
            () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenuScene");
            }
        );
    }

    // =========================================================
    // EVENT SYSTEM
    // =========================================================
    private void SetupEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");

            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }

    // =========================================================
    // STAT ROW
    // =========================================================
    private void CreateStatRow(
        Transform parent,
        string left,
        string right,
        Vector2 pos,
        Color? valueColor = null
    )
    {
        GameObject row = CreateUIObject(
            "StatRow",
            parent
        );

        RectTransform rt =
            row.AddComponent<RectTransform>();

        rt.sizeDelta = new Vector2(700f, 50f);
        rt.anchoredPosition = pos;

        Text leftText = CreateText(
            row.transform,
            left,
            new Vector2(-165f, 0f),
            28,
            FontStyle.Normal,
            Color.white,
            new Vector2(330f, 50f)
        );

        leftText.alignment =
            TextAnchor.MiddleLeft;

        Text rightText = CreateText(
            row.transform,
            right,
            new Vector2(165f, 0f),
            28,
            FontStyle.Bold,
            valueColor ?? Color.white,
            new Vector2(330f, 50f)
        );

        rightText.alignment =
            TextAnchor.MiddleRight;
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private GameObject CreateUIObject(
        string name,
        Transform parent
    )
    {
        GameObject go = new GameObject(name);

        go.transform.SetParent(parent, false);

        return go;
    }

    private GameObject CreatePanel(
        Transform parent,
        Vector2 pos,
        Vector2 size,
        Color color
    )
    {
        GameObject panel =
            CreateUIObject("Panel", parent);

        RectTransform rt =
            panel.AddComponent<RectTransform>();

        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
    }

    private Text CreateText(
        Transform parent,
        string content,
        Vector2 pos,
        int fontSize,
        FontStyle style,
        Color color,
        Vector2? customSize = null
    )
    {
        GameObject go =
            CreateUIObject("Text", parent);

        RectTransform rt =
            go.AddComponent<RectTransform>();

        rt.sizeDelta = customSize ?? new Vector2(800f, 80f);
        rt.anchoredPosition = pos;

        Text txt = go.AddComponent<Text>();

        txt.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;

        return txt;
    }

    private Button CreateButton(
        Transform parent,
        string label,
        Vector2 pos,
        Vector2 size,
        Color color,
        UnityEngine.Events.UnityAction action
    )
    {
        GameObject go =
            CreateUIObject("Button", parent);

        RectTransform rt =
            go.AddComponent<RectTransform>();

        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.color = color;

        Button btn = go.AddComponent<Button>();

        ColorBlock cb = btn.colors;

        cb.highlightedColor =
            color * 1.2f;

        cb.pressedColor =
            color * 0.8f;

        btn.colors = cb;

        btn.onClick.AddListener(action);

        CreateText(
            go.transform,
            label,
            Vector2.zero,
            26,
            FontStyle.Bold,
            Color.white
        );

        return btn;
    }
}