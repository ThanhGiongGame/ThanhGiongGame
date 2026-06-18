using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuManager : MonoBehaviour
{
    private const string ShopSceneName = "GameShopScene";

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

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        ResolveReferences();
        NormalizeMenuLayout();
    }

    private void Start()
    {
        SetSettingsVisible(false);
        SetIntroVideoVisible(false);

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

        introVideoPlayer.gameObject.SetActive(true);
        introVideoPlayer.enabled = true;

        if (backgroundMusic != null)
        {
            backgroundMusic.Stop();
        }

        introVideoPlayer.Stop();
        introVideoPlayer.Play();
    }

    public void SkipVideo()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.Stop();
        }

        LoadNextScene();
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
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isLoadingScene)
        {
            return;
        }

        isLoadingScene = true;

        if (!PlayerPrefs.HasKey("VinhDanhTotal"))
        {
            PlayerPrefs.SetInt("VinhDanhTotal", 0);
            PlayerPrefs.Save();
        }

        SceneManager.LoadScene(ShopSceneName);
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

        // 1. Nhân bản nút SETTINGS thành MAP_SELECT nếu chưa có
        GameObject settingsBtnGO = FindByName("SETTINGS");
        GameObject mapBtnGO = FindByName("MAP_SELECT");
        if (settingsBtnGO != null && mapBtnGO == null)
        {
            mapBtnGO = Instantiate(settingsBtnGO, settingsBtnGO.transform.parent);
            mapBtnGO.name = "MAP_SELECT";
        }

        if (mapBtnGO != null)
        {
            Button oldBtn = mapBtnGO.GetComponent<Button>();
            if (oldBtn != null)
            {
                ColorBlock colors = oldBtn.colors;
                UnityEngine.UI.Graphic targetGraphic = oldBtn.targetGraphic;
                
                DestroyImmediate(oldBtn);
                
                Button newBtn = mapBtnGO.AddComponent<Button>();
                newBtn.colors = colors;
                newBtn.targetGraphic = targetGraphic;
                newBtn.onClick.AddListener(OpenMapSelection);
            }
        }

        // 2. Định vị lại 3 nút cho cân đối
        ConfigureMenuButton("PLAY", new Vector2(0f, 130f), "PLAY");
        ConfigureMenuButton("SETTINGS", new Vector2(0f, 10f), "SETTING");
        ConfigureMenuButton("MAP_SELECT", new Vector2(0f, -110f), "BẢN ĐỒ");
        NormalizeSettingsPanel();
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
            rect.sizeDelta = new Vector2(560f, 96f);
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
            label.fontSize = 54f;
            label.enableAutoSizing = false;
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
            rect.sizeDelta = new Vector2(760f, 500f);
            rect.localScale = Vector3.one;
        }

        Image panelImage = settingsPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.9f, 0.9f, 0.88f, 0.96f);
            panelImage.raycastTarget = true;
        }

        ConfigureSettingsText("text_title", "SETTING", new Vector2(0f, 195f), new Vector2(620f, 72f), 54f);
        ConfigureSettingsText("SOUND", "Sound", new Vector2(-225f, 70f), new Vector2(210f, 56f), 38f);
        ConfigureSettingsRect("Slider", new Vector2(145f, 72f), new Vector2(430f, 38f));
        ConfigureSettingsRect("MuteButton", new Vector2(0f, -110f), new Vector2(120f, 120f));
        ConfigureSettingsRect("CloseButton", new Vector2(340f, 210f), new Vector2(58f, 58f));
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
    }

    private void SetIntroVideoVisible(bool visible)
    {
        if (introVideoPanel != null)
        {
            introVideoPanel.SetActive(visible);
        }
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
