using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameplayPauseMenu : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenuScene";
    private const string ShopSceneName = "GameShopScene";
    private const string GameplaySceneName = "SampleScene";

    private Canvas canvas;
    private GameObject pausePanel;
    private Button pauseButton;
    private Slider volumeSlider;
    private Toggle muteToggle;

    private bool isPaused;
    private float previousTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForCurrentScene()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsurePauseMenuForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePauseMenuForScene(scene);
    }

    private static void EnsurePauseMenuForScene(Scene scene)
    {
        if (scene.name != GameplaySceneName)
        {
            return;
        }

        if (FindFirstObjectByType<GameplayPauseMenu>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject pauseMenuObject = new GameObject("GameplayPauseMenu");
        pauseMenuObject.AddComponent<GameplayPauseMenu>();
    }

    private void Awake()
    {
        BuildPauseUi();
        ApplySavedAudioSettings();
        SetPaused(false);
    }

    private void OnDestroy()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    private void Update()
    {
        if (WasPausePressed())
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void BackToShop()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(ShopSceneName);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void SetVolume(float value)
    {
        float volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.Save();

        bool muted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        AudioListener.volume = muted ? 0f : volume;
    }

    public void SetMuted(bool muted)
    {
        PlayerPrefs.SetInt("IsMuted", muted ? 1 : 0);
        PlayerPrefs.Save();

        float volume = PlayerPrefs.GetFloat("Volume", 1f);
        AudioListener.volume = muted ? 0f : volume;
    }

    private void SetPaused(bool paused)
    {
        if (paused == isPaused)
        {
            ApplyPauseUiVisibility();
            return;
        }

        isPaused = paused;

        if (isPaused)
        {
            previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }

        ApplyPauseUiVisibility();
    }

    private void ApplyPauseUiVisibility()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(!isPaused);
        }
    }

    private void BuildPauseUi()
    {
        SetupEventSystem();

        GameObject canvasObject = new GameObject("GameplayPauseCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        BuildPauseButton(canvasObject.transform);
        BuildPausePanel(canvasObject.transform);
    }

    private void BuildPauseButton(Transform parent)
    {
        GameObject buttonObject = CreateUiObject("PauseButton", parent);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(58f, 50f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.05f, 0.045f, 0.025f, 0.96f);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.86f, 0.26f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        pauseButton = buttonObject.AddComponent<Button>();
        pauseButton.onClick.AddListener(TogglePause);
        ConfigureButtonColors(pauseButton, image.color);

        CreateText(buttonObject.transform, "=", Vector2.zero, new Vector2(58f, 50f), 36, FontStyle.Bold, new Color(1f, 0.92f, 0.36f, 1f));
    }

    private void BuildPausePanel(Transform parent)
    {
        pausePanel = CreateUiObject("PausePanel", parent);
        RectTransform panelRect = pausePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image overlay = pausePanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.72f);
        overlay.raycastTarget = true;

        GameObject card = CreatePanel(
            pausePanel.transform,
            "PauseCard",
            Vector2.zero,
            new Vector2(720f, 680f),
            new Color(0.09f, 0.1f, 0.13f, 0.96f)
        );

        CreateText(card.transform, "TẠM DỪNG", new Vector2(0f, 250f), new Vector2(620f, 80f), 52, FontStyle.Bold, new Color(1f, 0.86f, 0.25f));
        CreateText(card.transform, "Chỉnh âm lượng hoặc rời trận", new Vector2(0f, 195f), new Vector2(620f, 50f), 24, FontStyle.Normal, new Color(0.82f, 0.82f, 0.86f));

        BuildSettingsRow(card.transform);

        CreateButton(card.transform, "TIẾP TỤC", new Vector2(0f, 60f), new Vector2(460f, 74f), new Color(0.18f, 0.58f, 0.36f), ResumeGame);
        CreateButton(card.transform, "VỀ SHOP / ĐỔI TRANG BỊ", new Vector2(0f, -35f), new Vector2(460f, 74f), new Color(0.2f, 0.42f, 0.78f), BackToShop);
        CreateButton(card.transform, "VỀ MENU CHÍNH", new Vector2(0f, -130f), new Vector2(460f, 74f), new Color(0.34f, 0.34f, 0.4f), BackToMainMenu);
        CreateText(card.transform, "ESC hoặc = để đóng/mở", new Vector2(0f, -260f), new Vector2(620f, 42f), 22, FontStyle.Italic, new Color(0.62f, 0.62f, 0.68f));
    }

    private void BuildSettingsRow(Transform parent)
    {
        CreateText(parent, "ÂM LƯỢNG", new Vector2(-185f, 135f), new Vector2(220f, 44f), 24, FontStyle.Bold, Color.white);

        GameObject sliderObject = CreateUiObject("VolumeSlider", parent);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(330f, 34f);
        sliderRect.anchoredPosition = new Vector2(75f, 135f);
        volumeSlider = sliderObject.AddComponent<Slider>();
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.onValueChanged.AddListener(SetVolume);

        Image background = CreateSliderImage(sliderObject.transform, "Background", new Color(0.18f, 0.18f, 0.22f), Vector2.zero, Vector2.one);
        Image fill = CreateSliderImage(sliderObject.transform, "Fill", new Color(1f, 0.82f, 0.22f), Vector2.zero, new Vector2(1f, 1f));
        Image handle = CreateSliderImage(sliderObject.transform, "Handle", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        handle.rectTransform.sizeDelta = new Vector2(28f, 44f);

        volumeSlider.targetGraphic = handle;
        volumeSlider.fillRect = fill.rectTransform;
        volumeSlider.handleRect = handle.rectTransform;

        GameObject toggleObject = CreateUiObject("MuteToggle", parent);
        RectTransform toggleRect = toggleObject.AddComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(190f, 46f);
        toggleRect.anchoredPosition = new Vector2(0f, 82f);

        muteToggle = toggleObject.AddComponent<Toggle>();
        muteToggle.onValueChanged.AddListener(SetMuted);

        Image toggleBackground = toggleObject.AddComponent<Image>();
        toggleBackground.color = new Color(0.16f, 0.16f, 0.2f, 1f);
        muteToggle.targetGraphic = toggleBackground;

        CreateText(toggleObject.transform, "TẮT ÂM", new Vector2(22f, 0f), new Vector2(140f, 42f), 22, FontStyle.Bold, Color.white);
    }

    private void ApplySavedAudioSettings()
    {
        float volume = PlayerPrefs.GetFloat("Volume", 1f);
        bool muted = PlayerPrefs.GetInt("IsMuted", 0) == 1;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
        }

        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(muted);
        }

        AudioListener.volume = muted ? 0f : volume;
    }

    private static bool WasPausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            return true;
        }
#endif
        return false;
    }

    private static void SetupEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreatePanel(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = CreateUiObject(objectName, parent);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return panel;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateUiObject(label, parent);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        ConfigureButtonColors(button, color);

        CreateText(buttonObject.transform, label, Vector2.zero, size, 25, FontStyle.Bold, Color.white);
        return button;
    }

    private static Text CreateText(Transform parent, string content, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject textObject = CreateUiObject("Text", parent);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateSliderImage(Transform parent, string objectName, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject imageObject = CreateUiObject(objectName, parent);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static void ConfigureButtonColors(Button button, Color baseColor)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = baseColor * 1.18f;
        colors.pressedColor = baseColor * 0.78f;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }
}
