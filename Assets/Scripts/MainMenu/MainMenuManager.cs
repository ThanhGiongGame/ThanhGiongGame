using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenuManager : MonoBehaviour
{
    private const string ShopSceneName = "GameShopScene";
    private const string TutorialSceneName = "TutorialScene";
    private const string TutorialCompleteKey = "TutorialComplete";
    private const string HasSeenIntroKey = "HasSeenIntro";
    private const string VinhDanhTotalKey = "VinhDanhTotal";

    [Header("UI Panels")]
    public GameObject settingsPanel;

    [Header("Intro Video")]
    public GameObject introVideoPanel;
    public VideoPlayer introVideoPlayer;
    public GameObject mainMenuContainer;

    [Header("Audio Settings")]
    public AudioSource backgroundMusic;
    public Image muteButtonImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public Slider volumeSlider;

    private bool isMuted;
    private float currentVolume = 1f;
    private bool isLoadingScene;
    private bool isIntroPlaying;
    private float introStartedAt = -1f;
    private Coroutine loadSceneCoroutine;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        ResolveReferences();
        NormalizeMenuLayout();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        isLoadingScene = false;
        isIntroPlaying = false;
        introStartedAt = -1f;

        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(true);
        }

        if (introVideoPlayer != null)
        {
            introVideoPlayer.Stop();
        }

        SetSettingsVisible(false);
        SetIntroVideoVisible(false);
        HookSkipButton();

        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        currentVolume = PlayerPrefs.GetFloat("Volume", 1f);

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(currentVolume);
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoFinished;
            introVideoPlayer.loopPointReached += OnIntroVideoFinished;
        }

        ApplyMuteState();
        SetupMapSelection();
    }

    private void Update()
    {
        if (!isIntroPlaying || isLoadingScene)
        {
            return;
        }

        if (Time.unscaledTime - introStartedAt < 0.35f)
        {
            return;
        }

        bool skipRequested = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        skipRequested = Input.anyKeyDown || Input.GetMouseButtonDown(0);
#endif

#if ENABLE_INPUT_SYSTEM
        skipRequested = skipRequested
            || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
#endif

        if (skipRequested)
        {
            SkipVideo();
        }
    }

    private void OnDestroy()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoFinished;
        }
    }

    public void PlayGame()
    {
        if (isLoadingScene)
        {
            return;
        }

        if (introVideoPanel == null || introVideoPlayer == null)
        {
            LoadNextScene();
            return;
        }

        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(false);
        }

        SetSettingsVisible(false);
        SetIntroVideoVisible(true);
        isIntroPlaying = true;
        introStartedAt = Time.unscaledTime;

        introVideoPlayer.gameObject.SetActive(true);
        introVideoPlayer.enabled = true;

        if (backgroundMusic != null)
        {
            backgroundMusic.Stop();
        }

        introVideoPlayer.Stop();
        introVideoPlayer.Play();
    }

    public void ContinueGame()
    {
        if (isLoadingScene)
        {
            return;
        }

        isLoadingScene = true;
        isIntroPlaying = false;
        SetSettingsVisible(false);
        SetIntroVideoVisible(false);

        if (introVideoPlayer != null)
        {
            introVideoPlayer.Stop();
        }

        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(false);
        }

        StartCoroutine(LoadContinueSceneAfterUiEvent());
    }

    public void SkipVideo()
    {
        if (isLoadingScene)
        {
            return;
        }

        BeginIntroTransition();
        StartDeferredLoadNextScene();
    }

    private void BeginIntroTransition()
    {
        isIntroPlaying = false;
        isLoadingScene = true;

        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoFinished;
            introVideoPlayer.Stop();
        }

        SetIntroVideoVisible(false);
    }

    public void ToggleSettings()
    {
        SetSettingsVisible(settingsPanel != null && !settingsPanel.activeSelf);
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMuteState();
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("Volume", currentVolume);
        PlayerPrefs.Save();
        ApplyMuteState();
    }

    private void OnIntroVideoFinished(VideoPlayer videoPlayer)
    {
        if (isLoadingScene)
        {
            return;
        }

        BeginIntroTransition();
        StartDeferredLoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isLoadingScene)
        {
            return;
        }

        BeginIntroTransition();
        StartDeferredLoadNextScene();
    }

    private void StartDeferredLoadNextScene()
    {
        if (loadSceneCoroutine != null)
        {
            return;
        }

        loadSceneCoroutine = StartCoroutine(LoadNextSceneAfterUiEvent());
    }

    private IEnumerator LoadNextSceneAfterUiEvent()
    {
        yield return null;

        if (!PlayerPrefs.HasKey(VinhDanhTotalKey))
        {
            PlayerPrefs.SetInt(VinhDanhTotalKey, 0);
        }

        PlayerPrefs.SetInt(HasSeenIntroKey, 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(TutorialSceneName);
    }

    private IEnumerator LoadContinueSceneAfterUiEvent()
    {
        yield return null;

        if (!PlayerPrefs.HasKey(VinhDanhTotalKey))
        {
            PlayerPrefs.SetInt(VinhDanhTotalKey, 0);
            PlayerPrefs.Save();
        }

        Time.timeScale = 1f;
        bool hasProgress = PlayerPrefs.GetInt(TutorialCompleteKey, 0) == 1
            || PlayerPrefs.GetInt(HasSeenIntroKey, 0) == 1
            || PlayerPrefs.GetInt(VinhDanhTotalKey, 0) > 0;

        SceneManager.LoadScene(hasProgress ? ShopSceneName : TutorialSceneName);
    }

    private void ResolveReferences()
    {
        if (settingsPanel == null)
        {
            settingsPanel = FindByName("SettingsPanel");
        }

        if (introVideoPanel == null)
        {
            introVideoPanel = FindByName("IntroVideoPanel");
        }

        if (introVideoPlayer == null)
        {
            introVideoPlayer = FindFirstObjectByType<VideoPlayer>(FindObjectsInactive.Include);
        }

        if (volumeSlider == null)
        {
            volumeSlider = FindFirstObjectByType<Slider>(FindObjectsInactive.Include);
        }

        if (backgroundMusic == null)
        {
            backgroundMusic = FindFirstObjectByType<AudioSource>(FindObjectsInactive.Include);
        }

        GameObject canvasRoot = FindByName("MainMenuCanvas");
        if (mainMenuContainer == null || mainMenuContainer == canvasRoot)
        {
            mainMenuContainer = FindByName("MenuLayer");
        }
    }

    private void NormalizeMenuLayout()
    {
        UiSceneNormalizer.NormalizeScene("MainMenuCanvas");
        ConfigureCanvasRoot();
        ConfigureFullScreenLayer("BackgroundLayer");
        ConfigureFullScreenLayer("MenuLayer");
        ConfigureFullScreenLayer("ModalLayer");
        ConfigureFullScreenLayer("VideoOverlayLayer");
        ConfigureBackground();
        ConfigureTitle();

        // 1. Nhân bản nút SETTINGS thành các nút phụ nếu chưa có
        GameObject settingsBtnGO = FindByName("SETTINGS");
        if (settingsBtnGO != null)
        {
            EnsureMenuButtonClone(settingsBtnGO, "CONTINUE", ContinueGame);
        }

        GameObject mapButtonObject = FindByName("MAP_SELECT");
        if (mapButtonObject != null)
        {
            mapButtonObject.SetActive(false);
        }

        // 2. Định vị lại menu chính cho flow rõ hơn.
        ConfigureMenuButton("PLAY", new Vector2(0f, 112f), "BẮT ĐẦU HÀNH TRÌNH");
        ConfigureMenuButton("CONTINUE", new Vector2(0f, 0f), "TIẾP TỤC");
        ConfigureMenuButton("SETTINGS", new Vector2(0f, -112f), "CÀI ĐẶT");
        NormalizeSettingsPanel();
        NormalizeIntroVideoPanel();
    }

    private static GameObject EnsureMenuButtonClone(GameObject template, string objectName, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = FindByName(objectName);
        if (buttonObject == null)
        {
            buttonObject = Instantiate(template, template.transform.parent);
            buttonObject.name = objectName;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        return buttonObject;
    }

    private void ConfigureCanvasRoot()
    {
        GameObject canvasObject = FindByName("MainMenuCanvas");
        if (canvasObject == null)
        {
            return;
        }

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    private void ConfigureFullScreenLayer(string objectName)
    {
        GameObject layerObject = FindByName(objectName);
        if (layerObject == null)
        {
            return;
        }

        RectTransform rect = layerObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private void ConfigureBackground()
    {
        GameObject background = FindByName("Image");
        if (background == null)
        {
            return;
        }

        RectTransform rect = background.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(-120f, -80f);
            rect.offsetMax = new Vector2(120f, 80f);
            rect.localScale = Vector3.one;
        }

        Image image = background.GetComponent<Image>();
        if (image != null)
        {
            image.preserveAspect = false;
            image.raycastTarget = false;
        }
    }

    private void ConfigureTitle()
    {
        GameObject titleObject = FindByName("TITLE");
        if (titleObject == null)
        {
            return;
        }

        RectTransform rect = titleObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -92f);
            rect.sizeDelta = new Vector2(1480f, 220f);
            rect.localScale = Vector3.one;
        }

        TMP_Text label = titleObject.GetComponent<TMP_Text>();
        if (label != null)
        {
            label.text = "THANH GIONG";
            label.fontSize = 118f;
            label.enableAutoSizing = false;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.raycastTarget = false;
        }
    }

    private void ConfigureMenuButton(string objectName, Vector2 position, string labelText)
    {
        GameObject buttonObject = FindByName(objectName);
        if (buttonObject == null)
        {
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(560f, 86f);
            rect.localScale = Vector3.one;
        }

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.43f, 0.19f, 0.08f, 0.62f);
            image.raycastTarget = true;
        }

        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = labelText;
            label.fontSize = 44f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 30f;
            label.fontSizeMax = 44f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                labelRect.localScale = Vector3.one;
            }
        }
    }

    private void NormalizeSettingsPanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        RectTransform rect = settingsPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(2200f, 1160f);
            rect.localScale = Vector3.one;
        }

        Image panelImage = settingsPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.02f, 0.018f, 0.018f, 0.86f);
            panelImage.raycastTarget = true;
        }

        ConfigureSettingsText("text_title", "SETTINGS", new Vector2(160f, 360f), new Vector2(980f, 82f), 48f);
        ConfigureSettingsText("SOUND", "Sound", new Vector2(-220f, 120f), new Vector2(280f, 56f), 34f);
        ConfigureSettingsRect("Slider", new Vector2(260f, 120f), new Vector2(620f, 42f));
        ConfigureSettingsRect("MuteButton", new Vector2(160f, -115f), new Vector2(120f, 120f));
        ConfigureSettingsRect("CloseButton", new Vector2(843f, 413f), new Vector2(64f, 64f));
        ConfigureSettingsRect("SettingsPage", Vector2.zero, new Vector2(2200f, 1160f));
        ConfigureSettingsRect("ControlsPage", Vector2.zero, new Vector2(2200f, 1160f));
        ConfigureSettingsRect("SettingsTabButton", new Vector2(-668f, 330f), new Vector2(300f, 70f));
        ConfigureSettingsRect("ControlsTabButton", new Vector2(-668f, 230f), new Vector2(300f, 70f));
    }

    private void ConfigureSettingsText(string objectName, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject targetObject = FindChildByName(settingsPanel.transform, objectName);
        if (targetObject == null)
        {
            return;
        }

        ConfigureSettingsRect(targetObject, position, size);

        TMP_Text label = targetObject.GetComponent<TMP_Text>();
        if (label == null)
        {
            label = targetObject.GetComponentInChildren<TMP_Text>(true);
        }

        if (label == null)
        {
            return;
        }

        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.alignment = TextAlignmentOptions.Center;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
    }

    private void ConfigureSettingsRect(string objectName, Vector2 position, Vector2 size)
    {
        GameObject targetObject = FindChildByName(settingsPanel.transform, objectName);
        if (targetObject != null)
        {
            ConfigureSettingsRect(targetObject, position, size);
        }
    }

    private static void ConfigureSettingsRect(GameObject targetObject, Vector2 position, Vector2 size)
    {
        RectTransform rect = targetObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void SetSettingsVisible(bool visible)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(visible);
        }

        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(!visible && !isIntroPlaying && !isLoadingScene);
        }
    }

    private void SetIntroVideoVisible(bool visible)
    {
        if (introVideoPanel != null)
        {
            introVideoPanel.SetActive(visible);
            introVideoPanel.transform.SetAsLastSibling();
        }
    }

    private void NormalizeIntroVideoPanel()
    {
        if (introVideoPanel == null)
        {
            return;
        }

        RectTransform panelRect = introVideoPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
        }

        Image panelImage = introVideoPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = false;
        }

        RawImage[] rawImages = introVideoPanel.GetComponentsInChildren<RawImage>(true);
        foreach (RawImage rawImage in rawImages)
        {
            rawImage.raycastTarget = false;
        }

        Image[] images = introVideoPanel.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image.GetComponent<Button>() == null && image.name != "IntroSkipClickArea")
            {
                image.raycastTarget = false;
            }
        }

        ConfigureIntroSkipClickArea();
        ConfigureSkipButton();
    }

    private void ConfigureIntroSkipClickArea()
    {
        RectTransform clickArea = EnsureRect(introVideoPanel.transform, "IntroSkipClickArea");
        clickArea.SetAsLastSibling();
        clickArea.anchorMin = Vector2.zero;
        clickArea.anchorMax = Vector2.one;
        clickArea.pivot = new Vector2(0.5f, 0.5f);
        clickArea.anchoredPosition = Vector2.zero;
        clickArea.offsetMin = Vector2.zero;
        clickArea.offsetMax = Vector2.zero;
        clickArea.localScale = Vector3.one;

        Image image = clickArea.GetComponent<Image>();
        if (image == null)
        {
            image = clickArea.gameObject.AddComponent<Image>();
        }

        image.color = new Color(0f, 0f, 0f, 0.001f);
        image.raycastTarget = true;

        Button button = clickArea.GetComponent<Button>();
        if (button == null)
        {
            button = clickArea.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(SkipVideo);
        button.interactable = true;
    }

    private void ConfigureSkipButton()
    {
        GameObject skipObject = FindByName("SkipButton");
        if (skipObject == null)
        {
            return;
        }

        skipObject.transform.SetAsLastSibling();

        RectTransform rect = skipObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-32f, 28f);
            rect.sizeDelta = new Vector2(260f, 64f);
            rect.localScale = Vector3.one;
        }

        Image image = skipObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0f, 0f, 0f, 0.62f);
            image.raycastTarget = true;
        }

        TMP_Text label = skipObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "SKIP";
            label.fontSize = 30f;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        Button button = skipObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SkipVideo);
            button.interactable = true;
        }
    }

    private static RectTransform EnsureRect(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            return existingRect;
        }

        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private void HookSkipButton()
    {
        ConfigureSkipButton();
    }

    private void ApplyMuteState()
    {
        AudioListener.volume = isMuted ? 0f : currentVolume;

        if (muteButtonImage == null)
        {
            return;
        }

        if (isMuted && soundOffSprite != null)
        {
            muteButtonImage.sprite = soundOffSprite;
        }
        else if (!isMuted && soundOnSprite != null)
        {
            muteButtonImage.sprite = soundOnSprite;
        }
    }

    private static GameObject FindByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform transform in transforms)
        {
            if (transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindChildByName(Transform root, string objectName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform transform in transforms)
        {
            if (transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private MapSelectionUI _mapSelectionUI;

    private void SetupMapSelection()
    {
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas != null)
        {
            _mapSelectionUI = mainCanvas.gameObject.AddComponent<MapSelectionUI>();
            _mapSelectionUI.Initialize(() => {
                if (mainMenuContainer != null)
                {
                    mainMenuContainer.SetActive(true);
                }
            });
            _mapSelectionUI.Hide();
        }
    }

    public void OpenMapSelection()
    {
        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(false);
        }
        if (_mapSelectionUI != null)
        {
            _mapSelectionUI.Show();
        }
    }
}
